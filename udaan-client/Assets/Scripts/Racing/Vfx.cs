using UnityEngine;

/// <summary>
/// Code-built one-shot VFX (no art assets): an expanding, fading flash sphere for explosions and hit
/// sparks, plus SFX + camera shake hooks. Uses the transparent "Sprites/Default" shader so alpha fades.
/// </summary>
public static class Vfx
{
    public static void Explode(Vector3 pos, float radius, Color color)
    {
        Flash(pos, radius, color, 0.45f);
        Sfx.Explosion(pos);
        ShakeByDistance(pos, 0.7f, 70f);
    }

    public static void Spark(Vector3 pos, Color color)
    {
        Flash(pos, 0.7f, color, 0.16f);
        Sfx.Hit(pos);
    }

    private static void Flash(Vector3 pos, float radius, Color color, float life)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        go.name = "VfxFlash";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.3f;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(sh);
        color.a = 1f;
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        go.AddComponent<ExpandFade>().Init(radius, life, mat, color);
    }

    private static void ShakeByDistance(Vector3 pos, float baseAmount, float range)
    {
        var cam = Camera.main;
        if (cam == null) return;
        float d = Vector3.Distance(cam.transform.position, pos);
        float amt = baseAmount * Mathf.Clamp01(1f - d / range);
        if (amt > 0.01f) CameraController.Shake(amt);
    }
}

/// <summary>Expands a flash sphere and fades its alpha out, then self-destructs.</summary>
public class ExpandFade : MonoBehaviour
{
    private float _radius, _life, _t;
    private Material _mat;
    private Color _color;

    public void Init(float radius, float life, Material mat, Color color)
    {
        _radius = radius; _life = life; _mat = mat; _color = color;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = _t / _life;
        if (k >= 1f) { Destroy(gameObject); return; }

        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, _radius * 2f, k);
        if (_mat != null)
        {
            var c = _color; c.a = 1f - k;
            _mat.color = c;
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
        }
    }
}
