/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */
using System;

public struct Headset
{
    public Vector3 postion;
    public Quaternion rotation;
}

public struct LeftHandController
{
    public Vector3 postion;
    public Quaternion rotation;
}

public struct RightHandController
{
    public Vector3 postion;
    public Quaternion rotation;
}

public struct LeftJoystick
{
    public Vector2 position;
}

public struct RightJoystick;
{
    public Vector2 position;
}

public struct Timestamp
{
    public DateTime time;
}