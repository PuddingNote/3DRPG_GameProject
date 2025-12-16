public abstract class PlayerState : IState
{
    protected PlayerController player;

    public PlayerState(PlayerController player)
    {
        this.player = player;
    }

    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}
