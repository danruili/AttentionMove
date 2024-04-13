using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnvManager : MonoBehaviour
{

    public CorridorWidth corridorWidth = new();
    public enum CorridorWidth
    {
        normal, wide, narrow, custom, fromFile
    };

    public StoreWidth storeWidth = new();
    public enum StoreWidth
    {
        normal, wide
    };

    public FlowRate flowRate = new();
    public enum FlowRate
    {
        observation, normal, high, custom, fromFile
    };

    public DisplayDepth displayDepth = new();
    public enum DisplayDepth
    {
        observation, normal, shallow
    };

    public float customCorridorWidth = 3.5f;
    public float customFlowRate = 4f;

    public SampleFromDistribution[] flows;
    public GameObject storeCenter;
    public GameObject store;
    public GameObject corridor;

    private const float wideCorridor = 8f;
    private const float normalCorridor = 5.4f;
    private const float narrowCorridor = 3.5f;
    private float corridorWidthValue = 1f;

    public static EnvManager instance;


    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        ConfigureEnv();
    }

    public static void ConfigureEnv()
    {
        if (instance != null)
        {
            instance.SetEnvironment();
            WallManager.instance.InitWallAndNavMesh();
            AttractorManager.instance.InitAttractors();
            for (var i = 0; i < instance.flows.Length; i++) instance.flows[i].Reboot();
        }
    }

    private void SetEnvironment()
    {
        for (var i = 0; i < flows.Length; i++) flows[i].enabled = true;
        switch (storeWidth)
        {
            case StoreWidth.normal:
                break;
            case StoreWidth.wide:
                store.transform.localScale = new Vector3(8.4f, 2f, 1f);
                break;
            default:
                break;
        }
        switch (corridorWidth)
        {
            case CorridorWidth.normal:
                break;
            case CorridorWidth.wide:
                corridorWidthValue = wideCorridor;
                corridor.transform.localScale = new Vector3(1f, 1f, corridorWidthValue / normalCorridor);
                store.transform.Translate(new Vector3(0, 0, corridorWidthValue / 2 - 0.02f - store.transform.position.z));
                for (var i = 0; i < flows.Length; i++) flows[i].corridorWidth = corridorWidthValue;
                break;
            case CorridorWidth.narrow:
                corridorWidthValue = narrowCorridor;
                corridor.transform.localScale = new Vector3(1f, 1f, corridorWidthValue / normalCorridor);
                store.transform.Translate(new Vector3(0, 0, corridorWidthValue / 2 - 0.02f - store.transform.position.z));
                for (var i = 0; i < flows.Length; i++) flows[i].corridorWidth = corridorWidthValue;
                break;
            case CorridorWidth.custom:
                corridorWidthValue = customCorridorWidth;
                corridor.transform.localScale = new Vector3(1f, 1f, corridorWidthValue / normalCorridor);
                store.transform.Translate(new Vector3(0, 0, corridorWidthValue / 2 - 0.02f - store.transform.position.z));
                for (var i = 0; i < flows.Length; i++) flows[i].corridorWidth = corridorWidthValue;
                break;
            case CorridorWidth.fromFile:
                corridorWidthValue = ExpControl.current.corridor_width / 100;
                corridor.transform.localScale = new Vector3(1f, 1f, corridorWidthValue / normalCorridor);
                store.transform.Translate(new Vector3(0, 0, corridorWidthValue / 2 - 0.02f - store.transform.position.z));
                for (var i = 0; i < flows.Length; i++) flows[i].corridorWidth = corridorWidthValue;
                break;
            default:
                break;
        }
        switch (displayDepth)
        {
            case DisplayDepth.observation:
                break;
            case DisplayDepth.normal:
                //storeCenter.transform.Translate(new Vector3(0 - storeCenter.transform.position.x / 2, 0, 0));
                break;
            case DisplayDepth.shallow:
                storeCenter.transform.Translate(new Vector3(0, 0, corridorWidthValue/2 - storeCenter.transform.position.z));
                break;

        }
        float interval;
        switch (flowRate)
        {
            case FlowRate.observation:
                break;
            case FlowRate.normal:
                /*
                 * 0.08 ppl/(s*m) in two directions
                 */
                interval = 1 / (0.08f * corridorWidthValue / 2);
                for (var i = 0; i < flows.Length; i++) flows[i].agentSpawnInterval = interval;
                break;
            case FlowRate.high:
                /*
                 * 0.5 ppl/(s*m) in two directions
                 */
                interval = 1 / (0.5f * corridorWidthValue / 2);
                for (var i = 0; i < flows.Length; i++) flows[i].agentSpawnInterval = interval;
                break;
            case FlowRate.custom:
                for (var i = 0; i < flows.Length; i++) flows[i].agentSpawnInterval = customFlowRate;
                break;
            case FlowRate.fromFile:
                flows[0].agentSpawnInterval = ExpControl.current.flow_rate_positive;
                flows[1].agentSpawnInterval = ExpControl.current.flow_rate_negative;
                break;
        }
    }

}
