using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Table : Interactable
{
    private GameObject _rockObject;
    private Slider _slider;
    private bool _isProcessing = false;

    protected override void Awake()
    {
        // ensure Interactable.Awake runs and _highlight is hooked up
        base.Awake();

        // cache the Rock child
        _rockObject = transform.Find("Rock")?.gameObject;

        // cache the Slider in our Canvas (include inactive!)
        _slider = GetComponentInChildren<Slider>(true);
        if (_slider == null)
        {
            Debug.LogError($"{name}: no Slider found in children!");
        }
        else
        {
            _slider.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // as soon as Rock is enabled and we're not already processing...
        if (_rockObject != null
            && _rockObject.activeSelf
            && !_isProcessing
            && _slider != null)
        {
            _isProcessing = true;

            // initialize slider
            _slider.value = 0f;
            _slider.gameObject.SetActive(true);

            StartCoroutine(ProcessRoutine());
        }
    }

    private IEnumerator ProcessRoutine()
    {
        float elapsed = 0f;
        const float duration = 4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _slider.value = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        // done
        _slider.gameObject.SetActive(false);
        _slider.value = 0f;
        _rockObject.SetActive(false);
        _isProcessing = false;
    }

    protected override void OnClicked()
    {
        Debug.Log("Table clicked!");
    }
}
