public class FinishEnemyMoveGA : GameAction
{
    public EnemyMoveGA MoveAction { get; }

    public FinishEnemyMoveGA(EnemyMoveGA moveAction)
    {
        MoveAction = moveAction;
    }
}