using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System;
using System.IO;

public class AgentManager : MonoBehaviour
{
    public float speedMeanDefault = 1.5f;
    public float speedStdDefault = 0.3f;
    public float timeScale = 6f;
    public GameObject agentPrefab;


    [Header("Model")]

    [SerializeField]
    BaseModel baseModelType = new();
    enum BaseModel
    {
        SFM, SFMES, SFMNES, SFMCP, Moussaid, Custom
    };

    [SerializeField]
    AttentionModel attentionType = new();
    enum AttentionModel
    {
        None, Wang, Ours
    }

    public bool AddTP = false;
    public bool AddTV = false;
    public static bool enableGazingLocomotion = true;

    public static Dictionary<GameObject, Agent> agentsObjs = new Dictionary<GameObject, Agent>();
    private static List<Agent> agents = new List<Agent>();
    public static AgentManager instance;

    [Header("Database")]
    public static string pedDataSaveFolder = "sim-results";
    public float databaseLeftBound = -50.0f;
    public float databaseRightBound = 50.0f;

    [Header("Visualization")]
    public Material attractedMaterial;
    public Material normalMaterial;
    

    private static string modelName = "";
    private static float simulationTime = 0;
    public static int simulationFrame = 0;
    private static int databaseRefreshRate = 10;
    private int databasePedID = 0;
    private static List<Dictionary<string, object>> pedDatabase = new();

    // The Awake function is called on all objects in the Scene before any object's Start function is called.
    // This fact is useful in cases where object A's initialisation code needs to rely on object B's already being initialised;
    // B's initialisation should be done in Awake, while A's should be done in Start.
    void Awake()
    {
        instance = this;
        
        enableGazingLocomotion = ExpControl.current.gazing_locomotion;

        /*
         * SET SIMULATION SPEED
         * Since Moussaid Method is slow
         * the simulation will only be 2 times faster
         */
        Time.timeScale = ((baseModelType == BaseModel.Moussaid) & timeScale > 2f) ? 2f : timeScale;
    }

    /**
     * Frame-rate independent
     * 
     * MonoBehaviour.FixedUpdate has the frequency of the physics system; 
     * it is called every fixed frame-rate frame. 
     * Compute Physics system calculations after FixedUpdate. 
     * 0.02 seconds (50 calls per second) is the default time between calls. 
     * Use Time.fixedDeltaTime to access this value. 
    **/
    void FixedUpdate()
    {
        simulationTime += Time.deltaTime;
        simulationFrame++;
        var text = "time: " + simulationTime.ToString() 
            + "\nphysic fps: " + (simulationFrame / simulationTime).ToString(CultureInfo.InvariantCulture)
            + "\nCurrent config: " + ExpControl.current.name;
        UiManager.instance.ShowText(text);
        if(simulationFrame % databaseRefreshRate == 0)
        {
            UpdatePedDatabase();
        }
        // if simulation time is greater than total time, export the database and exit
        if (simulationTime > ExpControl.current.simulation_duration)
        {
            ExportPedData();

            // after the simulation is done, move to the next experiment
            // ExpControl.NextExperiment();

            // after the simulation is done, exit the application
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    public static void Reboot()
    {
        ClearAllAgents();
        ResetDatabase();
        enableGazingLocomotion = ExpControl.current.gazing_locomotion;
    }

    #region Agent Creation/Deletion/Lookup
    /// <summary>
    /// Create an agent
    /// </summary>
    public void addSFMAgent(string name, Vector3 origin, Vector3 destination, 
        string originName="", string destinationName="", float givenSpeed=0.0f)
    {
        // lift the agent so it will not be buried in the ground
        var randPos = origin + Vector3.up;

        // Initialize a new agent
        GameObject agent = Instantiate(agentPrefab, randPos, Quaternion.identity);
        agent.name = name;
        agent.transform.parent = transform;

        // Activate models for the new agent
        Agent agentScript;
        switch (baseModelType)
        {
            case BaseModel.SFMCP:
                agentScript = agent.AddComponent<SFMCP>();
                modelName = "SFM-CP";
                if (AddTP)
                {
                    agentScript.tiltPosition = true;
                    modelName = "SFM-CP-TP";
                }
                else if (AddTV)
                {
                    agentScript.tiltVelocity = true;
                    modelName = "SFM-CP-TV";
                }
                break;
            case BaseModel.Moussaid:
                agentScript = agent.AddComponent<Moussaid>();
                modelName = "Mou";
                break;
            case BaseModel.Custom:
                agentScript = agent.AddComponent<SFMCustom>();
                modelName = "Custom";
                break;
            case BaseModel.SFMES:
                agentScript = agent.AddComponent<SFMES>();
                modelName = "SFM-ES";
                if (AddTP)
                {
                    agentScript.tiltPosition = true;
                    modelName = "SFM-ES-TP";
                }
                break;
            case BaseModel.SFMNES:
                agentScript = agent.AddComponent<SFMNES>();
                modelName = "SFM-NES";
                if (AddTP)
                {
                    agentScript.tiltPosition = true;
                    modelName = "SFM-NES-TP";
                }
                else if (AddTV)
                {
                    agentScript.tiltVelocity = true;
                    modelName = "SFM-NES-TV";
                }
                break;
            case BaseModel.SFM:
                agentScript = agent.AddComponent<SFMAgent>();
                modelName = "SFM";
                break;
            default:
                agentScript = agent.AddComponent<SFMAgent>();
                modelName = "SFM";
                break;
        }
        agentScript.enabled = true;

        BaseAttn perceptron;
        switch (attentionType)
        {
            case AttentionModel.None:
                perceptron = agent.AddComponent<BaseAttn>();
                perceptron.enabled = true;
                perceptron.agentComponent = agentScript;
                agentScript.attentionComponent = perceptron;
                break;
            case AttentionModel.Wang:
                perceptron = agent.AddComponent<Wang>();
                perceptron.enabled = true;
                perceptron.agentComponent = agentScript;
                agentScript.attentionComponent = perceptron;
                break;
            case AttentionModel.Ours:
                perceptron = agent.AddComponent<OursAttn>();
                perceptron.enabled = true;
                perceptron.agentComponent = agentScript;
                agentScript.attentionComponent = perceptron;
                modelName += "-Ours";
                break;
            default:
                break;
        }


        // Set basic attributes for the agent
        agentScript.destination = destination;
        if (givenSpeed > 0.0f)
        {
            agentScript.desiredSpeed = givenSpeed;
        }
        else
        {
            agentScript.desiredSpeed = Utils.NextGaussian() * speedStdDefault + speedMeanDefault;
        }
        agentScript.databaseName = name;
        agentScript.originName = originName;
        agentScript.destinationName = destinationName;
        agentScript.databasePedID = databasePedID;
        agentScript.selfGameObject = agent;

        // Update PedID in the database
        databasePedID++;

        // Add agent to the lists
        agents.Add(agentScript);
        agentsObjs.Add(agent, agentScript);
    }

    public static bool IsAgent(GameObject obj)
    {
        return agentsObjs.ContainsKey(obj);
    }

    public static void RemoveAgent(GameObject agentObj)
    {
        agents.Remove(agentObj.GetComponent<Agent>());
        agentsObjs.Remove(agentObj);
        agentObj.GetComponent<Agent>().enabled = false;
        if (agentObj.GetComponent<BaseAttn>() != null) agentObj.GetComponent<BaseAttn>().enabled = false;
        agentObj.SetActive(false);
        Destroy(agentObj);
    }

    public static void ClearAllAgents()
    {
        foreach (var keyPair in agentsObjs)
        {
            var agentObj = keyPair.Key;
            var agentScript = keyPair.Value;
            Destroy(agentScript);
            Destroy(agentObj);
        }
        agentsObjs.Clear();
    }
    #endregion

    #region Database

    /// <summary>
    /// Record all active pedestrian status to the database
    /// </summary>
    /// <returns>Wait for a few seconds for the next update</returns>
    private void UpdatePedDatabase()
    {
        foreach (var keyPair in agentsObjs)
        {
            var agent = keyPair.Value;
            var x = agent.GetPosition()[0];

            if (x > databaseLeftBound & x < databaseRightBound) {
                Dictionary<string, object> p = new()
                {
                    { "ID",agent.databasePedID },
                    { "name", agent.databaseName },
                    { "origin", agent.originName },
                    { "destination", agent.destinationName },
                    { "time", simulationTime },
                    { "x", agent.GetPosition()[0] },
                    { "y", agent.GetPosition()[2] },
                    { "vx", agent.GetVelocity()[0] },
                    { "vy", agent.GetVelocity()[2] },
                    { "speed", agent.GetVelocity().magnitude },
                    { "desiredSpeed", agent.desiredSpeed },

                    { "neutralSpeed", agent.attentionComponent.neutralSpeed },
                    { "vision", agent.attentionComponent.vision },
                    { "dist_store", agent.attentionComponent.distToStore },
                    { "omega", agent.attentionComponent.omega },
                    { "cum_gaze", agent.attentionComponent.cumulativeGaze },
                    { "attractScore", agent.attentionComponent.maxAttentionScore },
                    { "gaze", agent.attentionComponent.isAttracted ? 1 : 0 },
                    { "angle", agent.attentionComponent.angleToStore },
                    { "small_angle", agent.attentionComponent.smallAngle },
                    { "big_angle", agent.attentionComponent.largeAngle },
                    { "separation", agent.attentionComponent.separationAngle }

                };
                pedDatabase.Add(p);
            }
        }
    }

    /// <summary>
    /// Export pedestrian trajectories to csv file
    /// </summary>
    public static void ExportPedData()
    {
        Utils.ListOfDictToCsv(pedDatabase, Directory.GetCurrentDirectory() + "/" + pedDataSaveFolder + "/" + modelName + "-" + ExpControl.current.name + ".csv");
    }

    public static void ResetDatabase()
    {
        pedDatabase.Clear();
        simulationFrame = 0;
        simulationTime = 0;
    }

    #endregion


}
