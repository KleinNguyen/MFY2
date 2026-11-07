using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private SpriteRenderer battleBackground;
    [SerializeField] private PerkData perkData;
    [SerializeField] private List<EnemyData> enemyDatas;
    private void Start()
    {
        if (RunSystem.Instance == null) return;
        if (RunSystem.Instance.CurrentEncounter == null) return;
        ActData currentAct = RunSystem.Instance.CurrentAct;

        if (currentAct != null &&
            currentAct.MapBackground != null &&
            battleBackground != null)
        {
            battleBackground.sprite = currentAct.MapBackground;
            FitBackgroundToCamera();
        }
        else
        {
            Debug.LogWarning(
                "Battle background is missing. Check Current Act, " +
                "Map Background and Battle Background."
            );
        }
        HeroSystem.Instance.Setup(RunSystem.Instance.HeroData);
        HeroSystem.Instance.HeroView.SetCurrentHealth(RunSystem.Instance.CurrentHealth);
        EnemySystem.Instance.Setup(RunSystem.Instance.CurrentEncounter);
        CardSystem.Instance.Setup(RunSystem.Instance.Deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }

    private void FitBackgroundToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("A Main Camera in orthographic mode is required to fit the Act background.");
            return;
        }

        Vector2 spriteSize = battleBackground.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            Debug.LogError("The current Act background has invalid dimensions.");
            return;
        }

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        float coverScale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
        Vector3 parentScale = battleBackground.transform.parent != null
            ? battleBackground.transform.parent.lossyScale
            : Vector3.one;

        if (Mathf.Approximately(parentScale.x, 0f) || Mathf.Approximately(parentScale.y, 0f))
        {
            Debug.LogError("The Act background parent must have a non-zero scale.");
            return;
        }

        battleBackground.transform.localScale = new Vector3(
            coverScale / parentScale.x,
            coverScale / parentScale.y,
            battleBackground.transform.localScale.z);
    }
}
