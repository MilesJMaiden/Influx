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

    NavMeshAgent _nav;
    GameObject _highlight;
    float _attackRate = 0.33f; // drain speed
    Slider _targetSlider;
    Component _currentTarget;  // holds Computer or Bin component

    void Awake()
    {
        _nav = GetComponent<NavMeshAgent>();

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
        if (_targetSlider == null || _targetSlider.value <= 0f)
        {
            // if we were attacking something, stop its sabotage
            if (_state == State.Attack && _currentTarget != null)
                StopSabotageOn(_currentTarget);

            _state = State.Seek;
            FindNextTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetPosition);

        if (_state == State.Seek)
        {
            if (!_nav.pathPending && dist <= _nav.stoppingDistance + 0.1f)
            {
                // arrived → start attack
                _state = State.Attack;
                if (_highlight != null) _highlight.SetActive(true);

                // identify the parent Interactable we're attacking
                var comp = _targetSlider.GetComponentInParent<Computer>();
                if (comp != null)
                {
                    _currentTarget = comp;
                    comp.StartSabotage();
                }
                else
                {
                    var bin = _targetSlider.GetComponentInParent<Bin>();
                    if (bin != null)
                    {
                        _currentTarget = bin;
                        bin.StartSabotage();
                    }
                }
            }
        }
        else if (_state == State.Attack)
        {
            // drain!
            _targetSlider.value = Mathf.Max(0f,
                _targetSlider.value - _attackRate * Time.deltaTime
            );

            if (_targetSlider.value <= 0f)
            {
                if (_highlight != null) _highlight.SetActive(false);

                // stop sabotage on this target
                if (_currentTarget != null)
                    StopSabotageOn(_currentTarget);

                _state = State.Seek;
                FindNextTarget();
            }
        }
    }

    Vector3 targetPosition;
    void FindNextTarget()
    {
        // look for any computer sliders > 0
        var comps = FindObjectsOfType<Computer>()
            .Select(c => c.GetComponentInChildren<Slider>())
            .Where(s => s != null && s.value > 0f)
            .ToList();

        if (comps.Any())
        {
            // nearest
            _targetSlider = comps
                .OrderBy(s => Vector3.Distance(transform.position, s.transform.position))
                .First();
            targetPosition = _targetSlider.transform.position;
            _nav.SetDestination(targetPosition);
        }
        else
        {
            // fallback: wander randomly
            _targetSlider = null;
            Vector3 rnd = Random.insideUnitSphere * 10f + transform.position;
            if (NavMesh.SamplePosition(rnd, out var hit, 10f, NavMesh.AllAreas))
                _nav.SetDestination(hit.position);
        }

        // reset highlight and current target when seeking
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
