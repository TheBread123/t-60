using UnityEngine;
using UnityEngine.Events;

namespace T60.StateMachine
{
    public abstract class BaseState : MonoBehaviour, IState
    {
        [Header("State Events")]
        [SerializeField] protected UnityEvent onEnterState;
        [SerializeField] protected UnityEvent onExitState;

        public UnityEvent OnEnterState => onEnterState;
        public UnityEvent OnExitState => onExitState;

        protected MatchStateMachineRunner Runner { get; private set; }
        protected MatchContext Context => Runner != null ? Runner.Context : null;

        public virtual void InitializeState(MatchStateMachineRunner runner)
        {
            Runner = runner;
            enabled = false;
        }

        public virtual void Enter()
        {
            onEnterState?.Invoke();
        }

        public virtual void StateUpdate()
        {
        }

        public virtual void Exit()
        {
            onExitState?.Invoke();
            enabled = false;
        }
    }
}

