using System.Collections.Generic;
using UnityEngine;

public class Triangulator
{
    private List<Vector2> _points;

    public Triangulator(Vector2[] points)
    {
        _points = new List<Vector2>(points);
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        if (_points.Count < 3)
            return indices.ToArray();

        int[] V = new int[_points.Count];
        if (Area() > 0)
        {
            for (int v = 0; v < _points.Count; v++)
                V[v] = v;
        }
        else
        {
            for (int v = 0; v < _points.Count; v++)
                V[v] = (_points.Count - 1) - v;
        }

        int nv = _points.Count;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                return indices.ToArray(); // Polygon is probably not simple

            int u = v;
            if (nv <= u)
                u = 0;
            v = u + 1;
            if (nv <= v)
                v = 0;
            int w = v + 1;
            if (nv <= w)
                w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a = V[u];
                int b = V[v];
                int c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                for (int s = v, t = v + 1; t < nv; s++, t++)
                    V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse(); // Optional: flip winding order if needed
        return indices.ToArray();
    }

    private float Area()
    {
        float area = 0;
        int n = _points.Count;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = _points[p];
            Vector2 qval = _points[q];
            area += (pval.x * qval.y) - (qval.x * pval.y);
        }
        return area * 0.5f;
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        Vector2 A = _points[V[u]];
        Vector2 B = _points[V[v]];
        Vector2 C = _points[V[w]];

        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;

        for (int p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Vector2 P = _points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }

        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax = C.x - B.x, ay = C.y - B.y;
        float bx = A.x - C.x, by = A.y - C.y;
        float cx = B.x - A.x, cy = B.y - A.y;

        float apx = P.x - A.x, apy = P.y - A.y;
        float bpx = P.x - B.x, bpy = P.y - B.y;
        float cpx = P.x - C.x, cpy = P.y - C.y;

        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }
}