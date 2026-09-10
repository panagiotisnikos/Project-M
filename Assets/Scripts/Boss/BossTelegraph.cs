using UnityEngine;

/// <summary>
/// Ground indicators for the boss's three attacks. Grows a flat shape over the
/// wind-up so the player can read the area and the timing, then flashes on the
/// strike. Meshes are generated in code (disc / arc / bar) with an unlit
/// transparent material - no art assets. Sits as a child of the boss.
/// </summary>
public class BossTelegraph : MonoBehaviour
{
    public enum Shape { None, Ring, Arc, Line }

    [Header("Colors")]
    [SerializeField] private Color ringColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private Color arcColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color lineColor = new Color(1f, 0.3f, 0.9f, 1f);

    [Header("Look")]
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private float fillAlpha = 0.35f;
    [SerializeField] private float edgeStartAlpha = 0.15f;
    [SerializeField] private float strikeFlashDuration = 0.16f;
    [SerializeField] private float lineWidth = 0.7f;

    private Transform ring;
    private Transform arc;
    private Transform line;
    private Material ringMat;
    private Material arcMat;
    private Material lineMat;

    private Shape active = Shape.None;
    private float beginTime;
    private float windup = 1f;
    private float targetSize;
    private Vector3 ringWorldCenter;

    private bool striking;
    private float strikeTime;

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");

        ring = BuildMesh("Ring", DiscMesh(1f, 40), ringColor, out ringMat, shader);
        arc = BuildMesh("Arc", ArcMesh(1f, 180f, 40), arcColor, out arcMat, shader);
        line = BuildMesh("Line", BarMesh(), lineColor, out lineMat, shader);

        SetActiveShape(Shape.None);
    }

    private Transform BuildMesh(string name, Mesh mesh, Color color, out Material mat, Shader shader)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(transform, false);

        go.GetComponent<MeshFilter>().sharedMesh = mesh;

        mat = new Material(shader);
        mat.color = new Color(color.r, color.g, color.b, edgeStartAlpha);

        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        go.SetActive(false);
        return go.transform;
    }

    // --- Public API driven by BossCombat ------------------------------

    public void Begin(Shape shape, Vector3 center, Vector3 forward, float size, float arcDegrees, float windupDuration)
    {
        Clear();

        active = shape;
        beginTime = Time.time;
        windup = Mathf.Max(0.05f, windupDuration);
        targetSize = Mathf.Max(0.1f, size);
        striking = false;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        forward.Normalize();

        Quaternion flat = Quaternion.LookRotation(forward) * Quaternion.Euler(90f, 0f, 0f);

        switch (shape)
        {
            case Shape.Ring:
                ringWorldCenter = center;
                ring.SetPositionAndRotation(center + Vector3.up * groundOffset, Quaternion.Euler(90f, 0f, 0f));
                ring.localScale = Vector3.one * 0.01f;
                SetActiveShape(Shape.Ring);
                break;

            case Shape.Arc:
                arc.GetComponent<MeshFilter>().sharedMesh =
                    ArcMesh(1f, Mathf.Clamp(arcDegrees, 10f, 360f), 40);
                arc.SetPositionAndRotation(transform.position + Vector3.up * groundOffset, flat);
                arc.localScale = Vector3.one * 0.01f;
                SetActiveShape(Shape.Arc);
                break;

            case Shape.Line:
                line.SetPositionAndRotation(transform.position + Vector3.up * groundOffset, flat);
                line.localScale = new Vector3(lineWidth, 0.01f, 1f);
                SetActiveShape(Shape.Line);
                break;
        }
    }

    public void Strike()
    {
        if (active == Shape.None)
            return;

        striking = true;
        strikeTime = Time.time;
    }

    public void Clear()
    {
        active = Shape.None;
        striking = false;
        SetActiveShape(Shape.None);
    }

    private void Update()
    {
        if (active == Shape.None)
            return;

        float grow = striking
            ? 1f
            : Mathf.Clamp01((Time.time - beginTime) / windup);

        float alpha;
        if (striking)
        {
            float k = Mathf.Clamp01((Time.time - strikeTime) / Mathf.Max(0.01f, strikeFlashDuration));
            alpha = Mathf.Lerp(0.9f, 0f, k);
            if (k >= 1f)
            {
                Clear();
                return;
            }
        }
        else
        {
            alpha = Mathf.Lerp(edgeStartAlpha, fillAlpha, grow);
        }

        switch (active)
        {
            case Shape.Ring:
                ring.position = ringWorldCenter + Vector3.up * groundOffset;
                ring.localScale = Vector3.one * (targetSize * grow);
                Tint(ringMat, alpha);
                break;

            case Shape.Arc:
                arc.position = transform.position + Vector3.up * groundOffset;
                arc.localScale = Vector3.one * (targetSize * grow);
                Tint(arcMat, alpha);
                break;

            case Shape.Line:
                line.position = transform.position + Vector3.up * groundOffset;
                line.localScale = new Vector3(lineWidth, targetSize * grow, 1f);
                Tint(lineMat, alpha);
                break;
        }
    }

    private static void Tint(Material mat, float a)
    {
        Color c = mat.color;
        c.a = a;
        mat.color = c;
    }

    private void SetActiveShape(Shape shape)
    {
        if (ring != null) ring.gameObject.SetActive(shape == Shape.Ring);
        if (arc != null) arc.gameObject.SetActive(shape == Shape.Arc);
        if (line != null) line.gameObject.SetActive(shape == Shape.Line);
    }

    // --- Procedural meshes (XY plane, laid flat via a 90deg X rotation) ---

    private static Mesh DiscMesh(float radius, int segments)
    {
        return ArcMesh(radius, 360f, segments);
    }

    private static Mesh ArcMesh(float radius, float degrees, int segments)
    {
        segments = Mathf.Max(3, segments);
        var verts = new Vector3[segments + 2];
        var tris = new int[segments * 3];

        verts[0] = Vector3.zero;
        float start = -degrees * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float ang = (start + degrees * (i / (float)segments)) * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Sin(ang) * radius, Mathf.Cos(ang) * radius, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        var m = new Mesh { name = $"Telegraph_Arc_{degrees}" };
        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static Mesh BarMesh()
    {
        var m = new Mesh { name = "Telegraph_Bar" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0.5f, 1f, 0f),
            new Vector3(-0.5f, 1f, 0f)
        };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
