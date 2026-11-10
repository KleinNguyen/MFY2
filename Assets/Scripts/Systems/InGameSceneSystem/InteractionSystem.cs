using UnityEngine;

public class InteractionSystem : Singleton<InteractionSystem>
{
    public bool PlayerIsDragging { get; set; } = false;
    public bool PlayerCanInteract()
    {
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return false;
        if (BattleSystem.Instance.IsBattleOver) return false;
        if (!ActionSystem.Instance.IsPerforming) return true;
        else return false;
    }
    public bool PlayerCanHover()
    {
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return false;
        if (BattleSystem.Instance.IsBattleOver) return false;
        if (PlayerIsDragging) return false;
        return true;
    }
}
