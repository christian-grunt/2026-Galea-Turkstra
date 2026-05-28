using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Randomize_location : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RandomizePosition();
        }
    }


    void RandomizePosition() 
    {
        // TODO: Update to find proper bounds of camera scene
        Vector3 newPos = transform.localPosition;
        float parentHeight = transform.parent.localScale.x *4;
        float parentX = transform.parent.localPosition.x;
        newPos.x = Random.Range(-parentHeight / 2, parentHeight / 2);

        float parentWidth = transform.parent.localScale.z *4;
        float parentZ = transform.parent.localPosition.z;
        newPos.z = Random.Range(-parentWidth / 2, parentWidth / 2);

        newPos.y = 0.05f;
        //Debug.Log("Height = " + parentHeight);
        //Debug.Log("Width = " + parentWidth);
        //Debug.Log("NewPos = " + newPos);
        transform.localPosition = newPos;
    }
}
