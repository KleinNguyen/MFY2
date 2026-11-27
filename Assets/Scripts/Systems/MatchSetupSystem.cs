using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    private static readonly string[] MysteryLabels =
    {
        "FULL HEAL",
        "EXTRA TURN",
        "+2 MANA",
        "5 JUNK",
        "LOSE 30% HP",
        "GET ALL DEBUFFS\n3 TURNS"
    };

    [SerializeField] private HeroData heroData;
    [SerializeField] private SpriteRenderer battleBackground;
    [SerializeField] private List<EnemyData> enemyDatas;
    [SerializeField] private LuckyWheelUI mysteryWheel;
    [SerializeField] private Sprite junkCardImage;

    private int mysteryResult = -1;
    private bool mysteryConfirmed;

    private IEnumerator Start()
    {
        // Let scene objects finish initialization before querying their systems.
        yield return null;

        RunSystem run = RunSystem.Instance;
        if (run != null && run.IsRunFinished)
        {
            yield break;
        }

        if (!ValidateSetup(run))
        {
            yield break;
        }

        if (!run.CaptureBattleStart())
        {
            yield break;
        }

        run.StartRunTimer();
        mysteryResult = run.SavedMysteryResult;

        SetupBackground(run.CurrentAct);
        HeroSystem.Instance.Setup(run.HeroData);
        HeroSystem.Instance.HeroView.SetCurrentHealth(run.CurrentHealth);
        EnemySystem.Instance.Setup(run.CurrentEncounter);
        if (run.CurrentEncounter.Type == EncounterType.Boss)
        {
            SceneMusicPlayer.PlayBossMusic();
        }
        CardSystem.Instance.Setup(run.Deck);

        MapPoint point = run.GetMapPoint(run.SelectedMapPointId);
        if (point != null &&
            point.Type == MapPointType.Mystery)
        {
            if (mysteryWheel == null ||
                !mysteryWheel.IsConfigured ||
                junkCardImage == null)
            {
                Debug.LogError(
                    "Assign Mystery Wheel and Junk Card Image in MatchSetupSystem.",
                    this
                );
                yield break;
            }

            if (!OpenMysteryWheel()) yield break;

            while (!mysteryConfirmed)
            {
                // Reopen the wheel if it was closed without confirming a result.
                if (!mysteryWheel.gameObject.activeInHierarchy &&
                    !OpenMysteryWheel())
                {
                    yield break;
                }

                yield return null;
            }

            ApplyMysteryResult(run);
        }

        PerkData startingPerk = run.HeroData.StartingPerk;
        if (startingPerk != null)
        {
            PerkSystem.Instance.AddPerk(new Perk(startingPerk));
        }

        ActionSystem.Instance.Perform(new DrawCardsGA(5));
        while (ActionSystem.Instance.IsPerforming &&
               !BattleSystem.Instance.IsBattleOver)
        {
            yield return null;
        }

        if (!BattleSystem.Instance.IsBattleOver)
        {
            BattleSystem.Instance.BeginBattle();
        }
    }

    private bool ValidateSetup(RunSystem run)
    {
        if (run == null ||
            run.HeroData == null ||
            run.Deck == null ||
            run.CurrentEncounter == null ||
            run.IsRunFinished)
        {
            Debug.LogError(
                "Select a map battle from an active run before loading InGameScene.",
                this
            );
            return false;
        }

        if (BattleSystem.Instance == null ||
            HeroSystem.Instance == null ||
            HeroSystem.Instance.HeroView == null ||
            EnemySystem.Instance == null ||
            CardSystem.Instance == null ||
            ManaSystem.Instance == null ||
            ActionSystem.Instance == null ||
            PerkSystem.Instance == null)
        {
            Debug.LogError("Battle systems are missing in InGameScene.", this);
            return false;
        }

        return true;
    }

    private bool OpenMysteryWheel()
    {
        return mysteryWheel.Show(
            MysteryLabels,
            index =>
            {
                mysteryResult = index;
                RunSystem.Instance.SaveMysteryResult(index);
            },
            () => mysteryConfirmed = true,
            canCancel: false,
            previousResult: mysteryResult,
            closeCaption: "Begin battle",
            character: RunSystem.Instance.HeroData.Character
        );
    }

    private void ApplyMysteryResult(RunSystem run)
    {
        // Apply the confirmed wheel result before the opening hand is drawn.
        switch (mysteryResult)
        {
            case 0:
                run.SaveHealth(run.HeroData.Health);
                break;

            case 1:
                EnemySystem.Instance.SetExtraOpeningTurns(1);
                break;

            case 2:
                ManaSystem.Instance.SetBattleManaBonus(2);
                break;

            case 3:
                CardSystem.Instance.AddJunkCards(5, junkCardImage);
                break;

            case 4:
                int healthLost = Mathf.FloorToInt(run.CurrentHealth * 0.3f);
                run.SaveHealth(run.CurrentHealth - healthLost);
                break;

            case 5:
                HeroView hero = HeroSystem.Instance.HeroView;
                hero.AddStatusEffect(StatusEffectType.WEAK, 3);
                hero.AddStatusEffect(StatusEffectType.POISON, 3);
                hero.AddStatusEffect(StatusEffectType.EXPOSED, 3);
                hero.AddStatusEffect(StatusEffectType.ARMOR_BREAK, 3);
                break;
        }

        HeroSystem.Instance.HeroView.SetCurrentHealth(run.CurrentHealth);
        Debug.Log(
            "Mystery result: " + MysteryLabels[mysteryResult],
            this
        );
    }

    private void SetupBackground(ActData act)
    {
        if (act != null &&
            act.MapBackground != null &&
            battleBackground != null)
        {
            battleBackground.sprite = act.MapBackground;
            FitBackgroundToCamera();
        }
        else
        {
            Debug.LogWarning("Assign the Act Map Background and Battle Background.", this);
        }
    }

    private void FitBackgroundToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null ||
            !mainCamera.orthographic)
        {
            Debug.LogError(
                "A Main Camera in orthographic mode is required to fit the Act background."
            );
            return;
        }

        Vector2 spriteSize = battleBackground.sprite.bounds.size;
        if (spriteSize.x <= 0f ||
            spriteSize.y <= 0f)
        {
            Debug.LogError("The current Act background has invalid dimensions.");
            return;
        }

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        // Use the larger scale so the background covers the full camera view.
        float coverScale = Mathf.Max(
            cameraWidth / spriteSize.x,
            cameraHeight / spriteSize.y
        );
        Vector3 parentScale = battleBackground.transform.parent != null
            ? battleBackground.transform.parent.lossyScale
            : Vector3.one;

        if (Mathf.Approximately(parentScale.x, 0f) ||
            Mathf.Approximately(parentScale.y, 0f))
        {
            Debug.LogError("The Act background parent must have a non-zero scale.");
            return;
        }

        battleBackground.transform.localScale = new Vector3(
            coverScale / parentScale.x,
            coverScale / parentScale.y,
            battleBackground.transform.localScale.z
        );
    }
}