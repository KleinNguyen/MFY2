using UnityEngine;

public class BattleSystem : Singleton<BattleSystem>
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private RewardUI rewardUI;
    public bool IsBattleOver { get; private set; } = false;
    private void Start()
    {
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
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
        defeatPanel.SetActive(true);
        Debug.Log("Defeat");
    }
}


