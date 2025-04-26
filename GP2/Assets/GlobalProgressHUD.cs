using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class GlobalProgressHUD : MonoBehaviour
{
    [Header("Per-Category Sliders")]
    public Slider computersSlider;
    public Slider binsSlider;
    public Slider allSlider;

    [Header("Game-Over UI")]
    [SerializeField] private Canvas gameOverCanvas;

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.enabled = false;

        if (gameOverCanvas != null)
            gameOverCanvas.enabled = false;
        else
            Debug.LogWarning("GlobalProgressHUD: no GameOver Canvas assigned!");
    }

    void OnEnable()
    {
        _canvas.enabled = true;
    }

    void Update()
    {
        // hide HUD if paused
        if (Time.timeScale == 0f)
        {
            _canvas.enabled = false;
            return;
        }
        else if (!_canvas.enabled)
        {
            _canvas.enabled = true;
        }

        // 1) recompute averages
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

        // 2) check for game-over
        if (allSlider.value <= 0f)
        {
            // pause game
            Time.timeScale = 0f;

            // show game-over
            if (gameOverCanvas != null)
                gameOverCanvas.enabled = true;

            // ensure HUD is hidden
            _canvas.enabled = false;
        }
    }
}
