/*
 * Respond to packets from server machine.
 */

using System;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using UnityEngine;

/*
 * Packet sent to DSN Rain machine.
 */
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

public class UdpReceive : MonoBehaviour {

    Thread receiveThread;
    UdpClient client;
    GameObject sphere;
    public int port; // defined in init()
    public string lastReceivedUDPPacket = "";
    public string allReceivedUDPPackets = ""; // clean frequently
    public float x, y, z;

    /*
     * Start from shell.
     */
    private static void Main() {

        UdpReceive receiveObj = new UdpReceive();
        receiveObj.init();

        string text = "";
        do {
            text = Console.ReadLine();
        } while (!text.Equals("exit"));
    }
    
    /*
     * Start from Unity Engine.
     */
    public void Start() {

        init();
    }

    /*
     * Update object position at every time tick.
     */
    public void Update() {

        sphere.transform.position = new Vector3(x, y, z);
    }

    /*
     * GUI for Unity.
     */
    void OnGUI() {

        Rect rectObj = new Rect(40, 10, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "# UDPReceive\n127.0.0.1 " + port + " #\n"
                    + "shell> nc -u 127.0.0.1 : " + port + " \n"
                    + "\nLast Packet: \n" + lastReceivedUDPPacket
                    + "\n\nAll Messages: \n" + allReceivedUDPPackets
                    , style);
    }

    /*
     * Initialize virtual environment and thread.
     */
    private void init() {

        Debug.Log("UDPSend.init()");
        port = 8051; // change port here 

        // Status output
        Debug.Log("Sending to 127.0.0.1 : " + port);
        Debug.Log("Test-Sending to this Port: nc -u 127.0.0.1  " + port + "");

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
    private void ReceiveData() {

        client = new UdpClient(port); // port to listen on
        byte[] temp = Encoding.UTF8.GetBytes("OCULUS");
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("128.220.221.21"), 4578);
        client.Send(temp, temp.Length, remoteEndPoint); // first packet to override weird 0.0.0.0 IP address packet 
        client.Send(temp, temp.Length, remoteEndPoint);

        // continuously accept server data
        while (true) {
            try {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP); // blocks until data is received from somewhere 
                string text = Encoding.UTF8.GetString(data); // convert struct to string
                string[] pieces = text.Split(' '); // TODO: better encoding
                Debug.Log(">> " + text);
                lastReceivedUDPPacket = text;
                allReceivedUDPPackets = allReceivedUDPPackets + text;

                // Update object position asynchronously via Update thread 
                x = float.Parse(pieces[0], CultureInfo.InvariantCulture.NumberFormat);
                y = float.Parse(pieces[1], CultureInfo.InvariantCulture.NumberFormat);
                z = float.Parse(pieces[2], CultureInfo.InvariantCulture.NumberFormat);
            } catch (Exception err) {
                Debug.Log(err.ToString());
            }
        }
    }

    /*
     * Get the most recent UDP packet.
     * clears previously received packets.
     */ 
    public string getLatestUDPPacket() {

        allReceivedUDPPackets = "";
        return lastReceivedUDPPacket;
    }
}
