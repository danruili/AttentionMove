using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SimConfig
{
    // Attention Module
    public float init_theta_1;
    public float init_theta_2;
    public float init_theta_3;
    public float init_theta_4;
    public float init_theta_5;
    public float init_theta_6;
    public float end_theta_1;
    public float end_theta_2;
    public float end_theta_3;
    public float end_theta_4;
    public float end_theta_5;
    public float end_theta_6;

    // Locomotion-regulation Module
    public float mean_ideal_angular_speed;
    public float std_ideal_angular_speed;

    // SFM Module
    public float agent_force_weight;
    public float agent_force_discount;
    public float perception_range;
    public float wall_force_weight;
    public float wall_force_discount;
    public float asymmetry_factor;
    public float relaxation_time;
    public float body_collision_force_weight;
    public float max_collision_time; // tMax, for SFMCP
    public float velocity_bias; // for SFMCP-TV
    public float speed_cap;

    // Boundary conditions
    public float flow_rate_positive;
    public float flow_rate_negative;
    public float neutral_spd_func_weight;
    public float neutral_spd_func_intercept;
    public float desired_spd_std;
    public float factor_dist_to_wall;
    public float factor_flow_width;
    public float factor_max_density;
    public float factor_wrong_side;
    public float corridor_width;

    // simulation
    public float simulation_duration;
    public string name;
    public string comments;
    public bool gazing_locomotion;
}

public class ConfigList
{
    public List<string> list;
}


public class ExpControl: MonoBehaviour
{
    public static ExpControl instance;
    public static SimConfig current;

    private int currentConfigIndex;
    private string[] configFiles;

    void Awake()
    {
        // find all json files in folder at the root directory
        configFiles = Directory.GetFiles(Directory.GetCurrentDirectory()+"/sim-configs", "*.json", SearchOption.TopDirectoryOnly);

        // if the pedDataSaveFolder does not exist, create it
        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/" + AgentManager.pedDataSaveFolder))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + AgentManager.pedDataSaveFolder);
        }

        // load the first JSON file
        currentConfigIndex = -1;
        ReadNextConfig();

        // print the corridor width of the first SimConfig
        Debug.Log(current.corridor_width);

        // set instance
        instance = this;
    }

    public SimConfig ReadNextConfig()
    {
        currentConfigIndex++;
        if (currentConfigIndex >= configFiles.Length)
        {
            return null;
        }
        current = ReadConfigFromJSONFile(configFiles[currentConfigIndex]);

        // print the current config name
        Debug.Log(current.name);

        // search root directory in the project to see if there is a csv file where the name contains the current config name
        string[] files = Directory.GetFiles(
            Directory.GetCurrentDirectory() + "/" + AgentManager.pedDataSaveFolder,
            "*" + current.name + ".csv", 
            SearchOption.TopDirectoryOnly);
        if (files.Length > 0)
        {
            // if there is a csv file, read the next config
            return ReadNextConfig();
        }
        return current;
    }


    public static SimConfig ReadConfigFromJSONFile(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        SimConfig config = JsonUtility.FromJson<SimConfig>(jsonString);
        return config;
    }

    public static void NextExperiment()
    {
        // Clear all existing states
        AgentManager.Reboot();

        // Read next config
        SimConfig nextConfig = instance.ReadNextConfig();

        // Quit if there is no more config
        if (nextConfig == null)
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // Use the new config to set environment
        EnvManager.ConfigureEnv();
    }

}
