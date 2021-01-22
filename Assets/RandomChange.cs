using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomChange : MonoBehaviour
{
    public GameObject knotPusher;
    private bool random = false;
    private float number;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        number = Random.Range(1, 255);
        if (Input.GetKeyDown(KeyCode.R))
        {
            //random = TRUE;
            knotPusher.transform.position += transform.right * Time.deltaTime * number;
            //Debug.Log("new position = " + knotPusher.transform.position.x);
        }
    }
}
