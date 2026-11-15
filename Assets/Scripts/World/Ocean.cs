using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Radial LOD ocean mesh centred on the camera; all shaping happens in the shader.</summary>
public class Ocean : MonoBehaviour
{
    public static Ocean I;
    Transform cam;
    Vector4 shipA = new Vector4(99999, 99999, 0, 1), shipB = new Vector4(99999, 99999, 0, 1), shipC = new Vector4(99999, 99999, 0, 1);
    Vector4 speeds;

    public static Ocean Create(Transform parent, Transform camera)
    {
        var go = new GameObject("Ocean");
        go.transform.SetParent(parent, false);
        var o = go.AddComponent<Ocean>();
        o.cam = camera;
        I = o;
        o.Build();
        return o;
    }

    void Build()
    {
        int seg = 160;
        int rings = 0;
        var radii = new System.Collections.Generic.List<float> { 0f };
        float r = 0.5f, step = 0.45f;
        while (r < 3600f)
        {
            radii.Add(r);
            step *= 1.042f;
            r += step;
            rings++;
        }
        var verts = new Vector3[radii.Count * seg];
        for (int j = 0; j < radii.Count; j++)
            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f + (j % 2) * Mathf.PI / seg;
                verts[j * seg + i] = new Vector3(Mathf.Cos(a) * radii[j], 0, Mathf.Sin(a) * radii[j]);
            }
        var tris = new System.Collections.Generic.List<int>();
        for (int j = 0; j < radii.Count - 1; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = j * seg + i, b = j * seg + (i + 1) % seg;
                int c = (j + 1) * seg + i, d = (j + 1) * seg + (i + 1) % seg;
                // winding: viewed from above, clockwise
                tris.Add(a); tris.Add(d); tris.Add(c);
                tris.Add(a); tris.Add(b); tris.Add(d);
            }
        var m = new Mesh { name = "OceanRadial", indexFormat = IndexFormat.UInt32 };
        m.vertices = verts;
        m.SetTriangles(tris, 0);
        m.bounds = new Bounds(Vector3.zero, new Vector3(20000, 400, 20000));
        gameObject.AddComponent<MeshFilter>().sharedMesh = m;
        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mats.Ocean;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    public void SetShip(int idx, Vector3 pos, Vector3 fwd, float speed)
    {
        var v = new Vector4(pos.x, pos.z, fwd.x, fwd.z);
        if (idx == 0) { shipA = v; speeds.x = speed; }
        else if (idx == 1) { shipB = v; speeds.y = speed; }
        else { shipC = v; speeds.z = speed; }
    }

    public void ClearShip(int idx)
    {
        if (idx == 0) { shipA = new Vector4(99999, 99999, 0, 1); speeds.x = 0; }
        else if (idx == 1) { shipB = new Vector4(99999, 99999, 0, 1); speeds.y = 0; }
        else { shipC = new Vector4(99999, 99999, 0, 1); speeds.z = 0; }
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            Vector3 p = cam.position;
            transform.position = new Vector3(Mathf.Round(p.x / 2f) * 2f, 0, Mathf.Round(p.z / 2f) * 2f);
        }
        Shader.SetGlobalVector("_ShipA", shipA);
        Shader.SetGlobalVector("_ShipB", shipB);
        Shader.SetGlobalVector("_ShipC", shipC);
        Shader.SetGlobalVector("_ShipSpeeds", speeds);
    }
}
