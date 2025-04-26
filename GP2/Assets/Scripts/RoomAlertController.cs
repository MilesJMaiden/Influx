// RoomAlertController.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Transform))]
public class RoomAlertController : MonoBehaviour
{
    GameObject _alertLight;
    List<Slider> _computerSliders;
    Material _mat;
    Coroutine _flashRoutine;
    AudioSource _audioSource;
    AudioClip _alertClip;
    GameObject _alienPrefab;

    // track which containers we've already spawned for
    HashSet<Container> _spawned = new HashSet<Container>();

    public void Initialize(
        GameObject alertLight,
        List<Slider> computerSliders,
        AudioClip alertClip,
        GameObject alienPrefab)
    {
        _alertLight = alertLight;
        _computerSliders = computerSliders ?? new List<Slider>();
        _alertClip = alertClip;
        _alienPrefab = alienPrefab;

        // clone material so each room fades independently
        var mr = _alertLight.GetComponentInChildren<MeshRenderer>();
        _mat = new Material(mr.sharedMaterial);
        mr.material = _mat;

        // find your global AudioSource
        var audioGO = GameObject.Find("Audio Source") ?? Camera.main?.gameObject;
        if (audioGO != null) _audioSource = audioGO.GetComponent<AudioSource>();
        if (_audioSource == null)
            Debug.LogError("RoomAlertController: no AudioSource named 'Audio Source' found!");
    }

    void Update()
    {
        if (_computerSliders == null || !_computerSliders.Any(s => s != null))
            return;

        bool allZero = _computerSliders
            .Where(s => s != null)
            .All(s => s.value <= 0f);

        // alert starts
        if (allZero && !_alertLight.activeSelf)
        {
            // 1) enable visuals & audio
            _alertLight.SetActive(true);
            _flashRoutine = StartCoroutine(FlashAlpha());
            if (_audioSource != null && _alertClip != null)
            {
                _audioSource.clip = _alertClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }

            // 2) spawn one alien per Container
            foreach (var container in GetComponentsInChildren<Container>())
            {
                if (!_spawned.Contains(container) &&
                    container.alienSpawnPoint != null)
                {
                    var pos = container.alienSpawnPoint.transform.position;
                    var alien = Instantiate(_alienPrefab, pos, Quaternion.identity);
                    alien.transform.SetParent(transform, true);
                    _spawned.Add(container);
                }
            }
        }
        // alert ends
        else if (!allZero && _alertLight.activeSelf)
        {
            _alertLight.SetActive(false);
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.loop = false;
            }

            // clear so next alert respawns at all containers
            _spawned.Clear();
        }
    }

    IEnumerator FlashAlpha()
    {
        const float minA = 40f / 255f, dur = 1.5f;
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
            var c = _mat.color;
            c.a = Mathf.Lerp(from, to, t / dur);
            _mat.color = c;
            yield return null;
        }
    }
}
