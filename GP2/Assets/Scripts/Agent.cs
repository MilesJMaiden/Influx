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

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // Attempt to find the child named "SelectedCylinder"
        Transform indicator = transform.Find("SelectedCylinder");
        if (indicator != null)
        {
            selectedIndicator = indicator.gameObject;
            selectedIndicator.SetActive(false);
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
        // If no agent is currently selected, start the FSM.
        if (SelectedAgent == null)
        {
            TransitionToState(AgentState.Idle);
        }
    }

    private void Update()
    {
        // If this agent is the selected agent, override FSM behavior.
        if (SelectedAgent == this)
        {
            HandleUserInput();
            // Do not process FSM state transitions.
            return;
        }

        // Normal FSM behavior.
        stateTimer -= Time.deltaTime;
        switch (currentState)
        {
            case AgentState.Idle:
                if (stateTimer <= 0f)
                {
                    TransitionToState(AgentState.Wander);
                }
                break;
            case AgentState.Wander:
                // Once the agent has reached its destination (or close enough) transition to Idle.
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    TransitionToState(AgentState.Idle);
                }
                break;
        }
    }

    /// <summary>
    /// Handles user input when this agent is selected.
    /// Q key: Deselect the agent.
    /// Left mouse click on the floor: Set the agent's destination to that point.
    /// </summary>
    private void HandleUserInput()
    {
        // Deselect when the user presses Q.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Deselect();
            return;
        }

        // If left mouse button is pressed, perform a raycast to the floor.
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // You may adjust the maxDistance and layer mask as necessary.
            if (Physics.Raycast(ray, out hit, 100f))
            {
                // Check if the hit object is tagged as "Floor".
                if (hit.collider.CompareTag("Floor"))
                {
                    navAgent.SetDestination(hit.point);
                }
            }
        }
    }

    /// <summary>
    /// Called when this agent is clicked.
    /// Sets this agent as the selected agent.
    /// </summary>
    private void OnMouseDown()
    {
        // If this agent is already selected, do nothing.
        if (SelectedAgent == this)
            return;

        // If another agent is selected, deselect it.
        if (SelectedAgent != null)
            SelectedAgent.Deselect();

        Select();
    }

    /// <summary>
    /// Selects this agent and enables its selection indicator.
    /// </summary>
    private void Select()
    {
        SelectedAgent = this;
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(true);
        }
        // Stop any current navigation.
        navAgent.ResetPath();
    }

    /// <summary>
    /// Deselects this agent and disables its selection indicator.
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
            // Restart the FSM.
            TransitionToState(AgentState.Idle);
        }
    }

    /// <summary>
    /// Transitions the agent to the specified FSM state.
    /// </summary>
    /// <param name="newState">The new state to enter.</param>
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
    /// Sets a random destination within wanderRadius and assigns it to the NavMeshAgent.
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
