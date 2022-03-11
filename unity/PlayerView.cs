using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerView : MonoBehaviour
{
    public Dictionary<string, PlayerInfo> player_infos = new Dictionary<string, PlayerInfo>(); // players
    public Vector3 sphere_loc, plane_loc;
    public Config config;
    public int num_players = 0;

    private Dictionary<string, GameObject> player_heads = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> player_lefthands = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> player_righthands = new Dictionary<string, GameObject>();
    private Vector3 init_pos;
    private GameObject sphere, plane;
    private Dictionary<int, Color> colors = new Dictionary<int, Color>();
    private int player_idx = 0;
    private double max_latency = long.MinValue;
    private double latency_sum = 0;
    private int latency_count = 0;

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

        colors.Add(0, Color.magenta);
        colors.Add(1, Color.cyan);
        colors.Add(2, Color.yellow);
        colors.Add(3, Color.green);
        colors.Add(4, Color.grey);
        colors.Add(5, Color.blue);
        colors.Add(6, Color.red);
    }

    private string GetTimeStamp(DateTime time)
    {
        return time.ToString("yyyyMMddHHmmssffff");
    }

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
    }

    // Update is called once per frame
    public void Update()
    {
        sphere.transform.position = sphere_loc;
        plane.transform.position = plane_loc;

        Debug.Log("Current player name : " + config.player_name);
        Debug.Log("Player number :" + player_infos.Count);
        Debug.Log("Player sphere position: " + sphere_loc.ToString());
        //LogLatency();

        foreach (KeyValuePair<string, PlayerInfo> name_2_player_info in player_infos)
        {
            string player_name = name_2_player_info.Key;

            PlayerInfo player_info = name_2_player_info.Value;
            try
            {

                // old player
                if (player_heads.ContainsKey(player_name))
                {
                    // update head position and rotation
                    player_heads[player_name].transform.position = player_info.headset.position;
                    player_heads[player_name].transform.rotation = player_info.headset.rotation;
                    Debug.Log("Player " + player_name + "head pos:" + player_info.headset.position.ToString());

                    // update left hand position and rotation
                    player_lefthands[player_name].transform.position = player_info.left_hand.position;
                    player_lefthands[player_name].transform.rotation = player_info.left_hand.rotation;
                    Debug.Log("Player " + player_name + "lefthand pos:" + player_info.left_hand.position.ToString());

                    // update right hand position and rotation
                    player_righthands[player_name].transform.position = player_info.right_hand.position;
                    player_righthands[player_name].transform.rotation = player_info.right_hand.rotation;
                    Debug.Log("Player " + player_name + "righthand pos:" + player_info.left_hand.position.ToString());
                }
                else // new player
                {
                    // new head
                    player_heads.Add(player_name, GameObject.CreatePrimitive(PrimitiveType.Cube));
                    player_heads[player_name].transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    player_heads[player_name].transform.position = init_pos;
                    var head_renderer = player_heads[player_name].GetComponent<Renderer>();
                    head_renderer.material.SetColor("_Color", colors[player_idx % colors.Count]);

                    // new left hand
                    player_lefthands.Add(player_name, GameObject.CreatePrimitive(PrimitiveType.Sphere));
                    player_lefthands[player_name].transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    player_lefthands[player_name].transform.position = init_pos;
                    var left_renderer = player_lefthands[player_name].GetComponent<Renderer>();
                    left_renderer.material.SetColor("_Color", colors[player_idx % colors.Count]);

                    // new right hand
                    player_righthands.Add(player_name, GameObject.CreatePrimitive(PrimitiveType.Sphere));
                    player_righthands[player_name].transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    player_righthands[player_name].transform.position = init_pos;
                    var right_renderer = player_righthands[player_name].GetComponent<Renderer>();
                    right_renderer.material.SetColor("_Color", colors[player_idx % colors.Count]);

                    // change init values for new player
                    init_pos.x += 10;
                    init_pos.y += 0;
                    init_pos.z += 10;
                    player_idx += 1;
                }
            } catch (Exception e)
            {
                Debug.Log("Exception in player view: " + e);
            }
        }
    }
}
