using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance;

    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeDuration = 1f;

    private bool _isEntered;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public IEnumerator FadeInOut(Action onMiddleAction)
    {
        _isEntered = true;

        yield return StartCoroutine(Fade(0f, 1f, 0f));

        onMiddleAction?.Invoke();

        _isEntered = false;

        yield return StartCoroutine(Fade(1f, 0f, 2f));
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float waittime)
    {
        if (waittime > 0f)
            yield return new WaitForSeconds(waittime);

        _fadeImage.gameObject.SetActive(true);

        float timer = 0f;
        Color color = _fadeImage.color;

        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / _fadeDuration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            _fadeImage.color = color;

            // FadeIn이 끝나기 0.3초 전에 잠금 해제
            if (!_isEntered && _fadeDuration - timer <= 0.4f)
            {
                MapManager.Instance.Player.Controller.SetEnterPortal(false);
            }

            yield return null;
        }

        color.a = toAlpha;
        _fadeImage.color = color;

        if (toAlpha == 0f)
            _fadeImage.gameObject.SetActive(false);
    }
}
