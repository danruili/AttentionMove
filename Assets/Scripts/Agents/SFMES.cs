using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFMES : SFMAgent
{
    /**
    * Implementation of the Elliptical Specification 1 in:
    * 
    * Zanlungo, F., Ikeda, T. and Kanda, T. (2011) 
    * ¡®Social force model with explicit collision prediction¡¯, 
    * EPL (Europhysics Letters), 93(6), p. 68005. 
    * doi: 10.1209/0295-5075/93/68005.
    * 
    * Original Work:
    * A. Johansson, D. Helbing and P. K. Shukla (2007) 
    * Specification of the social force pedestrian model by evolutionary adjustment to video tracking data. 
    * Advances in Complex Systems 10, 271-288
    * 
    **/

    private const float tau = 0.53f;
    private new const float A = 0.11f; //9.2f
    private new const float B = 1.19f;//0.44f
    private new float K = ExpControl.current.body_collision_force_weight;

    private const float positionBias = 0.16f;

    public override Vector3 CalculateAgentForce()
    {
        var agentForce = Vector3.zero;
        var agentVelocity = GetVelocity();

        foreach (var n in perceivedNeighbors)
        {
            if (!AgentManager.IsAgent(n))
            {
                continue;
            }

            var neighbor = AgentManager.agentsObjs[n];
            Vector3 neighborVelocity = neighbor.GetVelocity();
            Vector3 velocityDiff = neighborVelocity - agentVelocity;

            Vector3 positionDiff = transform.position - neighbor.transform.position;
            float realDistance = -(radius + neighbor.radius) + positionDiff.magnitude;

            /**
             * Tilt-Position Method
             * 
             * Zanlungo, F., Ikeda, T., Kanda, T.
             * A Microscopic ¡°Social Norm¡± Model to Obtain Realistic Macroscopic Velocity and Density Pedestrian Distributions.
             * PLoS ONE. 7, e50720 (2012).
             * https://doi.org/10.1371/journal.pone.0050720.
             * 
             * Slightly modified from Zanlungo 2012
             * now avoid to the right and overtake from the left
             * 
             **/
            if (tiltPosition)
            {
                //if (UiManager.showAgentForce)
                //    Debug.DrawLine(transform.position, transform.position + positionDiff, Color.yellow);
                var cosineTheta = Vector3.Dot(neighborVelocity.normalized, agentVelocity.normalized);
                positionDiff = Quaternion.Euler(0, cosineTheta * positionBias / 3.14f * 180, 0) * positionDiff;
                //if (UiManager.showAgentForce)
                //    Debug.DrawLine(transform.position, transform.position + positionDiff, Color.green);
            }


            // predictive avoidance
            Vector3 yij = positionDiff - neighborVelocity * tau;
            float bij = Mathf.Sqrt(Mathf.Pow(positionDiff.magnitude + yij.magnitude,2)
                - Mathf.Pow((neighborVelocity * tau).magnitude, 2))/2;
            float term1 = (positionDiff.magnitude + yij.magnitude) / (4 * bij);
            Vector3 term2 = positionDiff.normalized + yij.normalized;
            var agentForceComponent = A * Mathf.Exp(-bij / B) * term1 * term2;


            /**
            * Add collision force as Moussaid
            * this part is not included in the original work
            * */
            Vector3 physicalComponent = K / mass * (realDistance < 0f ? 1 : 0) *
                (transform.position - neighbor.transform.position).normalized;
            agentForceComponent += physicalComponent;

            if (UiManager.instance.showAgentForce)
                Debug.DrawLine(transform.position, transform.position + agentForceComponent, Color.blue);

            agentForce += agentForceComponent;

        }

        // set y-component to zero
        // But such action may not be correct
        agentForce.y = 0;

        return agentForce;
    }
}
