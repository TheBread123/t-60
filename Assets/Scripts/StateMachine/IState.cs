namespace T60.StateMachine
{
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
