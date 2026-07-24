using UnityEngine;

namespace T60.StateMachine
{
    public abstract class BaseState : MonoBehaviour, IState
    {
        protected MatchStateMachineRunner Runner { get; private set; }
        protected MatchContext Context => Runner != null ? Runner.Context : null;

        public virtual void InitializeState(MatchStateMachineRunner runner)
        {
            Runner = runner;
            enabled = false;
        }

        public virtual void Enter()
        {
            enabled = true;
        }

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
            enabled = false;
        }
    }
}
