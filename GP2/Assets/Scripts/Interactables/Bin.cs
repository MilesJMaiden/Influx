// Bin.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Bin : Interactable
{
    private float _currentFuel = 1f;
    private Slider _slider;
    private Coroutine _depleteCoroutine;
    private Coroutine _sabotageCoroutine;
    private bool _isSabotaged;

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
        _slider.value = _currentFuel;

        _depleteCoroutine = StartCoroutine(DepleteFuelRoutine());
    }

    private IEnumerator DepleteFuelRoutine()
    {
        while (true)
        {
            if (!_isSabotaged)
            {
                yield return new WaitForSeconds(1f);
                _currentFuel = Mathf.Clamp01(_currentFuel - 0.01f);
                _slider.value = _currentFuel;
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary>Called by an Agent when depositing refined rock.</summary>
    public void AddFuel(float amount)
    {
        _currentFuel = Mathf.Clamp01(_currentFuel + amount);
        if (_slider != null)
            _slider.value = _currentFuel;
    }

    /// <summary>
    /// Begin sabotage: overrides normal depletion and drains at 0.25/sec.
    /// </summary>
    public void StartSabotage()
    {
        if (_isSabotaged) return;
        _isSabotaged = true;
        if (_depleteCoroutine != null)
        {
            StopCoroutine(_depleteCoroutine);
            _depleteCoroutine = null;
        }
        _sabotageCoroutine = StartCoroutine(SabotageRoutine());
    }

    /// <summary>End sabotage and resume normal behavior.</summary>
    public void StopSabotage()
    {
        if (!_isSabotaged) return;
        _isSabotaged = false;
        if (_sabotageCoroutine != null)
        {
            StopCoroutine(_sabotageCoroutine);
            _sabotageCoroutine = null;
        }
        if (_depleteCoroutine == null)
            _depleteCoroutine = StartCoroutine(DepleteFuelRoutine());
    }

    private IEnumerator SabotageRoutine()
    {
        const float rate = 0.25f; // per second
        while (_isSabotaged && this.enabled)
        {
            _currentFuel = Mathf.Max(0f, _currentFuel - rate * Time.deltaTime);
            _slider.value = _currentFuel;
            yield return null;
        }
    }

    private void OnDisable()
    {
        if (_isSabotaged)
            StopSabotage();
    }

    protected override void OnClicked()
    {
        Debug.Log("Bin clicked!");
        var agent = Agent.SelectedAgent;
        if (agent != null && agent.IsCarryingRefined)
            agent.CommandDeposit(this);
    }
}
