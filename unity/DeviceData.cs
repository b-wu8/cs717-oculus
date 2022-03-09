/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */

using System;
using UnityEngine;

/*
 * Devices whose data is trackable.
 * Includes the headset and controllers.
 */
public class TrackableDevice 
{
    public Vector3 position;
    public Quaternion rotation;

    public TrackableDevice(Vector3 pos, Quaternion rot)
    {
        this.position.x = pos.x;
        this.position.y = pos.y;
        this.position.z = pos.z;
        this.rotation.x = rot.x;
        this.rotation.y = rot.y;
        this.rotation.z = rot.z;
        this.rotation.w = rot.w;
    }

    public TrackableDevice()
    {
        this.position.x = 0;
        this.position.y = 0;
        this.position.z = 0;
        this.rotation.x = 0;
        this.rotation.y = 0;
        this.rotation.z = 0;
        this.rotation.w = 0;
    }

    /*
     * Serializes metadata in string format.
     */
    public string to_string() 
    {
        string data = System.String.Empty;
        data += this.position.x + " " + 
                this.position.y + " " + 
                this.position.z + " ";
        data += this.rotation.x + " " + 
                this.rotation.y + " " + 
                this.rotation.z + " " + 
                this.rotation.w;
        return data;
    }

    /*
     * Creates a deep copy of the class.
     */
    public TrackableDevice deep_copy(TrackableDevice device) 
    {
        return new TrackableDevice(device.position, device.rotation);
    }
}

public class Headset : TrackableDevice
{
    public Headset(): base() {}
    public Headset(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }
}

public class LeftHandController : TrackableDevice
{
    public LeftHandController(): base() {}

    public LeftHandController(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }

}

public class RightHandController : TrackableDevice
{
    public RightHandController() : base() { }
    public RightHandController(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }
}

/* 
 * Each of the joysticks on the device controllers.
 */
public class Joystick {
    public Vector2 position;
    public Joystick()
    {
        this.position.x = 0;
        this.position.y = 0;
    }
    public Joystick(Vector2 pos)
    {
        this.position.x = pos.x;
        this.position.y = pos.y;
    }

    /*
     * Serialize joystick movement metadata as a string.
     */
    public string to_string() 
    {
        string data = System.String.Empty;
        data += this.position.x + " " + 
                this.position.y;
        return data;
    }

    /*
     * Creates a deep copy of the class.
     */
    public Joystick deep_copy(Joystick device) 
    {
        return new Joystick(device.position);
    }
}

public class LeftJoystick : Joystick
{
    public LeftJoystick() : base() {}
    public LeftJoystick(Vector2 pos) : base(pos)
    {
    }
}

public class RightJoystick : Joystick
{
    public RightJoystick() : base() {}
    public RightJoystick(Vector2 pos) : base(pos)
    {
    }
}

public class Timestamp
{
    public DateTime time;
    public Timestamp(DateTime t)
    {
        time = t;
    }
}