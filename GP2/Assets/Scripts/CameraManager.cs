using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Camera Assignment")]
    [Tooltip("Assign the camera to control. If left unassigned, Camera.main (tagged 'MainCamera') will be used.")]
    public Camera assignedCamera;

    [Header("Level Root")]
    [Tooltip("Assign the root GameObject of your generated environment (for example, your 'GameManager').")]
    public GameObject levelRoot;

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
    [Tooltip("These boundaries should enclose the entire generated level. Set via SetLevelBounds() after generation.")]
    public Bounds levelBounds;
    [Tooltip("If true, camera movement is clamped to the level boundaries.")]
    public bool useLevelBounds = false;

    [Header("Initial Settings")]
    [Tooltip("The fixed Y position of the camera (must be above the environment).")]
    public float fixedYPosition = 25f;
    [Tooltip("Padding added when focusing on the level.")]
    public float focusPadding = 2f;
    [Tooltip("Delay in seconds before recalculating level bounds (to ensure generation is complete).")]
    public float boundsDelay = 0.2f;

    [Header("Rotation Settings")]
    [Tooltip("Speed at which the camera tilts with vertical mouse movement.")]
    public float tiltSpeed = 10f;
    [Tooltip("Speed at which the camera rotates horizontally with mouse movement.")]
    public float yawSpeed = 10f;
    [Tooltip("Minimum tilt (X rotation in degrees).")]
    public float minTilt = 75f;
    [Tooltip("Maximum tilt (X rotation in degrees).")]
    public float maxTilt = 90f;
    [Tooltip("Time (in seconds) to lerp back to default rotation when RMB is released.")]
    public float resetRotationDuration = 0.5f;

    private Camera cam;
    // controllerTransform is used for movement; attach this script to a rig that controls the camera.
    private Transform controllerTransform;

    private bool isResettingRotation = false;
    private Quaternion defaultRotation = Quaternion.Euler(90f, 0f, 0f);

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

        // Force the camera into orthographic mode and a top-down view.
        cam.orthographic = true;
        cam.transform.rotation = defaultRotation;

        // Fix the camera's Y position.
        Vector3 currentPos = cam.transform.position;
        cam.transform.position = new Vector3(currentPos.x, fixedYPosition, currentPos.z);

        cam.orthographicSize = minOrthographicSize;

        // Optionally, if levelRoot is assigned, start computing bounds after a delay.
        if (levelRoot != null)
        {
            StartCoroutine(ComputeLevelBoundsAfterDelay(boundsDelay));
        }
        else if (useLevelBounds)
        {
            FocusOnLevel();
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();

        // Reset camera (center/zoom) if F is pressed.
        if (Input.GetKeyDown(KeyCode.F))
        {
            ResetCamera();
        }
    }

    /// <summary>
    /// Handles camera movement.
    /// - When the right mouse button is held, movement is relative to the camera's current orientation.
    ///   (W moves the camera forward relative to its facing, S backward, etc.)
    /// - Otherwise, movement follows world axes (W increases Z, S decreases Z).
    /// </summary>
    private void HandleMovement()
    {
        // Get horizontal input.
        float inputX = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(inputX) < deadZone)
            inputX = 0f;

        // Get vertical input (for Z) using explicit key checks.
        float inputZ = 0f;
        if (Input.GetKey(KeyCode.W))
            inputZ += 1f;
        if (Input.GetKey(KeyCode.S))
            inputZ -= 1f;

        Vector3 delta;

        // If the right mouse button is held, movement is relative to the camera's current orientation.
        if (Input.GetMouseButton(1))
        {
            // Get the camera's forward and right vectors, zeroing out the y component.
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 camRight = cam.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 relativeMove = (camRight * inputX) + (camForward * inputZ);
            delta = relativeMove * moveSpeed * Time.deltaTime;
        }
        else
        {
            // Otherwise, movement is along world axes.
            delta = new Vector3(inputX, 0, inputZ) * moveSpeed * Time.deltaTime;
        }

        Vector3 newPos = controllerTransform.position + delta;
        if (useLevelBounds)
        {
            newPos.x = Mathf.Clamp(newPos.x, levelBounds.min.x, levelBounds.max.x);
            newPos.z = Mathf.Clamp(newPos.z, levelBounds.min.z, levelBounds.max.z);
        }
        controllerTransform.position = newPos;
    }

    /// <summary>
    /// Handles camera zoom via the mouse scroll wheel.
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
    /// Handles camera rotation when the right mouse button is held.
    /// Mouse movement adjusts tilt (X) and yaw (Y). When released, rotation lerps back to default over resetRotationDuration.
    /// </summary>
    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            // Cancel any ongoing reset.
            isResettingRotation = false;

            float mouseDeltaX = Input.GetAxis("Mouse X");
            float mouseDeltaY = Input.GetAxis("Mouse Y");

            Vector3 currentEuler = cam.transform.rotation.eulerAngles;

            // Adjust tilt (X rotation) based on vertical mouse movement.
            float currentTilt = currentEuler.x;
            currentTilt -= mouseDeltaY * tiltSpeed;
            currentTilt = Mathf.Clamp(currentTilt, minTilt, maxTilt);

            // Adjust yaw (Y rotation) based on horizontal mouse movement.
            float newYaw = currentEuler.y + mouseDeltaX * yawSpeed;

            cam.transform.rotation = Quaternion.Euler(currentTilt, newYaw, 0f);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (!isResettingRotation)
            {
                StartCoroutine(ResetCameraRotation());
            }
        }
    }

    /// <summary>
    /// Lerps the camera's rotation back to the default rotation (90, 0, 0) over resetRotationDuration seconds.
    /// </summary>
    private IEnumerator ResetCameraRotation()
    {
        isResettingRotation = true;
        Quaternion startRotation = cam.transform.rotation;
        float elapsed = 0f;
        while (elapsed < resetRotationDuration)
        {
            cam.transform.rotation = Quaternion.Lerp(startRotation, defaultRotation, elapsed / resetRotationDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.rotation = defaultRotation;
        isResettingRotation = false;
    }

    /// <summary>
    /// Resets the camera by recomputing level bounds (if levelRoot is assigned) and focusing on the level.
    /// </summary>
    private void ResetCamera()
    {
        if (levelRoot != null)
        {
            Bounds computedBounds = ComputeBoundsFromChildren(levelRoot);
            SetLevelBounds(computedBounds);
        }
        else if (useLevelBounds)
        {
            FocusOnLevel();
        }
    }

    /// <summary>
    /// Coroutine that waits for a specified delay then computes level bounds from levelRoot.
    /// </summary>
    private IEnumerator ComputeLevelBoundsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Bounds computedBounds = ComputeBoundsFromChildren(levelRoot);
        SetLevelBounds(computedBounds);
    }

    /// <summary>
    /// Computes a bounding box that encapsulates all Renderer bounds in the children of the given root.
    /// </summary>
    private Bounds ComputeBoundsFromChildren(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
        {
            bounds.Encapsulate(rend.bounds);
        }
        return bounds;
    }

    /// <summary>
    /// Sets the level boundaries (in world space) and then focuses the camera on the generated level.
    /// Call this from your LevelGenerator after your environment is fully generated.
    /// </summary>
    public void SetLevelBounds(Bounds generatedBounds)
    {
        levelBounds = generatedBounds;
        useLevelBounds = true;
        if (cam == null)
        {
            StartCoroutine(WaitForCameraAndFocus());
        }
        else
        {
            FocusOnLevel();
        }
    }

    private IEnumerator WaitForCameraAndFocus()
    {
        while (cam == null)
            yield return null;
        FocusOnLevel();
    }

    /// <summary>
    /// Centers the camera on the level boundaries and adjusts the orthographic size so that the entire level is in frame.
    /// The camera's Y position remains fixed.
    /// In this version, maxOrthographicSize is updated to the computed required size.
    /// </summary>
    private void FocusOnLevel()
    {
        Vector3 center = levelBounds.center;
        controllerTransform.position = new Vector3(center.x, fixedYPosition, center.z);

        float verticalExtent = levelBounds.extents.z;
        float horizontalExtent = levelBounds.extents.x;
        float aspect = cam.aspect;
        float requiredSize = Mathf.Max(verticalExtent, horizontalExtent / aspect) + focusPadding;

        // Update the maximum orthographic size to the computed required size.
        maxOrthographicSize = requiredSize;
        cam.orthographicSize = Mathf.Clamp(requiredSize, minOrthographicSize, maxOrthographicSize);
    }
}
