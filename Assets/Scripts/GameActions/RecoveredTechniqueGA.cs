public class RecoveredTechniqueGA : GameAction
{
    public int AdditionalManaCost { get; private set; }

    public RecoveredTechniqueGA(int additionalManaCost)
    {
        AdditionalManaCost = additionalManaCost;
    }
}
