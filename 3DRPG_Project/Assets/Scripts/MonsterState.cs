public abstract class MonsterState : IState
{
    protected MonsterController monster;

    public MonsterState(MonsterController monster)
    {
        this.monster = monster;
    }

    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}
