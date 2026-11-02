using TMPro;
using UnityEngine;

public class HeroSelectionUI : MonoBehaviour
{
    [SerializeField] private HeroData hero1;
    [SerializeField] private HeroData hero2;
    [SerializeField] private HeroData hero3;

    [SerializeField] private TMP_Text selectedHeroText;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TMP_Text descriptionText;

    private HeroData selectedHero;

    public void SelectHero(int index)
    {
        HeroData hero = null;

        if (index == 0)
        {
            hero = hero1;
        }
        else if (index == 1)
        {
            hero = hero2;
        }
        else if (index == 2)
        {
            hero = hero3;
        }
        else
        {
            Debug.LogWarning("Invalid hero index: " + index);
            return;
        }

        if (hero == null)
        {
            Debug.LogError("Hero " + (index + 1) + " is not assigned.");
            return;
        }

        selectedHero = hero;

        if (selectedHeroText != null)
        {
            selectedHeroText.text = "Selected: " + selectedHero.Character;
        }

        if (backgroundRenderer != null)
        {
            if (selectedHero.SelectionBackground != null)
            {
                backgroundRenderer.sprite = selectedHero.SelectionBackground;
                FitBackgroundToCamera();
            }
            else
            {
                Debug.LogError("Selection background is not assigned for hero " + (index + 1) + ".");
            }
        }
        else
        {
            Debug.LogError("Background Renderer is not assigned in HeroSelectionUI.");
        }

        if (descriptionText != null)
        {
            descriptionText.text = selectedHero.Description;
        }
        else
        {
            Debug.LogError("Description Text is not assigned in HeroSelectionUI.");
        }
    }

    private void FitBackgroundToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("A Main Camera in orthographic mode is required to fit the hero background.");
            return;
        }

        Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            Debug.LogError("The selected hero background has invalid dimensions.");
            return;
        }

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        float coverScale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
        Vector3 parentScale = backgroundRenderer.transform.parent != null
            ? backgroundRenderer.transform.parent.lossyScale
            : Vector3.one;

        if (Mathf.Approximately(parentScale.x, 0f) || Mathf.Approximately(parentScale.y, 0f))
        {
            Debug.LogError("The background parent must have a non-zero scale.");
            return;
        }

        backgroundRenderer.transform.localScale = new Vector3(
            coverScale / parentScale.x,
            coverScale / parentScale.y,
            backgroundRenderer.transform.localScale.z);
    }

    public bool StartSelectedRun()
    {
        if (selectedHero == null)
        {
            Debug.LogWarning("Please select a hero first.");
            return false;
        }

        if (RunSystem.Instance == null)
        {
            Debug.LogError("RunSystem is missing in SelectHeroScene.");
            return false;
        }

        RunSystem.Instance.StartNewRun(selectedHero);
        return true;
    }
}