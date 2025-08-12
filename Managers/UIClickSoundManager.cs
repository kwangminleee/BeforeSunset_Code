using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIClickSoundManager : MonoBehaviour
{
    private void Start()
    {
        AllButtons();
    }

    private void AllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(() => PlayClickSound());
        }
    }

    private void PlayClickSound()
    {
        AudioManager.Instance.PlaySFX("UIClick");
    }
}
