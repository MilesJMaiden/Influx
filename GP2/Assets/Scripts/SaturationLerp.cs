using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SaturationLerp : MonoBehaviour
{
    [Tooltip("How far above/below the original saturation to go (0–1). 0.05 = ±5% saturation")]
    [Range(0f, 1f)]
    public float saturationOffset = 0.05f;

    [Tooltip("Time (in seconds) to go from min→max→min saturation")]
    public float cycleDuration = 2f;

    // Internal
    private Material _mat;
    private float _h, _s, _v, _a;
    private float _sMin, _sMax;
    private float _timer;

    void Start()
    {
        var rend = GetComponent<Renderer>();
        // This creates an instance so we don't change sharedMaterial
        _mat = rend.material;

        // Extract HSV + alpha from the starting color
        Color orig = _mat.color;
        Color.RGBToHSV(orig, out _h, out _s, out _v);
        _a = orig.a;

        // Clamp to [0,1]
        _sMin = Mathf.Clamp01(_s - saturationOffset);
        _sMax = Mathf.Clamp01(_s + saturationOffset);
    }

    void Update()
    {
        // Advance timer
        _timer += Time.deltaTime;
        // PingPong gives a smooth back-and-forth between 0→1→0 over cycleDuration
        float t = Mathf.PingPong(_timer / cycleDuration, 1f);
        // Lerp saturation
        float sNow = Mathf.Lerp(_sMin, _sMax, t);

        // Build new color and apply
        Color c = Color.HSVToRGB(_h, sNow, _v);
        c.a = _a;
        _mat.color = c;
    }
}
