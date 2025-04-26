using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class AlienController : MonoBehaviour
{
    enum State { Seek, Attack, Flee, Wander }

    // Sabotage ranges
    const float ComputerRange = 3f;
    const float BinRange = 5f;

    // Movement speeds
    const float ChaseSpeed = 4f;
    const float WanderSpeed = 2f;
    const float FleeSpeed = 6f;

    // Attack rate (slider units per second)
    const float AttackRate = 0.25f;

    // When no sabotage target: roam radius
    const float WanderRadius = 10f;

    // If an agent is within this distance, trigger flee
    const float ThreatRange = 10f;

    State _state = State.Wander;

    NavMeshAgent _nav;
    GameObject _highlight;

    Slider _targetSlider;
    Component _currentTarget;  // Computer or Bin
    Vector3 _targetPosition;

    // For wandering
    Vector3 _wanderDestination;

    void Awake()
    {
        _nav = GetComponent<NavMeshAgent>();
        _nav.speed = WanderSpeed;

        // Find & hide highlight
        _highlight = transform.Find("HighlightCylinder")?.gameObject;
        if (_highlight != null) _highlight.SetActive(false);
    }

    void Start()
    {
        ChooseWanderDestination();
    }

    void Update()
    {
        // 1) If paused out of any Attack state, stop sabotage
        if (_targetSlider != null && _targetSlider.value <= 0f)
        {
            if (_state == State.Attack && _currentTarget != null)
                StopSabotageOn(_currentTarget);

            _targetSlider = null;
            _currentTarget = null;
            _state = State.Seek;
        }

        // 2) Always check for a valid sabotage target first
        if (_state != State.Attack)
        {
            TryAcquireSabotageTarget();
            if (_targetSlider != null)
                _state = State.Seek;
        }

        // 3) Handle each state
        switch (_state)
        {
            case State.Seek:
                HandleSeek();
                break;
            case State.Attack:
                HandleAttack();
                break;
            case State.Flee:
                HandleFlee();
                break;
            case State.Wander:
                HandleWander();
                break;
        }
    }

    void TryAcquireSabotageTarget()
    {
        // gather all sliders > 0 on Computers and Bins
        var comps = FindObjectsOfType<Computer>()
                    .Select(c => c.GetComponentInChildren<Slider>());

        var bins = FindObjectsOfType<Bin>()
                   .Select(b => b.GetComponentInChildren<Slider>());

        var candidates = comps.Concat(bins)
                              .Where(s => s != null && s.value > 0f)
                              .ToList();

        if (candidates.Any())
        {
            // pick nearest
            _targetSlider = candidates
                .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                .First();
            _targetPosition = _targetSlider.transform.position;
            _nav.speed = ChaseSpeed;
            _nav.SetDestination(_targetPosition);
        }
    }

    void HandleSeek()
    {
        if (_targetSlider == null)
        {
            // no sabotage target → maybe flee or wander
            var nearestAgent = FindNearestAgent();
            if (nearestAgent != null &&
                Vector3.Distance(transform.position, nearestAgent.transform.position) <= ThreatRange)
            {
                _state = State.Flee;
                return;
            }

            _state = State.Wander;
            return;
        }

        float dist = Vector3.Distance(transform.position, _targetPosition);

        bool inRange = (_targetSlider.GetComponentInParent<Computer>() != null && dist <= ComputerRange)
                    || (_targetSlider.GetComponentInParent<Bin>() != null && dist <= BinRange);

        if (!_nav.pathPending && inRange)
        {
            // arrived → start attack
            _state = State.Attack;
            _highlight?.SetActive(true);

            if (_targetSlider.GetComponentInParent<Computer>() is Computer comp)
            {
                _currentTarget = comp;
                comp.StartSabotage();
            }
            else if (_targetSlider.GetComponentInParent<Bin>() is Bin bin)
            {
                _currentTarget = bin;
                bin.StartSabotage();
            }
        }
        else
        {
            // continue chasing
            _nav.speed = ChaseSpeed;
            _nav.SetDestination(_targetPosition);
        }
    }

    void HandleAttack()
    {
        _targetSlider.value = Mathf.Max(0f,
            _targetSlider.value - AttackRate * Time.deltaTime
        );

        if (_targetSlider.value <= 0f)
        {
            _highlight?.SetActive(false);
            if (_currentTarget != null)
                StopSabotageOn(_currentTarget);

            _state = State.Seek;
        }
    }

    void HandleFlee()
    {
        var nearestAgent = FindNearestAgent();
        if (nearestAgent == null || Vector3.Distance(transform.position, nearestAgent.transform.position) > ThreatRange)
        {
            // no longer threatened → go back to seek or wander
            _state = _targetSlider != null ? State.Seek : State.Wander;
            return;
        }

        // run directly away from the closest agent
        Vector3 away = (transform.position - nearestAgent.transform.position).normalized;
        Vector3 fleeTarget = transform.position + away * WanderRadius;

        _nav.speed = FleeSpeed;
        _nav.SetDestination(fleeTarget);
    }

    void HandleWander()
    {
        // if agents nearby, switch to flee
        var nearestAgent = FindNearestAgent();
        if (nearestAgent != null &&
            Vector3.Distance(transform.position, nearestAgent.transform.position) <= ThreatRange)
        {
            _state = State.Flee;
            return;
        }

        // reached wander dest?
        if (!_nav.pathPending && _nav.remainingDistance <= _nav.stoppingDistance + 0.1f)
            ChooseWanderDestination();
    }

    void ChooseWanderDestination()
    {
        Vector3 rnd = Random.insideUnitSphere * WanderRadius + transform.position;
        if (NavMesh.SamplePosition(rnd, out var hit, WanderRadius, NavMesh.AllAreas))
        {
            _wanderDestination = hit.position;
            _nav.speed = WanderSpeed;
            _nav.SetDestination(_wanderDestination);
        }
    }

    Agent FindNearestAgent()
    {
        // assumes your agents have an "Agent" component
        return FindObjectsOfType<Agent>()
            .OrderBy(a => Vector3.Distance(transform.position, a.transform.position))
            .FirstOrDefault();
    }

    void StopSabotageOn(Component target)
    {
        if (target is Computer comp) comp.StopSabotage();
        else if (target is Bin bin) bin.StopSabotage();
    }

    private void OnMouseEnter() => _highlight?.SetActive(true);
    private void OnMouseExit() => _highlight?.SetActive(false);
}
