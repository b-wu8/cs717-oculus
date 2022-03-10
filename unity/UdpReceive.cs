/*
 
    -----------------------
    UDP-Receive (send to)
    -----------------------
    // [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]
   
   
    // > receive
    // 127.0.0.1 : 8051
   
    // send
    // nc -u 127.0.0.1 8051
 
*/
using UnityEngine;
using Unity;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections.Generic;

/*
[StructLayout(LayoutKind.Sequential, Pack=1)]
public unsafe struct RainPacket {
    public int flag;
    public double x;
    public double y;
    public double z;
    public fixed byte mess[256];
};
*/

public class PlayerInfo 
{
    public string name;
    public Headset headset;
    public LeftHandController left_hand;
    public RightHandController right_hand;
    public string timestamp;
    
    //string constructor
    public PlayerInfo(string name, Headset headset, LeftHandController left_hand, RightHandController right_hand)
    {
        this.name = name;
        this.headset = (Headset) Headset.deep_copy(headset);
        this.left_hand = (LeftHandController) LeftHandController.deep_copy(left_hand);
        this.right_hand = (RightHandController) RightHandController.deep_copy(right_hand);
    }

    Vector3 StringToVec3(string str_x, string str_y, string str_z){
        return new Vector3(float.Parse(str_x), float.Parse(str_y), float.Parse(str_z));
    }

    Vector2 StringToVec2(string str_x, string str_y){
        return new Vector2(float.Parse(str_x), float.Parse(str_y));
    }

    Quaternion StringToQuat(string str_x, string str_y, string str_z, string str_w){
        return new Quaternion(float.Parse(str_x), float.Parse(str_y), float.Parse(str_z), float.Parse(str_w));
    }

    public PlayerInfo(string data)
    {
        string[] infos = data.Split(' ');
        this.name = infos[0];
        this.headset = new Headset(StringToVec3(infos[1], infos[2], infos[3]), StringToQuat(infos[4], infos[5], infos[6], infos[7]));
        this.left_hand = new LeftHandController(StringToVec3(infos[8], infos[9], infos[10]), StringToQuat(infos[11], infos[12], infos[13], infos[14]));
        this.right_hand = new RightHandController(StringToVec3(infos[15], infos[16], infos[17]), StringToQuat(infos[18], infos[19], infos[20], infos[21]));
        this.timestamp = infos[22];
        
    }
}

public class UdpReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    GameObject sphere;
    public Config config;
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = ""; // clean up this from time to time!
    public float x, y, z;
    List<PlayerInfo> all_player_info = new List<PlayerInfo>();

    // start from shell
    private static void Main()
    {
        UdpReceive receiveObj = new UdpReceive();
        receiveObj.init();

        string text = "";
        do
        {
            text = Console.ReadLine();
        }
        while (!text.Equals("exit"));
    }
    
    /*
     * Start from Unity Engine.
     */
    public void Start()
    {
        init();
    }

    /*
     * Update object position at every time tick.
     */
    public void Update()
    {
        sphere.transform.position = new Vector3(x, y, z);
    }

    void OnGUI()
    {
        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "# UDPReceive\n127.0.0.1 " + config.local_port + " #\n"
                    + "shell> nc -u 127.0.0.1 : " + config.local_port + " \n"
                    + "\nLast Packet: \n" + lastReceivedUDPPacket
                    + "\n\nAll Messages: \n" + allReceivedUDPPackets
                , style);
    }

    // init
    private void init()
    {
        Debug.Log("UDPSend.init()");

        // status
        Debug.Log("Sending to "+ config.remote_ip_address  +": " + config.remote_port);
        //Debug.Log("Test-Sending to this Port: nc -u 127.0.0.1  " + port + "");

        // Create red sphere object
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(1f, 1.5f, 1f);
        var sphere_renderer = sphere.GetComponent<Renderer>();
        sphere_renderer.material.SetColor("_Color", Color.red);

        // Run ReceiveData() in the background 
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    /*
     * Receive data from background thread.
     * Updates class variables that are used when Update() is called.  
     */
    private void ReceiveData()
    {

        client = new UdpClient(config.local_port); // port to listen on

        byte[] temp = Encoding.UTF8.GetBytes("OCULUS melody test");
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
        client.Send(temp, temp.Length, remoteEndPoint); // first packet to override weird 0.0.0.0 IP address packet 
        client.Send(temp, temp.Length, remoteEndPoint);

        while (true)
        {
            try
            { 
                IPEndPoint remoteIP = new IPEndPoint(IPAddress.Parse(config.remote_ip_address), config.remote_port);
                byte[] data = client.Receive(ref remoteIP); // blocks until data is received from somewhere 
                string text = Encoding.UTF8.GetString(data); // convert struct to string

                string[] lines = text.Split('\n'); // TODO: better encoding
                // 0 is lobby and # players
                // 1 is object
                // 2 is plane
                for (int i = 3; i < lines.Length; i++) 
                {
                    string[] players = lines[i].Split(' ');
                    if (all_player_info.Exists(x => x.name.Equals(players[i])))
                    {
                        continue; // player already added
                    } 
                    else
                    {
                        PlayerInfo new_player = new PlayerInfo(players[i]);
                    }
                } 

                Debug.Log(">> " + text);
                lastReceivedUDPPacket = text;
                allReceivedUDPPackets = allReceivedUDPPackets + text;

                /* Update object position asynchronously via Update thread*/
                /*
                x = float.Parse(pieces[0], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(pieces[2], CultureInfo.InvariantCulture.NumberFormat);
                Debug.Log("pos rcvd:" + "X("+x+") Y("+y+") Z("+z+")");
                */
            }
            catch (Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }


    /*
     * Get the most recent UDP packet.
     * clears previously received packets.
     */ 
    public string getLatestUDPPacket()
    {
        allReceivedUDPPackets = "";
        return lastReceivedUDPPacket;
    }
}
