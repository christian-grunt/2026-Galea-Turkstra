using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using experiment;

public class ExperimentManager : MonoBehaviour
{
    GameObject stimulus;
    // Start is called before the first frame update
    void Start()
    {
        stimulus = GameObject.Find("GaborStimulus").gameObject;
        stimulus.GetComponent<MeshRenderer>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartBaseline()
    {

    }

    public void StartTrial()
    {
        stimulus.GetComponent<MeshRenderer>().enabled = true;
    }
}
