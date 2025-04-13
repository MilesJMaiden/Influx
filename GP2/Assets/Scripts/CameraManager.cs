using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Camera Assignment")]
    [Tooltip("Assign the camera to control. If unassigned, Camera.main (tagged 'MainCamera') will be used.")]
    public Camera assignedCamera;

    [Header("Movement Settings")]
    [Tooltip("Movement speed in world units per second.")]
    public float moveSpeed = 20f;
    [Tooltip("Input dead zone threshold; values below this are ignored.")]
    public float deadZone = 0.1f;

    [Header("Zoom Settings")]
    [Tooltip("Zoom speed when scrolling the mouse wheel.")]
    public float zoomSpeed = 10f;
    [Tooltip("Minimum orthographic size (cannot zoom in closer than this).")]
    public float minOrthographicSize = 50f;
    [Tooltip("Maximum orthographic size (cannot zoom out further than this).")]
    public float maxOrthographicSize = 100f;

    [Header("Level Boundaries")]
    [Tooltip("These boundaries should exactly enclose your generated environment.")]
    public Bounds levelBounds;
    [Tooltip("If true, camera movement is clamped to levelBounds.")]
    public bool useLevelBounds = false;

    [Header("Initial Settings")]
    [Tooltip("Fixed Y position for the camera (set above the level).")]
    public float fixedYPosition = 25f;
    [Tooltip("Padding added when focusing the camera on the entire level.")]
    public float focusPadding = 2f;

    private Camera cam;
    private Transform controllerTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        controllerTransform = transform;
    }

    private void Start()
    {
        // Use the assigned camera or fallback to Camera.main.
        if (assignedCamera != null)
        {
            cam = assignedCamera;
        }
        else
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("CameraManager: No camera assigned and no Main Camera found!");
                enabled = false;
                return;
            }
        }

        // Force orthographic mode.
        cam.orthographic = true;
        // Force a top-down view.
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Fix the camera's Y position.
        Vector3 currentPos = cam.transform.position;
        cam.transform.position = new Vector3(currentPos.x, fixedYPosition, currentPos.z);

        // Set the default orthographic size.
        cam.orthographicSize = minOrthographicSize;

        // If level bounds were set already, focus on the level.
        if (useLevelBounds)
        {
            FocusOnLevel();
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    /// <summary>
    /// Handles camera movement using WASD/Arrow keys.
    /// A/D move the camera left/right (affecting the X position),
    /// W moves the camera upward (increasing the Z position), and
    /// S moves the camera downward (decreasing the Z position).
    /// Movement is applied only if input exceeds the dead zone.
    /// </summary>
    private void HandleMovement()
    {
        // Get raw input.
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        // Apply dead zone.
        if (Mathf.Abs(inputX) < deadZone) { inputX = 0f; }
        if (Mathf.Abs(inputZ) < deadZone) { inputZ = 0f; }
        // For a standard top-down mapping:
        // W should increase Z (inputZ > 0), S decrease Z (inputZ < 0).
        // A/D modify X.
        Vector3 delta = new Vector3(inputX, 0, inputZ) * moveSpeed * Time.deltaTime;
        Vector3 newPos = controllerTransform.position + delta;

        if (useLevelBounds)
        {
            newPos.x = Mathf.Clamp(newPos.x, levelBounds.min.x, levelBounds.max.x);
            newPos.z = Mathf.Clamp(newPos.z, levelBounds.min.z, levelBounds.max.z);
        }

        controllerTransform.position = newPos;
    }

    /// <summary>
    /// Adjusts the orthographic size (zoom) using the mouse scroll wheel.
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        }
    }

    /// <summary>
    /// Sets the level boundaries (which must enclose the entire generated environment)
    /// and immediately focuses the camera so that the full level is in frame.
    /// </summary>
    /// <param name="generatedBounds">The Bounds that enclose the environment (in world space).</param>
    public void SetLevelBounds(Bounds generatedBounds)
    {
        levelBounds = generatedBounds;
        useLevelBounds = true;
        FocusOnLevel();
    }

    /// <summary>
    /// Centers the camera on the level boundaries and adjusts the orthographic size
    /// so that the entire level is visible. The camera's Y position remains fixed.
    /// </summary>
    private void FocusOnLevel()
    {
        Vector3 center = levelBounds.center;
        // Set camera position (maintaining fixedYPosition).
        controllerTransform.position = new Vector3(center.x, fixedYPosition, center.z);

        // Determine the required orthographic size.
        float verticalExtent = levelBounds.extents.z;
        float horizontalExtent = levelBounds.extents.x;
        float aspect = cam.aspect;
        float requiredSize = Mathf.Max(verticalExtent, horizontalExtent / aspect) + focusPadding;
        cam.orthographicSize = Mathf.Clamp(requiredSize, minOrthographicSize, maxOrthographicSize);
    }
}
