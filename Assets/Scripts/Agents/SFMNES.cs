using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFMNES : SFMAgent
{
    /**
    * 
    * Johansson, A., Helbing, D., Shukla, P.
    * Specification of the social force pedestrian model by evolutionary adjustment to video tracking data. (2008). 
    * https://doi.org/10.48550/ARXIV.0810.4587.
    * 
    * Original Work:
    * Shukla P. S., Applications of Evolutionary Computation, Lect. Notes Comput. Sci., 6024 (2010) 21.
    **/

    /**
     * Zanlungo, F., Ikeda, T. and Kanda, T. (2011) 
     * Social force model with explicit collision prediction, 
     * EPL (Europhysics Letters), 93(6), p. 68005. 
     * doi: 10.1209/0295-5075/93/68005.
    **/
    private const float tau = 1.78f;
    private new const float A = 1.33f;
    private new const float B = 0.34f;
    private new float K = ExpControl.current.body_collision_force_weight;

    // Zanlungo 2012: CP-TV model
    private const float velocityBias = -0.16f;

    // Zanlungo 2012: ES-TP model
    private const float positionBias = 0.16f;

    private const float lambdaAlpha = 0.29f; // 0.06

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

            /**
             * Tilt-Velocity Method
             * 
             * Zanlungo, F., Ikeda, T., Kanda, T.
             * A Microscopic ¡°Social Norm¡± Model to Obtain Realistic Macroscopic Velocity and Density Pedestrian Distributions.
             * PLoS ONE. 7, e50720 (2012).
             * https://doi.org/10.1371/journal.pone.0050720.
             **/
            if (tiltVelocity)
            {
                //if (UiManager.showAgentForce)
                //    Debug.DrawLine(transform.position, transform.position + neighborVelocity, Color.yellow);
                var cosineTheta = Vector3.Dot(agentVelocity.normalized, neighborVelocity.normalized);
                neighborVelocity = Quaternion.Euler(0, cosineTheta * velocityBias / 3.14f * 180, 0) * neighborVelocity;
                //if (UiManager.showAgentForce)
                //    Debug.DrawLine(transform.position, transform.position + neighborVelocity, Color.green);
            }
            

            Vector3 positionDiff = transform.position - neighbor.transform.position;
            Vector3 velocityDiff = neighborVelocity - agentVelocity;
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
            Vector3 yij = positionDiff - velocityDiff * tau;  // expected distance vector
            float bij = Mathf.Sqrt(Mathf.Pow(positionDiff.magnitude + yij.magnitude, 2)
                - Mathf.Pow((velocityDiff * tau).magnitude, 2)) / 2;
            float term1 = (positionDiff.magnitude + yij.magnitude) / (4 * bij);
            Vector3 term2 = positionDiff.normalized + yij.normalized;
            Vector3 interactionComponent = A * Mathf.Exp(-bij / B) * term1 * term2;

            /**
             * Direction-Aware Factor
             * 
             * Johansson, A., Helbing, D., Shukla, P.
             * Specification of the social force pedestrian model by evolutionary adjustment to video tracking data. (2008). 
             * https://doi.org/10.48550/ARXIV.0810.4587.
             * 
             **/
            var cosine = Vector3.Dot(agentVelocity.normalized, (neighbor.transform.position - transform.position).normalized);
            float directionFactor = lambdaAlpha + (1 - lambdaAlpha) * (1 + cosine) / 2;
            interactionComponent *= directionFactor;


            /**
            * Add collision force as Moussaid
            * this part is not included in the original work
            * */
            Vector3 physicalComponent = K / mass * (realDistance < 0f ? 1 : 0) * 
                (transform.position - neighbor.transform.position).normalized;

            agentForce += physicalComponent + interactionComponent;

            if (UiManager.instance.showAgentForce)
                Debug.DrawLine(transform.position, transform.position + (physicalComponent + interactionComponent)*10, Color.blue);

        }

        // set y-component to zero
        // But such action may not be correct
        agentForce.y = 0;

        return agentForce;
    }
}
