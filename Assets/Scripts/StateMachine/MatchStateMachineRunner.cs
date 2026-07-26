using UnityEngine;
using System.Collections.Generic;
using T60.Cards;
using T60.Cards.Effects;

namespace T60.StateMachine
{
    public class MatchStateMachineRunner : MonoBehaviour
    {
        public StateMachine StateMachine { get; private set; }
        public MatchContext Context { get; private set; }

        [Header("State Machine Setup")]
        [SerializeField] private BaseState startingState;

        [Header("Debug Info")]
        [SerializeField] private string currentStateName;
        [SerializeField] private float mainClockSeconds;
        [SerializeField] private float turnClockSeconds;
        [SerializeField] private int activePlayer;

        private void Awake()
        {
            Context = new MatchContext();
            StateMachine = new StateMachine();

            // Find all child or attached BaseState components and initialize them
            BaseState[] states = GetComponentsInChildren<BaseState>(true);
            foreach (var state in states)
            {
                state.InitializeState(this);
            }
        }

        private void Start()
        {
            if (startingState != null)
            {
                StateMachine.Initialize(startingState);
            }
            else
            {
                Debug.LogError("[Runner] Starting state is not assigned in the Inspector!");
            }
        }

        private void Update()
        {
            StateMachine?.Update();

            if (StateMachine?.CurrentState != null)
            {
                currentStateName = StateMachine.CurrentState.GetType().Name;
            }
            if (Context != null)
            {
                mainClockSeconds = Context.MainClockSeconds;
                turnClockSeconds = Context.TurnClockSeconds;
                activePlayer = Context.ActivePlayerIndex + 1;
            }
        }

        public void TestPlayCard(string cardName, float clockTimeDelta, bool switchTurn = true)
        {
            if (StateMachine.CurrentState is PlayerTurnState playerTurnState)
            {
                Card testCard = ScriptableObject.CreateInstance<Card>();
                testCard.CardName = cardName;

                List<Effect> effectsList = new List<Effect>();

                if (clockTimeDelta != 0f)
                {
                    ModifyMainClockEffect clockEffect = new ModifyMainClockEffect();
                    clockEffect.TimeDelta = clockTimeDelta;
                    effectsList.Add(clockEffect);
                }

                if (switchTurn)
                {
                    SwitchTurnEffect switchEffect = new SwitchTurnEffect();
                    effectsList.Add(switchEffect);
                }

                testCard.SetEffects(effectsList.ToArray());
                playerTurnState.PlayCard(testCard);
            }
            else
            {
                Debug.LogWarning($"[Runner] Cannot play card '{cardName}' outside of PlayerTurnState!");
            }
        }

        public void RestartMatch()
        {
            if (startingState != null)
            {
                StateMachine.ChangeState(startingState);
            }
            else
            {
                Debug.LogError("[Runner] Cannot restart match; startingState is null!");
            }
        }
    }
}
