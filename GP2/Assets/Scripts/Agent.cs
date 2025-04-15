using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum AgentState
{
    Idle,
    Wander
}

[RequireComponent(typeof(NavMeshAgent))]
public class Agent : MonoBehaviour
{
    /// <summary>
    /// Holds the currently selected agent. Only one agent can be selected at a time.
    /// </summary>
    public static Agent SelectedAgent { get; private set; }

    private NavMeshAgent navAgent;
    private AgentState currentState;
    private float stateTimer;

    [Header("FSM Settings")]
    [Tooltip("Duration in seconds to remain Idle before transitioning to Wander.")]
    public float idleDuration = 2f;
    [Tooltip("Radius within which the agent picks a random destination while wandering.")]
    public float wanderRadius = 10f;

    // Cached reference to the selection indicator child (named "SelectedCylinder").
    private GameObject selectedIndicator;
    // Cached reference to the highlight indicator child (named "HighlightCylinder").
    private GameObject highlightIndicator;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // Find and cache the "SelectedCylinder" child.
        Transform sel = transform.Find("SelectedCylinder");
        if (sel != null)
        {
            selectedIndicator = sel.gameObject;
            selectedIndicator.SetActive(false);
        }

        // Find and cache the "HighlightCylinder" child.
        Transform high = transform.Find("HighlightCylinder");
        if (high != null)
        {
            highlightIndicator = high.gameObject;
            highlightIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        // Delay starting the FSM until the agent is on a valid NavMesh.
        StartCoroutine(EnableAgentWhenReady());
    }

    private IEnumerator EnableAgentWhenReady()
    {
        while (!navAgent.isOnNavMesh)
        {
            yield return null;
        }
        // Begin FSM if no agent is currently selected.
        if (SelectedAgent == null)
        {
            TransitionToState(AgentState.Idle);
        }
    }

    private void Update()
    {
        // If this agent is selected, override normal FSM with user input.
        if (SelectedAgent == this)
        {
            HandleUserInput();
            return;
        }

        stateTimer -= Time.deltaTime;
        switch (currentState)
        {
            case AgentState.Idle:
                if (stateTimer <= 0f)
                    TransitionToState(AgentState.Wander);
                break;
            case AgentState.Wander:
                // Transition to Idle when the destination is reached.
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                    TransitionToState(AgentState.Idle);
                break;
        }
    }

    // --------------- Mouse Hover for Cursor Change ---------------

    /// <summary>
    /// Called when the mouse enters this agent's collider.
    /// If this agent is not selected, it enables its highlight indicator
    /// and signals the UICursorManager to change the cursor image.
    /// </summary>
    private void OnMouseEnter()
    {
        if (SelectedAgent != this)
        {
            if (highlightIndicator != null)
                highlightIndicator.SetActive(true);

            if (UICursorManager.Instance != null)
                UICursorManager.Instance.SetHoverCursor();
        }
    }

    /// <summary>
    /// Called when the mouse exits this agent's collider.
    /// Disables the highlight indicator and signals the UICursorManager to reset the cursor image.
    /// </summary>
    private void OnMouseExit()
    {
        if (SelectedAgent != this)
        {
            if (highlightIndicator != null)
                highlightIndicator.SetActive(false);

            if (UICursorManager.Instance != null)
                UICursorManager.Instance.ResetCursorImage();
        }
    }

    // --------------- Selection and User Input ---------------

    /// <summary>
    /// Called when this agent is clicked.
    /// If this agent is not already selected, it becomes the selected agent.
    /// </summary>
    private void OnMouseDown()
    {
        if (SelectedAgent == this)
            return;

        if (SelectedAgent != null)
            SelectedAgent.Deselect();

        Select();
    }

    /// <summary>
    /// Selects this agent, enables its selection indicator, disables its highlight indicator,
    /// and resets its navigation.
    /// </summary>
    private void Select()
    {
        SelectedAgent = this;
        if (selectedIndicator != null)
            selectedIndicator.SetActive(true);
        if (highlightIndicator != null)
            highlightIndicator.SetActive(false);
        navAgent.ResetPath();

        // Also, ensure the cursor reverts to its default image.
        if (UICursorManager.Instance != null)
            UICursorManager.Instance.ResetCursorImage();
    }

    /// <summary>
    /// Deselects this agent, disables its selection indicator, and restarts its FSM.
    /// </summary>
    public void Deselect()
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
        if (SelectedAgent == this)
        {
            SelectedAgent = null;
            TransitionToState(AgentState.Idle);
        }
    }

    /// <summary>
    /// Handles user input when this agent is selected.
    /// Q key deselects, left mouse click on a "Floor" sets a destination.
    /// </summary>
    private void HandleUserInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Deselect();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    navAgent.SetDestination(hit.point);
                }
            }
        }
    }

    // --------------- FSM Methods ---------------

    private void TransitionToState(AgentState newState)
    {
        currentState = newState;
        if (newState == AgentState.Idle)
        {
            stateTimer = idleDuration;
            navAgent.ResetPath();
        }
        else if (newState == AgentState.Wander)
        {
            SetRandomDestination();
        }
    }

    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
        }
    }
}
