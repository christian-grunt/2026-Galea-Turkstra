using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenBCI.EyeTracking;

namespace experiment
{
    public class Start_Trial : MonoBehaviour, IGazeTarget
    {
        [SerializeField] ExperimentManager experimentManager;
        public float counter = 0;
        public float focusTime = 3f;
        public bool focus = false;
        public bool achievedFocus = false;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            if (focus)
            {
                counter += Time.deltaTime;
                if (counter >= focusTime)
                {
                    achievedFocus = true;
                    GetComponent<MeshRenderer>().enabled = false;
                    experimentManager.StartTrial();
                }
            }
        }

        public void StartGaze(EyeGazeManager source)
        {
            focus = true;
            counter = 0;
        }
        public void StopGaze(EyeGazeManager source)
        {
            focus = false;

        }
    }
}