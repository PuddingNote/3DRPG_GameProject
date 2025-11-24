public abstract class PlayerState : IState
{
    protected CharacterManager player;

    public PlayerState(CharacterManager player)
    {
        this.player = player;
    }

    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}
