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
    [Tooltip("Dead zone for input; values below this are ignored.")]
    public float deadZone = 0.1f;

    [Header("Zoom Settings")]
    [Tooltip("Zoom speed when scrolling the mouse wheel.")]
    public float zoomSpeed = 10f;
    [Tooltip("Minimum orthographic size (cannot zoom in closer than this).")]
    public float minOrthographicSize = 50f;
    [Tooltip("Maximum orthographic size (cannot zoom out further than this).")]
    public float maxOrthographicSize = 100f;

    [Header("Level Boundaries")]
    [Tooltip("These boundaries should enclose the entire generated level. Set this by calling SetLevelBounds(generatedBounds) from your generation code.")]
    public Bounds levelBounds;
    [Tooltip("If true, camera movement is clamped to the level boundaries.")]
    public bool useLevelBounds = false;

    [Header("Initial Settings")]
    [Tooltip("The fixed Y position of the camera (must be above the environment).")]
    public float fixedYPosition = 25f;
    [Tooltip("Padding added when focusing on the level.")]
    public float focusPadding = 2f;

    private Camera cam;
    // controllerTransform is used for movement. Attach this script to a Camera Rig if desired.
    private Transform controllerTransform;

    private void Awake()
    {
        // Singleton pattern.
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
        // Use the assigned camera if provided; otherwise fallback to Camera.main.
        if (assignedCamera != null)
            cam = assignedCamera;
        else
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("CameraManager: No camera assigned and no Main Camera found!");
            enabled = false;
            return;
        }

        // Force orthographic mode and a top-down view.
        cam.orthographic = true;
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Fix the camera's Y position.
        Vector3 currentPos = cam.transform.position;
        cam.transform.position = new Vector3(currentPos.x, fixedYPosition, currentPos.z);

        // Set default orthographic size.
        cam.orthographicSize = minOrthographicSize;

        // Note: We now rely on an external call to SetLevelBounds(...) to center and size the camera.
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    /// <summary>
    /// Handles camera movement:
    /// - Horizontal movement (X axis) is controlled via the Horizontal axis (A/D keys).
    /// - Vertical movement (Z axis) is controlled explicitly:
    ///     • W key increases Z.
    ///     • S key decreases Z.
    /// No unwanted Z drift will occur when neither key is pressed.
    /// </summary>
    private void HandleMovement()
    {
        // Horizontal input from the Horizontal axis.
        float inputX = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(inputX) < deadZone)
            inputX = 0f;

        // Vertical input: explicitly check for keys.
        float inputZ = 0f;
        if (Input.GetKey(KeyCode.W))
            inputZ += 1f;  // W increases Z.
        if (Input.GetKey(KeyCode.S))
            inputZ -= 1f;  // S decreases Z.

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
    /// Handles zooming with the mouse scroll wheel by adjusting the orthographic size.
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
    /// Sets the level boundaries (in world space) and centers/zooms the camera on the full level.
    /// Call this from your LevelGenerator after the environment is fully generated.
    /// </summary>
    /// <param name="generatedBounds">The Bounds that enclose the generated level.</param>
    public void SetLevelBounds(Bounds generatedBounds)
    {
        levelBounds = generatedBounds;
        useLevelBounds = true;
        FocusOnLevel();
    }

    /// <summary>
    /// Centers the camera on the level boundaries and adjusts the orthographic size so that the entire level is in frame.
    /// The camera's Y position remains fixed.
    /// </summary>
    private void FocusOnLevel()
    {
        Vector3 center = levelBounds.center;
        controllerTransform.position = new Vector3(center.x, fixedYPosition, center.z);

        float verticalExtent = levelBounds.extents.z;
        float horizontalExtent = levelBounds.extents.x;
        float aspect = cam.aspect;
        float requiredSize = Mathf.Max(verticalExtent, horizontalExtent / aspect) + focusPadding;
        cam.orthographicSize = Mathf.Clamp(requiredSize, minOrthographicSize, maxOrthographicSize);
    }
}
