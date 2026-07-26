namespace T60.StateMachine
{
    public interface IState
    {
        void Enter();
        void StateUpdate();
        void Exit();
    }
}
