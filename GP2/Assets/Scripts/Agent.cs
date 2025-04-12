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
    private NavMeshAgent navAgent;
    private AgentState currentState;
    private float stateTimer;

    // Duration to remain Idle before transitioning.
    public float idleDuration = 2f;
    // Radius within which the agent picks random destinations while wandering.
    public float wanderRadius = 10f;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Delay starting the agent's behavior until it is on a valid NavMesh.
        StartCoroutine(EnableAgentWhenReady());
    }

    /// <summary>
    /// Waits until the agent is on a NavMesh before starting the FSM.
    /// </summary>
    private IEnumerator EnableAgentWhenReady()
    {
        // Continue waiting until the agent's NavMeshAgent is on a NavMesh.
        while (!navAgent.isOnNavMesh)
        {
            yield return null;
        }
        TransitionToState(AgentState.Idle);
    }

    private void Update()
    {
        // Guard: if the agent is not on a NavMesh, do not process further.
        if (!navAgent.isOnNavMesh)
        {
            return;
        }

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
                // Ensure path is complete before checking remainingDistance.
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    TransitionToState(AgentState.Idle);
                }
                break;
        }
    }

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
        // Choose a random direction within wanderRadius.
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
        }
    }
}
