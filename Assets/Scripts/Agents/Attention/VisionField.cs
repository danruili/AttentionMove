using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionField
{
    // Parameters in Wang 2014
    public const int angularResolution = 40;
    public const float visionRange = 10.0f;  // 10.0f in the original work of Wang.


    /// <summary>
    /// Construct Vision Field
    /// </summary>
    /// <param name="agentVelocity"></param>
    /// <param name="visionPhi"></param> 70.0f in the original work of Wang.
    /// <returns></returns>
    public static Vector3[] ConstructVisionField(Vector3 agentVelocity, float visionPhi = 70f)
    {
        // Construct Vision Raycasts
        var raycastStartingDir = Quaternion.Euler(0, -visionPhi, 0) * agentVelocity.normalized;
        var raycastDirs = new Vector3[angularResolution];
        var dirIncrement = visionPhi * 2 / angularResolution;
        for (int i = 0; i < angularResolution; i++)
        {
            raycastDirs[i] = Quaternion.Euler(0, dirIncrement * i, 0) * raycastStartingDir;
        }

        return raycastDirs;
    }


    public static float[] UpdateVisionField(Vector3[] raycastDirs, Vector3 agentPosition, GameObject attractor, bool drawVisionField=false)
    {
        // Distance to the attractor
        float[] distCollision = new float[angularResolution];
        for (int i = 0; i < angularResolution; i++)
        {
            bool hitResult = Physics.Raycast(agentPosition + Vector3.up / 2, raycastDirs[i],
                out RaycastHit hit, visionRange);
            Collider attractorMeshCollider = attractor.GetComponent<MeshCollider>();

            if (hitResult & hit.collider == attractorMeshCollider)
            {
                distCollision[i] = hit.distance;

                if (drawVisionField) Debug.DrawLine(agentPosition + Vector3.up / 2, hit.point, Color.magenta);
            }
            else
            {
                distCollision[i] = visionRange;
                if (drawVisionField) Debug.DrawLine(agentPosition + Vector3.up / 2, agentPosition + Vector3.up / 2+ raycastDirs[i], Color.blue);
            }
        }
        return distCollision;

    }

    public static float VisibilityScore(float[] distCollision)
    {
        float result = 0.0f;
        for (int i = 0; i < angularResolution; i++)
        {
            if (distCollision[i] < visionRange)
            {
                //result -= Mathf.Pow(i+1 - angularResolution/2, 2);
                result += 1f;
            }
        }
        return result / (angularResolution);
    }

}
