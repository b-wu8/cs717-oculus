/*
 * This is where all the device inputs getters(e.g. the headset and lefthand controller) are located.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;

/*
static class DeviceInput
{
    public const string RIGHT_PRIMARY_BUTTON = "RIGHT_PRIMARY_BUTTON";
    public const string RIGHT_SECOND_BUTTON = "RIGHT_SECOND_BUTTON";
    public const string LEFT_PRIMARY_BUTTON = "LEFT_PRIMARY_BUTTON";
    public const string LEFT_SECOND_BUTTON = "LEFT_SECOND_BUTTON";

    public const string RIGHT_CONTROLLER_POSITION = "RIGHT_CONTROLLER_POSITION";
    public const string RIGHT_CONTROLLER_ROTATION = "RIGHT_CONTROLLER_ROTATION";
    public const string LEFT_CONTROLLER_POSITION = "LEFT_CONTROLLER_POSITION";
    public const string LEFT_CONTROLLER_ROTATION = "LEFT_CONTROLLER_ROTATION";

    public const string HEADSET_POSITION = "HEADSET_POSITION";
    public const string HEADSET_ROTATION = "HEADSET_ROTATION";

    public const string LEFT_JOYSTICK_POSITION = "LEFT_JOYSTICK_POSITION";
    public const string RIGHT_JOYSTICK_POSITION = "RIGHT_JOYSTICK_POSITION";
}
*/

[System.Serializable]
public class DeviceInfoWatcher : MonoBehaviour
{
    // input devices
    public InputDevice head_device;
    public InputDevice right_controller;
    public InputDevice left_controller;
    bool head_connected = false, right_connected = false, left_connected = false;

    //public Dictionary<string, Func<bool>> boolean_inputs;

    void OnEnable()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach(InputDevice device in allDevices)
            InputDevices_deviceConnected(device);
        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;

        //boolean_inputs = new Dictionary<string, Func<bool>>();
        //boolean_inputs[DeviceInput.RIGHT_PRIMARY_BUTTON] =

    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) == InputDeviceCharacteristics.HeadMounted)
        {
            head_device = device;
            head_connected = true;
        } else
        {
            // Debug.Log("Head not found");
        }
        if ((device.characteristics & InputDeviceCharacteristics.Left) == InputDeviceCharacteristics.Left)
        {
            left_controller = device;
            left_connected = true;
        }
        else
        {
            // Debug.Log("left controller not found");
        }
        if ((device.characteristics & InputDeviceCharacteristics.Right) == InputDeviceCharacteristics.Right)
        {
            right_controller = device;
            right_connected = true;
        }
        else
        {
            // Debug.Log("right controller not found");
        }
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        // Debug.Log("Device disconnected: " + device);
        if(device == head_device)
        {
            head_connected = false;
        }
        else if (device == right_controller)
        {
            right_connected = false;
        }
        else if (device == left_controller)
        {
            left_connected = false;
        }
    }

    public string GetControllerData() {
        string data = System.String.Empty;
        data += GetHeadset().to_string() + " ";
        data += GetLeftHandController().to_string() + " ";
        data += GetRightHandController().to_string()+ " ";
        data += GetLeftJoystick().to_string() + " ";
        data += GetRightJoystick().to_string() + " ";
        data += DateTime.Now.ToString("yyyyMMddHHmmssffff");
        return data;
    }

    public LeftHandController GetLeftHandController()
    {
        if (!left_connected)
        {
            // Debug.Log("Err: Left hand controller not connected");
            return new LeftHandController();
        }
        // get left controller position
        Vector3 left_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.devicePosition, out left_controller_pos)) {
            // Debug.Log("Left controller pos: " + left_controller_pos.ToString("F2"));
        }
        // get left controller rotation
        Quaternion left_controller_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out left_controller_rotation)) {
            // Debug.Log("Left controller rot: " + left_controller_rotation.ToString("F2"));
        }
        return new LeftHandController(left_controller_pos, left_controller_rotation);
    }

    // get head position
    public Vector3 GetHeadPosition()
    {
        Vector3 headset_pos = new Vector3(0.0f, 0.0f, 0.0f);

        if (!head_connected)
            return headset_pos;

        head_device.TryGetFeatureValue(CommonUsages.centerEyePosition, out headset_pos);
        return headset_pos;
    }

    // get head rotation
    public Quaternion GetHeadRotation()
    {
        Quaternion headset_rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);

        if (!head_connected)
            return headset_rot;

        head_device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out headset_rot);
        return headset_rot;
    }

    // get left primary button
    public bool GetLeftControllerPrimaryButtonPushed()
    {
        bool new_state = false;

        if (!left_connected)
            return new_state;

        left_controller.TryGetFeatureValue(CommonUsages.primaryButton, out new_state);
        return new_state;
    }

    // get right primary button
    public bool GetRightControllerPrimaryButtonPushed()
    {
        bool state = false;

        if (!right_connected)
            return state;

        right_controller.TryGetFeatureValue(CommonUsages.primaryButton, out state);
        return state;
    }

    // get right secondary button
    public bool GetRightControllerSecondaryButtonPushed()
    {
        bool state = false;

        if (!right_connected)
            return state;

        right_controller.TryGetFeatureValue(CommonUsages.secondaryButton, out state);
        return state;
    }

    // get left secondary button
    public bool GetLeftControllerSecondaryButtonPushed()
    {
        bool state = false;
        if (!left_connected)
            return state;

        left_controller.TryGetFeatureValue(CommonUsages.secondaryButton, out state);
        return state;
    }

    // get right controller position
    public Vector3 GetRightControllerPosition()
    {
        Vector3 right_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);

        if (!right_connected)
            return right_controller_pos;

        right_controller.TryGetFeatureValue(CommonUsages.devicePosition, out right_controller_pos);

        return right_controller_pos;
    }

    // get left controller position
    public Vector3 GetLeftControllerPosition()
    {
        Vector3 left_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);

        if (!left_connected)
            return left_controller_pos;

        left_controller.TryGetFeatureValue(CommonUsages.devicePosition, out left_controller_pos);
        return left_controller_pos;
    }

    // get right controller rotation
    public Quaternion GetRightControllerRotation()
    {
        Quaternion right_rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);

        if (!right_connected)
            return right_rot;

        right_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out right_rot);
        return right_rot;
    }

    // get left controller rotation
    public Quaternion GetLeftControllerRotation()
    {
        Quaternion left_rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);

        if (!left_connected)
            return left_rot;

        left_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out left_rot);
        return left_rot;
    }

    // get right joystick
    public Vector2 GetRightJoystickVec2()
    {
        Vector2 right_joystick_vec2 = new Vector2(0.0f, 0.0f);

        if (!right_connected)
        {
            return right_joystick_vec2;
        }
        // get right controller position
        right_controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out right_joystick_vec2);
        return right_joystick_vec2;
    }

    // get left joystick
    public Vector2 GetLeftJoystickVec2()
    {
        Vector2 left_joystick_vec2 = new Vector2(0.0f, 0.0f);

        if (!left_connected)
            return left_joystick_vec2;

        right_controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out left_joystick_vec2);
        return left_joystick_vec2;
    }

    public RightHandController GetRightHandController()
    {
        if (!right_connected)
        {
            // Debug.Log("Err: Right hand controller not connected");
            return new RightHandController();
        }
        // get right controller position
        Vector3 right_controller_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.devicePosition, out right_controller_pos)) {
            // Debug.Log("Right controller pos: " + right_controller_pos.ToString("F2"));
        }
        // get right controller rotation
        Quaternion right_controller_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out right_controller_rotation)) {
            // Debug.Log("Right controller rot: " + right_controller_rotation.ToString("F2"));
        }
        return new RightHandController(right_controller_pos, right_controller_rotation);
    }

    public Headset GetHeadset() 
    {
        if (!head_connected)
        {
            // Debug.Log("Err: Headset not connected");
            return new Headset();
        }
        // get headset position
        Vector3 headset_pos = new Vector3(0.0f, 0.0f, 0.0f);
        if (head_device.TryGetFeatureValue(CommonUsages.centerEyePosition, out headset_pos)) {
            // Debug.Log("Headset pos: " + headset_pos.ToString("F2"));
        }
        // get headset rotation
        Quaternion head_device_rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        if (head_device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out head_device_rotation)) {
            // Debug.Log(head_device.name + " rot: " + head_device_rotation.ToString("F2"));
        }
        //if (head_device.TryGetFeatureValue(CommonUsages.deviceRotation, out head_device_rotation))
        //    // Debug.Log(head_device.name + " rot: " + head_device_rotation.ToString("F2"));
        return new Headset(headset_pos, head_device_rotation);
    }
    
    public RightJoystick GetRightJoystick()
    {
        if (!right_connected)
        {
            // Debug.Log("Err: Right hand controller not connected");
            return new RightJoystick();
        }
        // get right controller position
        Vector2 right_joystick_vec2 = new Vector2(0.0f, 0.0f);
        if (right_controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out right_joystick_vec2)) {
            // Debug.Log("Right controller Joystick: " + right_joystick_vec2.ToString("F2"));
        }
        return new RightJoystick(right_joystick_vec2);
    }

    public LeftJoystick GetLeftJoystick()
    {
        if (!left_connected)
        {
            // Debug.Log("Err: Left hand controller not connected");
            return new LeftJoystick();
        }
        // get left controller position
        Vector2 left_joystick_vec2 = new Vector2(0.0f, 0.0f);
        if (left_controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out left_joystick_vec2)) {
            // Debug.Log("Left controller Joystick: " + left_joystick_vec2.ToString("F2"));
        }
        return new LeftJoystick(left_joystick_vec2);
    }
}