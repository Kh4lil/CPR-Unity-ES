using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionCheckpoint : MonoBehaviour
{

    public GameObject knotPusher;
    List<float> savedPosition = new List<float>();

    //Random Injection
    //private bool random;
    private float number;




    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("savePosition", 1f, 1f);
        //random = false;
    }

    void savePosition()
    {
        savedPosition.Add(knotPusher.transform.position.x);
    }

    void recoveryFunction()
    {
        Vector3 temp2 = knotPusher.transform.position;
        temp2 = new Vector3(savedPosition[savedPosition.Count - 1], knotPusher.transform.position.y, -15.90f);
        //knotPusher.transform.position = temp2;
        //Debug.Log("savedPosition[savedPosition.Count - 1] =:> " + -savedPosition[savedPosition.Count - 1]);
        //Debug.Log("recoverd = " + knotPusher.transform.position.x);
        knotPusher.transform.position = temp2;
        Debug.Log("RECOVERED");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        number = Random.Range(1, 255);
        //random = false;
        if (Input.GetKeyDown(KeyCode.R))
        {
            knotPusher.transform.position += transform.right * Time.deltaTime * number;
            //Debug.Log("new position = " + -knotPusher.transform.position.x);

            //Vector3 temp = new Vector3(-15.909f, 0.021f, number);
            //knotPusher.transform.position = temp;
            //random = true;
            //Debug.Log("new position = " + knotPusher.transform.position.x);
            recoveryFunction();
        }
        /*
        if(random = true)
        {
            Vector3 temp2 = new Vector3(0.0f, 0.0f, savedPosition[savedPosition.Count - 1]);
            knotPusher.transform.position = temp2;
            Debug.Log("recoverd = " + knotPusher.transform.position.x);
        }
        */
    }
}
