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
    /// Holds the currently selected agent.
    /// Only one agent can be selected at a time.
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

        // Find the child "SelectedCylinder" and disable it.
        Transform sel = transform.Find("SelectedCylinder");
        if (sel != null)
        {
            selectedIndicator = sel.gameObject;
            selectedIndicator.SetActive(false);
        }

        // Find the child "HighlightCylinder" and disable it.
        Transform high = transform.Find("HighlightCylinder");
        if (high != null)
        {
            highlightIndicator = high.gameObject;
            highlightIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        // Delay starting the agent's behavior until it's on a valid NavMesh.
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

    // ---------------- Mouse Over Highlighting ----------------

    /// <summary>
    /// Called when the mouse cursor enters this agent's collider.
    /// If this agent is not selected, the "HighlightCylinder" child is enabled.
    /// </summary>
    private void OnMouseEnter()
    {
        if (SelectedAgent != this && highlightIndicator != null)
        {
            highlightIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the mouse cursor exits this agent's collider.
    /// Disables the "HighlightCylinder" child.
    /// </summary>
    private void OnMouseExit()
    {
        if (highlightIndicator != null)
        {
            highlightIndicator.SetActive(false);
        }
    }

    // ---------------- Selection and User Input ----------------

    /// <summary>
    /// Called when this agent is clicked.
    /// If it is not already selected, it becomes the selected agent.
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
    /// Selects this agent. Enables its selection indicator and stops its current navigation.
    /// </summary>
    private void Select()
    {
        SelectedAgent = this;
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(true);
        }
        // Disable the highlight when selected.
        if (highlightIndicator != null)
        {
            highlightIndicator.SetActive(false);
        }
        navAgent.ResetPath();
    }

    /// <summary>
    /// Deselects this agent. Disables its selection indicator and restarts the FSM.
    /// </summary>
    public void Deselect()
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(false);
        }
        if (SelectedAgent == this)
        {
            SelectedAgent = null;
            TransitionToState(AgentState.Idle);
        }
    }

    /// <summary>
    /// Handles user input when this agent is selected.
    /// Q key deselects the agent, and a left mouse click on a floor (tagged "Floor") causes the agent to move there.
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

    // ---------------- FSM Methods ----------------

    /// <summary>
    /// Transitions the agent to the specified state.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
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

    /// <summary>
    /// Sets a random destination for the agent within wanderRadius.
    /// </summary>
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
