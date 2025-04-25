using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Computer : Interactable
{
    private Slider _slider;
    private Coroutine _depletionCoroutine;
    private Coroutine _sabotageCoroutine;
    private bool _isUnderRepair;
    private bool _isSabotaged;

    // Expose for agents
    public bool IsUnderRepair
    {
        get => _isUnderRepair;
        private set => _isUnderRepair = value;
    }

    private void Start()
    {
        _slider = GetComponentInChildren<Slider>();
        if (_slider == null)
        {
            Debug.LogError($"{name}: no Slider found in children!");
            enabled = false;
            return;
        }

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.value = 1f;

        _depletionCoroutine = StartCoroutine(DepletionRoutine());
    }

    private IEnumerator DepletionRoutine()
    {
        while (true)
        {
            // pause if under repair or sabotaged
            if (IsUnderRepair || _isSabotaged)
            {
                yield return null;
                continue;
            }

            // 1) WAIT 5s
            float waited = 0f;
            while (waited < 5f)
            {
                if (!IsUnderRepair && !_isSabotaged)
                    waited += Time.deltaTime;
                yield return null;
            }

            if (IsUnderRepair || _isSabotaged)
                continue;

            // 2) 33% chance to drain
            if (Random.value <= 0.33f)
            {
                float duration = 10f, elapsed = 0f;
                float startVal = _slider.value;

                while (elapsed < duration && !_isSabotaged && !IsUnderRepair)
                {
                    elapsed += Time.deltaTime;
                    _slider.value = Mathf.Lerp(startVal, 0f, elapsed / duration);
                    yield return null;
                }

                if (!_isSabotaged && !IsUnderRepair)
                {
                    _slider.value = 0f;
                    Debug.Log($"{name} has depleted");
                }
            }

            yield return null;
        }
    }

    /// <summary>Pause depletion (agent started repairing).</summary>
    public void NotifyRepairStart()
    {
        if (IsUnderRepair) return;

        IsUnderRepair = true;
        if (_depletionCoroutine != null)
        {
            StopCoroutine(_depletionCoroutine);
            _depletionCoroutine = null;
        }
    }

    /// <summary>Called when repair finishes to restore to full and resume normal depletion.</summary>
    public void NotifyRepairEnd()
    {
        if (!IsUnderRepair) return;

        IsUnderRepair = false;

        // fully repaired: reset to full
        _slider.value = 1f;

        if (_depletionCoroutine == null)
            _depletionCoroutine = StartCoroutine(DepletionRoutine());
    }

    /// <summary>
    /// Called when we abort a repair (e.g. user deselects the agent).
    /// Resumes normal depletion but does NOT reset the slider to full.
    /// </summary>
    public void CancelRepair()
    {
        if (!_isUnderRepair) return;
        _isUnderRepair = false;

        // restart the depletion coroutine from whatever value it currently has
        if (_depletionCoroutine == null)
            _depletionCoroutine = StartCoroutine(DepletionRoutine());
    }

    /// <summary>
    /// Begin sabotage: overrides all other behaviors and drains at 0.2/sec.
    /// </summary>
    public void StartSabotage()
    {
        if (_isSabotaged) return;
        _isSabotaged = true;

        if (_depletionCoroutine != null)
        {
            StopCoroutine(_depletionCoroutine);
            _depletionCoroutine = null;
        }

        _sabotageCoroutine = StartCoroutine(SabotageRoutine());
    }

    /// <summary>
    /// End sabotage and resume normal behavior.
    /// </summary>
    public void StopSabotage()
    {
        if (!_isSabotaged) return;
        _isSabotaged = false;

        if (_sabotageCoroutine != null)
        {
            StopCoroutine(_sabotageCoroutine);
            _sabotageCoroutine = null;
        }

        if (_depletionCoroutine == null && !IsUnderRepair)
            _depletionCoroutine = StartCoroutine(DepletionRoutine());
    }

    private IEnumerator SabotageRoutine()
    {
        const float rate = 0.2f; // per second
        while (_isSabotaged && this.enabled)
        {
            _slider.value = Mathf.Max(0f, _slider.value - rate * Time.deltaTime);
            yield return null;
        }
    }

    private void OnDisable()
    {
        // if destroyed/disabled while sabotaging, stop it
        if (_isSabotaged)
            StopSabotage();
    }

    protected override void OnClicked()
    {
        var agent = Agent.SelectedAgent;
        if (agent != null)
            agent.CommandRepair(this);
    }
}
