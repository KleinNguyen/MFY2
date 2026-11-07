using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunHeaderUI : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text potionText;
    [SerializeField] private TMP_Text timeText;

    private void Update()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.HeroData == null)
        {
            SetText(healthText, "HP: -- / --");
            SetText(goldText, "Gold: --");
            SetText(floorText, "Floor: --");
            SetText(timeText, "Time: 00:00");

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

        int elapsedSeconds = Mathf.FloorToInt(run.ElapsedRunTime);

        int hours = elapsedSeconds / 3600;
        int minutes = (elapsedSeconds / 60) % 60;
        int seconds = elapsedSeconds % 60;

        string formattedTime = hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";

        SetText(timeText, "Time: " + formattedTime);


        MapPoint currentPoint = run.GetMapPoint(
            run.SelectedMapPointId
        );

        if (currentPoint == null)
        {
            currentPoint = run.GetMapPoint(
                run.CompletedMapPointId
            );
        }

        if (currentPoint != null)
        {
            SetText(
                floorText,
                "Floor: " + (currentPoint.Floor + 1)
            );
        }
        else
        {
            SetText(floorText, "Floor: 1");
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