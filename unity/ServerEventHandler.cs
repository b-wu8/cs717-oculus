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

public class ServerEventHandler : MonoBehaviour
{
    public Config config;
    public PlayerView view;
    private string last_packet;
    public UdpClient client;
    private Vector3 xr_loc;
    private float x, y, z, qx, qy, qz, qw;
    private Thread receive_thread;

    // todo: delete the debug variable
    public Text debug_display;
    public int num_of_players = 0;
    public string debug_playerinfo;
    public IPEndPoint server_endpoint;

    public void Start()
    {
        client = new UdpClient();
        byte[] temp = Encoding.UTF8.GetBytes(Constants.SYN + " " + config.player_name + " " + config.lobby);
        server_endpoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        client.Send(temp, temp.Length, server_endpoint);
        client.Send(temp, temp.Length, server_endpoint);

        receive_thread = new Thread(new ThreadStart(ReceiveData));
        receive_thread.IsBackground = true;
        receive_thread.Start();
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
    }

    public void Send(byte[] data) {
        if (client == null)
            return;
        client.Send(data, data.Length, server_endpoint);
    }

    // receive thread
    private void ReceiveData()
    {
        while (true)
        {
            try {
                IPEndPoint from_addr = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref from_addr);
                // Debug.Log("Received message!!!!!!!!!!!!!!!!!!");
                string text = Encoding.UTF8.GetString(data);
                last_packet = text;
                string[] lines = text.Split('\n');

                string[] msg = lines[0].Split(' ');
                view.num_players = int.Parse(msg[1]);
                num_of_players = view.num_players;

                string[] sphere_pieces = lines[1].Split(' ');
                x = float.Parse(sphere_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(sphere_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(sphere_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
                view.sphere_loc = new Vector3(x, y, z);

                string[] plane_pieces = lines[2].Split(' ');
                x = float.Parse(plane_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(plane_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(plane_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
                view.plane_loc = new Vector3(x, y, z);

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
            catch (Exception e)
            {
                Debug.Log("Exception in server handler: " + e);
            }
        }
    }
}
