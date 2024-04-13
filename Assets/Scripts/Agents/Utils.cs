using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Random = UnityEngine.Random;

public class Utils
{
    public static float NextGaussian()
    {
        float v1, v2, s;
        do
        {
            v1 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            v2 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0f);
        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

        return v1 * s;
    }

    public static float SampleFromExpo(float mean, int sampleNum, float maxX=10)
    {
        float[] cdf = GenerateExpoCDF(mean, sampleNum, maxX);
        var ratio = sampleFromCDF(cdf);
        return ratio * maxX;
    }
    public static float SampleFromGaussian(float mean, float std, float minX = -1e+3f, float maxX=1e+3f)
    {
        float p = NextGaussian() * std + mean;
        p = p > maxX ? maxX : p;
        _ = p < minX ? minX : p;
        return p;
    }


    public static float sampleFromCDF(float[] CDF)
    {
        var x = Random.value;
        for (var i = 0; i < CDF.Count(); i++)
        {
            if (CDF[i] > x)
            {
                return (float)i / CDF.Count();
            }
        }
        return 1f;
    }

    public static float[] GenerateExpoCDF(float mean, int sampleNum, float maxX=5)
    {
        var lambda = 1 / mean;
        float[] cdf = new float[sampleNum];
        for (int i = 0; i < sampleNum; i++)
        {
            cdf[i] = 1 - Mathf.Exp(-lambda * i * maxX / sampleNum);
        }
        return cdf;
    }

    public static float angleBetweenVecsInRad(Vector3 vec1, Vector3 vec2)
    {
        return Mathf.Atan2(vec1.x * vec2.z - vec1.z * vec2.x, vec1.x * vec2.x + vec1.z * vec2.z); ;
    }

    public static float sigmoid(float x)
    {
        return 1 / (1 + Mathf.Exp(-x));
    }

    public static float PolyLogistic(float[] inputs, PolyLogParam param)
    {
        float score = 0f;
        for (int i = 0; i < param.means.Length; i++)
        {
            score += (inputs[i] - param.means[i]) / param.stds[i] * param.weights[i];
        }
        return sigmoid(score + param.intercept);
    }

    public static void ListOfDictToCsv(List<Dictionary<string, object>> database, string filePath)
    {
        var csv = new StringBuilder();

        for (int i = 0; i < database.Count; i++)
        {
            var p = database[i];

            if (i == 0)
            {
                csv.AppendLine(String.Join(",", p.Keys));
            }
            csv.AppendLine(String.Join(",", p.Values));
        }

        File.WriteAllText(filePath, csv.ToString());
    }
}
