using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttractorManager : MonoBehaviour
{
    public GameObject AttractorRootObject;
    public static AttractorManager instance;
    public float givenAttractiveness = 6.0f;
    public static HashSet<GameObject> attractorContainer = new();
    public static Dictionary<GameObject, float> attractiveness = new();
    public static Dictionary<GameObject, Vector3> center = new();
    private static Dictionary<GameObject, Vector3> leftBound = new();
    private static Dictionary<GameObject, Vector3> rightBound = new();

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public void InitAttractors()
    {
        foreach (Transform attractor in AttractorRootObject.transform)
        {
            if (attractor.gameObject.activeSelf)
            {
                attractorContainer.Add(attractor.gameObject);
                attractiveness[attractor.gameObject] = givenAttractiveness;
                center[attractor.gameObject] = attractor.transform.Find("center").position;
                leftBound[attractor.gameObject] = attractor.transform.Find("LeftBound").position;
                rightBound[attractor.gameObject] = attractor.transform.Find("RightBound").position;
            }
            
        }
    }

    public static bool IsAttractor(GameObject collidedObject)
    {
        return attractorContainer.Contains(collidedObject);
    }

    public static float VisionCoverage(Vector3 agentPosition, GameObject attractor)
    {
        var diff1 = leftBound[attractor] - agentPosition;
        var diff2 = rightBound[attractor] - agentPosition;
        return Mathf.Acos(Vector3.Dot(diff1.normalized, diff2.normalized)); 
    }

    public static bool InVCA(Vector3 agentPosition, GameObject attractor)
    {
        return VisionCoverage(agentPosition, attractor) >= 0.29f;
    }

    public static (Vector3, Vector3) VisionCoverageAngles(Vector3 agentPosition, GameObject attractor)
    {
        var diff1 = leftBound[attractor] - agentPosition;
        var diff2 = rightBound[attractor] - agentPosition;
        return (diff1, diff2);
    }

    public static Vector3 StoreFrontVectorDiff(Vector3 agentPosition, GameObject attractor)
    {
        var frontCenter = (leftBound[attractor] + rightBound[attractor]) / 2;
        return frontCenter - agentPosition;
    }
}
