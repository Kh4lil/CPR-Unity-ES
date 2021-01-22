using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyBoardMovementTest : MonoBehaviour
{

    private float m_Speed; //Sets the speed.


    // Use this for initialization
    void Start()
    {
        m_Speed = 2.0f; //Set the speed of the GameObject
    }

    // Update is called once per frame
    void Update()
    {


        //Turn Right.
        if (Input.GetKey("a")) 
        {
            if (this.transform.localPosition.x <= 0.0f)
            {
                //Rotate our Tank about the Y axis in the positive direction
                this.transform.position += transform.right * Time.deltaTime * m_Speed;
            }
        }

        //Turn Left.
        if (Input.GetKey("s")) 
        {
            if (this.transform.localPosition.x >= 0.0f)
            {
                //Rotate our Tank about the Y axis in the negative direction
                this.transform.position += -transform.right * Time.deltaTime * m_Speed;
            }

        }
    }

}