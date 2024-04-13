using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

public class WallUtils
{
    public static HashSet<(Vector3, Vector3)> ReadWallsFromFile(string boundaryFilePath)
    {
        var boundaryEdges = new HashSet<(Vector3, Vector3)>();
        using (var reader = new StreamReader(@boundaryFilePath))
        {
            // skip the header
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                Vector3 s = new(float.Parse(values[0], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(values[1], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(values[2], CultureInfo.InvariantCulture.NumberFormat));
                Vector3 t = new(float.Parse(values[3], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(values[4], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(values[5], CultureInfo.InvariantCulture.NumberFormat));

                boundaryEdges.Add((s, t));

            }
        }
        return boundaryEdges;
    }

    public Color colorFromID(int id)
    {
        return new Color((37 * id) % 255 / 255f, (17 * id) % 255 / 255f, (29 * id) % 255 / 255f);
    }

    public static void ExportBoundaryData(HashSet<(Vector3, Vector3)> boundaryEdges, string boundaryFilePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("name,origin,destination,time,x,y,z,v,vx,vy,vz");

        foreach (var edge in boundaryEdges)
        {
            Vector3 s = edge.Item1;
            Vector3 t = edge.Item2;

            var newLine = string.Format("{0},{1},{2},{3},{4},{5}",
                s[0].ToString(CultureInfo.InvariantCulture), s[1].ToString(CultureInfo.InvariantCulture),
                s[2].ToString(CultureInfo.InvariantCulture), t[0].ToString(CultureInfo.InvariantCulture),
                t[1].ToString(CultureInfo.InvariantCulture), t[2].ToString(CultureInfo.InvariantCulture));
            csv.AppendLine(newLine);
        }

        File.WriteAllText(boundaryFilePath, csv.ToString());
    }
    public static string EdgeHashing(Vector3 p1, Vector3 p2)
    {
        return IdStr(p1[0]) + "-" + IdStr(p1[1]) + "-" + IdStr(p1[2]) + "," + IdStr(p2[0]) + "-" + IdStr(p2[1]) + "-" + IdStr(p2[2]);
    }
    private static string IdStr(float i)
    {
        return ((int)Mathf.Round(i * 100f)).ToString();
    }


}
