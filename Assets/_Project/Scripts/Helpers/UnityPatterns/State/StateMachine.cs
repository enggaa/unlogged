using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPatterns.FiniteStateMachine
{
    [System.Serializable]
    public sealed class EmptyState : IState
    {
        public void OnStateEnter(StateMachineController fsm) { }
        public void OnStateUpdate(StateMachineController fsm) { }
        public void OnStateExit(StateMachineController fsm) { }
    }

    [CreateAssetMenu(fileName = "SerializedStateMachine", menuName = "BrightSouls/State/SerializedStateMachine", order = 0)]
    public sealed class SerializedStateMachine : ScriptableObject
    {
        /* ------------------------------- Properties ------------------------------- */

        public IState DefaultState
        {
            get => defaultState;
        }

        public IState[] States
        {
            get => states;
        }

        public StateTransition[] Transitions
        {
            get => transitions;
        }

        /* ------------------------ Inspector-assigned Fields ----------------------- */

        [SerializeReference] private IState defaultState = new EmptyState();
        [SerializeReference] private IState[] states = { new EmptyState() };
        [SerializeReference] private StateTransition[] transitions;


        private void OnEnable()
        {
            EnsureDefaults();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaults();
        }
#endif

        private void EnsureDefaults()
        {
            if (defaultState == null)
            {
                defaultState = new EmptyState();
            }

            if (states == null || states.Length == 0)
            {
                states = new IState[] { new EmptyState() };
            }
        }

        /* -------------------------------------------------------------------------- */
    }
}
