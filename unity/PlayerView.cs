using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerView : MonoBehaviour
{
    public Transform main_camera_transform, left_controller_transform, right_controller_transform;
    public Dictionary<string, Avatar> avatars;
    public Config config;
    private GameObject sphere, plane, xtemp;
    public Vector3 sphere_loc, plane_loc;
    public int num_players;

    /*
    public Dictionary<string, PlayerInfo> player_infos = new Dictionary<string, PlayerInfo>(); // players
    public Vector3 sphere_loc, plane_loc;
    public Config config;
    public int num_players = 0;

    private Dictionary<string, GameObject> player_heads = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> player_lefthands = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> player_righthands = new Dictionary<string, GameObject>();
    private Vector3 init_pos;
    private GameObject sphere, plane, xtemp;
    private Dictionary<int, Color> colors = new Dictionary<int, Color>();
    private int player_idx = 0;
    private double max_latency = long.MinValue;
    private double latency_sum = 0;
    private int latency_count = 0;
    */

    // Start is called before the first frame update
    void Start()
    {
        // init game objects
        Debug.Log("Player view : Start");
        // sphere init
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(0f, 0f, 0f);        // init location
        var sphere_renderer = sphere.GetComponent<Renderer>();
        sphere_renderer.material.SetColor("_Color", Color.red);     // change color

        // plane init
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);    
        plane.transform.position = new Vector3(0f, 0f, 0f);         // init location
        var plane_renderer = plane.GetComponent<Renderer>();
        plane_renderer.material.SetColor("_Color", Color.white);    // change color

        // test_ball
        xtemp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        xtemp.transform.position = new Vector3(4f, 4f, 4f);        // init location
        var xtemp_renderer = xtemp.GetComponent<Renderer>();
        xtemp_renderer.material.SetColor("_Color", Color.green);     // change color

        avatars = new Dictionary<string, Avatar>();
        /*
        colors.Add(Color.magenta);
        colors.Add(Color.cyan);
        colors.Add(Color.yellow);
        colors.Add(Color.green);
        colors.Add(Color.grey);
        colors.Add(Color.blue);
        colors.Add(Color.red);
        */
    }

    private string GetTimeStamp(DateTime time)
    {
        return time.ToString("yyyyMMddHHmmssffff");
    }

    /*
    private void LogLatency()
    {

        if (player_infos.ContainsKey(config.player_name))
        {
            long old_timestamp = Int64.Parse(player_infos[config.player_name].timestamp);
            long current_timestamp = Int64.Parse(GetTimeStamp(DateTime.Now));
            Debug.Log("received timestamp: " + old_timestamp);
            double latency = (((double)(current_timestamp - old_timestamp)) / ((double)10));
            latency_sum += latency;
            latency_count += 1;
            Debug.Log("Latency in ms: " + latency);
            if(latency > max_latency)
            {
                max_latency = latency;
                Debug.Log("Max latency in ms: " + latency);
            }
            Debug.Log("Average latency in ms: " + (latency_sum/(double) latency_count));
        }
    }*/

    // Update is called once per frame
    public void Update()
    {
        
        sphere.transform.position = sphere_loc;
        plane.transform.position = plane_loc;
        
        foreach (KeyValuePair<string, Avatar> kv_avatar in avatars) {
            kv_avatar.Value.render();
        }

        if (avatars.ContainsKey(config.player_name)) {
            Avatar main_avatar = avatars[config.player_name];
            main_camera_transform.position = main_avatar.headset_controller.position + main_avatar.offset;
            left_controller_transform.position = main_avatar.left_controller.position + main_avatar.offset;
            right_controller_transform.position = main_avatar.right_controller.position + main_avatar.offset;
            // main_camera_transform.rotation = main_avatar.headset_controller.rotation;
        }
    }
}
