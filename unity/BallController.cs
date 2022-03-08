/*
 * Event listener for packets from server machine.
 * 
 * Uses PrimaryButtonWatcher.cs to detect when a primary button is pressed 
 * and rotates its parent GameObject. To use this class, add it to a visible 
 * GameObject and drag the PrimaryButtonWatcher reference to the watcher 
 * property in Unity.
 */

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class BallController : MonoBehaviour {

    public PrimaryButtonWatcher watcher;
    public bool IsPressed = false; // used to display button state in the Unity Inspector window
    public Vector3 rotationAngle = new Vector3(45, 45, 45);
    public float rotationDuration = 0.25f; // seconds
    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotator;

    /* 
     * Start from Unity Engine.
     */
    void Start() {

        watcher.primaryButtonPress.AddListener(onPrimaryButtonEvent);
        offRotation = this.transform.rotation;
        onRotation = Quaternion.Euler(rotationAngle) * offRotation;
    }

    /* 
     * Sends back a message to server following an event.
     */ 
    public void onPrimaryButtonEvent(bool pressed)
    {
        IsPressed = pressed;
        if (rotator != null) StopCoroutine(rotator);

        if (pressed) {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse("128.220.221.21"), 4577);
            UdpClient client = new UdpClient();
            string text = "";
            byte[] data = Encoding.UTF8.GetBytes(text);
            client.Send(data, data.Length, remoteEndPoint);
            rotator = StartCoroutine(AnimateRotation(this.transform.rotation, onRotation));
        } else {
            rotator = StartCoroutine(AnimateRotation(this.transform.rotation, offRotation));
        }
    }

    /*
     * Animate object change in position. 
     */
    private IEnumerator AnimateRotation(Quaternion fromRotation, Quaternion toRotation) {

        float t = 0;
        while (t < rotationDuration) {
            transform.rotation = Quaternion.Lerp(fromRotation, toRotation, t / rotationDuration);
            t += Time.deltaTime;
            yield return null;
        }
    }
}
