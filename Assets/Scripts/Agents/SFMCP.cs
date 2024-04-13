using UnityEngine;

/// <summary>
/// Collition-Prediction Model (Tilt-Velocity Condition)
/// </summary>
public class SFMCP : SFMAgent
{
    /**
     * Implementation of the Collition-Prediction Model (Tilt-Velocity Condition) in:
     * 
     * Zanlungo, F., Ikeda, T. and Kanda, T. (2011) 
     * ¡®Social force model with explicit collision prediction¡¯, 
     * EPL (Europhysics Letters), 93(6), p. 68005. 
     * doi: 10.1209/0295-5075/93/68005.
     * 
     * 
     * Zanlungo, F., Ikeda, T. and Kanda, T. (2012) 
     * ¡®A Microscopic ¡°Social Norm¡± Model to Obtain Realistic Macroscopic Velocity and Density Pedestrian Distributions¡¯, 
     * PLoS ONE. Edited by M. Perc, 7(12), p. e50720. 
     * doi: 10.1371/journal.pone.0050720.
     * 
    **/

    public override Vector3 CalculateAgentForce()
    {
        /**
         * General Idea:
         * 
         * each pedestrian will try to compute, 
         * for each approaching pedestrian in her environment, 
         * the time in the future at which their relative distance will be minimum, assuming straight motion.
         * 
         * **/
        var agentForce = Vector3.zero;
        var agentVelocity = GetVelocity();

        float minClosestTime = Mathf.Infinity;

        foreach (var n in perceivedNeighbors)
        {
            if (!AgentManager.IsAgent(n))
            {
                continue;
            }

            var neighbor = AgentManager.agentsObjs[n];
            Vector3 neighborVelocity = neighbor.GetVelocity();

            Vector3 positionDiff = neighbor.transform.position - transform.position;
            Vector3 velocityDiff = - neighborVelocity + agentVelocity;


            if (tiltVelocity)
            {
                /**
                 * Tilt-Velocity Method
                 * In Unity Quaternion.Euler, a positie rotation is a clockwise rotation
                 * https://gamedev.stackexchange.com/questions/87612/quaternion-rotation-clockwise-or-counter-clockwise
                **/
                var cosineTheta = Vector3.Dot(positionDiff, agentVelocity) / (positionDiff.magnitude * agentVelocity.magnitude);
                neighborVelocity = Quaternion.Euler(0, cosineTheta * ExpControl.current.velocity_bias / 3.14f * 180, 0) * neighborVelocity;
            }

            /**
             * The pedestrian will check for the minimum of these collision times, 
             * If the angle between di,j and vi,j,is smaller than ¦Ð/4 
             * we compute the time ti,j at which the pedestrians i and j will be closest,
             * If |¦Èi,j| >¦Ð/4, thenti,j = ¡Þ.
             **/
            float closestTime = Mathf.Infinity;
            if (Mathf.Abs(Vector3.Angle(positionDiff, velocityDiff)) < 45f)
            {
                closestTime = (Vector3.Project(positionDiff, velocityDiff).magnitude - radius * 2) / velocityDiff.magnitude;
                if (closestTime < minClosestTime)
                {
                    minClosestTime = closestTime;
                }
            }
        }

        if(minClosestTime != Mathf.Infinity)
        {
            minClosestTime = minClosestTime > ExpControl.current.max_collision_time ? ExpControl.current.max_collision_time : minClosestTime;
            minClosestTime = minClosestTime < Time.deltaTime ? Time.deltaTime : minClosestTime;

            /**
             * will predict the positions of herself 
             * and all the other pedestrians at that point in the future
             **/

            foreach (var n in perceivedNeighbors)
            {
                if (!AgentManager.IsAgent(n))
                {
                    continue;
                }

                /**
                 * Interaction forces will then be circular symmetric forces as those used in CS, 
                 * but based on this future situation
                 */
                var neighbor = AgentManager.agentsObjs[n];
                Vector3 neighborVelocity = neighbor.GetVelocity();
                Vector3 positionDiff = neighbor.transform.position - transform.position;
                Vector3 velocityDiff = neighborVelocity - agentVelocity;
                Vector3 expectedPositionDiff = positionDiff + velocityDiff * minClosestTime;

                Vector3 rotatedExpectation;
                // Tilt-Position Method
                if (tiltPosition)
                {
                    rotatedExpectation = Quaternion.Euler(0, -0.07f / 3.14f * 90, 0) * expectedPositionDiff;
                }
                else
                {
                    rotatedExpectation = Quaternion.Euler(0, 0, 0) * expectedPositionDiff;
                } 

                var agentForceComponent = ExpControl.current.agent_force_weight * (agentVelocity.magnitude / minClosestTime) *
                    Mathf.Exp(-(positionDiff.magnitude - radius * 2) / ExpControl.current.agent_force_discount) * ((-rotatedExpectation).normalized);


                /**
                 * Direction-Aware Factor
                 **/
                var cosine = Vector3.Dot(agentVelocity.normalized, (neighbor.transform.position - transform.position).normalized);
                float directionFactor = ExpControl.current.asymmetry_factor + (1 - ExpControl.current.asymmetry_factor) * (1 + cosine) / 2;
                agentForceComponent *= directionFactor;

                // Visualize the force
                if (UiManager.instance.showAgentForce)
                {
                    Debug.DrawLine(transform.position, transform.position + agentForceComponent, Color.blue);
                }

                agentForce += agentForceComponent;
            }
        }

        /**
        * Add collision force as Moussaid
        * this part is not included in the original work
        * */
        foreach (var n in perceivedNeighbors)
        {
            if (!AgentManager.IsAgent(n))
            {
                continue;
            }
            var neighbor = AgentManager.agentsObjs[n];
            var dir = (transform.position - neighbor.transform.position).normalized;
            var overlap = (radius + neighbor.radius) - Vector3.Distance(transform.position, n.transform.position);
            agentForce += ExpControl.current.body_collision_force_weight / mass * (overlap > 0f ? 1 : 0) * dir;
        }

        // set y-component to zero
        // But such action may not be correct
        agentForce.y = 0;

        return agentForce;
    }

    public override Vector3 CalculateWallForce()
    {
        var wallForce = Vector3.zero;

        Vector3 agentPosition = transform.position;
        Vector3 agentVelocity = GetVelocity();


        var query = QueryWalls(agentPosition);
        var closestPoints = query.Item1;
        var wallEdges = query.Item2;


        float minClosestTime = Mathf.Infinity;
        int minWall = 0;

        for (var i = 0; i < wallEdges.Count; i++)
        {
            var edge = wallEdges[i];
            var closestPoint = closestPoints[i];

            var agentToWallVec = closestPoint - agentPosition;
            agentToWallVec.y = 0;
            var agentVec = agentPosition - edge.Item1;
            var wallVec = edge.Item2 - edge.Item1;

            // Collision Forces
            var normal = agentVec - Vector3.Project(agentVec, wallVec);
            normal.Normalize();
            var overlap = -(agentToWallVec.magnitude - radius);
            wallForce += ExpControl.current.body_collision_force_weight / mass * (overlap > 0f ? 1 : 0) * normal;

            // Collision Time
            var closestTime = (agentToWallVec.magnitude - radius) / Vector3.Project(agentVelocity, agentToWallVec).magnitude;
            if (closestTime < minClosestTime)
            {
                minClosestTime = closestTime;
                minWall = i;
            }
        }

        if (minClosestTime != Mathf.Infinity)
        {
            minClosestTime = minClosestTime > ExpControl.current.max_collision_time ? ExpControl.current.max_collision_time : minClosestTime;
            minClosestTime = minClosestTime < Time.deltaTime ? Time.deltaTime : minClosestTime;

            var closestPoint = closestPoints[minWall];
            var agentToWallVec = closestPoint - agentPosition;
            var overlap = -(agentToWallVec.magnitude - radius);

            var wallForceComponent = ExpControl.current.wall_force_weight * (agentVelocity.magnitude / minClosestTime) *
                Mathf.Exp(overlap / ExpControl.current.wall_force_discount) * (-agentToWallVec).normalized;

            // Visualize the force
            if (UiManager.instance.showAgentForce)
            {
                Debug.DrawLine(transform.position, transform.position + wallForceComponent, Color.cyan);
                Debug.DrawLine(transform.position, transform.position + agentToWallVec, Color.white);
            }
            
            wallForce += wallForceComponent;
        }



        return wallForce;
    }
}
