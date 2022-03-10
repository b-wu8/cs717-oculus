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

struct Player
{
    public string name;
    public string lobby;
    public GameObject head;
    public Vector3 head_loc;
    public Quaternion head_quat;
    public GameObject right;
    public Vector3 right_loc;
    public Quaternion right_quat;
    public GameObject left;
    public Vector3 left_loc;
    public Quaternion left_quat;
}

public class ServerEventHandler : MonoBehaviour
{
    public Transform xr_transform;
    public Config config;
    public PlayerView pv;
    private string last_packet;
    public UdpClient client;
    private Vector3 xr_loc;
    private float x, y, z, qx, qy, qz, qw;
    private Thread receive_thread;

    public void Start()
    {
        client = new UdpClient();
        byte[] temp = Encoding.UTF8.GetBytes(Constants.SYN + " " + config.player_name + " " + config.lobby);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        // client.Send(temp, temp.Length, remoteEndPoint);
        // client.Send(temp, temp.Length, remoteEndPoint);

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

    // receive thread
    private async void ReceiveData()
    {
        while (true)
        {
            IPEndPoint from_addr = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = client.Receive(ref from_addr);
            string text = Encoding.UTF8.GetString(data);
            last_packet = text;
            string[] lines = text.Split('\n');

            string[] msg = lines[0].Split(' ');
            pv.num_players = int.Parse(msg[1]);
            Debug.Log("Number of players: " + pv.num_players);
                
            string[] sphere_pieces = lines[1].Split(' ');
            x = float.Parse(sphere_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
            y = float.Parse(sphere_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
            z = float.Parse(sphere_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
            pv.sphere_loc = new Vector3(x, y, z);
            Debug.Log("Sphere position: " + pv.sphere_loc.ToString());

            string[] plane_pieces = lines[2].Split(' ');
            x = float.Parse(plane_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
            y = float.Parse(plane_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
            z = float.Parse(plane_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
            pv.plane_loc = new Vector3(x, y, z);

            for (int i = 3; i < lines.Length; i++) 
            {
                string[] players = lines[i].Split(' ');
                try 
                {
                    PlayerInfo curr_player = pv.player_infos[players[0]];
                    string[] head_pieces = lines[i].Split(' ');
                    x = float.Parse(head_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                    y = float.Parse(head_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                    z = float.Parse(head_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
                    qx = float.Parse(head_pieces[4], CultureInfo.InvariantCulture.NumberFormat);
                    qy = float.Parse(head_pieces[5], CultureInfo.InvariantCulture.NumberFormat);
                    qz = float.Parse(head_pieces[6], CultureInfo.InvariantCulture.NumberFormat);
                    qw = float.Parse(head_pieces[7], CultureInfo.InvariantCulture.NumberFormat);
                    curr_player.headset = new Headset(new Vector3(x, y, z), new Quaternion(qx, qy, qz, qw));

                    // TODO: LH RH   
                }
                catch (KeyNotFoundException)
                {
                    Debug.Log("New player being added");
                    pv.player_infos.Add(players[0], new PlayerInfo(lines[i])); // add new player
                    Debug.Log("Name: " + players[0]);
                    Debug.Log("Head Location " + pv.player_infos[players[0]].headset.position.ToString());
                }
                
            }
        }
    }
}
