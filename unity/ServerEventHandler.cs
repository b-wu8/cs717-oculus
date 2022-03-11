/* 
 * Handle all the incoming data from the server
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
using System.Collections.Generic;
using UnityEngine.UI;

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
    private volatile bool thread_run = false;

    // todo: delete the debug variable
    public Text debug_display;
    public int num_of_players = 0;
    public string debug_playerinfo;

    public void Start()
    {
        client = new UdpClient();
        byte[] temp = Encoding.UTF8.GetBytes(Constants.SYN + " " + config.player_name + " " + config.lobby);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        client.Send(temp, temp.Length, remoteEndPoint);
        client.Send(temp, temp.Length, remoteEndPoint);

        receive_thread = new Thread(new ThreadStart(ReceiveData));
        receive_thread.IsBackground = true;
        receive_thread.Start();
        thread_run = true;
    }

    public void OnApplicationQuit()
    {
        thread_run = false;
        Debug.Log("Thread: ReceiveData stopped");
    }

    void OnGUI()
    {
        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "Last Packet: \n" + last_packet, style);
    }

    public void Update()
    {
        Debug.Log("Last packet: " + last_packet);

        Dictionary<string, PlayerInfo> player_infos_copy = new Dictionary<string, PlayerInfo>(pv.player_infos);
        foreach (KeyValuePair<string, PlayerInfo> name_2_player_info in player_infos_copy)
        {
            Debug.Log("Player name: " + name_2_player_info.Key);
        }
    }

    // receive thread
    private void ReceiveData()
    {
        while (thread_run)
        {
            IPEndPoint from_addr = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = client.Receive(ref from_addr);
            string text = Encoding.UTF8.GetString(data);
            last_packet = text;
            string[] lines = text.Split('\n');

            string[] msg = lines[0].Split(' ');
            pv.num_players = int.Parse(msg[1]);
            num_of_players = pv.num_players;

            string[] sphere_pieces = lines[1].Split(' ');
            x = float.Parse(sphere_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
            y = float.Parse(sphere_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
            z = float.Parse(sphere_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
            pv.sphere_loc = new Vector3(x, y, z);
            

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
                    if (pv.player_infos.ContainsKey(players[0]))
                    {
                        pv.player_infos[players[0]] = new PlayerInfo(lines[i]);
                    } else
                    {
                        //Debug.Log("New player being added");
                        pv.player_infos.Add(players[0], new PlayerInfo(lines[i])); // add new player
                    }

                    //PlayerInfo = pv.player_infos[players[0]];
                    //string[] head_pieces = lines[i].Split(' ');
                    //x = float.Parse(head_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                    //y = float.Parse(head_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                    //z = float.Parse(head_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
                    //qx = float.Parse(head_pieces[4], CultureInfo.InvariantCulture.NumberFormat);
                    //qy = float.Parse(head_pieces[5], CultureInfo.InvariantCulture.NumberFormat);
                    //qz = float.Parse(head_pieces[6], CultureInfo.InvariantCulture.NumberFormat);
                    //qw = float.Parse(head_pieces[7], CultureInfo.InvariantCulture.NumberFormat);
                    //curr_player.headset = new Headset(new Vector3(x, y, z), new Quaternion(qx, qy, qz, qw));

                    // TODO: LH RH   
                }
                catch (Exception e)
                {
                    Debug.Log("Exception in server handler: " + e);
                }
                
            }
            System.Threading.Thread.Sleep(config.receive_thread_sleep_time);
        }
    }
}
