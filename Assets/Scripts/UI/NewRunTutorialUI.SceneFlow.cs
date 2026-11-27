using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class NewRunTutorialUI
{
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        StopAllCoroutines();
        HideWindow();

        if (
            scene.name == "MenuScene" ||
            scene.name == "EndingScene"
        )
        {
            mapPending = false;
            battlePending = false;

            return;
        }

        if (scene.name == "MapScene" && mapPending)
        {
            ShowPages(
                mapPages,
                TutorialPart.Map
            );
        }
        else if (scene.name == "InGameScene" && battlePending)
        {
            StartCoroutine(
                WaitForBattle(scene.handle)
            );
        }
    }

    private IEnumerator WaitForBattle(int sceneHandle)
    {
        // Allow Mystery setup and the opening card draw to finish first.
        while (SceneManager.GetActiveScene().handle == sceneHandle)
        {
            BattleSystem battle = BattleSystem.Instance;

            if (battle != null && battle.IsBattleOver)
            {
                battlePending = false;
                yield break;
            }

            if (
                battle != null &&
                !battle.IsPreparingBattle &&
                ActionSystem.Instance != null &&
                !ActionSystem.Instance.IsPerforming
            )
            {
                ShowPages(
                    battlePages,
                    TutorialPart.Battle
                );

                yield break;
            }

            yield return null;
        }
    }

    private void CompletePart(TutorialPart part)
    {
        if (part == TutorialPart.Map)
        {
            mapPending = false;
        }
        else if (part == TutorialPart.Battle)
        {
            battlePending = false;
        }
    }

    private void SkipTutorial()
    {
        StopAllCoroutines();

        mapPending = false;
        battlePending = false;

        HideWindow();
    }
}
