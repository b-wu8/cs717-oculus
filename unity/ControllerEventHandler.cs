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
    private Thread thread; // TODO: stop the thread if game ix killed

    public ServerEventHandler se_handler;

    // remote_ip_address, remote_port = server IP address, server port
    private UdpClient client;
    private IPEndPoint server_endpoint;

    /*
     * Start from Unity Engine.
     */

    public void Start()
    {
        /*
        string payload = Constants.SYN + " " + config.player_name + " " + config.lobby;
        byte[] data = Encoding.UTF8.GetBytes(payload);
        client = new UdpClient();
        server_endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        client.Send(data, data.Length, server_endpoint);
        */
        client = new UdpClient();
        server_endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);

        // Run SendData() in the background  thread
        thread = new Thread(new ThreadStart(SendData));
        thread.IsBackground = true;
        thread.Start();
    }

    public static string GetTimeStamp(DateTime time){
        return time.ToString("yyyyMMddHHmmssffff");
    }

    void OnApplicationQuit() {
        if (thread != null)
            thread.Abort();
    }


    /*
     * Send data in string format to the server.
     */
    private void SendData()
    {
        while (true)
        {
            System.Threading.Thread.Sleep(config.sleep_ms);
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

            byte[] bytes = Encoding.UTF8.GetBytes(data);
            client.Send(bytes, bytes.Length, server_endpoint);
            /*
            try
            {
                
            }
            catch (Exception e)
            {
                Debug.Log("Exception: " + e.Message);
            }
            */
        }
    }
}
