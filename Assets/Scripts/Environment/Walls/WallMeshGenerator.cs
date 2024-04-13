using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WallMeshGenerator
{

    private NavMeshTriangulation triangles;
    private readonly Dictionary<string, int> edgeStringCounter = new();
    private readonly Dictionary<string, (Vector3, Vector3)> edgeStringDict = new();


    public HashSet<(Vector3, Vector3)> GetNavMeshBoundaryEdges()
    {
        HashSet<(Vector3, Vector3)> boundaryEdges = new();

        triangles = NavMesh.CalculateTriangulation();

        // Count edge duplications
        for (int i = 0; i < triangles.areas.Length; i++)
        {
            if (triangles.areas[i] == 0)
            {
                int j = i * 3;
                updateEdgeSet(triangles.indices[j], triangles.indices[j + 1]);
                updateEdgeSet(triangles.indices[j + 1], triangles.indices[j + 2]);
                updateEdgeSet(triangles.indices[j + 2], triangles.indices[j]);
            }
        }

        // Find boundary wall lines
        foreach (var edgeStr in edgeStringCounter)
        {
            if (edgeStr.Value < 2)
            {
                var ps = edgeStringDict[edgeStr.Key];
                Vector3 p1 = ps.Item1;
                Vector3 p2 = ps.Item2;
                boundaryEdges.Add((p1, p2));
            }
        }
        return boundaryEdges;
    }
    private void updateEdgeSet(int id1, int id2)
    {
        Vector3 p1 = triangles.vertices[id1];
        Vector3 p2 = triangles.vertices[id2];

        /**
         * Because of the imprecision of float numbers,
         * a hashSet like <Vector3, Vector3> will not work as expected.
         * 
         * Instead, a Vector3 should be convert to a string.
         * */

        string e1 = WallUtils.EdgeHashing(p1, p2);
        string e2 = WallUtils.EdgeHashing(p2, p1);
        if (edgeStringCounter.ContainsKey(e1) | edgeStringCounter.ContainsKey(e2))
        {
            edgeStringCounter[e1] += 1;
            edgeStringCounter[e2] += 1;
        }
        else
        {
            edgeStringCounter[e1] = 1;
            edgeStringCounter[e2] = 1;
            edgeStringDict[e1] = (p1, p2);
            edgeStringDict[e2] = (p1, p2);
        }
    }

}
