public interface IState
{
    void Enter();       // 상태 진입 시 1회 실행
    void Execute();     // 매 프레임 실행 (Update)
    void Exit();        // 상태 종료 시 1회 실행
}
