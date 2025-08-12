using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffect : MonoBehaviour
{
    private float dashImageInterval = 0.05f;
    private float dashImageFadeTime = 0.4f;
    private Color dashImageStartColor = new Color(1f, 1f, 1f, 0.8f);
    private bool useGradientFade = true;

    private SpriteRenderer playerSpriteRenderer;
    private Transform playerTransform;

    private void Awake()
    {
        playerTransform = transform;
        playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void PlayDashEffect(float dashDuration)
    {
        if (playerSpriteRenderer == null || playerSpriteRenderer.sprite == null)
        {
            return;
        }

        StartCoroutine(C_CreateDashImages(dashDuration));
    }

    private IEnumerator C_CreateDashImages(float dashDuration)
    {
        float elapsed = 0f;
        
        while (elapsed < dashDuration)
        {
            CreateDashImage();
            elapsed += dashImageInterval;
            yield return new WaitForSeconds(dashImageInterval);
        }
    }

    private void CreateDashImage()
    {
        if (!ValidateComponents()) return;

        GameObject dashImage = CreateDashImageObject();
        SetupDashImage(dashImage);
        StartCoroutine(C_FadeOutDashImage(dashImage));
    }

    private bool ValidateComponents()
    {
        return playerSpriteRenderer != null &&
               playerTransform != null &&
               playerSpriteRenderer.sprite != null;
    }

    private GameObject CreateDashImageObject()
    {
        GameObject dashImage = new GameObject("DashImage");
        Vector3 pos = playerTransform.position;
        pos.y += 0.7f;
        dashImage.transform.position = pos;
        dashImage.transform.rotation = playerTransform.rotation;
        dashImage.transform.localScale = new Vector3(2.8f, 2.8f, 1f);

        return dashImage;
    }

    private void SetupDashImage(GameObject dashImage)
    {
        SpriteRenderer dashImageSR = dashImage.AddComponent<SpriteRenderer>();

        dashImageSR.sprite = playerSpriteRenderer.sprite;
        dashImageSR.color = dashImageStartColor;

        try
        {
            dashImageSR.sortingLayerName = playerSpriteRenderer.sortingLayerName;
            dashImageSR.sortingOrder = playerSpriteRenderer.sortingOrder - 1;
        }
        catch (System.Exception)
        {
            dashImageSR.sortingLayerName = "Default";
            dashImageSR.sortingOrder = -1;
        }

        dashImageSR.flipX = playerSpriteRenderer.flipX;
        dashImageSR.flipY = playerSpriteRenderer.flipY;

        if (playerSpriteRenderer.material != null)
        {
            dashImageSR.material = playerSpriteRenderer.material;
        }
    }

    private IEnumerator C_FadeOutDashImage(GameObject dashImage)
    {
        SpriteRenderer sr = dashImage.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Destroy(dashImage);
            yield break;
        }

        Color originalColor = sr.color;
        float elapsed = 0f;

        while (elapsed < dashImageFadeTime)
        {
            elapsed += Time.deltaTime;

            if (sr == null) break;

            float progress = elapsed / dashImageFadeTime;

            if (useGradientFade)
            {
                float alpha = Mathf.Lerp(originalColor.a, 0f, Mathf.SmoothStep(0f, 1f, progress));
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            else
            {
                float alpha = Mathf.Lerp(originalColor.a, 0f, progress);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }

            yield return null;
        }

        if (dashImage != null)
        {
            Destroy(dashImage);
        }
    }
}