using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cinemachine;

public class CameraZoomController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCam;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomSmoothSpeed = 0.2f;

    [Header("Focus Settings")]
    [SerializeField] private float focusZoom = 5f;
    [SerializeField] private float focusDuration = 0.5f;

    private float _targetZoom;
    private float _currentZoom;
    private bool _isFocusing;


    private void Start()
    {
        if (virtualCam == null)
        {
            Debug.LogWarning("Virtual Camera 연결되지않음");
            return;
        }

        _currentZoom = virtualCam.m_Lens.OrthographicSize;
        _targetZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
        virtualCam.m_Lens.OrthographicSize = _targetZoom;
    }

    private void Update()
    {
        if (virtualCam == null && !_isFocusing) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetZoom -= scroll * zoomSpeed * 0.1f;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, zoomSmoothSpeed);

        var lens = virtualCam.m_Lens;
        lens.OrthographicSize = _currentZoom;
        virtualCam.m_Lens = lens;
    }

    public void FocusGameOver(Transform target, Action onComplete = null)
    {
        if (virtualCam == null || target == null) return;
        StopAllCoroutines();
        StartCoroutine(GameOverRoutine(target, onComplete));
    }

    private IEnumerator GameOverRoutine(Transform target, Action onComplete, float pixelThreshold = 8f, float timeout = 3f)
    {
        _isFocusing = true;      
        virtualCam.Follow = target;

        var framing = virtualCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framing != null)
        {
            framing.m_DeadZoneWidth = 0f;
            framing.m_DeadZoneHeight = 0f;
        }

        yield return null;

        var cam = Camera.main;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float t = 0f;

        while (t < timeout)
        {
            if (target == null || cam == null) break;

            Vector2 p = cam.WorldToScreenPoint(target.position);
            if ((p - screenCenter).sqrMagnitude <= pixelThreshold * pixelThreshold)
                break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _isFocusing = false;
        onComplete?.Invoke();
    }
}
