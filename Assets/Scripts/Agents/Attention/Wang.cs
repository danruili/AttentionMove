using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public class Wang : BaseAttn
{

    /**
     * Based on the work as below:
     * 
     * Wang, W.L., Lo, S.M., Liu, S.B., Kuang, H.
     * Microscopic modeling of pedestrian movement behavior: Interacting with visual attractors in the environment. 
     * Transportation Research Part C: Emerging Technologies. (2014). 
     * https://doi.org/10.1016/j.trc.2014.03.009.
     * 
    **/
    

    // Parameters in Wang 2014
    private const float distanceWeight = 0.5f;  // 0.5f in the original work of Wang.
    private const float visibilityWeight = 1.0f - distanceWeight;
    private const float demandThreshold = 1f; // fixed


    // Variables for implementations of Wang's model
    // The readonly modifier prevents the field from being replaced by a different instance of the reference type
    private readonly Dictionary<GameObject, float> preferenceScore = new();
    private readonly Dictionary<GameObject, float> distanceScore = new();
    private readonly Dictionary<GameObject, float> shortestDist = new();


    public override void InitParam()
    {
        // Initialize the preference  ~ N(1, 1) and truncated to [0,inf]
        foreach (var attractor in AttractorManager.attractorContainer)
        {
            //var p = Utils.SampleFromExpo(2f, 200, 50);
            var p = Utils.SampleFromGaussian(1, 3, 0f);
            preferenceScore[attractor] = p;
        }
    }

    public override bool Attracted(GameObject noticedAttractor, float maxAttentionScore)
    {
        return (noticedAttractor != null) & (maxAttentionScore > demandThreshold);
    }

    public override void UpdateScores(Vector3 agentPosition, Vector3 agentVelocity, GameObject attractor, Vector3[] raycastDirs)
    {
        // Distance to the attractor
        var distCollision = VisionField.UpdateVisionField(raycastDirs, agentPosition, attractor);
        shortestDist[attractor] = Mathf.Min(distCollision);

        // Visibility of the attractor ~ [0, 1]
        visionDict[attractor] = VisionField.VisibilityScore(distCollision);

        // Proximity to the attractor ~ [0, 1]
        distanceScore[attractor] = DistanceScore(shortestDist[attractor]);

        // Apply weights and preference
        finalScore[attractor] = preferenceScore[attractor] * AttractorManager.attractiveness[attractor] *
            (distanceScore[attractor] * distanceWeight + visionDict[attractor] * visibilityWeight);

        // Stochastic component
        finalScore[attractor] += Random.Range(0f, Mathf.Min(finalScore[attractor] / 2, 1 - finalScore[attractor]));
    }

    private float DistanceScore(float distance)
    {
        return 1 - (distance / VisionField.visionRange);
    }

}
