using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.XR;

[System.Serializable]
public class SecondaryButtonEvent : UnityEvent<bool> { }

public class SecondaryButtonWatcher : MonoBehaviour
{
    public SecondaryButtonEvent secondaryButtonPress; // object accessed as a variable of Unity script (BallController's watcher)
    public Config config;
    private bool lastButtonState = false;
    public DeviceInfoWatcher deviceInfoWatcher;
    public GameObject debugConvas;
    private bool debugCanvasShown = false;

    private void Awake()
    {
        if (secondaryButtonPress == null)
        {
            secondaryButtonPress = new SecondaryButtonEvent();
        }
        secondaryButtonPress.AddListener(ShowOrHideLog);
        debugCanvasShown = debugConvas.activeSelf;
    }

    void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    private static string GetTimestamp(DateTime value)
    {
        return value.ToString("yyyy-MM-dd hh:mm:ss tt");
    }

    void Update()
    {
        // if right hand or left hand secondary button pushed
        bool tempState = deviceInfoWatcher.GetRightControllerSecondaryButtonPushed();
        tempState |= deviceInfoWatcher.GetLeftControllerSecondaryButtonPushed();
        if (tempState != lastButtonState) // Button state changed since last frame AND its pushed
        {
            secondaryButtonPress.Invoke(tempState);
            lastButtonState = tempState;
        }
    }

    void ShowOrHideLog(bool pushed)
    {
        if(pushed)
        {
            Debug.Log("Secondary button pushed: " + GetTimestamp(DateTime.Now));
            debugCanvasShown = !debugCanvasShown;
            debugConvas.SetActive(debugCanvasShown);
        } else
        {
            Debug.Log("Secondary button released: " + GetTimestamp(DateTime.Now));
        }
    }
}