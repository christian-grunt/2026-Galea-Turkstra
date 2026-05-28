using UnityEngine;

public class GaborController : MonoBehaviour
{
    public Material material;

    [Header("Stimulus")]
    public float orientation = 45f;
    public float spatialFrequency = 32f;
    public float contrast = 0.2f;
    public float sigma = 0.2f;

    [Header("Animation")]
    public float driftSpeed = 0f;
    public float phase = 0f;

    [Header("Display")]
    public float meanLuminance = 0.15f;
    public float edgeSoftness = 0.2f;

    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        material.SetFloat("_Orientation", orientation);
        material.SetFloat("_SpatialFrequency", spatialFrequency);
        material.SetFloat("_Contrast", contrast);
        material.SetFloat("_Sigma", sigma);
        material.SetFloat("_DriftSpeed", driftSpeed);
        material.SetFloat("_Phase", phase);
        material.SetFloat("_MeanLuminance", meanLuminance);
        material.SetFloat("_EdgeSoftness", edgeSoftness);
    }
}

/*
 * 
 * 
 */

