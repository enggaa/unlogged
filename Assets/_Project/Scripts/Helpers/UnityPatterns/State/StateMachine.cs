using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPatterns.FiniteStateMachine
{
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

        [SerializeReference] private IState defaultState;
        [SerializeReference] private IState[] states;
        [SerializeReference] private StateTransition[] transitions;

        /* -------------------------------------------------------------------------- */
    }
}
