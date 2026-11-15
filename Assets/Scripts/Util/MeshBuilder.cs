using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Procedural mesh assembly with vertex colours (stored linear), uv and a sway channel (uv2.x).
/// Everything in the game is built from this — there are no imported models.
/// </summary>
public class MeshBuilder
{
    public readonly List<Vector3> V = new List<Vector3>();
    public readonly List<Vector3> N = new List<Vector3>();
    public readonly List<Color> C = new List<Color>();
    public readonly List<Vector2> UV = new List<Vector2>();
    public readonly List<Vector2> UV2 = new List<Vector2>();
    public readonly List<int> T = new List<int>();

    public Matrix4x4 M = Matrix4x4.identity;
    public float Sway;

    public int Count => V.Count;

    public int Vert(Vector3 p, Vector3 n, Color c, Vector2 uv, float sway)
    {
        V.Add(M.MultiplyPoint3x4(p));
        N.Add(M.MultiplyVector(n).normalized);
        C.Add(c.linear);
        UV.Add(uv);
        UV2.Add(new Vector2(sway, 0));
        return V.Count - 1;
    }

    public int Vert(Vector3 p, Vector3 n, Color c) => Vert(p, n, c, Vector2.zero, Sway);
    public int Vert(Vector3 p, Vector3 n, Color c, Vector2 uv) => Vert(p, n, c, uv, Sway);

    public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }

    /// <summary>Adds a triangle and fixes winding so that it faces along the vertex normals.</summary>
    public void TriAuto(int a, int b, int c)
    {
        Vector3 gn = Vector3.Cross(V[b] - V[a], V[c] - V[a]);
        Vector3 vn = N[a] + N[b] + N[c];
        if (Vector3.Dot(gn, vn) < 0) Tri(a, c, b); else Tri(a, b, c);
    }

    public void QuadAuto(int a, int b, int c, int d)
    {
        TriAuto(a, b, c);
        TriAuto(a, c, d);
    }

    public void FlatTri(Vector3 a, Vector3 b, Vector3 c, Color col, Vector3 hint = default)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (hint != Vector3.zero && Vector3.Dot(n, hint) < 0)
        {
            var t = b; b = c; c = t;
            n = -n;
        }
        n.Normalize();
        int i = Vert(a, n, col);
        Vert(b, n, col);
        Vert(c, n, col);
        Tri(i, i + 1, i + 2);
    }

    public void FlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col, Vector3 hint = default)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (hint != Vector3.zero && Vector3.Dot(n, hint) < 0)
        {
            var t = b; b = d; d = t;
            n = -n;
        }
        n.Normalize();
        int i = Vert(a, n, col, new Vector2(0, 0));
        Vert(b, n, col, new Vector2(0, 1));
        Vert(c, n, col, new Vector2(1, 1));
        Vert(d, n, col, new Vector2(1, 0));
        Tri(i, i + 1, i + 2);
        Tri(i, i + 2, i + 3);
    }

    /// <summary>Double sided quad (two faces with opposite normals).</summary>
    public void Card(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        FlatQuad(a, b, c, d, col, n);
        FlatQuad(a, b, c, d, col, -n);
    }

    public void Box(Vector3 center, Vector3 size, Quaternion rot, Color col)
    {
        Vector3 h = size * 0.5f;
        Vector3 X = rot * new Vector3(h.x, 0, 0), Y = rot * new Vector3(0, h.y, 0), Z = rot * new Vector3(0, 0, h.z);
        Vector3 c = center;
        FlatQuad(c + X - Y - Z, c + X + Y - Z, c + X + Y + Z, c + X - Y + Z, col, X);
        FlatQuad(c - X - Y - Z, c - X + Y - Z, c - X + Y + Z, c - X - Y + Z, col, -X);
        FlatQuad(c - X + Y - Z, c - X + Y + Z, c + X + Y + Z, c + X + Y - Z, col, Y);
        FlatQuad(c - X - Y - Z, c - X - Y + Z, c + X - Y + Z, c + X - Y - Z, col, -Y);
        FlatQuad(c - X - Y + Z, c - X + Y + Z, c + X + Y + Z, c + X - Y + Z, col, Z);
        FlatQuad(c - X - Y - Z, c - X + Y - Z, c + X + Y - Z, c + X - Y - Z, col, -Z);
    }

    public void Box(Vector3 center, Vector3 size, Color col) => Box(center, size, Quaternion.identity, col);

    static void Basis(Vector3 axis, out Vector3 u, out Vector3 v)
    {
        Vector3 refv = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        u = Vector3.Cross(axis, refv).normalized;
        v = Vector3.Cross(axis, u).normalized;
    }

    public void Cylinder(Vector3 a, Vector3 b, float ra, float rb, int seg, Color col, bool capA = false, bool capB = false, Color? colB = null)
    {
        Vector3 ax = b - a;
        float len = ax.magnitude;
        if (len < 1e-5f) return;
        Vector3 axis = ax / len;
        Basis(axis, out Vector3 u, out Vector3 v);
        Color cb = colB ?? col;
        int start = Count;
        for (int i = 0; i <= seg; i++)
        {
            float t = (float)i / seg * Mathf.PI * 2f;
            Vector3 dir = u * Mathf.Cos(t) + v * Mathf.Sin(t);
            Vector3 n = (dir * len + axis * (ra - rb)).normalized;
            Vert(a + dir * ra, n, col, new Vector2((float)i / seg, 0));
            Vert(b + dir * rb, n, cb, new Vector2((float)i / seg, 1));
        }
        for (int i = 0; i < seg; i++)
        {
            int i0 = start + i * 2;
            QuadAuto(i0, i0 + 2, i0 + 3, i0 + 1);
        }
        if (capA && ra > 0) Cap(a, -axis, u, v, ra, seg, col);
        if (capB && rb > 0) Cap(b, axis, u, v, rb, seg, cb);
    }

    void Cap(Vector3 c, Vector3 n, Vector3 u, Vector3 v, float r, int seg, Color col)
    {
        int ci = Vert(c, n, col);
        int first = Count;
        for (int i = 0; i <= seg; i++)
        {
            float t = (float)i / seg * Mathf.PI * 2f;
            Vert(c + (u * Mathf.Cos(t) + v * Mathf.Sin(t)) * r, n, col);
        }
        for (int i = 0; i < seg; i++) TriAuto(ci, first + i, first + i + 1);
    }

    public void Sphere(Vector3 c, Vector3 radius, int segU, int segV, Color col, Color? colBottom = null)
    {
        Color cb = colBottom ?? col;
        int start = Count;
        for (int j = 0; j <= segV; j++)
        {
            float pv = (float)j / segV;
            float phi = pv * Mathf.PI;
            for (int i = 0; i <= segU; i++)
            {
                float th = (float)i / segU * Mathf.PI * 2f;
                Vector3 n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                Vector3 p = c + Vector3.Scale(n, radius);
                Vert(p, new Vector3(n.x / radius.x, n.y / radius.y, n.z / radius.z).normalized, Color.Lerp(col, cb, pv));
            }
        }
        for (int j = 0; j < segV; j++)
            for (int i = 0; i < segU; i++)
            {
                int a = start + j * (segU + 1) + i;
                int b = a + segU + 1;
                QuadAuto(a, a + 1, b + 1, b);
            }
    }

    /// <summary>Faceted noisy boulder.</summary>
    public void Rock(Vector3 c, Vector3 size, int seed, Color col, Color col2)
    {
        var rng = new System.Random(seed);
        int su = 7, sv = 5;
        Vector3[,] pts = new Vector3[su + 1, sv + 1];
        for (int j = 0; j <= sv; j++)
            for (int i = 0; i <= su; i++)
            {
                float phi = (float)j / sv * Mathf.PI;
                float th = (float)(i % su) / su * Mathf.PI * 2f;
                Vector3 n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                float k = (j == 0 || j == sv) ? 1f : 0.75f + (float)rng.NextDouble() * 0.45f;
                pts[i, j] = c + Vector3.Scale(n * k, size);
            }
        for (int i = 0; i <= sv; i++) pts[su, i] = pts[0, i];
        for (int j = 0; j < sv; j++)
            for (int i = 0; i < su; i++)
            {
                Vector3 a = pts[i, j], b = pts[i + 1, j], d = pts[i, j + 1], e = pts[i + 1, j + 1];
                Color k = Color.Lerp(col, col2, (float)rng.NextDouble());
                Vector3 mid = (a + b + d + e) * 0.25f - c;
                if (j > 0) FlatTri(a, b, e, k, mid);
                if (j < sv - 1) FlatTri(a, e, d, Color.Lerp(k, col2, 0.15f), mid);
            }
    }

    public void Append(MeshBuilder o)
    {
        int off = Count;
        V.AddRange(o.V); N.AddRange(o.N); C.AddRange(o.C); UV.AddRange(o.UV); UV2.AddRange(o.UV2);
        for (int i = 0; i < o.T.Count; i++) T.Add(o.T[i] + off);
    }

    public Mesh Build(string name = "mesh", bool recalcNormals = false)
    {
        var m = new Mesh { name = name };
        if (V.Count > 65000) m.indexFormat = IndexFormat.UInt32;
        bool logged = false;
        for (int i = 0; i < V.Count; i++)
        {
            Vector3 v = V[i];
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z))
            {
                if (!logged) { Debug.LogWarning("[SpiceRoute] NaN vertex in mesh '" + name + "' index " + i + " of " + V.Count); logged = true; }
                V[i] = Vector3.zero;
            }
            Vector3 n = N[i];
            if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z)) N[i] = Vector3.up;
        }
        m.SetVertices(V);
        m.SetNormals(N);
        m.SetColors(C);
        m.SetUVs(0, UV);
        m.SetUVs(1, UV2);
        m.SetTriangles(T, 0);
        if (recalcNormals) m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    public static GameObject Spawn(string name, Mesh mesh, Material mat, Transform parent, bool cast = true, bool receive = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
        mr.receiveShadows = receive;
        return go;
    }

    public GameObject SpawnSelf(string name, Material mat, Transform parent, bool cast = true, bool receive = true, bool recalc = false)
    {
        if (Count == 0) return null;
        return Spawn(name, Build(name, recalc), mat, parent, cast, receive);
    }
}
