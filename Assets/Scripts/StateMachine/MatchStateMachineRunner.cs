using UnityEngine;

namespace T60.StateMachine
{
    public class MatchStateMachineRunner : MonoBehaviour
    {
        public StateMachine StateMachine { get; private set; }
        public MatchContext Context { get; private set; }

        [Header("Debug Info")]
        [SerializeField] private string currentStateName;
        [SerializeField] private float mainClockSeconds;
        [SerializeField] private float turnClockSeconds;
        [SerializeField] private int activePlayer;
        [SerializeField] private bool reflexActive;

        private void Awake()
        {
            Context = new MatchContext();
            StateMachine = new StateMachine();
        }

        private void Start()
        {
            StateMachine.Initialize(new MatchSetupState(this, Context));
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
                reflexActive = Context.ReflexWindowActive;
            }
        }

        public void TestPlayCard(string cardName, float clockTimeDelta)
        {
            if (StateMachine.CurrentState is PlayerTurnState playerTurnState)
            {
                playerTurnState.PlayCard(cardName, clockTimeDelta);
            }
            else
            {
                Debug.LogWarning($"[Runner] Cannot play normal card '{cardName}' outside of PlayerTurnState!");
            }
        }

        public void TestPlayReflexCard(string reflexCardName, float addedSeconds = 15f)
        {
            if (StateMachine.CurrentState is ReflexWindowState reflexState)
            {
                reflexState.PlayReflexCard(reflexCardName, addedSeconds);
            }
            else
            {
                Debug.LogWarning($"[Runner] Cannot play Reflex card '{reflexCardName}' unless in ReflexWindowState!");
            }
        }

        public void RestartMatch()
        {
            StateMachine.ChangeState(new MatchSetupState(this, Context));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 320, 300), "T60 State Machine Debug", GUI.skin.window);
            
            GUILayout.Label($"Current State: {currentStateName}");
            GUILayout.Label($"Active Player: Player {activePlayer}");
            GUILayout.Label($"Main Clock: {mainClockSeconds:F2}s");
            GUILayout.Label($"Turn Clock: {turnClockSeconds:F2}s");

            if (Context != null && Context.MatchOver)
            {
                GUILayout.Label($"GAME OVER! Winner: Player {Context.WinnerPlayerIndex + 1}");
                if (GUILayout.Button("Restart Match"))
                {
                    RestartMatch();
                }
            }
            else if (StateMachine?.CurrentState is ReflexWindowState)
            {
                GUI.color = Color.red;
                GUILayout.Label("!!! MAIN CLOCK AT ZERO !!!");
                GUI.color = Color.white;
                if (GUILayout.Button("Play Reflex Card (Emergency Vent +15s)"))
                {
                    TestPlayReflexCard("Emergency Vent", 15f);
                }
            }
            else if (StateMachine?.CurrentState is PlayerTurnState)
            {
                if (GUILayout.Button("Play Coolant Flush (+20s Main Clock)"))
                {
                    TestPlayCard("Coolant Flush", +20f);
                }
                if (GUILayout.Button("Play Power Surge (-15s Main Clock)"))
                {
                    TestPlayCard("Power Surge", -15f);
                }
                if (GUILayout.Button("Play Neutral Card (0s Shift)"))
                {
                    TestPlayCard("Neutral Protocol", 0f);
                }
            }

            GUILayout.EndArea();
        }
    }
}
