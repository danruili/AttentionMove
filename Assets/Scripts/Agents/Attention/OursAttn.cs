using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyLogParam
{
    public float[] means;
    public float[] stds;
    public float[] weights;
    public float intercept;

    public PolyLogParam(float[] meanArray, float[] stdArray, float[] weightArray, float interceptValue)
    {
        means = meanArray;
        stds = stdArray;
        weights = weightArray;
        intercept = interceptValue;
    }
}

public class OursAttn : BaseAttn
{
    private PolyLogParam starting;
    private PolyLogParam stopping;

    public override void InitParam()
    {
        starting = new PolyLogParam(
            meanArray: new float[] { 1.795f, 0.981f, 3.534f, 1.149f, 1.636f, },
            stdArray: new float[] { 0.559f, 0.432f, 1.873f, 1.005f, 0.691f, },
            weightArray: new float[] { 
                ExpControl.current.init_theta_1, 
                ExpControl.current.init_theta_2,
                ExpControl.current.init_theta_3,
                ExpControl.current.init_theta_4,
                ExpControl.current.init_theta_5,
            },
            interceptValue: ExpControl.current.init_theta_6
            );
        stopping = new PolyLogParam(
            meanArray: new float[] { 1.353f, 1.366f, 2.084f, 2.014f, 1.810f, },
            stdArray: new float[] { 0.503f, 0.383f, 1.469f, 1.053f, 0.775f, },
            weightArray: new float[] { 
                ExpControl.current.end_theta_1,
                ExpControl.current.end_theta_2,
                ExpControl.current.end_theta_3,
                ExpControl.current.end_theta_4,
                ExpControl.current.end_theta_5,
            },
            interceptValue: ExpControl.current.end_theta_6
            );
    }

    public override bool Attracted(GameObject noticedAttractor, float maxAttentionScore)
    {
        if (noticedAttractor == null) return false;
        else
        {
            return finalScore[noticedAttractor] > Random.Range(0f, 1f);
        }
    }

    public float[] GetStartInputs(GameObject attractor)
    {
        return new float[] { angleDict[attractor], separationAngleDict[attractor], angleDict[attractor] * angleDict[attractor],
            separationAngleDict[attractor] * separationAngleDict[attractor], separationAngleDict[attractor] * angleDict[attractor],
        };
    }
    public float[] GetStopInputs(GameObject attractor, float v)
    {
        return new float[] { angleDict[attractor], separationAngleDict[attractor], angleDict[attractor] * angleDict[attractor],
            separationAngleDict[attractor] * separationAngleDict[attractor], separationAngleDict[attractor] * angleDict[attractor],
        };
    }

    public override void UpdateScores(Vector3 agentPosition, Vector3 agentVelocity, GameObject attractor, Vector3[] raycastDirs)
    {
        if (!isAttracted)
        {
            finalScore[attractor] = Utils.PolyLogistic(GetStartInputs(attractor), starting);
        }
        else
        {
            finalScore[attractor] = Utils.PolyLogistic(GetStopInputs(attractor, agentVelocity.magnitude * 100), stopping);
        }
    }


}
