using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class SampleFromDistribution : MonoBehaviour
{
    public float agentSpawnInterval = 10.0f;
    public GameObject goal;
    public bool flip = true;
    public float corridorWidth = 5.4f;

    private Vector3 destination;
    private const int sampleNum = 100;
    private float[] densityCDF;
    private float[] intervalCDF;

    void Start()
    {
        Reboot();
        StartCoroutine(Flow());
    }

    public void Reboot()
    {
        densityCDF = GenerateDensityCDF();
        intervalCDF = GenerateExpoCDF(agentSpawnInterval);
        destination = goal.transform.position;
    }

    IEnumerator Flow()
    {
        yield return null;

        int i = 0;

        for (int frames = 0; ; frames++)
        {
            // the lateral position in ratio, 0=leftest, 1=rightest
            var r = sampleFromCDF(densityCDF) / densityCDF.Count();

            // the lateral position in actual coord system, -2.7=leftest
            var randZ = flip ? (0.5f - r) * corridorWidth : (r - 0.5f) * corridorWidth;

            // Find a birth point on the Navmesh
            var randPos = new Vector3(0, 0, randZ) + transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randPos, out hit, 10, NavMesh.AllAreas);

            // Define the destination
            var randDestination = new Vector3(destination[0], destination[1], randZ);

            // Add the agent to the stage
            AgentManager.instance.addSFMAgent(this.name + "-" + i.ToString(), hit.position, 
                randDestination, this.name, goal.name, GetNeutralSpeed(randZ, corridorWidth));

            i++;
            yield return new WaitForSeconds(sampleFromCDF(intervalCDF));
        }
    }

    public static float sampleFromCDF(float[] CDF)
    {
        var x = Random.value;
        for(var i=0; i < CDF.Count(); i++)
        {
            if(CDF[i] > x)
            {
                return (float)i;
            }
        }
        return 1;
    }

    /// <summary>
    /// Sample a neutral speed where the mean is a function of the lateral position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="corridorWidth"></param>
    /// <returns></returns>
    public static float GetNeutralSpeed(float x, float corridorWidth)
    {
        float xInCentermeter = (x / corridorWidth * 5.4f) * 100;
        float a = ExpControl.current.neutral_spd_func_weight;
        float h = ExpControl.current.neutral_spd_func_intercept;
        float speedStd = ExpControl.current.desired_spd_std;
        return (a * xInCentermeter * xInCentermeter + h + Utils.NextGaussian() * speedStd) / 100;
    }

    public virtual float[] GenerateDensityCDF()
    {
        float a = ExpControl.current.factor_dist_to_wall;
        float b = ExpControl.current.factor_flow_width;
        float c = ExpControl.current.factor_max_density;
        float d = ExpControl.current.factor_wrong_side;
        const float L = 540; // the parameters above are fitted in a 5.4m wide corridor, so this is fixed

        float[] f = new float[sampleNum];
        float[] cdf = new float[sampleNum];
        float totalF = 0f;
        for (int i = 0; i < sampleNum; i++)
        {
            var x = (float)(L / sampleNum * i);
            var delta = Mathf.Abs(x - c * L);
            delta = delta < d * L ? x - c * L : d * L;
            var u = a / x + a / (L - x) + (delta / (b * L)) * delta / (b * L);
            var boltzman = Mathf.Exp(-u);
            f[i] = boltzman;
            totalF += boltzman;
        }
        for (int i = 0; i < sampleNum; i++)
        {
            f[i] /= totalF;
        }
        for (int i = 0; i < sampleNum; i++)
        {
            float sum = 0.000f;
            for (int j = 0; j < i; j++)
            {
                sum += f[j];
            }
            cdf[i] = sum;
        }
        return cdf;
    }

    public static float[] GenerateExpoCDF(float mean)
    {
        var lambda = 1/mean;
        float[] cdf = new float[sampleNum];
        for (int i = 0; i < sampleNum; i++)
        {
            cdf[i] = 1 - Mathf.Exp(-lambda * i);
        }
        return cdf;
    }
}
