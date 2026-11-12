using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;

    public void Show()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || !run.IsRunFinished)
        {
            Debug.LogError("The run has not finished.");
            return;
        }
        if (titleText == null || summaryText == null)
        {
            Debug.LogError("Assign Title Text and Summary Text in RunResultUI.", this);
            return;
        }

        titleText.text = run.Result == RunResult.Won ? "VICTORY" : "DEFEAT";
        int elapsedSeconds = Mathf.FloorToInt(run.ElapsedRunTime);
        int hours = elapsedSeconds / 3600;
        int minutes = (elapsedSeconds / 60) % 60;
        int seconds = elapsedSeconds % 60;
        string formattedTime = hours > 0 ? $"{hours:00}:{minutes:00}:{seconds:00}" : $"{minutes:00}:{seconds:00}";
        string heroName = run.HeroData != null ? run.HeroData.Character.ToString() : "--";
        int deckCount = run.Deck != null ? run.Deck.Count : 0;
        summaryText.text = $"Hero: {heroName}\nAct: {run.CurrentActIndex + 1}\nTime: {formattedTime}\nGold: {run.Gold}\nCards: {deckCount}";

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void RetrySameHero()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.HeroData == null)
        {
            Debug.LogError("Cannot retry: hero data is missing.");
            return;
        }

        HeroData selectedHero = run.HeroData;
        run.StartNewRun(selectedHero);
        SceneManager.LoadScene("MapScene");
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}