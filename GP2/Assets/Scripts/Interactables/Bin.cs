// Bin.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Bin : Interactable
{
    private float _currentFuel = 1f;
    private Slider _slider;
    private Coroutine _depletionCoroutine;

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

        _depletionCoroutine = StartCoroutine(DepleteFuelRoutine());
    }

    private IEnumerator DepleteFuelRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            _currentFuel = Mathf.Clamp01(_currentFuel - 0.01f);
            _slider.value = _currentFuel;
        }
    }

    /// <summary>
    /// Called by an Agent when depositing refined rock.
    /// </summary>
    public void AddFuel(float amount)
    {
        _currentFuel = Mathf.Clamp01(_currentFuel + amount);
        if (_slider != null)
            _slider.value = _currentFuel;
    }

    protected override void OnClicked()
    {
        Debug.Log("Bin clicked!");
        var agent = Agent.SelectedAgent;
        if (agent != null && agent.IsCarryingRefined)
        {
            agent.CommandDeposit(this);
        }
    }
}
