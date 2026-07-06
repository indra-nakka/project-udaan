using UnityEngine;

/// <summary>
/// Floating world-space health bar above a TargetHealth (billboards to the camera). Built from two
/// unlit quads (bg + fill); the fill shrinks from the left and lerps green→red with health.
/// Add to any drone/target with a TargetHealth.
/// </summary>
public class HealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 3f, 0f);
    public float width = 2.2f;
    public float height = 0.28f;

    private TargetHealth _health;
    private Transform _root, _fill;
    private Material _fillMat;
    private Camera _cam;

    void Start()
    {
        _health = GetComponent<TargetHealth>();

        _root = new GameObject("HealthBar").transform;
        _root.SetParent(transform, false);
        _root.localPosition = offset;

        MakeQuad(_root, width, height, 0f, MakeUnlit(new Color(0f, 0f, 0f, 0.65f)));
        _fillMat = MakeUnlit(new Color(0.3f, 1f, 0.4f, 1f));
        _fill = MakeQuad(_root, width, height * 0.7f, -0.01f, _fillMat);
    }

    void LateUpdate()
    {
        if (_root == null) return;
        if (_cam == null) _cam = Camera.main;

        float f = _health != null ? _health.HealthFraction : 1f;
        _fill.localScale = new Vector3(width * f, _fill.localScale.y, 1f);
        _fill.localPosition = new Vector3(-width * (1f - f) * 0.5f, 0f, -0.01f);
        SetColor(_fillMat, Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.3f, 1f, 0.4f), f));

        if (_cam != null)
            _root.rotation = Quaternion.LookRotation(_root.position - _cam.transform.position, Vector3.up);
    }

    private Transform MakeQuad(Transform parent, float w, float h, float z, Material mat)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var col = q.GetComponent<Collider>();
        if (col != null) Destroy(col);
        q.transform.SetParent(parent, false);
        q.transform.localPosition = new Vector3(0f, 0f, z);
        q.transform.localScale = new Vector3(w, h, 1f);
        q.GetComponent<Renderer>().sharedMaterial = mat;
        return q.transform;
    }

    private Material MakeUnlit(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Sprites/Default");
        var m = new Material(s);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // double-sided so billboard facing never hides it
        SetColor(m, c);
        return m;
    }

    private static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
    }
}
