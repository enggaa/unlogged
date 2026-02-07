using UnityEngine;
using UnityPatterns.FiniteStateMachine;

namespace BrightSouls.Gameplay
{
    public sealed class PlayerStateController : MonoBehaviour, IStateMachineOwner
    {
        /* ------------------------------- Properties ------------------------------- */

        public bool IsDead
        {
            get => fsm != null && fsm.IsInState<PlayerStateDead>();
        }

        public bool IsAttacking
        {
            get => fsm != null && fsm.IsInAnyState(typeof(PlayerStateAttacking), typeof(PlayerStateComboing), typeof(PlayerStateComboEnding));
        }

        public bool IsStaggered
        {
            get => fsm != null && fsm.IsInState<PlayerStateStaggered>();
        }

        public bool IsBlocking
        {
            get => fsm != null && fsm.IsInState<PlayerStateBlocking>();
        }

        public bool IsDodging
        {
            get => fsm != null && fsm.IsInState<PlayerStateDodging>();
        }

        public bool IsJumping
        {
            get => fsm != null && fsm.IsInState<PlayerStateJumping>();
        }

        public StateMachineController Fsm
        {
            get => fsm;
        }

        /* ------------------------ Inspector-Assigned Fields ----------------------- */

        [SerializeField] private StateMachineController fsm;

        /* ------------------------------ Unity Events ------------------------------ */

        private void Awake()
        {
            if (fsm == null)
            {
                fsm = GetComponent<StateMachineController>();
            }

            if (fsm == null)
            {
                fsm = GetComponentInChildren<StateMachineController>();
            }

            if (fsm == null)
            {
                fsm = GetComponentInParent<StateMachineController>();
            }
        }
    }
}