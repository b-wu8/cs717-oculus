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
 * Event handler for Oculus.
 */
public class ControllerEventHandler : MonoBehaviour
{
    public Config config; // must map in Unity Engine
    public DeviceInfoWatcher device_watcher;
    private Thread sendThread; // TODO: stop the thread if game ix killed
    private volatile bool thread_run = false;

    // remote_ip_address, remote_port = server IP address, server port
    IPEndPoint remoteEndPoint;

    /*
     * Start from Unity Engine.
     */

    public void Start()
    {
        init();
        byte[] first_data = Encoding.UTF8.GetBytes(Constants.SYN + " " + config.player_name + " " + config.lobby);
        UdpClient client = new UdpClient();
        int sent_bytes = client.Send(first_data, first_data.Length, remoteEndPoint);
        sent_bytes = client.Send(first_data, first_data.Length, remoteEndPoint); //TODO: 0.0.0.0:0
        if (sent_bytes == first_data.Length)
            Debug.Log("Sent to " + config.remote_ip_address + ":" + Constants.SYN + " " + config.player_name + " " + config.lobby);
        else
            Debug.Log("Err: Sending failure.");
    }

    public void OnApplicationQuit()
    {
        thread_run = false;
        Debug.Log("Thread: SendData stopped");
    }

    /*
     * Initialization.
     */
    private void init()
    { 
        // remote_ip_address, remote_port = server IP address, server port
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);

        // Run SendData() in the background  thread
        sendThread = new Thread(new ThreadStart(SendData));
        sendThread.IsBackground = true;
        sendThread.Start();
        thread_run = true;
    }
    public static string GetTimeStamp(DateTime time){
        return time.ToString("yyyyMMddHHmmssffff");
    }

    /*
     * Send data in string format to the server.
     */
    private void SendData()
    {
        while (thread_run)
        {
            try
            {
                string data = System.String.Empty;

                data += Constants.INPUT + " "; 
                data += config.player_name + " ";
                data += config.lobby + " ";
                data += device_watcher.GetHeadset().to_string() + " ";
                data += device_watcher.GetLeftHandController().to_string() + " ";
                data += device_watcher.GetRightHandController().to_string()+ " ";
                data += device_watcher.GetLeftJoystick().to_string() + " ";
                data += device_watcher.GetRightJoystick().to_string() + " ";
                data += GetTimeStamp(DateTime.Now);

                byte[] data_bytes = Encoding.UTF8.GetBytes(data);
                UdpClient client = new UdpClient();
                int sent_bytes = client.Send(data_bytes, data_bytes.Length, remoteEndPoint);
                if (sent_bytes == data_bytes.Length)
                    Debug.Log("Sent to " + config.remote_ip_address + ":" + data);
                else
                    Debug.Log("Err: Sending failure.");

                System.Threading.Thread.Sleep(config.send_thread_sleep_time);
            }
            catch (Exception e)
            {
                Debug.Log("Exception: " + e.Message);
            }
        }
    }
}
