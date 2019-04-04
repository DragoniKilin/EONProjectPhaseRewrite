﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Vuforia;

public class StoryManager : MonoBehaviour {

    public enum TapOrDrag {
        Tap,
        Drag
    }
    public enum ManipulationType {
        Transform,
        Rotate,
        Scale
    }
    public enum ManipulationAxis {
        X,
        Y,
        Z
    }

    GameObject qAPanel;

    AudioSource audioSource;

    public int currentStep;
    private bool introPlayed = false;
    private bool finished = false;
    [SerializeField]
    public AudioClip introAudio;
    [SerializeField]
    public AudioClip outroAudio;

    [SerializeField]
    public GameObject slider;

    [SerializeField]
    public Step[] steps;

    [System.Serializable]
    public class Step : object {
        [SerializeField]
        public GameObject objectTarget;
        [SerializeField]
        public AudioClip narateAudio;
        [SerializeField]
        public SoundEffect[] soundEffects;
        [SerializeField]
        public AudioClip missTap;
        [SerializeField]
        public AnimationClip animClip;
        [SerializeField]
        public int stepOrder;
        [SerializeField]
        public AnimationClip highlightThis;
        [SerializeField]
        public GameObject highlightTarget;
        [SerializeField]
        public TapOrDrag tapOrDrag;
        [SerializeField]
        public bool hasSlider;
        [SerializeField, Range(0,1)]
        public float sliderTarget;
        [SerializeField]
        public bool manipulateObject;
        [SerializeField]
        public ManipulationType manipulationType;
        [SerializeField]
        public ManipulationAxis manipulationAxis;
        [SerializeField]
        public float manipulationMultiplier;
        [SerializeField]
        public bool hasQuestion;
        [SerializeField]
        public Question question;        
    }
    [System.Serializable]
    public class Question : object {
        [SerializeField]
        public String question;
        [SerializeField]
        //Do not set this array to be larger than 4
        public String[] choices;
        [SerializeField]
        public int correctChoice;
    }

    [System.Serializable]
    public class SoundEffect : object {
        [SerializeField]
        public bool loop;
        [SerializeField]
        public AudioClip soundEffect;
        [SerializeField]
        public float delay;
    }

    public void Awake() {
        qAPanel = GameObject.Find("QAPanel");
        audioSource = GetComponent<AudioSource>();
        currentStep = 0;
    }

    public void Start() {
        //move this to play intro audio when the marker first comes into view
        AudioListener.pause = false;
    }

    public void Update() {
        if (currentStep == steps.Length && !audioSource.isPlaying && finished == false && !qAPanel.activeSelf) {
            finished = true;
            //PlayAudio(outroAudio);
            GameObject.Find("PauseUI").GetComponent<PauseMenu>().Pause();
            GameObject.Find("PlayButton").SetActive(false);
        }
        if (!audioSource.isPlaying && introPlayed) {
            if(currentStep <= steps.Length - 1) {
                if (steps[currentStep].highlightThis != null) {
                    steps[currentStep].highlightTarget.gameObject.GetComponent<Animator>().Play(steps[currentStep].highlightThis.name);
                }
            }   
        }
        for (var i = 0; i < Input.touchCount; ++i) {
            if (Input.GetTouch(i).phase == TouchPhase.Began) {
                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(i).position);
                RaycastHit hit;
                if (Physics.Raycast(ray,out hit)) {
                    //GameObject.Find("TextMeshPro Text").GetComponent<TextMeshProUGUI>().text = hit.transform.gameObject.name;
                    foreach (Step elem in steps) {
                        if (hit.transform.gameObject == elem.objectTarget && currentStep == elem.stepOrder && !audioSource.isPlaying && !audioSource.loop) {
                            currentStep++;
                            AudioSource[] audioSources = gameObject.GetComponents<AudioSource>();
                            for(int j = 1; j < audioSources.Length; j++) {
                                Destroy(audioSources[j]);
                            }
                            if (elem.animClip != null) {
                                //play the animation for the step
                                //maybe update for next sprint multiple animations to play in sequence
                                hit.transform.gameObject.GetComponent<Animator>().Play(elem.animClip.name);
                            }
                            if (elem.narateAudio != null) {
                                //play audio for the step
                                PlayAudio(elem.narateAudio);
                            }
                            if (elem.soundEffects != null) {
                                PlaySoundEffects(elem.soundEffects);
                            }
                            if (elem.hasSlider) {
                                if (!slider.activeSelf) {
                                    //activate slider and add an EventListener that calls CheckSlider(Step) everytime the slider value changes
                                    slider.SetActive(true);
                                    slider.GetComponent<Slider>().onValueChanged.AddListener(delegate { CheckSlider(elem); });
                                } else {
                                    slider.SetActive(false);
                                }
                            } 
                            if (elem.hasQuestion) {
                                //send necessary data to the QuestionManager and call Question()
                                qAPanel.GetComponent<QuestionManager>().question = elem.question.question;
                                qAPanel.GetComponent<QuestionManager>().choices = elem.question.choices;
                                qAPanel.GetComponent<QuestionManager>().answer = elem.question.correctChoice;
                                if (elem.narateAudio != null) {
                                    if (elem.animClip != null) {
                                        if(elem.narateAudio.length > elem.animClip.length) {
                                            Invoke("CallQuestion",elem.narateAudio.length);
                                        } else {
                                            Invoke("CallQuestion",elem.animClip.length);
                                        }
                                    }
                                    Invoke("CallQuestion",elem.narateAudio.length);
                                } else if(elem.animClip != null) {
                                    Invoke("CallQuestion",elem.animClip.length);
                                }
                                CallQuestion();
                            }
                        } else if (hit.transform.gameObject != elem.objectTarget && currentStep == elem.stepOrder && !audioSource.isPlaying) {
                            PlayAudio(elem.missTap);
                        }
                    }
                }
            }
        }
    }

    public void CallQuestion() {
        qAPanel.GetComponent<QuestionManager>().Question();
    }

    public void PlayAudio(AudioClip audio) {
        audioSource.clip = audio;
        audioSource.Play();
    }

    public void PlaySoundEffects(SoundEffect[] effects) {
        foreach(SoundEffect effect in effects) {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = effect.soundEffect;
            if (effect.loop) {
                audioSource.loop = true;
            }
            audioSource.PlayDelayed(effect.delay);
        }
    }

    IEnumerator DelaySoundEffect(AudioSource source, float delay) {
        float elapsedTime = 0;

        while(elapsedTime < delay) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        source.Play();
    }

    public void PlayIntro() {
        if (!introPlayed) {
            introPlayed = !introPlayed;
            PlayAudio(introAudio);
        }
    }

    //adjusts the position/rotation/scale of the object along one axis depending on the value of the slider.
    public void CheckSlider(Step elem) {
        Vector3 p = elem.objectTarget.transform.localPosition;
        Quaternion r = elem.objectTarget.transform.localRotation;
        Vector3 s = elem.objectTarget.transform.localScale;
        float sliderMultiply = slider.GetComponent<Slider>().value * elem.manipulationMultiplier;
        switch (elem.manipulationType) {
            case ManipulationType.Transform:
                switch (elem.manipulationAxis) {
                    case ManipulationAxis.X:
                        p.x = sliderMultiply;
                        break;
                    case ManipulationAxis.Y:
                        p.y = sliderMultiply;
                        break;
                    case ManipulationAxis.Z:
                        p.z = sliderMultiply;
                        break;
                }
                break;
            case ManipulationType.Rotate:
                switch (elem.manipulationAxis) {
                    case ManipulationAxis.X:
                        GameObject.Find("TextMeshPro Text").GetComponent<TextMeshProUGUI>().text = ("" + r);
                        GameObject.Find("TextMeshPro Text (1)").GetComponent<TextMeshProUGUI>().text = ("" + (slider.GetComponent<Slider>().value * elem.manipulationMultiplier));
                        elem.objectTarget.transform.Rotate(new Vector3(sliderMultiply,r.y,r.z));
                        break;
                    case ManipulationAxis.Y:
                        elem.objectTarget.transform.Rotate(new Vector3(r.x,sliderMultiply,r.z));
                        break;
                    case ManipulationAxis.Z:
                        elem.objectTarget.transform.Rotate(new Vector3(r.x,r.y,sliderMultiply));
                        break;
                }
                break;
            case ManipulationType.Scale:
                switch (elem.manipulationAxis) {
                    case ManipulationAxis.X:
                        s.x = slider.GetComponent<Slider>().value * elem.manipulationMultiplier;
                        break;
                    case ManipulationAxis.Y:
                        s.y = slider.GetComponent<Slider>().value * elem.manipulationMultiplier;
                        break;
                    case ManipulationAxis.Z:
                        s.z = slider.GetComponent<Slider>().value * elem.manipulationMultiplier;
                        break;
                }
                break;
        }
    }
}
