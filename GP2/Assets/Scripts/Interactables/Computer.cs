// Computer.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Computer : Interactable
{
    private Slider _slider;
    private Coroutine _depletionCoroutine;

    // Expose for agents
    public bool IsUnderRepair { get; private set; }

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
            // 1) WAIT 5s (paused if under repair)
            float waited = 0f;
            while (waited < 5f)
            {
                if (!IsUnderRepair)
                    waited += Time.deltaTime;
                yield return null;
            }

            if (IsUnderRepair)
                continue;  // restart wait

            // 2) 33% chance to drain
            if (Random.value <= 0.33f)
            {
                float duration = 10f, elapsed = 0f;
                float startVal = _slider.value;

                while (elapsed < duration)
                {
                    if (!IsUnderRepair)
                    {
                        elapsed += Time.deltaTime;
                        _slider.value = Mathf.Lerp(startVal, 0f, elapsed / duration);
                    }
                    yield return null;
                }

                _slider.value = 0f;
                Debug.Log($"{name} has depleted");
            }
            else
            {
                // 3) Reset instantly to full
                _slider.value = 1f;
            }
        }
    }

    /// <summary>Called by Agent to pause depletion.</summary>
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

    /// <summary>Called by Agent when repair completes to resume depletion.</summary>
    public void NotifyRepairEnd()
    {
        if (!IsUnderRepair) return;

        IsUnderRepair = false;
        if (_depletionCoroutine == null)
            _depletionCoroutine = StartCoroutine(DepletionRoutine());
    }

    protected override void OnClicked()
    {
        var agent = Agent.SelectedAgent;
        if (agent != null)
            agent.CommandRepair(this);
    }
}
