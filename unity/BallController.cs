using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

public class BallController : MonoBehaviour
{
    public PrimaryButtonWatcher watcher;
    public Config config;
    public bool IsPressed = false; // used to display button state in the Unity Inspector window
    public Vector3 rotationAngle = new Vector3(45, 45, 45);
    public float rotationDuration = 0.25f; // seconds
    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotator;

    void Start()
    {
        watcher.primaryButtonPress.AddListener(onPrimaryButtonEvent);
        offRotation = this.transform.rotation;
        onRotation = Quaternion.Euler(rotationAngle) * offRotation;
    }

    public void onPrimaryButtonEvent(bool pressed)
    {
        IsPressed = pressed;
        if (rotator != null)
            StopCoroutine(rotator);
        if (pressed)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.ip_address), config.port);
            UdpClient client = new UdpClient();
            string text = "";
            byte[] data = Encoding.UTF8.GetBytes(text);
            client.Send(data, data.Length, remoteEndPoint);
            rotator = StartCoroutine(AnimateRotation(this.transform.rotation, onRotation));
        }
        else
            rotator = StartCoroutine(AnimateRotation(this.transform.rotation, offRotation));
    }

    private IEnumerator AnimateRotation(Quaternion fromRotation, Quaternion toRotation)
    {
        float t = 0;
        while (t < rotationDuration)
        {
            transform.rotation = Quaternion.Lerp(fromRotation, toRotation, t / rotationDuration);
            t += Time.deltaTime;
            yield return null;
        }
    }
}
