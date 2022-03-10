using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;

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

    private GameObject sphere, plane;
    private Player player;
    // public List<PlayerInfo> all_player_info = new List<PlayerInfo>();
    public PlayerView pv;
    // public Dictionary<string, PlayerInfo> all_players = new Dictionary<string, PlayerInfo>();
    private string last_packet;
    public UdpClient client;
    private Vector3 sphere_loc, plane_loc, xr_loc;
    private float x, y, z, qx, qy, qz, qw;
    private Thread receive_thread;

    public void Start()
    {
        client = new UdpClient();
        byte[] temp = Encoding.UTF8.GetBytes(Constants.SYN + " " + config.player_name + " " + config.lobby);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        client.Send(temp, temp.Length, remoteEndPoint);
        client.Send(temp, temp.Length, remoteEndPoint);

        sphere_loc = new Vector3(0f, 0f, 0f);
        plane_loc = new Vector3(0f, 0f, 0f);
        xr_loc = new Vector3(0f, 1f, 0f);

        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(0f, 0f, 0f);
        var sphere_renderer = sphere.GetComponent<Renderer>();
        sphere_renderer.material.SetColor("_Color", Color.red);

        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = new Vector3(0f, 0f, 0f);
        var plane_renderer = plane.GetComponent<Renderer>();
        plane_renderer.material.SetColor("_Color", Color.white);

        player.head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        player.head.transform.position = new Vector3(0f, 0f, 0f);
        player.head.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        var head_renderer = player.head.GetComponent<Renderer>();
        head_renderer.material.SetColor("_Color", Color.magenta);

        player.right = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.right.transform.position = new Vector3(0f, 0f, 0f);
        player.right.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        var right_renderer = player.right.GetComponent<Renderer>();
        right_renderer.material.SetColor("_Color", Color.green);

        player.left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.left.transform.position = new Vector3(0f, 0f, 0f);
        player.left.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        var left_renderer = player.left.GetComponent<Renderer>();
        left_renderer.material.SetColor("_Color", Color.cyan);

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

    // Update is called once per frame
    public void Update()
    {
        sphere.transform.position = sphere_loc;
        plane.transform.position = plane_loc;
        
        player.head.transform.position = player.head_loc;
        player.head.transform.rotation = player.head_quat;

        player.right.transform.position = player.right_loc;
        player.left.transform.position = player.left_loc;


        xr_transform.position = xr_loc;
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

            if (int.Parse(lines[0]) == Constants.SYN) 
            {
                // new player entered
            } 
            else // INPUT packet
            {
                string[] sphere_pieces = lines[1].Split(' ');
                x = float.Parse(sphere_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(sphere_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(sphere_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
                sphere_loc = new Vector3(x, y, z);
                //player.head_loc = new Vector3(x + 2f, y + 2f, z + 2f);
                player.left_loc = new Vector3(x + 1.5f, y + 1.5f, z);
                player.right_loc = new Vector3(x + 2.5f, y + 1.5f, z);

                string[] plane_pieces = lines[2].Split(' ');
                x = float.Parse(plane_pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(plane_pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(plane_pieces[3], CultureInfo.InvariantCulture.NumberFormat);
                plane_loc = new Vector3(x, y, z);

                for (int i = 3; i < lines.Length; i++) 
                {
                    string[] players = lines[i].Split(' ');
                    try 
                    {
                        PlayerInfo curr_player = pv.player_infos[(string) players[i][0]];
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
                        all_players.add(players[i][0], new PlayerInfo(players[i])); // add new player
                    }
                } 
            }
        }
    }
}
