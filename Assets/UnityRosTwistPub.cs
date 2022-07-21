using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
//using Unity.Robotics.Core;
using RosMessageTypes.Sensor;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.UnityRoboticsDemo;


/* This script would help in taking the input from HMD's joysticks and relay it to ROS which
 will publish on cmd_vel topic and move the robot.
 sensor_msgs/Joy to cmd_vel

 The base of this script was taken from shared_controller/scripts/2021/Shared/JoystickPublisher.cs
*/

public class UnityRosTwistPub : MonoBehaviour
{
    ROSConnection m_Ros;
    // public string topicName = "joy";
    // public OVRInput.Controller controller;
    // public OVRInput.Button bt;
    
    //public string vertical_joystick, horizontal_joystick;
    [Range(-1,1)]
    public float vertical,horizontal;
    public bool button;

    public bool manual = false; // Check
    public bool use_button = true;
    public string button_name;
    public OVRInput.Controller controller;

    public bool relative;
    float current_linear_speed, start_linear_speed, current_angular_speed, start_angular_speed;


    float[] axes = new float[2];
    int[] buttons = new int[1];
    [Range(0,1)]
    public float horizontalScale = 0.75f;
    [Range(0,1)]
    public float vertical_scale = 0.75f;

    //This is enabled when one joystick is pressed and the other one is moved.
    //// Check shared controller code to see what;s the requirement of the component///
    protected virtual void OnEnable()
    {
        m_Ros = ROSConnection.instance;
        m_Ros.RegisterPublisher("joy", "sensor_msgs/Joy"); 
        m_TimeNextScanSeconds = Clock.Now + PublishPeriodSeconds;
        sharedController = GetComponent<SharedController>();
        if(sharedController!=null)
            track=true;
    }

    // Publish every N seconds
    // public float publishMessageFrequency = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        //Start the ROS connection
        // ros = ROSConnection.GetOrCreateInstance();
        // ros.RegisterPublisher(topicName, "sensor_msgs/Joy");
        Debug.Log(UnityEngine.Input.GetJoystickNames());
    }
    
    

    // Update is called once per frame - for sending the oreintation of the joysticks
    void Update()
    {
        Vector2 ovr = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller);
        float h = ovr.x;//-Input.GetAxis(horizontal_joystick);
        float v = ovr.y;//Input.GetAxis(vertical_joystick);
        v = SpeedCurve.Evaluate(Mathf.Abs(v)) * Mathf.Sign(v) * vertical_scale;
        h = SpeedCurve.Evaluate(Mathf.Abs(h)) * Mathf.Sign(h) * horizontalScale;
        if(Mathf.Abs(v)<0.1f)
            v = 0;
        if(Mathf.Abs(h)<0.1f)
            h = 0;
        if(use_button)
        {
            bool btn = OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, controller);
            if(button != btn)
            {
                
                button = btn;
                if(button)
                {
                    start_linear_speed = current_linear_speed;
                    start_angular_speed = current_angular_speed;
                }
                else
                {
                    if(send_goal)
                        goalPublisher.send_goal();
                }
            }
            if(button)
            {
                if(relative)
                {
                    vertical = clamp(start_linear_speed + v, -1, 1);
                    horizontal = -h;
                }
                else
                {
                    vertical = clamp(v, -1, 1);
                    horizontal = clamp(-h, -1,1);  
                }
                
            }
            else
            {
                vertical = 0;
                horizontal = 0;
            }
        }
        else
        {
            button = Mathf.Abs(v)>0.1 || Mathf.Abs(h)>0.1;
            vertical = v;
            horizontal = -h;
        }

        axes[0] = horizontal;
        axes[1] = vertical;
        buttons[0] = button ? 1 : 0;
        if (Clock.NowTimeInSeconds > m_TimeNextScanSeconds)
        {
            var timestamp = new TimeStamp(Clock.time);
            var msg = new JoyMsg {
                header = new HeaderMsg
                {
                    frame_id = "map",
                    stamp = new TimeMsg
                    {
                        sec = timestamp.Seconds,
                        nanosec = timestamp.NanoSeconds,
                    }
                },
                axes = axes,
                buttons = buttons
            };
            m_Ros.Send("joy", msg);
            m_TimeNextScanSeconds = Clock.Now + PublishPeriodSeconds;
        }              
    }
    public float clamp(float x, float l, float h)
    {
        return Mathf.Min(Mathf.Max(x, l), h);
    }
}
