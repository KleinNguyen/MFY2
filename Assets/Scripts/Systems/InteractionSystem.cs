using UnityEngine;

public class InteractionSystem : Singleton<InteractionSystem>
{
    public bool PlayerIsDragging { get; set; } = false;
    public bool PlayerCanInteract()
    {
        if (BattleSystem.Instance.IsBattleOver) return false;
        if (!ActionSystem.Instance.IsPerforming) return true;
        else return false;
    }
    public bool PlayerCanHover()
    {
        if (BattleSystem.Instance.IsBattleOver) return false;
        if (PlayerIsDragging) return false;
        return true;
    }
}
