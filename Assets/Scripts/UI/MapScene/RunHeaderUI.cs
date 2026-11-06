using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunHeaderUI : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text actText;
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text potionText;

    private void Update()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.HeroData == null)
        {
            SetText(healthText, "HP: -- / --");
            SetText(goldText, "Gold: --");
            SetText(actText, "Act: --");
            SetText(floorText, "Floor: --");

            SetText(
                potionText,
                "HP Potion: --  Mana Potion: --"
            );

            return;
        }

        int currentHealth = run.CurrentHealth;
        int maxHealth = run.HeroData.Health;

        if (SceneManager.GetActiveScene().name == "InGameScene" &&
            HeroSystem.Instance != null &&
            HeroSystem.Instance.HeroView != null &&
            HeroSystem.Instance.HeroView.MaxHealth > 0)
        {
            currentHealth =
                HeroSystem.Instance.HeroView.CurrentHealth;

            maxHealth =
                HeroSystem.Instance.HeroView.MaxHealth;
        }


        SetText(
            healthText,
            "HP: " + currentHealth + " / " + maxHealth
        );

        SetText(
            goldText,
            "Gold: " + run.Gold
        );

        SetText(
            actText,
            "Act: " + (run.CurrentActIndex + 1)
        );

        MapPoint selectedPoint = run.GetMapPoint(
            run.SelectedMapPointId
        );

        if (selectedPoint != null)
        {
            SetText(
                floorText,
                "Floor: " + (selectedPoint.Floor + 1)
            );
        }

        SetText(
            potionText,
            "HP Potion: " + run.HealthPotionCount +
            "  Mana Potion: " + run.ManaPotionCount
        );
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target == null) return;

        if (target.text == value) return;

        target.text = value;
    }
}