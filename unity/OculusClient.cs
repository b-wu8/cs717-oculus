using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.XR;

public class OculusClient : MonoBehaviour
{
    public Config config;
    public PlayerView view;
    public DeviceInfoWatcher device_watcher;
    private string last_packet;
    private int discrete_timeout_ms;
    private Dictionary<string, bool> current_discrete_state;
    public UdpClient receive_client, heartbeat_client, continuous_input_client, discrete_input_client;
    private Thread receive_thread, heartbeat_thread, continuous_input_thread, discrete_input_thread;
    private DateTime ping_start_time;
    public IPEndPoint server_endpoint;
    private TouchScreenKeyboard overlayKeyboard;
    public static string inputText = "";

    public void Start()
    {
        receive_client = new UdpClient();
        byte[] temp = Encoding.UTF8.GetBytes(Constants.SYN + " " + config.player_name + " " + config.lobby);
        server_endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        receive_client.Send(temp, temp.Length, server_endpoint);
        receive_client.Send(temp, temp.Length, server_endpoint);
        receive_thread = new Thread(new ThreadStart(ReceiveData));
        receive_thread.IsBackground = true;
        receive_thread.Start();

        heartbeat_client = new UdpClient();
        heartbeat_thread = new Thread(new ThreadStart(Heartbeat));
        heartbeat_thread.IsBackground = true;
        heartbeat_thread.Start();

        continuous_input_client = new UdpClient();
        continuous_input_thread = new Thread(new ThreadStart(SendContinuousData));
        continuous_input_thread.IsBackground = true;
        continuous_input_thread.Start();

        discrete_timeout_ms = 10;
        current_discrete_state = new Dictionary<string, bool>();
        current_discrete_state.Add("LeftPrimaryButton", false);
        current_discrete_state.Add("RightPrimaryButton", false);
        current_discrete_state.Add("LeftSecondaryButton", false);
        current_discrete_state.Add("RightSecondaryButton", false);
        discrete_input_client = new UdpClient();
        discrete_input_thread = new Thread(new ThreadStart(SendDiscreteData));
        discrete_input_thread.IsBackground = true;
        discrete_input_thread.Start();

        overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        if (overlayKeyboard != null)
            inputText = overlayKeyboard.text;
    }

    void OnGUI()
    {
        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "Last Packet: \n" + last_packet, style);
    }

    void OnApplicationQuit()
    {   
        receive_thread.Abort();

        continuous_input_thread.Abort();
        discrete_input_thread.Abort();

        byte[] data = Encoding.UTF8.GetBytes(Constants.FIN + " " + config.player_name + " " + config.lobby);
        heartbeat_client.Send(data, data.Length, server_endpoint);
        heartbeat_thread.Abort();
    }

    private void HandleDataMessage(string message) {
        string[] lines = message.Split('\n');
        string[] msg = lines[0].Split(' ');
        view.num_players = int.Parse(msg[2]);

        string[] sphere_pieces = lines[1].Split(' ');
        view.sphere_loc = new Vector3(
            float.Parse(sphere_pieces[1], CultureInfo.InvariantCulture.NumberFormat), 
            float.Parse(sphere_pieces[2], CultureInfo.InvariantCulture.NumberFormat), 
            float.Parse(sphere_pieces[3], CultureInfo.InvariantCulture.NumberFormat));

        string[] plane_pieces = lines[2].Split(' ');
        view.plane_loc = new Vector3(
            float.Parse(plane_pieces[1], CultureInfo.InvariantCulture.NumberFormat), 
            float.Parse(plane_pieces[2], CultureInfo.InvariantCulture.NumberFormat), 
            float.Parse(plane_pieces[3], CultureInfo.InvariantCulture.NumberFormat));

        for (int i = 3; i < lines.Length; i++) 
        {
            string[] infos = lines[i].Split(' ');
            string avatar_name = infos[0];
            if (view.avatars.ContainsKey(avatar_name)) {
                view.avatars[avatar_name].update(lines[i]);
            } else {
                view.avatars.Add(avatar_name, new Avatar(config.player_name));
                view.avatars[avatar_name].update(lines[i]);
            } 
        }
    }

    private void HandleLeaveMessage(string message) {
        List<string> msg = new List<string>(message.Split(' '));
        int num_players = int.Parse(msg[2]);
        List<string> players = msg.GetRange(3, num_players);
        Debug.Log(string.Join( ", ", msg.ToArray()));
        Debug.Log(string.Join( ", ", players.ToArray()));
        foreach (KeyValuePair<string, Avatar> kv_avatar in view.avatars) {
            if (!players.Contains(kv_avatar.Key)) {
                kv_avatar.Value.to_be_destroyed = true;
                Debug.Log("Marked " + kv_avatar.Key + " for destruction");
            }
        }
    }

    private void HandleHeartbeatMessage(string message) {
        DateTime ping_end_time = DateTime.Now;
        TimeSpan ping_time = ping_end_time.Subtract(ping_start_time);
        Debug.Log("Ping time: " + ping_time.Milliseconds + " ms");
    }

    private void ReceiveData()
    {
        while (true)
        {
            try 
            {
                IPEndPoint from_endpoint = new IPEndPoint(IPAddress.Any, 0);
                string message = Encoding.UTF8.GetString(receive_client.Receive(ref from_endpoint));
                last_packet = message;
                int message_type = Int32.Parse(message.Substring(0, message.IndexOf(' ')));
                if (message_type == Constants.DATA)
                    HandleDataMessage(message);
                else if (message_type == Constants.LEAVE)
                    HandleLeaveMessage(message);
                else if (message_type == Constants.HEARTBEAT)
                    HandleHeartbeatMessage(message);
                else 
                    Debug.Log("Error - malformed message: " + message);

            } 
            catch (Exception e)
            {
                Debug.Log("Exception in ReceiveData(): " + e);
            }
        }
    }

    private void Heartbeat() {
        while (true) {
            System.Threading.Thread.Sleep(config.heartbeat_sleep_ms);
            try 
            {
                ping_start_time = DateTime.Now;
                byte[] data = Encoding.UTF8.GetBytes(Constants.HEARTBEAT + " " + config.player_name + " " + config.lobby);
                heartbeat_client.Send(data, data.Length, server_endpoint);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in Heartbeat(): " + e);
            }
        }
    }

    private void SendContinuousData()
    {
        while (true)
        {
            System.Threading.Thread.Sleep(config.controller_sleep_ms);
            try {
                string data = Constants.INPUT + " " + config.player_name + " " + config.lobby + " " + device_watcher.GetControllerData();
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                continuous_input_client.Send(bytes, bytes.Length, server_endpoint);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in SendControllerData(): " + e);
            }
        }
    }

    private void SendDiscreteData()
    {
        while (true)
        {
            System.Threading.Thread.Sleep(discrete_timeout_ms);
            try {
                //if left controller primary button pushed, send to send right away
                bool new_state = device_watcher.GetLeftControllerPrimaryButtonPushed();
                string btn_message;
                if (current_discrete_state["LeftPrimaryButton"] != new_state)
                {
                    current_discrete_state["LeftPrimaryButton"] = new_state;
                    if (new_state) // if button pushed
                    {
                        btn_message = Constants.DISCRETE + " " + "left hand primary button pushed :)";
                    }
                    else
                    {
                        btn_message = Constants.DISCRETE + " " + "left hand primary released";
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes(btn_message);
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }

                //if left controller secondary button pushed, send to send right away
                new_state = device_watcher.GetLeftControllerSecondaryButtonPushed();
                if (current_discrete_state["LeftSecondaryButton"] != new_state)
                {
                    // trigger event
                    current_discrete_state["LeftSecondaryButton"] = new_state;
                    if (new_state) // if button pushed
                    {
                        btn_message = Constants.DISCRETE + " " + "left hand secondary button pushed :)";
                    }
                    else
                    {
                        btn_message = Constants.DISCRETE + " " + "left hand secondary button released :)";
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes(btn_message);
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }

                new_state = device_watcher.GetRightControllerPrimaryButtonPushed();
                if (current_discrete_state["RightPrimaryButton"] != new_state)
                {
                    // trigger event
                    current_discrete_state["RightPrimaryButton"] = new_state;
                    if (new_state) // if button pushed
                    {
                        btn_message = Constants.DISCRETE + " " + "right hand primary button pushed :)";
                    }
                    else
                    {
                        btn_message = Constants.DISCRETE + " " + "right hand primary button released :)";
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes(btn_message);
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }

                new_state = device_watcher.GetRightControllerSecondaryButtonPushed();
                if (current_discrete_state["RightSecondaryButton"] != new_state)
                {
                    // trigger event
                    current_discrete_state["RightSecondaryButton"] = new_state;
                    if (new_state) // if button pushed
                    {
                        btn_message = Constants.DISCRETE + " " + "right hand secondary button pushed :)";
                    }
                    else
                    {
                        btn_message = Constants.DISCRETE + " " + "right hand secondary button released :)";
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes(btn_message);
                    discrete_input_client.Send(bytes, bytes.Length, server_endpoint);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception in SendControllerData(): " + e);
            }
        }
    }
}
