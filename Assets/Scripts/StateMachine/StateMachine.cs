using UnityEngine;

namespace T60.StateMachine
{
    /// <summary>
    /// Core StateMachine engine to manage transitions and tick updates for IState objects.
    /// </summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }

        /// <summary>
        /// Initializes the State Machine with a starting state.
        /// </summary>
        public void Initialize(IState startingState)
        {
            CurrentState = startingState;
            CurrentState?.Enter();
        }

        /// <summary>
        /// Transitions from the current state to a new state.
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[StateMachine] Attempted to transition to a null state!");
                return;
            }

            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.Enter();
        }

        /// <summary>
        /// Call this inside MonoBehaviour Update() to tick the current state.
        /// </summary>
        public void Update()
        {
            CurrentState?.Update();
        }
    }
}
