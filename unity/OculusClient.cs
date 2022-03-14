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

public class OculusClient : MonoBehaviour
{
    public Config config;
    public PlayerView view;
    public DeviceInfoWatcher device_watcher;
    private string last_packet;
    public UdpClient receive_client, heartbeat_client, controller_client;
    private Thread receive_thread, heartbeat_thread, controller_thread;
    public IPEndPoint server_endpoint;

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

        controller_client = new UdpClient();
        controller_thread = new Thread(new ThreadStart(SendControllerData));
        controller_thread.IsBackground = true;
        controller_thread.Start();
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

        controller_thread.Abort();

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
                byte[] data = Encoding.UTF8.GetBytes(Constants.HEARTBEAT + " " + config.player_name + " " + config.lobby);
                heartbeat_client.Send(data, data.Length, server_endpoint);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in Heartbeat(): " + e);
            }
        }
    }

    private void SendControllerData()
    {
        while (true)
        {
            System.Threading.Thread.Sleep(config.controller_sleep_ms);
            try {
                string data = Constants.INPUT + " " + config.player_name + " " + config.lobby + " " + device_watcher.GetControllerData();
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                controller_client.Send(bytes, bytes.Length, server_endpoint);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in SendControllerData(): " + e);
            }
        }
    }
}
