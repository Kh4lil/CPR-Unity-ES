using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using Random = UnityEngine.Random;
using System.Linq;

[AddComponentMenu("Vircast/Arduino/Cannula Movement Example")]
public class CannulaMovement : MonoBehaviour
{
    /* \\\ Variable declarations:  */
    public GameObject knotPusher;                        //This is the knot pusher visualization example that will be moving. 



    private float m_Speed           = 2.0f;              //Sets the speed in which the knot pusher moves.
    private float _nextPosition     = 0.0f;              //Gets the analog value from Arduino.
    private string _nextTimeStamp = "";
    List<float> savedAnalogData     = new List<float>(); // List where valid Analog Data being saved. 
    List<string> savedTimeStampData = new List<string>();
    private int _nextSuggestedValue = 1;                 //This value is used in case ES return "n" to the suggested value. 
    private float _suggestedDataValue;                   //When request rollback is ON, this variable gets sent to ES as a suggested point to rollback to.
    private string _suggestedTimeStamp;
    private bool is_FaultInjected   = false;             //Checks if a fault has been detected.
    private float randomFaultNumber;                     //Used to generate a random number to mess with the position. (gets called in FaultInjection)

    /* \\\ Setup the Arduino Device:  */
    private SerialPort stream;                           //Used to open the stream communication using portName and baudRate.
    public string portName        = "COM5";              //The port in which Arduino board is connected
    public int baudRate           = 38400;               //baudRate of serial communication
    string dataString;
    bool sendRequest = false;
    bool readyToRead = true;
    float lastVal = 0;
    float temp = 0;
    bool readNegotation;
    float alternativePointToSend;
    bool arduinoRequest = false;
    bool noFromArduinoFlag = false;


    /* \\\ Methods:  */

    /* Start() -> Gets called once, the first fram of our simulation. 
     */
    void Start()
    {
        /* \\\ Initializing Serial Communication: \\\ */
        stream             = new SerialPort(portName, baudRate);
        stream.Open();
        stream.ReadTimeout = 1000;
        m_Speed            = 2.0f;                     //Set the speed of the GameObject
        InvokeRepeating("saveAnalogPosition", 1f, 1f); //Call the function saveAnalogPosition() every 1 second.
    }

    /*saveAnalogPosition() -> Gets called every 1 second.
                              checks if there is no fault detected
                              Let ES know that Checkpointing has started
                              Saves analog data in savedAnalogData list
                              Let ES know that Checkpointing has ended.  
    */
    void saveAnalogPosition()
    {
        if(is_FaultInjected == false)
        {
            WriteToArduino("s"); 
            savedAnalogData.Add(_nextPosition);
            savedTimeStampData.Add(_nextTimeStamp);
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
        //Debug.Log("savedTimeStampList Length = " + savedTimeStampData.Count);
        //Debug.Log(System.DateTime.Now.ToString());
        if (Input.GetKeyDown(KeyCode.R)){
            faultInjectionFunction();
        }

        if (stream.IsOpen && readyToRead == true)
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
    }

    /* ReadSerial() -> Reads data from ES. 
     *                 Gets the data as string first, to check for messages.
     *                 Parses the data and converts it to float.
     */
    void ReadSerial()
    {

        dataString = stream.ReadLine(); //Reads value as a string from ES
        Debug.Log("dataString = >" + dataString);
        string alternativePoint = dataString; 
        if(noFromArduinoFlag == true)
        {
            dataString = "Y";
            noFromArduinoFlag = false;
            Debug.Log("RECOVERY SUCCESS after a No");
            readyToRead = true;
            WriteToArduino("Y");
        }


        //Debug.Log("Arduino data: " + dataString);
        switch (dataString)
        {
            case "@":
                //Debug.Log("ES starts Checkpoiting routine");
                break;
            case "!":
                //Debug.Log("ES finishes Checkpoiting routine");
                break;
            case "?":
                //Debug.Log("ES requests Rollback");
                //readyToRead = false;
                arduinoRequest = true;
                if (arduinoRequest == true)
                {
                    float suggestedDataFromArduino = 0f;
                    float.TryParse(dataString, out suggestedDataFromArduino);
                    //WriteToArduino("Y");
                    //arduinoRequest = false;
                    if (savedAnalogData.IndexOf(suggestedDataFromArduino) != -1)
                    {
                        WriteToArduino("Y");
                        arduinoRequest = false;
                    }
                    else
                    {
                        Debug.Log("Data cannot be found");
                        alternativePointToSend = savedAnalogData.Aggregate((x, y) => Math.Abs(x - suggestedDataFromArduino) < Math.Abs(y - suggestedDataFromArduino) ? x : y);
                        WriteToArduino(alternativePointToSend.ToString());
                        WriteToArduino("N");
                        arduinoRequest = true;
                    }
                }
                break;
            case "*":
                //Debug.Log("ES finished Rollback");
                break;
            case "Y":
                readyToRead = false;
                //recoveryFunction();
                Vector3 temp2 = knotPusher.transform.position;
                temp2 = new Vector3(0f, knotPusher.transform.position.y, -15.90f);
                //temp2 = new Vector3(2f, 0f, 0f);
                knotPusher.transform.position = temp2;
                //recoveryFunction();

                Debug.Log("RECOVERY SUCCESS");
                readyToRead = true;
                WriteToArduino("Y");                                                                                                     //return the suggestedDataValue in case we need to use it. (if ES aggrees).
                break;
            case "N":                                                                               //ES disagrees about the suggested back up data point
                readyToRead = false;
                //recoveryFunction();
                Vector3 temp3 = knotPusher.transform.position;
                temp3 = new Vector3(0f, knotPusher.transform.position.y, -15.90f);
                //temp2 = new Vector3(2f, 0f, 0f);
                knotPusher.transform.position = temp3;
                //recoveryFunction();
                //Get the Renderer component from the new cube
                //var sphereRenderer = sphere.GetComponent<Renderer>();

                //Call SetColor using the shader property name "_Color" and setting the color to red
                //sphereRenderer.material.SetColor("_BaseColor", Color.green);

                Debug.Log("RECOVERY SUCCESS after NO");
                readyToRead = true;
                WriteToArduino("Y");
                break;
        }
        movementFunction(dataString);

    }

    /* requestRollback() -> Requests rollback from ES by sending a suggested data point.
     *                      Let ES know that Visualizer wants to Rollback.
     *                      
     */
    private void requestRollback()
    {
        //readyToRead = true;
        WriteToArduino("r");                                                   //Send 'r' to ES
        //sendRequest = false;
        negotiate();
    }

    /* recoveryFunction() -> Assuming we get a 'y' from ES. this function does the recovery of the position.
    *                        Sets the value of position to the suggestedDataValue.
    *                        Let ES know that Recovery has ended
    */
    void recoveryFunction()
    {
        dataString = _suggestedDataValue.ToString();            //sets position to the backedup value.
        //movementFunction(dataString);

        //Sends 'q' to ES to declare recovery ended.
        readyToRead = true;
        lastVal = 0;
        
        //return dataString;
    }

    /*WriteToArduino(string) -> Handles the sending data from Unity to ES.
     *                          Gets a string, sends it to stream and flush.
     */
    public void WriteToArduino(string message)
    {
        stream.WriteLine(message);
        stream.BaseStream.Flush();
    }

    void movementFunction(string dataString)
    {
        if (dataString.Contains("-"))
        {
            string[] tmp = dataString.Split('-');
            lastVal = _nextPosition;
            float.TryParse(tmp[0], out _nextPosition);
            _nextTimeStamp = tmp[1];
        }
        //lastVal = _nextPosition;                                                       //Compares last value received with the next value received to check if there is any change.
        else
        {
            float.TryParse(dataString, out _nextPosition);
        }
        //float.TryParse(dataString, out _nextPosition);                                       //Converts data received to float. 

        //Debug.Log("_nextPosition = > " + _nextPosition);

        if (is_FaultInjected == true)
        {
            knotPusher.transform.position += 20 * transform.right * Time.deltaTime * m_Speed; //Move knotpusher to the right
            is_FaultInjected = false;
        }

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
        }
    }

    private void faultInjectionFunction()
    {

        is_FaultInjected = true;
        readyToRead = false;
        randomFaultNumber = Random.Range(1, 1000); //Generate a random number from 1 - 1000
        //_nextPosition = randomFaultNumber;                                           //Set our next position to the random number generated
        movementFunction(randomFaultNumber.ToString());
        requestRollback();                                                           // request rollback from ES to correct the error.
    }

    private float negotiate()
    {
        //foreach(int i in savedAnalogData)
        //{
        //  Debug.Log("savedAnalogData = >>>> " + i);
        //}
        //Debug.Log("savedAnalogData[savedAnalogData.Count - _nextSuggestedValue] " + savedAnalogData[savedAnalogData.Count - _nextSuggestedValue]);
        _suggestedDataValue = savedAnalogData[savedAnalogData.Count - _nextSuggestedValue]; //Sets the suggested backup point to the last valid saved point.
        //Use timestamps
        //_suggestedTimeStamp = savedTimeStampData[savedTimeStampData.Count - 1];
        string suggestedDataSent = _suggestedDataValue.ToString();             //Converts the suggested point to string before sending to ES.
        //Debug.Log("HEEEERE, SENDING DATA TO ARDUINO: " + _suggestedTimeStamp);
        WriteToArduino(_suggestedDataValue.ToString());                                     //Send suggested point to ES as string
        //string negotiationListener = stream.ReadLine();                        //Reads value as a string from ES
        readNegotation = true;
        readyToRead = true;
        ReadSerial();
        return _suggestedDataValue;                                         //return the suggestedDataValue in case we need to use it. (if ES aggrees).
       
    }
}
