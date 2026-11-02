using TMPro;
using UnityEngine;

public class HeroSelectionUI : MonoBehaviour
{
    [SerializeField] private HeroData hero1;
    [SerializeField] private HeroData hero2;
    [SerializeField] private HeroData hero3;

    [SerializeField] private TMP_Text selectedHeroText;

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