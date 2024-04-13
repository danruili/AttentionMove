using System.Collections;
using UnityEngine;


public class SFMAgent : Agent
{
    // Helbing 2000
    public float T = ExpControl.current.relaxation_time;

    public const float A = 2000f;
    public const float B = 0.08f;
    public const float K = 1.2f * 100000f;
    public const float KAPPA = 2.4f * 100000f;

    public const float WALL_A = 2000f;
    public const float WALL_B = 0.08f;
    public const float WALL_K = 1.2f * 100000f;
    public const float WALL_KAPPA = 2.4f * 100000f;

    public float SPEEDCAP = ExpControl.current.speed_cap;

    // Randomized parameters on operational layer
    private Vector3 noise_force = new Vector3(0f, 0f, 0f);
    private const float NOISE_REFRESH_INTERVAL = 0.9f;
    public float wallForceWeight = 1f;

    #region Public Functions
    public override void Start()
    {
        base.Start();
        wallForceWeight = Random.Range(0f, 1f);
    }


    public override void SetCoroutine()
    {
        StartCoroutine(UpdateDestination());
        StartCoroutine(UpdateNoise());
    }

    IEnumerator UpdateNoise()
    {
        yield return null;

        for (int frames = 0; ; frames++)
        {
            noise_force = new Vector3(Utils.NextGaussian(), 0, Utils.NextGaussian()) * 10 / mass;
            yield return new WaitForSeconds(NOISE_REFRESH_INTERVAL);
        }
    }

    public override void UpdateOperationalLayer()
    {
        var force = ComputeForce();

        // Clamp the velocity
        var currentV = GetVelocity();
        var proposedV = currentV + force * Time.deltaTime;
        var offsetV = Vector3.ClampMagnitude(proposedV, SPEEDCAP) - currentV;
        force = offsetV / Time.deltaTime;

        rb.AddForce(force, ForceMode.Acceleration);
    }


    #endregion

    #region Private Functions

    private Vector3 ComputeForce()
    {
        var wall_force = CalculateWallForce();
        var ped_force = CalculateAgentForce();
        var goal_force = CalculateGoalForce();

        var force = goal_force + ped_force + wall_force * wallForceWeight + noise_force;

        // Visualize the force
        if (UiManager.instance.showAgentForce)
        {
            Debug.DrawLine(transform.position, transform.position + noise_force, Color.magenta);
            Debug.DrawLine(transform.position, transform.position + goal_force, Color.yellow);
        }

        return force;
    }
    
    private Vector3 CalculateGoalForce()
    {
        if (path.Count == 0)
        {
            return Vector3.zero;
        }

        // goal is on the ground, agent position is above the groud
        // so Y coord needs +1
        var temp = path[0] - transform.position + new Vector3(0f, 1, 0f);
        var desiredVel = temp.normalized * desiredSpeed;
        var actualVelocity = rb.velocity;

        //Debug.DrawLine(transform.position, transform.position + actualVelocity, Color.magenta);
        Debug.DrawLine(transform.position, transform.position + desiredVel, Color.red);

        return (desiredVel - actualVelocity) / T;
    }

    public virtual Vector3 CalculateAgentForce()
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
            var neighborVelocity = neighbor.GetVelocity();

            Vector3 positionDiff = neighbor.transform.position - transform.position;
            var dir = (-positionDiff).normalized;
            var overlap = (radius + neighbor.radius) - positionDiff.magnitude;

            var agentForceComponent = A * Mathf.Exp(overlap / B) * dir;
            agentForceComponent += K * (overlap > 0f ? 1 : 0) * dir;

            var tangent = Vector3.Cross(Vector3.up, dir);
            agentForceComponent += KAPPA * (overlap > 0f ? overlap : 0) * 
                Vector3.Dot(agentVelocity - neighborVelocity, tangent) * tangent;

            if (UiManager.instance.showAgentForce)
                Debug.DrawLine(transform.position, transform.position + agentForceComponent / mass, Color.blue);

            agentForce += agentForceComponent;
        }

        // set y-component to zero
        // But such action may not be correct
        agentForce.y = 0;

        return agentForce / mass;
    }

    public virtual Vector3 CalculateWallForce()
    {
        var wallForce = Vector3.zero;

        Vector3 agentPosition = transform.position;
        var query = QueryWalls(agentPosition);
        var closestPoints = query.Item1;
        var edges = query.Item2;

        for (int i = 0; i < closestPoints.Count; i++)
        {
            /**
            * Formula in Helbing 2000:
            * d_iw means the distance to wall W, 
            * n_iw denotes the direction perpendicular to it, 
            * and tiW the direction tangential to it
            * function g(x) is zero if the pedestrians do not touch each other (ri - d_iw>0)
            * 
            * f = {A*exp[(r_i - d_iw)/B] + k * g * (r_i-d_iw)} * n_iw - kappa * g * (r_i - d_iw)(v_i * t_iw)t_iw
            * overlap = r_i - d_iw
            **/

            var agentToWallVec = closestPoints[i] - agentPosition;
            agentToWallVec.y = 0;
            var overlap = -(agentToWallVec.magnitude - radius);

            var edge = edges[i];
            var wallVec = edge.Item2 - edge.Item1;
            var agentVec = agentPosition - edge.Item1;
            agentVec.y = 0;
            var normal = agentVec - Vector3.Project(agentVec, wallVec);
            normal.Normalize();
            var tangent = Vector3.Cross(Vector3.up, normal);

            var wallForceComponent = WALL_A * Mathf.Exp(overlap / WALL_B) * normal;
            wallForceComponent += WALL_K * (overlap > 0f ? 1 : 0) * normal;
            wallForceComponent += WALL_KAPPA * (overlap > 0f ? overlap : 0) * Vector3.Dot(rb.velocity, tangent) * tangent;

            wallForce += wallForceComponent;

            if (UiManager.instance.showAgentForce)
            {
                Debug.DrawLine(transform.position, transform.position + wallForceComponent / mass, Color.cyan);
                //Debug.DrawLine(transform.position, transform.position + agentToWallVec, Color.white);
            }

        }
        return wallForce / mass;
    }


    
    #endregion
}
