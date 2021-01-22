using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports; //included to use SerialPorts
using Random = UnityEngine.Random;

[AddComponentMenu("Vircast/Arduino/Cannula Movement Example")]


public class CannulaMovement : MonoBehaviour
{
    public GameObject knotPusher; //This is the knot pusher visualization example that will be moving. 
    private float m_Speed; //Sets the speed in which the knot pusher moves.
    private float nextPosition = 0.0f;

    List<float> savedAnalogData = new List<float>(); //Analog Data being saved. 

    /* \\\ Setup the Arduino Device: \\\ */
    [Tooltip("SerialPort that the device is using (Example: COM4)")]
    public string portName = "COM5"; //The part in which Arduino board is connected
    [Tooltip("Baudrate of the communication (Example: 9600)")]
    public int baudRate = 38400; //baudRate of serial communication
    private SerialPort stream; //Used to open the stream communication using portName and baudRate.

    //Random Injection
    private bool random;
    private float number;

    private bool is_FaultInjected = false;



    // Start is called before the first frame update
    void Start()
    {
        /* \\\ Initializing Serial Communication: \\\ */
        stream = new SerialPort(portName, baudRate);
        stream.Open();
        stream.ReadTimeout = 1;

        m_Speed = 2.0f; //Set the speed of the GameObject
        InvokeRepeating("saveAnalogPosition", 1f, 1f);
    }

    void saveAnalogPosition()
    {
        if(is_FaultInjected == false)
        {
            savedAnalogData.Add(nextPosition);
            Debug.Log("Visualizer finishes Checkpoiting routine");
        }

    }

    void ReadSerial()
    {
        float lastVal = nextPosition;
        string dataString = stream.ReadLine();
        //var dataBlocks = dataString.Split(',');
        if (dataString == "@")
        {
            Debug.Log("ES requests Checkpoiting routine");
        }
        else if (dataString == "!")
        {
            Debug.Log("ES finishes Checkpoiting routine");
        }
        else if (dataString == "*")
        {
            Debug.Log("ES requests Rollback");
        }
        else if (dataString == "?")
        {
            Debug.Log("ES finish Rollback");
        }

        float.TryParse(dataString, out nextPosition);

        //Debug.Log("SENSOR 1 DATA: " + dataString);
        //Debug.Log("DATA FROM ARDUINO: " + position);
        if (lastVal != nextPosition)
        {
            if (lastVal - 2 < nextPosition)
            {
                knotPusher.transform.position += transform.right * Time.deltaTime * m_Speed;
            }
            if (lastVal + 2 > nextPosition)
            {
                knotPusher.transform.position += -transform.right * Time.deltaTime * m_Speed;
            }
            if(is_FaultInjected == true)
            {
                nextPosition = number;
                //Debug.Log("NEW nextPosition: " + nextPosition);
                recoveryFunction();
            }
        }
    }

    void recoveryFunction()
    {
        Debug.Log("Recovery Starting");
    }

    // Update is called once per frame
    void Update()
    {
        if (stream.IsOpen)
        {
            try
            {
                ReadSerial();     
            }
            catch (Exception)
            {
                Debug.LogWarning("Serial Failed");
            }
        }
        Debug.Log(String.Join(", ", savedAnalogData));

        number = Random.Range(1, 1000);
        if (Input.GetKeyDown(KeyCode.R))
        {
            is_FaultInjected = true;
            Debug.Log("FAULT");
            WriteToArduino("r");
        }


    }

public void WriteToArduino(string message)
    {
        stream.WriteLine(message);
        stream.BaseStream.Flush();
    }

}
