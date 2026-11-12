using UnityEngine;
using UnityEngine.EventSystems;

public class BattleSystem : Singleton<BattleSystem>
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private RewardUI rewardUI;
    [SerializeField] private RunResultUI runResultUI;
    public bool IsBattleOver { get; private set; } = false;

    private void Start()
    {
        UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        if (runResultUI != null) runResultUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        UIInputModuleCompatibility.MonitorAndConfigure(EventSystem.current, this);
    }

    public void Victory()
    {
        if (IsBattleOver) return;
        IsBattleOver = true;
        RunSystem.Instance.SaveHealth(HeroSystem.Instance.HeroView.CurrentHealth);
        victoryPanel.SetActive(true);
        rewardUI.Setup();
        Debug.Log("Victory");
    }

    public void Defeat()
    {
        if (IsBattleOver) return;
        IsBattleOver = true;
        RunSystem run = RunSystem.Instance;

        if (run != null)
        {
            if (HeroSystem.Instance != null && HeroSystem.Instance.HeroView != null)
            {
                run.SaveHealth(HeroSystem.Instance.HeroView.CurrentHealth);
            }
            run.FinishRun(RunResult.Lost);
        }

        victoryPanel.SetActive(false);
        if (runResultUI != null && run != null)
        {
            defeatPanel.SetActive(false);
            runResultUI.Show();
        }
        else
        {
            defeatPanel.SetActive(true);
            Debug.LogError("Assign Run Result UI in BattleSystem.");
        }
    }
}