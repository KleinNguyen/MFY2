public class GainAttackMilestonePowerGA : GameAction
{
    public int RequiredAttacks { get; private set; }
    public int ComboGain { get; private set; }
    public int DrawAmount { get; private set; }

    public GainAttackMilestonePowerGA(int requiredAttacks, int comboGain, int drawAmount = 0)
    {
        RequiredAttacks = requiredAttacks;
        ComboGain = comboGain;
        DrawAmount = drawAmount;
    }
}
