namespace T60.StateMachine
{
    /// <summary>
    /// Base interface for all states in the state machine.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called once when entering the state.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called every frame while this state is active.
        /// </summary>
        void Update();

        /// <summary>
        /// Called once when exiting the state.
        /// </summary>
        void Exit();
    }
}
