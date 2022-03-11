/* 
 * Declares the classes used in identifying data 
 * about a player using an Oculus device.
 */

using System;
using UnityEngine;

/*
 * Types to classify what kind of message is being sent.
 */
static class Constants
{
    // startup message indicates that a player is trying to connect to room
    // the player name is mapped to the included IP address
    public const int SYN = 1;

    // controller input message
    public const int INPUT = 2;
    
    // player formally quits
    public const int FIN = 3;
}
public class PlayerInfo 
{
    public string name;
    public Headset headset;
    public LeftHandController left_hand;
    public RightHandController right_hand;
    public string timestamp;
    public bool is_new_timestemp = false;
    
    //string constructor
    public PlayerInfo(string name, Headset headset, LeftHandController left_hand, RightHandController right_hand)
    {
        this.name = name;
        this.headset = new Headset(headset);
        this.left_hand = new LeftHandController(left_hand);
        this.right_hand = new RightHandController(right_hand);
        //TODO: add joysticks
    }

    // copy contructor
    public PlayerInfo(PlayerInfo other_player_info)
    {
        this.name = string.Copy(other_player_info.name);
        this.headset = new Headset(other_player_info.headset);
        this.left_hand = new LeftHandController(other_player_info.left_hand);
        this.right_hand = new RightHandController(other_player_info.right_hand);
        this.is_new_timestemp = other_player_info.is_new_timestemp;
        //TODO: add joysticks
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
        this.is_new_timestemp = true;
        // TODO: parse joystick
    }

    public string to_string()
    {
        string data = System.String.Empty;
        data += this.headset.to_string() + " ";
        data += this.left_hand.to_string() + " ";
        data += this.right_hand.to_string() + " ";
        data += this.timestamp;

        return data;
    }
}

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

    // copy constructor
    public TrackableDevice(TrackableDevice other)
    {
        this.position.x = other.position.x;
        this.position.y = other.position.y;
        this.position.z = other.position.z;
        this.rotation.x = other.rotation.x;
        this.rotation.y = other.rotation.y;
        this.rotation.z = other.rotation.z;
        this.rotation.w = other.rotation.w;
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
    public static TrackableDevice deep_copy(TrackableDevice device) 
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

    //copy constructor
    public Headset(Headset other): base(other)
    {

    }
}

public class LeftHandController : TrackableDevice
{
    public LeftHandController(): base() {}

    public LeftHandController(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }

    // copy contructor
    public LeftHandController(LeftHandController other) : base(other)
    {
    }

}

public class RightHandController : TrackableDevice
{
    public RightHandController() : base() { }
    public RightHandController(Vector3 pos, Quaternion rot) : base(pos, rot)
    {
    }
    // copy contructor
    public RightHandController(RightHandController other) : base(other)
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

