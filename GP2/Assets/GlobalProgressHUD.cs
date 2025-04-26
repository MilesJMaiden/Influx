using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class GlobalProgressHUD : MonoBehaviour
{
    public Slider computersSlider;
    public Slider binsSlider;
    public Slider allSlider;

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        // start off hidden until LevelGenerator turns us on
        _canvas.enabled = false;
    }

    void OnEnable()
    {
        // whenever LevelGenerator shows us, also ensure the Canvas component is on
        _canvas.enabled = true;
    }

    void Update()
    {
        // if the game is paused, hide the HUD entirely
        if (Time.timeScale == 0f)
        {
            _canvas.enabled = false;
            return;
        }
        else if (!_canvas.enabled)
        {
            // if unpaused *and* we had been shown by LevelGenerator, re-enable
            _canvas.enabled = true;
        }

        // — your existing slider‐averaging logic —
        var comp = FindObjectsOfType<Computer>()
                    .Select(c => c.GetComponentInChildren<Slider>())
                    .Where(s => s != null)
                    .ToArray();
        computersSlider.value = comp.Any() ? comp.Average(s => s.value) : 0f;

        var bin = FindObjectsOfType<Bin>()
                  .Select(b => b.GetComponentInChildren<Slider>())
                  .Where(s => s != null)
                  .ToArray();
        binsSlider.value = bin.Any() ? bin.Average(s => s.value) : 0f;

        var all = FindObjectsOfType<Slider>()
                  .Where(s => s != computersSlider && s != binsSlider && s != allSlider)
                  .ToArray();
        allSlider.value = all.Any() ? all.Average(s => s.value) : 0f;
    }
}
