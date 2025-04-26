using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class AlienController : MonoBehaviour
{
    enum State { Seek, Attack }
    State _state = State.Seek;

    const float repairDistance = 2f;
    NavMeshAgent _nav;
    GameObject _highlight;
    float _attackRate = 0.33f; // drain speed
    Slider _targetSlider;
    Component _currentTarget;  // holds Computer or Bin component
    Vector3 targetPosition;

    void Awake()
    {
        _nav = GetComponent<NavMeshAgent>();
        _nav.stoppingDistance = repairDistance;

        // find and hide highlight cylinder
        _highlight = transform.Find("HighlightCylinder")?.gameObject;
        if (_highlight != null) _highlight.SetActive(false);
    }

    void Start()
    {
        FindNextTarget();
    }

    void Update()
    {
        // 1) no valid target? go back to seek
        if (_targetSlider == null || _targetSlider.value <= 0f)
        {
            if (_state == State.Attack && _currentTarget != null)
                StopSabotageOn(_currentTarget);

            _state = State.Seek;
            FindNextTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetPosition);

        // 2) Seek → Attack when within repairDistance
        if (_state == State.Seek)
        {
            if (!_nav.pathPending && dist <= repairDistance)
            {
                _state = State.Attack;
                if (_highlight != null) _highlight.SetActive(true);

                // start sabotage on the correct component
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
        }
        // 3) Attack (drain) loop
        else if (_state == State.Attack)
        {
            _targetSlider.value = Mathf.Max(0f,
                _targetSlider.value - _attackRate * Time.deltaTime
            );

            if (_targetSlider.value <= 0f)
            {
                if (_highlight != null) _highlight.SetActive(false);
                if (_currentTarget != null)
                    StopSabotageOn(_currentTarget);

                _state = State.Seek;
                FindNextTarget();
            }
        }
    }

    void FindNextTarget()
    {
        // pick nearest computer slider > 0
        var comps = FindObjectsOfType<Computer>()
            .Select(c => c.GetComponentInChildren<Slider>())
            .Where(s => s != null && s.value > 0f)
            .ToList();

        if (comps.Any())
        {
            _targetSlider = comps
                .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                .First();
            targetPosition = _targetSlider.transform.position;
            _nav.SetDestination(targetPosition);
        }
        else
        {
            _targetSlider = null;
            Vector3 rnd = Random.insideUnitSphere * 10f + transform.position;
            if (NavMesh.SamplePosition(rnd, out var hit, 10f, NavMesh.AllAreas))
                _nav.SetDestination(hit.position);
        }

        // reset state
        _currentTarget = null;
        if (_highlight != null) _highlight.SetActive(false);
    }

    void StopSabotageOn(Component target)
    {
        if (target is Computer comp)
            comp.StopSabotage();
        else if (target is Bin bin)
            bin.StopSabotage();
    }

    // —— Mouse Hover & Cursor ——
    private void OnMouseEnter()
    {
        if (_highlight != null)
            _highlight.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (_highlight != null)
            _highlight.SetActive(false);
    }
}
