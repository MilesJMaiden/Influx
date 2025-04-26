using UnityEngine;
using Unity.VectorGraphics;  // Required for SVGImage
using UnityEngine.UI;

public class UICursorManager : MonoBehaviour
{
    public static UICursorManager Instance { get; private set; }

    [Header("Camera Assignment")]
    [Tooltip("Assign the camera to control. If left unassigned, Camera.main (tagged 'MainCamera') will be used.")]
    public Camera assignedCamera;

    [Header("Cursor Settings")]
    [Tooltip("SVGImage component to use as the custom cursor (must be on a Canvas).")]
    public SVGImage cursorImage;
    [Tooltip("The default cursor sprite (set via the Vector Graphics package).")]
    public Sprite defaultCursorSprite;
    [Tooltip("The cursor sprite to use when hovering over an agent.")]
    public Sprite hoverCursorSprite;

    private RectTransform canvasRectTransform;

    private void Awake()
    {
        // Singleton setup.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Use the assigned camera if provided; otherwise fallback to Camera.main.
        if (assignedCamera == null)
            assignedCamera = Camera.main;

        if (cursorImage == null)
        {
            Debug.LogError("UICursorManager: Please assign an SVG cursor image.");
            enabled = false;
            return;
        }

        // Hide the system (OS) cursor.
        Cursor.visible = false;

        // Set the default pivot for the cursor to top-left.
        cursorImage.rectTransform.pivot = new Vector2(0f, 1f);

        // Set the default cursor sprite.
        if (defaultCursorSprite != null)
        {
            cursorImage.sprite = defaultCursorSprite;
        }
        else
        {
            Debug.LogWarning("UICursorManager: Default cursor sprite is not assigned.");
        }

        // Cache the canvas's RectTransform.
        Canvas canvas = cursorImage.canvas;
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("UICursorManager: The SVG cursor image is not part of a Canvas.");
        }
    }

    private void Update()
    {
        // Update the custom cursor's position: convert mouse screen point to canvas local space.
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, Input.mousePosition, null, out localPoint);
        cursorImage.rectTransform.localPosition = localPoint;
    }

    private void OnDisable()
    {
        // When disabled, restore the OS cursor.
        Cursor.visible = true;
    }

    /// <summary>
    /// Sets the cursor image to the hover sprite and centers the pivot.
    /// </summary>
    public void SetHoverCursor()
    {
        if (cursorImage != null && hoverCursorSprite != null)
        {
            cursorImage.sprite = hoverCursorSprite;
            // Change pivot to center.
            cursorImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Resets the cursor image back to the default sprite and restores the top-left pivot.
    /// </summary>
    public void ResetCursorImage()
    {
        if (cursorImage != null && defaultCursorSprite != null)
        {
            cursorImage.sprite = defaultCursorSprite;
            // Reset pivot to top-left.
            cursorImage.rectTransform.pivot = new Vector2(0f, 1f);
        }
    }
}
