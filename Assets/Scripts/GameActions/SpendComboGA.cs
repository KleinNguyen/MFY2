using System.Collections.Generic;

public class SpendComboGA : GameAction
{
    public int Amount { get; private set; }
    public bool SpendAll { get; private set; }
    public int ActualSpent { get; set; }

    public int DrawPerComboSpent { get; set; }
    public int DamagePerComboSpent { get; set; }
    public int BaseDamageAfterSpend { get; set; }
    public int ComboOnKill { get; set; }

    public List<CombatantView> DamageTargets { get; set; } = new();

    public SpendComboGA(int amount, bool spendAll = false)
    {
        Amount = amount;
        SpendAll = spendAll;
    }
}