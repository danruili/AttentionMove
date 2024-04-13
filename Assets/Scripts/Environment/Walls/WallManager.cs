using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WallManager : MonoBehaviour
{
    public static WallManager instance;

    // NavMesh
    public NavMeshSurface[] surfaces;
    private int navTypeId;
    private int borderTypeId;

    // Boundary Edges
    private HashSet<(Vector3, Vector3)> boundaryEdges = new HashSet<(Vector3, Vector3)>();
    private HashSet<Mesh> boundaryMeshes = new HashSet<Mesh> ();
    private static HashSet<GameObject> wallContainers = new HashSet<GameObject>();
    private static Dictionary<GameObject, (Vector3, Vector3)> boundaryDict = new();

    void Awake()
    {
        instance = this;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying && UiManager.instance.showWall)
        {
            foreach (var edge in boundaryEdges)
            {
                var s = edge.Item1;
                var t = edge.Item2;
                Debug.DrawLine(s, t, Color.red);
                var middlePoint = (s + t) / 2;
                var norm = t - s;
                norm = Quaternion.Euler(0, 90, 0) * norm;
                norm = norm.normalized;
                Debug.DrawLine(middlePoint, middlePoint + norm / 3, Color.red);
            }
        }
    }
    public static bool IsWall(GameObject wallContainer)
    {
        return wallContainers.Contains(wallContainer);
    }

    public static (Vector3, Vector3) EdgeLookUp(GameObject wall)
    {
        return boundaryDict[wall];
    }

    public void InitWallAndNavMesh()
    {
        // initialize agent types
        //Debug.Log("Agent Types: " + NavMesh.GetSettingsCount());
        navTypeId = NavMesh.GetSettingsByIndex(0).agentTypeID;
        //Debug.Log("Navigation Agent Type ID: " + navTypeId);
        borderTypeId = NavMesh.GetSettingsByIndex(1).agentTypeID;
        //Debug.Log("Border Generation Agent Type ID: " + borderTypeId);

        // generate NavMesh for walls
        ConfigureNavMesh("Boundary");
        UpdateNavMesh();

        // NavMesh to Edges
        var generator = new WallMeshGenerator();
        boundaryEdges = generator.GetNavMeshBoundaryEdges();

        // Edges to Walls
        EdgeToWallMesh();

        // generate NavMesh for navigation
        ConfigureNavMesh("Navigate");
        UpdateNavMesh();
    }

    private void UpdateNavMesh()
    {
        for (int i = 0; i < surfaces.Length; i++)
        {
            surfaces[i].BuildNavMesh();
        }
    }
    private void ConfigureNavMesh(string mode)
    {
        for (int i = 0; i < surfaces.Length; i++)
        {
            switch (mode)
            {
                case "Boundary":
                    surfaces[i].voxelSize = 0.015f;
                    surfaces[i].agentTypeID = borderTypeId;
                    surfaces[i].layerMask = LayerMask.GetMask("Default");
                    break;
                case "Navigate":
                    surfaces[i].voxelSize = 0.04f;
                    surfaces[i].agentTypeID = navTypeId;
                    surfaces[i].layerMask = LayerMask.GetMask("Default");
                    break;
            }
        }
    }


    private void EdgeToWallMesh()
    {
        // Turn boundary wall lines into meshes
        int wallID = 0;
        foreach (var edge in boundaryEdges)
        {
            Mesh mesh = new Mesh();
            Vector3 s = edge.Item1;
            Vector3 t = edge.Item2;
            Vector3 s1 = s + new Vector3(0, 2, 0);
            Vector3 t1 = t + new Vector3(0, 2, 0.01f);
            Vector3[] newVertices = new Vector3[4] { s, t, s1, t1 };
            int[] newTriangles = new int[6] { 0, 2, 1, 2, 3, 1 };
            mesh.vertices = newVertices;
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;
            mesh.triangles = newTriangles;

            boundaryMeshes.Add(mesh);
            

            GameObject wall = new("wall" + wallID.ToString());
            wall.transform.parent = transform;
            wall.AddComponent<MeshFilter>();
            var mf = wall.GetComponent<MeshFilter>();
            mf.mesh = mesh;

            wall.AddComponent<MeshCollider>();
            var mc = wall.GetComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            mc.enabled = true;

            //wall.AddComponent<MeshRenderer>();

            boundaryDict[wall] = (s, t);
            wallContainers.Add(wall);
            
            wallID++;
        }
    }
    
    


}
