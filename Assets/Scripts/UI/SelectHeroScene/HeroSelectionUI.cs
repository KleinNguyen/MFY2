using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

public class HeroSelectionUI : MonoBehaviour
{
    [SerializeField] private HeroData hero1;
    [SerializeField] private HeroData hero2;
    [SerializeField] private HeroData hero3;

    [SerializeField] private TMP_Text selectedHeroText;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image themePanel;
    [SerializeField] private Color redPanelColor = new(0.32f, 0.07f, 0.09f, 0.9f);
    [SerializeField] private Color greenPanelColor = new(0.06f, 0.24f, 0.14f, 0.9f);
    [SerializeField] private Color yellowPanelColor = new(0.3f, 0.21f, 0.04f, 0.9f);
    [SerializeField] private Shader roundedCornerShader;
    [SerializeField, Range(0.01f, 0.5f)] private float cornerRadius = 0.1f;
    [SerializeField, Range(0f, 0.1f)] private float selectedBorderWidth = 0.035f;
    [SerializeField] private Color selectedBorderColor = new(1f, 0.9f, 0.05f, 1f);

    private Button[] heroButtons;
    private HeroData selectedHero;
    private readonly List<Material> roundedCornerMaterials = new();

    private void Awake()
    {
        heroButtons = GetComponentsInChildren<Button>(true);
        ApplyRoundedCorners();
    }

    private void OnDestroy()
    {
        foreach (Material material in roundedCornerMaterials)
        {
            Destroy(material);
        }
    }

    private void Start()
    {
        SelectHero(0);
    }

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
        UpdateSelectionFrame(index);
        UpdateThemePanel(selectedHero.Character);

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
            descriptionText.alignment = TextAlignmentOptions.TopJustified;
            descriptionText.text = selectedHero.Description +
                "\n\n<align=left>Health: " + selectedHero.Health +
                "\nMana: " + selectedHero.StartingMana + "</align>";
        }
        else
        {
            Debug.LogError("Description Text is not assigned in HeroSelectionUI.");
        }
    }

    private void UpdateThemePanel(CharacterType character)
    {
        if (themePanel == null)
        {
            Debug.LogError("Theme Panel is not assigned in HeroSelectionUI.");
            return;
        }

        switch (character)
        {
            case CharacterType.Red:
                themePanel.color = redPanelColor;
                break;
            case CharacterType.Green:
                themePanel.color = greenPanelColor;
                break;
            case CharacterType.Yellow:
                themePanel.color = yellowPanelColor;
                break;
            default:
                Debug.LogError("No panel color is configured for character: " + character);
                break;
        }
    }

    private void ApplyRoundedCorners()
    {
        Shader shader = roundedCornerShader != null
            ? roundedCornerShader
            : Shader.Find("UI/RoundedCorners");

        if (shader == null)
        {
            Debug.LogError(
                "Could not find shader 'UI/RoundedCorners'. Make sure UIRoundedCorners.shader " +
                "is imported, or assign it to HeroSelectionUI."
            );
            return;
        }

        foreach (Button button in heroButtons)
        {
            if (button == null)
            {
                Debug.LogError("A hero button is missing in HeroSelectionUI.");
                continue;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                Debug.LogError("Each hero button must have an Image as its target graphic.");
                continue;
            }

            if (image.sprite == null)
            {
                Debug.LogError("A hero button Image has no Sprite assigned.");
                continue;
            }

            Rect rect = image.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                Debug.LogError("A hero button Image must have a non-zero RectTransform size.");
                continue;
            }

            Material material = new(shader);
            material.SetFloat("_CornerRadius", cornerRadius);
            material.SetFloat("_Aspect", rect.width / rect.height);
            material.SetVector("_UVRect", DataUtility.GetOuterUV(image.sprite));
            material.SetFloat("_OutlineWidth", 0f);
            image.material = material;
            roundedCornerMaterials.Add(material);
        }
    }

    private void UpdateSelectionFrame(int selectedIndex)
    {
        if (heroButtons == null || heroButtons.Length != 3)
        {
            Debug.LogError("Hero Buttons must contain all three hero buttons.");
            return;
        }

        for (int i = 0; i < heroButtons.Length; i++)
        {
            Button button = heroButtons[i];
            if (button == null)
            {
                Debug.LogError("Hero button at index " + i + " is not assigned.");
                continue;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                Debug.LogError("Hero button at index " + i + " must use an Image as its target graphic.");
                continue;
            }

            Material material = image.material;
            if (material == null || !material.HasProperty("_OutlineWidth") ||
                !material.HasProperty("_OutlineColor"))
            {
                Debug.LogError("Rounded corner material is missing outline properties.");
                continue;
            }

            Outline existingOutline = image.GetComponent<Outline>();
            if (existingOutline != null)
            {
                existingOutline.enabled = false;
            }

            bool isSelected = i == selectedIndex;
            material.SetFloat("_OutlineWidth", isSelected ? selectedBorderWidth : 0f);
            material.SetColor("_OutlineColor", selectedBorderColor);
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