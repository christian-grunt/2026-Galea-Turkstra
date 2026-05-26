using onnx;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    AudioSource audio;
    DisplayEEG eeg;
    public float targetPitch = 1;
    public float scale = 0.2f;
    public float delta = 0.001f;
    // Start is called before the first frame update
    void Start()
    {
        audio = GetComponent<AudioSource>();
        eeg = GetComponent<DisplayEEG>();
        if (audio) audio.Play();
    }

    // Update is called once per frame
    void Update()
    {
        targetPitch = ((eeg.currentAlpha - eeg.baselineMean) / eeg.baselineStdDev) * scale;
        audio.pitch  = delta >= Mathf.Abs(targetPitch - audio.pitch)? targetPitch: audio.pitch + Mathf.Sign(targetPitch - audio.pitch) * delta;
        //Debug.Log("Current Alpha = " + eeg.currentAlpha);
    }
}
