using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunPotionUI : MonoBehaviour
{

    [SerializeField] private Button healthPotionButton;

    [SerializeField] private Button manaPotionButton;


    [SerializeField] private TMP_Text healthPotionCountText;

    [SerializeField] private TMP_Text manaPotionCountText;

    [SerializeField, Min(1)]
    private int healthRestoreAmount = 15;

    [SerializeField, Min(1)]
    private int manaRestoreAmount = 1;

    private void Update()
    {
        UpdatePotionUI();
    }

    private void UpdatePotionUI()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null)
        {
            SetCountText(healthPotionCountText, 0);
            SetCountText(manaPotionCountText, 0);

            SetInteractable(healthPotionButton, false);
            SetInteractable(manaPotionButton, false);

            return;
        }

        SetCountText(
            healthPotionCountText,
            run.HealthPotionCount
        );

        SetCountText(
            manaPotionCountText,
            run.ManaPotionCount
        );

        bool canUse = CanUsePotions();

        SetInteractable(
            healthPotionButton,
            canUse && run.HealthPotionCount > 0
        );

        SetInteractable(
            manaPotionButton,
            canUse && run.ManaPotionCount > 0
        );
    }

    private bool CanUsePotions()
    {
        if (SceneManager.GetActiveScene().name != "InGameScene")
        {
            return false;
        }

        if (RunSystem.Instance == null)
        {
            return false;
        }

        if (BattleSystem.Instance == null ||
            BattleSystem.Instance.IsBattleOver)
        {
            return false;
        }

        if (ActionSystem.Instance == null ||
            ActionSystem.Instance.IsPerforming)
        {
            return false;
        }

        if (InteractionSystem.Instance != null &&
            InteractionSystem.Instance.PlayerIsDragging)
        {
            return false;
        }

        return true;
    }

    public void UseHealthPotion()
    {
        if (!CanUsePotions()) return;

        RunSystem run = RunSystem.Instance;

        if (run.HealthPotionCount <= 0) return;

        if (HeroSystem.Instance == null ||
            HeroSystem.Instance.HeroView == null)
        {
            Debug.LogWarning("Hero is missing.");
            return;
        }

        HeroView hero = HeroSystem.Instance.HeroView;

        if (hero.MaxHealth <= 0 || hero.CurrentHealth <= 0)
        {
            return;
        }

        if (hero.CurrentHealth >= hero.MaxHealth)
        {
            Debug.Log("Health is already full.");
            return;
        }

        if (!run.TryConsumeHealthPotion())
        {
            return;
        }

        int newHealth = Mathf.Min(
            hero.CurrentHealth + healthRestoreAmount,
            hero.MaxHealth
        );

        hero.SetCurrentHealth(newHealth);
        run.SaveHealth(hero.CurrentHealth);

        UpdatePotionUI();
    }

    public void UseManaPotion()
    {
        if (!CanUsePotions()) return;

        RunSystem run = RunSystem.Instance;

        if (run.ManaPotionCount <= 0) return;

        if (ManaSystem.Instance == null)
        {
            Debug.LogWarning("ManaSystem is missing.");
            return;
        }

        ManaSystem mana = ManaSystem.Instance;

        if (mana.MaxMana <= 0)
        {
            return;
        }

        if (mana.CurrentMana >= mana.MaxMana)
        {
            Debug.Log("Mana is already full.");
            return;
        }

        if (!run.TryConsumeManaPotion())
        {
            return;
        }

        mana.RestoreMana(manaRestoreAmount);

        UpdatePotionUI();
    }

    private void SetCountText(TMP_Text target, int count)
    {
        if (target == null) return;

        string value = count.ToString();

        if (target.text != value)
        {
            target.text = value;
        }
    }

    private void SetInteractable(Button button, bool value)
    {
        if (button == null) return;

        if (button.interactable != value)
        {
            button.interactable = value;
        }
    }
}