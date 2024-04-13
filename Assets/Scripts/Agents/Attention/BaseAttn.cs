using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public class BaseAttn : MonoBehaviour
{
    

    // Link to the locomotion part of the agent
    public Agent agentComponent;
    public float neutralSpeed;

    // Attention Status
    public bool isAttracted;
    public GameObject noticedAttractor;
    private bool isColored;

    // Parameters for speed model
    private readonly float angularSpeedMean = ExpControl.current.mean_ideal_angular_speed;
    private readonly float angularSpeedStd = ExpControl.current.std_ideal_angular_speed;
    private float angularSpeed;

    // Refresh Rate
    protected const float ATTENTION_REFRESH_INTERVAL = 0.2f;
    protected int refreshInterval;

    // Score
    public float maxAttentionScore = 0f;
    protected readonly Dictionary<GameObject, float> finalScore = new();

    // Metrics for each attractor
    // The readonly modifier prevents the field from being replaced by a different instance of the reference type
    public readonly Dictionary<GameObject, float> visionDict = new();
    protected readonly Dictionary<GameObject, float> angleDict = new();
    protected readonly Dictionary<GameObject, float> distDict = new();
    protected readonly Dictionary<GameObject, float> smallAngleDict = new();
    protected readonly Dictionary<GameObject, float> largeAngleDict = new();
    protected readonly Dictionary<GameObject, float> separationAngleDict = new();

    // metrics to be exported
    public float vision = 0f;
    public float angleToStore = 0f;
    public float distToStore = 0f;
    public float smallAngle = float.NaN;
    public float largeAngle = float.NaN;
    public float separationAngle = float.NaN;
    public bool inVCA = true;
    public float cumulativeGaze = 0f;
    public float omega = 0f;
    public int gazingNumber = 0;

    void Start()
    {
        // Record the initial desired speed
        neutralSpeed = agentComponent.desiredSpeed; 

        // Initialize the angular speed
        angularSpeed = Utils.NextGaussian() * angularSpeedStd + angularSpeedMean;

        // Calcualate refresh interval
        refreshInterval = (int)(1 / Time.fixedDeltaTime * ATTENTION_REFRESH_INTERVAL);

        InitParam();
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying & agentComponent != null & UiManager.displayAttention)
        {
            // Show score
            Handles.Label(agentComponent.GetPosition(), maxAttentionScore.ToString(CultureInfo.InvariantCulture));
        }
    }

    void FixedUpdate()
    {
        if (AgentManager.enableGazingLocomotion & isAttracted) UpdateDesiredSpeed();

        if (isAttracted) cumulativeGaze += Time.fixedDeltaTime;

        if (AgentManager.simulationFrame % refreshInterval == 0)
        {
            // Update the score and the metrics for each attractor
            QueryAttractors(
                agentComponent.GetPosition(), agentComponent.GetVelocity(), agentComponent.perceivedAttractors);


            // Find max score and its attractor
            maxAttentionScore = -1000f;
            noticedAttractor = null;
            foreach (var attractor in agentComponent.perceivedAttractors)
            {
                if (maxAttentionScore < finalScore[attractor])
                {
                    maxAttentionScore = finalScore[attractor];
                    noticedAttractor = attractor;
                }
            }

            // If the agent is motivated by an attractor
            isAttracted = Attracted(noticedAttractor, maxAttentionScore);
            if (isAttracted)
            {
                if (!isColored)
                {
                    agentComponent.selfGameObject.GetComponent<MeshRenderer>().material = AgentManager.instance.attractedMaterial;
                    isColored = true;
                }
            }
            // If the agent is no longer motivated by an attractor
            else
            {
                isColored = false;
                agentComponent.selfGameObject.GetComponent<MeshRenderer>().material = AgentManager.instance.normalMaterial;
                agentComponent.desiredSpeed = neutralSpeed;
            }

        }
    }

    public virtual void InitParam()
    {

    }

    public virtual bool Attracted(GameObject noticedAttractor, float maxAttentionScore)
    {
        return false;
    }

    public virtual void UpdateScores(Vector3 agentPosition, Vector3 agentVelocity, GameObject attractor, Vector3[] raycastDirs)
    {
        
    }

    public virtual void UpdateVisualInfo(Vector3 agentPosition, Vector3 agentVelocity, GameObject attractor, Vector3[] raycastDirs)
    {
        var (angle1, angle2) = AttractorManager.VisionCoverageAngles(agentPosition, attractor);
        var angleNum1 = Mathf.Acos(Vector3.Dot(agentVelocity.normalized, angle1.normalized));
        var angleNum2 = Mathf.Acos(Vector3.Dot(agentVelocity.normalized, angle2.normalized));
        smallAngleDict[attractor] = Mathf.Min(angleNum2, angleNum1);
        largeAngleDict[attractor] = Mathf.Max(angleNum1, angleNum2);
        separationAngleDict[attractor] = Mathf.Acos(Vector3.Dot(angle2.normalized, angle1.normalized));

        var positionDiff = AttractorManager.StoreFrontVectorDiff(agentPosition, attractor);
        angleDict[attractor] = Mathf.Acos(Vector3.Dot(agentVelocity.normalized, positionDiff.normalized));
        distDict[attractor] = positionDiff.magnitude;

        var distCollision = VisionField.UpdateVisionField(raycastDirs, agentPosition, attractor);
        visionDict[attractor] = VisionField.VisibilityScore(distCollision);
    }

    protected void QueryAttractors(Vector3 agentPosition, Vector3 agentVelocity, HashSet<GameObject> perceivedAttractors)
    {

        // Construct Vision Raycasts
        var raycastDirs = VisionField.ConstructVisionField(agentVelocity);

        // Get a score for each attractor
        foreach (var attractor in perceivedAttractors)
        {
            // Always calculate the visual info
            UpdateVisualInfo(agentPosition, agentVelocity, attractor, raycastDirs);

            // temp code to inspect variables
            var queriedObj = attractor;
            vision = visionDict[queriedObj];
            smallAngle = smallAngleDict[queriedObj];
            largeAngle = largeAngleDict[queriedObj];
            separationAngle = separationAngleDict[queriedObj];
            angleToStore = angleDict[queriedObj];
            distToStore = distDict[queriedObj];

            var distance = AttractorManager.center[attractor] - agentComponent.transform.position;
            var tangentV = agentVelocity * Mathf.Sin(Mathf.Acos(Vector3.Dot(agentVelocity.normalized, distance.normalized)));
            omega = tangentV.magnitude / distance.magnitude;

            // Count the number of gazing pedestrians in front of the agent
            //int gazingNum = 0;
            //foreach (var neighbour in agentComponent.perceivedNeighbors)
            //{
            //    var cosine = Vector3.Dot(agentVelocity.normalized, (neighbour.transform.position - agentComponent.GetPosition()).normalized);
            //    if ((cosine > -0.5f) & (neighbour.GetComponent<Agent>().attentionComponent.isAttracted) 
            //        & ((neighbour.transform.position - agentComponent.GetPosition()).magnitude > 0.1f))
            //    {
            //        gazingNum++;
            //    }
            //}
            //gazingNumber = gazingNum;

            // But the probability is calculated only when in VCA
            inVCA = AttractorManager.InVCA(agentPosition, attractor);
            if (! inVCA)
            {
                finalScore[attractor] = float.NegativeInfinity;
            }
            else
            {
                UpdateScores(agentPosition, agentVelocity, attractor, raycastDirs);
            }
        }
    }


    private void UpdateDesiredSpeed()
    {
        var distance = AttractorManager.center[noticedAttractor] - agentComponent.transform.position;
        var agentVelocity = agentComponent.GetVelocity();
        var tangentV = agentVelocity * Mathf.Sin(Mathf.Acos(Vector3.Dot(agentVelocity.normalized, distance.normalized)));
        var currentAngularSpeed = tangentV.magnitude / distance.magnitude;
        var factor = Mathf.Min(angularSpeed/currentAngularSpeed, 1);
        agentComponent.desiredSpeed = neutralSpeed * factor;
    }


}
