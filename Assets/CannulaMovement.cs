using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using Random = UnityEngine.Random;

[AddComponentMenu("Vircast/Arduino/Cannula Movement Example")]
public class CannulaMovement : MonoBehaviour
{
    /* \\\ Variable declarations:  */
    public GameObject knotPusher;                        //This is the knot pusher visualization example that will be moving. 

    private float m_Speed           = 2.0f;              //Sets the speed in which the knot pusher moves.
    private float _nextPosition     = 0.0f;              //Gets the analog value from Arduino.
    List<float> savedAnalogData     = new List<float>(); // List where valid Analog Data being saved. 
    private int _nextSuggestedValue = 1;                 //This value is used in case ES return "n" to the suggested value. 
    private float _suggestedDataValue;                   //When request rollback is ON, this variable gets sent to ES as a suggested point to rollback to.
    private bool is_FaultInjected   = false;             //Checks if a fault has been detected.
    private float randomFaultNumber;                     //Used to generate a random number to mess with the position. (gets called in FaultInjection)

    /* \\\ Setup the Arduino Device:  */
    private SerialPort stream;                           //Used to open the stream communication using portName and baudRate.
    public string portName        = "COM5";              //The port in which Arduino board is connected
    public int baudRate           = 38400;               //baudRate of serial communication


    /* \\\ Methods:  */

    /* Start() -> Gets called once, the first fram of our simulation. 
     */
    void Start()
    {
        /* \\\ Initializing Serial Communication: \\\ */
        stream             = new SerialPort(portName, baudRate);
        stream.Open();
        stream.ReadTimeout = 1;

        m_Speed            = 2.0f;                     //Set the speed of the GameObject
        InvokeRepeating("saveAnalogPosition", 1f, 1f); //Call the function saveAnalogPosition() every 1 second.
    }

    /*saveAnalogPosition() -> Gets called every 1 second.
                              checks if there is no fault detected
                              Let ES know that Checkpointing has started
                              Saves analog data in savedAnalogData list
                              Let ED know that Checkpointing has ended.  
    */
    void saveAnalogPosition()
    {
        if(is_FaultInjected == false)
        {
            WriteToArduino("s"); 
            savedAnalogData.Add(_nextPosition);
            WriteToArduino("f");
        }
    }

    /* Update() -> Gets called every frame. 
     *            Checks if there is a COMM stream open and Reads data from ES. 
                        Calls ReadSerial() 
                   then, checks if there is a fault injection. 
    */
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
        //Debug.Log(String.Join(", ", savedAnalogData)); //Output all the savedData so far. 
      
        if (Input.GetKeyDown(KeyCode.R))               //We simulate a fault injection by pressing the R button.
        {
            randomFaultNumber = Random.Range(1, 1000); //Generate a random number from 1 - 1000
            is_FaultInjected  = true;
            WriteToArduino("r");                       //Let ES know that there was a fault detected, thus request for Rollback
        }
    }

    /* ReadSerial() -> Reads data from ES. 
     *                 Gets the data as string first, to check for messages.
     *                 Parses the data and converts it to float.
     */
    void ReadSerial()
    {
        float lastVal     = _nextPosition;     //Compares last value received with the next value received to check if there is any change.
        string dataString = stream.ReadLine(); //Reads value as a string from ES

        switch (dataString)
        {
            case "@":
                Debug.Log("ES requests Checkpoiting routine");
                break;
            case "!":
                Debug.Log("ES finishes Checkpoiting routine");
                break;
            case "*":
                Debug.Log("ES requests Rollback");
                break;
            case "?":
                Debug.Log("ES finish Rollback");
                break;
            case "y":                                                                               //ES aggrees about the suggested back up data point
                recoveryFunction();
                break;
            case "n":                                                                               //ES disagrees about the suggested back up data point
                _suggestedDataValue = savedAnalogData[savedAnalogData.Count - _nextSuggestedValue]; // Set new data point
                string suggestedDataSent = _suggestedDataValue.ToString();                          //Convert new data point to string before sending to ES.
                WriteToArduino(suggestedDataSent);                                                  //New data point sent to ES
                break;
        }
        
        float.TryParse(dataString, out _nextPosition);                                       //Converts data received to float. 

        if (lastVal != _nextPosition)                                                        //Compares last Value with the next Value to see if there was a change in value.
        {
            if (lastVal - 2 < _nextPosition)
            {
                knotPusher.transform.position += transform.right * Time.deltaTime * m_Speed; //Move knotpusher to the right
            }
            if (lastVal + 2 > _nextPosition)
            {
                knotPusher.transform.position += -transform.right * Time.deltaTime * m_Speed;//Move knotpusher to the left.
            }
            if(is_FaultInjected == true)                                                     //Simulate fault injection.
            {
                _nextPosition = randomFaultNumber;                                           //Set our next position to the random number generated
                requestRollback();                                                           // request rollback from ES to correct the error.
            }
        }
    }

    /* requestRollback() -> Requests rollback from ES by sending a suggested data point.
     *                      Let ES know that Visualizer wants to Rollback.
     *                      
     */
    private float requestRollback()
    {
        WriteToArduino("r");                                                   //Send 'r' to ES
        _suggestedDataValue      = savedAnalogData[savedAnalogData.Count - 1]; //Sets the suggested backup point to the last valid saved point.
        string suggestedDataSent = _suggestedDataValue.ToString();             //Converts the suggested point to string before sending to ES.
        WriteToArduino(suggestedDataSent);                                     //Send suggested point to ES as string
        return _suggestedDataValue;                                            //return the suggestedDataValue in case we need to use it. (if ES aggrees).
    }

    /* recoveryFunction() -> Assuming we get a 'y' from ES. this function does the recovery of the position.
    *                        Sets the value of position to the suggestedDataValue.
    *                        Let ES know that Recovery has ended
    */
    void recoveryFunction()
    {
        _nextPosition = _suggestedDataValue;            //sets position to the backedup value.
        WriteToArduino("q");                            //Sends 'q' to ES to declare recovery ended.
    }

    /*WriteToArduino(string) -> Handles the sending data from Unity to ES.
     *                          Gets a string, sends it to stream and flush.
     */
    public void WriteToArduino(string message)
    {
        stream.WriteLine(message);
        stream.BaseStream.Flush();
    }

}
