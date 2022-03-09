/*
 * Send data to the server in response to events occuring on headset.
 */

using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;

/*
 * Types to classify what kind of message is being sent.
 */
static class Constants
{
    // startup message indicates that a player is trying to connect to room
    // the player name is mapped to the included IP address
    public const string OCULUS = "OCULUS";  

    // controller input message
    public const string INPUT = "INPUT";    
}

/*
 * Event handler for Oculus.
 */
public class ControllerEventHandler : MonoBehaviour
{
    public Config config; // must map in Unity Engine
    public DeviceInfoWatcher device_watcher;
    private Thread sendThread;
    
    /*
     * Start from Unity Engine.
     */
    public void Start()
    {
        // TODO: send an OCULUS message to 
        init();

    }

    /*
     * Initialization.
     */
    private void init()
    {
        // Run SendData() in the background  thread
        sendThread = new Thread(new ThreadStart(SendData));
        sendThread.IsBackground = true;
        sendThread.Start();
    }
    public static string GetTimeStamp(DataTime time){
        return time.ToString("yyyyMMddHHmmssffff");
    }

    /*
     * Send data in string format to the server.
     */
    private void SendData()
    {
        string data = System.String.Empty; 

        // remote_ip_address, remote_port = server IP address, server port
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port); 

        while (true)
        {
            try
            { 
                data += Constants.INPUT + " "; 
                data += config.player_name + " ";
                data += config.lobby + " ";
                data += device_watcher.GetHeadset().to_string() + " ";
                data += device_watcher.GetLeftHandController.to_string() + " ";
                data += device_watcher.GetRightHandController.to_string()+ " ";
                data += device_watcher.GetLeftJoystick.to_string() + " ";
                data += device_watcher.GetRightJoystick.to_string() + " ";
                data += GetTimeStamp(DateTime.Now);
            }
            catch (Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }
}
