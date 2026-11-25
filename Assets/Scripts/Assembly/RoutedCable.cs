using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class RoutedCable : MonoBehaviour
{
    private Transform first, second;
    private Vector3 previousStart, previousEnd;
    private Mesh tube;
    private int lane;
    private float radius;

    public void Initialize(ConnectorPort source, ConnectorPort destination, Transform tray, Material jacket, float width)
    {
        transform.SetParent(tray, false);
        first = source.transform;
        second = destination.transform;
        lane = (int)source.Type;
        radius = Mathf.Clamp(width * .65f, .013f, .035f) * (lane >= 6 ? .67f : 1f);
        tube = new Mesh { name = source.PortId + " jacket" };
        GetComponent<MeshFilter>().sharedMesh = tube;
        GetComponent<MeshRenderer>().sharedMaterial = jacket;
        Rebuild(transform.InverseTransformPoint(first.position), transform.InverseTransformPoint(second.position));
    }

    private void LateUpdate()
    {
        if (!first || !second) return;
        Vector3 a = transform.InverseTransformPoint(first.position);
        Vector3 b = transform.InverseTransformPoint(second.position);
        if ((a - previousStart).sqrMagnitude + (b - previousEnd).sqrMagnitude > .000001f) Rebuild(a, b);
    }

    private void Rebuild(Vector3 a, Vector3 b)
    {
        previousStart = a;
        previousEnd = b;
        // The tray lies in the local XZ plane. Keep long runs against its inner face.
        float rail = -1.32f + lane * .035f;
        float level = .055f + radius;
        Vector3 outlet = a;
        if (lane <= (int)ConnectorType.SataData)
            outlet.z += lane == (int)ConnectorType.CpuFan ? .10f : -.11f;
        Vector3 entry = b;
        entry.z += .08f;
        var bends = new List<Vector3> { a, outlet, new Vector3(outlet.x, level, outlet.z),
            new Vector3(outlet.x, level, rail), new Vector3(entry.x, level, rail),
            new Vector3(entry.x, level, entry.z), entry, b };
        BuildTube(bends);
    }

    public void FitFixedRoute(List<Vector3> points, Material jacket, float cableRadius)
    {
        radius = cableRadius;
        tube = new Mesh { name = name };
        GetComponent<MeshFilter>().sharedMesh = tube;
        GetComponent<MeshRenderer>().sharedMaterial = jacket;
        BuildTube(points);
        enabled = false;
    }

    private void BuildTube(List<Vector3> bends)
    {
        for (int i = bends.Count - 1; i > 0; i--)
            if ((bends[i] - bends[i-1]).sqrMagnitude < .000001f) bends.RemoveAt(i);
        if (bends.Count < 2) { tube.Clear(); return; }
        var path = new List<Vector3> { bends[0] };
        for (int i = 1; i < bends.Count - 1; i++)
        {
            Vector3 corner = bends[i];
            Vector3 left = bends[i-1] - corner, right = bends[i+1] - corner;
            float round = Mathf.Min(.12f, .42f * Mathf.Min(left.magnitude, right.magnitude));
            Vector3 entry = corner + left.normalized * round, exit = corner + right.normalized * round;
            for (int j = 0; j <= 5; j++)
            {
                float t = j / 5f;
                path.Add(Vector3.Lerp(Vector3.Lerp(entry, corner, t), Vector3.Lerp(corner, exit, t), t));
            }
        }
        path.Add(bends[bends.Count - 1]);
        const int sides = 10;
        var vertices = new Vector3[path.Count * sides];
        var normals = new Vector3[vertices.Length];
        var triangles = new int[(path.Count - 1) * sides * 6];
        Vector3 axis = Vector3.right;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 tangent = (path[Mathf.Min(i+1,path.Count-1)] - path[Mathf.Max(0,i-1)]).normalized;
            axis = Vector3.ProjectOnPlane(axis, tangent);
            if (axis.sqrMagnitude < .00001f) axis = Vector3.Cross(tangent, Vector3.forward);
            axis.Normalize();
            Vector3 binormal = Vector3.Cross(tangent, axis);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * 2f * Mathf.PI / sides;
                int k = i * sides + j;
                normals[k] = axis * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                vertices[k] = path[i] + normals[k] * radius;
                if (i == path.Count - 1) continue;
                int next = i * sides + (j + 1) % sides, index = k * 6;
                triangles[index] = k; triangles[index+1] = next; triangles[index+2] = k+sides;
                triangles[index+3] = next; triangles[index+4] = next+sides; triangles[index+5] = k+sides;
            }
        }
        tube.Clear();
        tube.vertices = vertices;
        tube.normals = normals;
        tube.triangles = triangles;
        tube.RecalculateBounds();
    }

    private void OnDestroy() { if (tube) Destroy(tube); }
}
