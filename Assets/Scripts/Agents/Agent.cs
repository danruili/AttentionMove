using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEditor;
using System.Globalization;

/// <summary>
/// BaseClass for all agent model
/// </summary>
public class Agent : MonoBehaviour
{

    // Model configuration
    public bool tiltPosition = false;  // true: enable tilted position perception
    public bool tiltVelocity = false;

    // Fixed parameters on Operational Layer
    public float radius = 0.3f;
    public float mass = 80f;
    public readonly float perceptionRadius = ExpControl.current.perception_range;

    // Randomized parameters on Operational layer
    public float desiredSpeed;

    // Tactical Layer
    public Vector3 destination;
    private const int PATHFINDING_FRAME_SKIP = 25;

    // Unity
    public GameObject selfGameObject;
    protected List<Vector3> path; // store path
    private NavMeshAgent nma; // used for calculating path on NavMesh
    protected Rigidbody rb; // physical entity of the agent
    public HashSet<GameObject> perceivedNeighbors = new();
    protected HashSet<GameObject> adjacentWalls = new HashSet<GameObject>();
    public HashSet<GameObject> perceivedAttractors = new();

    // Perception Component
    public BaseAttn attentionComponent;

    // Database Attributes
    public string databaseName;
    public int databasePedID;
    public string originName = "";
    public string destinationName = "";

    public virtual void Start()
    {
        path = new List<Vector3>();
        nma = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        gameObject.transform.localScale = new Vector3(2 * radius, 1, 2 * radius);
        nma.radius = radius;
        rb.mass = mass;
        selfGameObject.transform.Find("SphereCollider").gameObject.GetComponent<SphereCollider>().radius = perceptionRadius;

        // A coroutine is a function that can suspend its execution (yield)
        // until the given YieldInstruction finishes.
        // will be executed in parralell with other codes
        SetCoroutine();
    }

    private void FixedUpdate()
    {
        // Update tactical layer
        UpdateTacticalLayer();
        
        // Correct the y coord of the agent
        // But it may slown down the performance
        NavMeshHit hit;
        NavMesh.SamplePosition(transform.position, out hit, 1.5f, NavMesh.AllAreas);
        transform.position = transform.position + new Vector3(0, hit.position.y + 1 - transform.position.y, 0);

        // Update operational layer
        UpdateOperationalLayer();

        #region Visualization in Debug Mode

        if (UiManager.instance.showPath)
        {
            if (path.Count > 0)
            {
                Debug.DrawLine(transform.position, path[0], Color.green);
            }
            //for (int i = 0; i < path.Count - 1; i++)
            //{
            //    Debug.DrawLine(path[i], path[i + 1], Color.yellow);
            //}
            //foreach (var neighbor in perceivedNeighbors)
            //{
            //Debug.DrawLine(transform.position, neighbor.transform.position, Color.blue);
            //}   
        } 
        #endregion
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Show velocity
            // Handles.Label(GetPosition(), GetVelocity().magnitude.ToString(CultureInfo.InvariantCulture));
            //foreach(var neighbour in perceivedNeighbors)
            //{
            //    Debug.DrawLine(GetPosition(), neighbour.transform.position, Color.green);
            //}
        }
    }

    public virtual void SetCoroutine()
    {
        StartCoroutine(UpdateDestination());
    }

    #region Tactical Layer
    public void UpdateTacticalLayer()
    {
        if (path.Count > 1 && Vector3.Distance(transform.position, path[0]) < 2f)
        {
            // If the agent gets close to the current goal, current goal will be replaced by the next goal
            path.RemoveAt(0);
        }
        else if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
        {
            // If the agent gets close to the current goal, and there is only one goal left
            // current goal will be replaced by the next goal
            path.RemoveAt(0);

            // If there are no goals left, the agent will be inactivated.
            if (path.Count == 0) AgentManager.RemoveAgent(gameObject);
        }
    }
    public IEnumerator UpdateDestination()
    {
        // yield return null;

        for (int frames = 0; ; frames++)
        {
            if (frames % PATHFINDING_FRAME_SKIP == 0)
            {
                // update destinations every 25 frames
                NavMeshHit hit;
                NavMesh.SamplePosition(destination, out hit, 10, NavMesh.AllAreas);
                ComputePath(hit.position);
            }
            yield return null;
        }
    }

    public void ComputePath(Vector3 destination)
    {
        nma.enabled = true;
        var nmPath = new NavMeshPath();
        nma.CalculatePath(destination, nmPath);
        path = nmPath.corners.Skip(1).ToList();
        //path = new List<Vector3>() { destination };
        //nma.SetDestination(destination);
        nma.enabled = false;
    }

    #endregion

    #region Basic func for Operational Layer
    public Vector3 GetVelocity()
    {
        try
        {
            return rb.velocity;
        }
        catch
        {
            return Vector3.zero;
        }
    }
    public Vector3 GetPosition()
    {
        try
        {
            return rb.position;
        }
        catch
        {
            return Vector3.zero;
        }
    }

    public virtual void UpdateOperationalLayer()
    {
        
    }

    #endregion

    #region Object perception

    public void OnTriggerEnter(Collider other)
    {
        if (AgentManager.IsAgent(other.gameObject))
        {
            perceivedNeighbors.Add(other.gameObject);
            
        }
        if (WallManager.IsWall(other.gameObject))
        {
            adjacentWalls.Add(other.gameObject);
        }
        if (AttractorManager.IsAttractor(other.gameObject))
        {
            perceivedAttractors.Add(other.gameObject);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (perceivedNeighbors.Contains(other.gameObject))
        {
            perceivedNeighbors.Remove(other.gameObject);
        }
        if (adjacentWalls.Contains(other.gameObject))
        {
            adjacentWalls.Remove(other.gameObject);
        }
        if (perceivedAttractors.Contains(other.gameObject))
        {
            perceivedAttractors.Remove(other.gameObject);
        }
    }

    public (List<Vector3>, List<(Vector3, Vector3)>) QueryWalls(Vector3 agentPosition)
    {
        // Temporarily stored wall meshes
        List<Vector3> closestPoints = new();
        List<(Vector3, Vector3)> edges = new();

        NavMeshHit Meshhit;
        NavMesh.SamplePosition(agentPosition, out Meshhit, 1.5f, NavMesh.AllAreas);
        var m = Meshhit.mask;
        if (m != 1)
        {
            return (closestPoints, edges);
        }


        foreach (var n in adjacentWalls)
        {
            var edge = WallManager.EdgeLookUp(n);
            var wallVec = edge.Item2 - edge.Item1;

            var agentVec = agentPosition - edge.Item1;
            agentVec.y = 0;

            // if agent locates at front side of the mesh and if wall is long enough
            if (Utils.angleBetweenVecsInRad(wallVec, agentVec) < 0 && wallVec.magnitude > 0.0f)
            {
                var closestPoint = n.GetComponent<MeshCollider>().ClosestPoint(agentPosition);

                // Bit shift the index of the layer (2) to get a bit mask
                int layerMask = 1 << 2;

                // This would cast rays only against colliders in layer 2.
                // But instead we want to collide against everything except layer 2. The ~ operator does this, it inverts a bitmask.
                layerMask = ~layerMask;

                /**
                 * Multiple raycasts are used here, because Raycast() is not so reliable: 
                 * if you Raycast() toward the edge of the mesh (which might be the closest point to the agent), 
                 * the collision will not be detected all the time.
                 * 
                 * So apart from the closest point, additional raycasts towards the wall mesh are called.
                 * */
                Vector3 q2Point = (edge.Item2 + edge.Item1) / 2;
                Vector3 q3Point = edge.Item1 * 1 / 4 + edge.Item2 * 3 / 4;
                bool hitResult1 = Physics.Raycast(agentPosition + Vector3.up, closestPoint - agentPosition, out RaycastHit hit, Mathf.Infinity, layerMask);
                bool hitResult2 = Physics.Raycast(agentPosition + Vector3.up, q2Point - agentPosition, out RaycastHit hit2, Mathf.Infinity, layerMask);
                bool hitResult3 = Physics.Raycast(agentPosition + Vector3.up, q3Point - agentPosition, out RaycastHit hit3, Mathf.Infinity, layerMask);

                if (hitResult1 | hitResult2 | hitResult3)
                {
                    Collider meshCollider = n.GetComponent<MeshCollider>();
                    if (hit.collider == meshCollider | hit2.collider == meshCollider | hit3.collider == meshCollider)
                    {
                        closestPoints.Add(closestPoint);
                        edges.Add(edge);
                    }
                }
            }
        }

        return (closestPoints, edges);
    }

    #endregion

}
