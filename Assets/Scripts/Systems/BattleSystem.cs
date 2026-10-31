using UnityEngine;

public class BattleSystem : Singleton<BattleSystem>
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
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
        victoryPanel.SetActive(true);
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


