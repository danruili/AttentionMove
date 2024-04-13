using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Moussaid : Agent
{
    /**
    * Implementation of the model in:
    * 
    * Moussaid, M., Helbing, D. and Theraulaz, G. (2011) 
    * ¡®How simple rules determine pedestrian behavior and crowd disasters¡¯, 
    * Proceedings of the National Academy of Sciences of the United States of America, 
    * 108(17), pp. 6884¨C6888. 
    * doi: 10.1073/pnas.1016507108.
    * 
    **/
    public new const float perceptionRadius = 10.0f;

    private const int angularResolution = 50;
    private const float visionPhi = 75f;
    private const float visionRange = 10f;
    private const float tau = 0.5f;
    private const float K = 5000f;

    public override void UpdateOperationalLayer()
    {
        var velocityForce = CalculateVelocityForce();
        var agentCollisionForce = CalculateAgentCollisionForce();
        var wallCollisionForce = CalculateWallCollisionForce();

        if (UiManager.instance.showAgentForce)
        {
            Debug.DrawLine(transform.position, transform.position + wallCollisionForce, Color.cyan);
            //Debug.DrawLine(transform.position, transform.position + velocityForce, Color.yellow);
            Debug.DrawLine(transform.position, transform.position + agentCollisionForce, Color.blue);
        }
        var force = velocityForce + agentCollisionForce + wallCollisionForce;
        rb.AddForce(force, ForceMode.Acceleration);
    }

    private Vector3 CalculateVelocityForce()
    {
        if (path.Count == 0)
        {
            return Vector3.zero;
        }

        var agentPosition = transform.position;
        var agentVelocity = GetVelocity();
        var agentToGoal = path[0] - agentPosition;

        if (agentVelocity.magnitude == 0)
        {
            agentVelocity = new Vector3(Utils.NextGaussian(), 0, Utils.NextGaussian());
        }

        // Call Raycast
        var raycastStartingDir = Quaternion.Euler(0, -visionPhi, 0) * agentVelocity.normalized;
        var raycastDirs = new Vector3[angularResolution];
        var dirIncrement = visionPhi * 2 / angularResolution;
        for (int i = 0; i < angularResolution; i++)
        {
            raycastDirs[i] = Quaternion.Euler(0, dirIncrement * i, 0) * raycastStartingDir;
        }

        // Distance to first collision
        float[] distCollision = new float[angularResolution];

        for (int i = 0; i < angularResolution; i++)
        {
            bool hitResult = Physics.Raycast(agentPosition + Vector3.up/2, raycastDirs[i],
                out RaycastHit hit, visionRange);
            if (hitResult)
            {
                distCollision[i] = hit.distance - radius;
            }
            else
            {
                distCollision[i] = visionRange;
            }
        }

        for (int i = 1; i < angularResolution; i++)
        {
            Vector3 t = distCollision[i] * raycastDirs[i];
            Vector3 s = distCollision[i - 1] * raycastDirs[i - 1];
            Debug.DrawLine(agentPosition + s/5, agentPosition + t/5, Color.green);
        }

        // Utility
        float[] utility = new float[angularResolution];
        float minUtility = Mathf.Infinity;
        int minIndex = 0;
        Vector3 minDir = new Vector3(0, 0, 0);
        for (int i = 1; i < angularResolution; i++)
        {
            float angularDiff = Vector3.Angle(raycastDirs[i], agentToGoal);
            var newUtility = Mathf.Pow(visionRange, 2) + Mathf.Pow(distCollision[i], 2)
                - 2 * visionRange * distCollision[i] * Mathf.Cos(Mathf.Deg2Rad * angularDiff);
            utility[i] = newUtility;
            if (newUtility < minUtility)
            {
                minUtility = newUtility;
                minDir = raycastDirs[i];
                minIndex = i;
            }
        }

        for (int i = 1; i < angularResolution; i++)
        {
            Vector3 t = utility[i] * raycastDirs[i];
            Vector3 s = utility[i - 1] * raycastDirs[i - 1];
            //Debug.DrawLine(agentPosition + s/30, agentPosition + t/30, Color.red);
        }

        // velocity force
        var expectedSpeed = Mathf.Min(distCollision[minIndex] / tau, desiredSpeed);
        var expectedVelocity = minDir * expectedSpeed;
        return (expectedVelocity - agentVelocity) / tau;
    }

    private Vector3 CalculateAgentCollisionForce()
    {
        var agentForce = Vector3.zero;

        foreach (var n in perceivedNeighbors)
        {
            if (!AgentManager.IsAgent(n))
            {
                continue;
            }

            var neighbor = AgentManager.agentsObjs[n];
            var dir = (transform.position - neighbor.transform.position).normalized;
            var overlap = (radius + neighbor.radius) - Vector3.Distance(transform.position, n.transform.position);
            agentForce += K / mass * (overlap > 0f ? 1 : 0) * dir;
        }

        // set y-component to zero
        // But such action may not be correct
        agentForce.y = 0;

        return agentForce;
    }

    private Vector3 CalculateWallCollisionForce()
    {
        var wallForce = Vector3.zero;

        Vector3 agentPosition = transform.position;

        var query = QueryWalls(agentPosition);
        var closestPoints = query.Item1;
        var edges = query.Item2;


        for (int i = 0; i < closestPoints.Count; i++)
        {
            var closestPoint = closestPoints[i];
            var edge = edges[i];
            var wallVec = edge.Item2 - edge.Item1;
            var agentVec = agentPosition - edge.Item1;
            agentVec.y = 0;
            var agentToWallVec = closestPoint - agentPosition;
            agentToWallVec.y = 0;

            var normal = agentVec - Vector3.Project(agentVec, wallVec);
            normal.Normalize();

            var overlap = -(agentToWallVec.magnitude - radius); // 0.3f for NavMesh erosion

            wallForce += K / mass * (overlap > 0f ? 1 : 0) * normal;
        }


        return wallForce;
    }

    

}
