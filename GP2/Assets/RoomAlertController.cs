using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RoomAlertController : MonoBehaviour
{
    GameObject _alertLight;
    List<Slider> _computerSliders;
    Material _mat;
    Coroutine _flashRoutine;

    AudioSource _audioSource;
    AudioClip _alertClip;

    /// <summary>
    /// Call once right after Instantiate:
    /// </summary>
    public void Initialize(GameObject alertLight, List<Slider> computerSliders, AudioClip alertClip)
    {
        _alertLight = alertLight;
        _computerSliders = computerSliders ?? new List<Slider>();
        _alertClip = alertClip;

        // clone the material so we can fade its alpha independently
        var mr = _alertLight.GetComponentInChildren<MeshRenderer>();
        _mat = new Material(mr.sharedMaterial);
        mr.material = _mat;

        // find the scene’s AudioSource named "Audio Source", or fall back to the main camera
        var audioGO = GameObject.Find("Audio Source") ?? Camera.main?.gameObject;
        if (audioGO != null)
            _audioSource = audioGO.GetComponent<AudioSource>();
        if (_audioSource == null)
            Debug.LogError("RoomAlertController: no AudioSource found in scene!");
    }

    void Update()
    {
        // nothing to do until we have at least one valid slider
        if (_computerSliders == null || !_computerSliders.Any(s => s != null))
            return;

        bool allZero = _computerSliders
            .Where(s => s != null)
            .All(s => s.value <= 0f);

        // turn on alert
        if (allZero && !_alertLight.activeSelf)
        {
            _alertLight.SetActive(true);
            _flashRoutine = StartCoroutine(FlashAlpha());

            if (_audioSource != null && _alertClip != null)
            {
                _audioSource.clip = _alertClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }
        // turn off alert
        else if (!allZero && _alertLight.activeSelf)
        {
            _alertLight.SetActive(false);
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.loop = false;
            }
        }
    }

    IEnumerator FlashAlpha()
    {
        const float minA = 40f / 255f;
        const float dur = 1.5f;
        while (true)
        {
            yield return LerpAlpha(minA, 0f, dur);
            yield return LerpAlpha(0f, minA, dur);
        }
    }

    IEnumerator LerpAlpha(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / dur);
            var c = _mat.color;
            c.a = a;
            _mat.color = c;
            yield return null;
        }
    }
}
