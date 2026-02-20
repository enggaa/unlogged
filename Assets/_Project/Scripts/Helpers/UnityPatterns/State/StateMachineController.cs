using UnityEngine;

namespace UnityPatterns.FiniteStateMachine
{
    public class StateMachineController : MonoBehaviour
    {
        /* ------------------------------- Properties ------------------------------- */

        public float CurrentStateTime
        {
            get => currentStateTime;
        }

        public IState CurrentState
        {
            get => currentState;
        }

        /* ------------------------ Inspector-Assigned Fields ----------------------- */

        [SerializeField] private SerializedStateMachine stateMachine;

        /* ----------------------------- Runtime Fields ----------------------------- */

        private string defaultName;
        private IState currentState;
        private float currentStateTime = 0f;

        /* ------------------------------ Unity Events ------------------------------ */
        private void Start()
        {
            defaultName = gameObject.name;
            if (stateMachine == null)
            {
                stateMachine = ScriptableObject.CreateInstance<SerializedStateMachine>();
                Debug.LogWarning($"StateMachineController on \"{defaultName}\" had no SerializedStateMachine assigned. Created a runtime fallback state machine.");
            }

            if (currentState == null)
            {
                var initialState = stateMachine.DefaultState ?? (stateMachine.States != null && stateMachine.States.Length > 0
                    ? stateMachine.States[0]
                    : null);
                if (initialState != null)
                {
                    SetState(initialState);
                }
                else
                {
                    Debug.LogError($"StateMachineController on \"{defaultName}\" has no default or configured states. Add at least one state (for Player, e.g. PlayerStateDefault) to SerializedStateMachine.");
                }
            }
        }

        private void Update()
        {
            if (currentState == null)
            {
                return;
            }

            CheckTransitions();
            currentStateTime += Time.deltaTime;
            currentState.OnStateUpdate(this);
        }

        /* ----------------------------- Public Methods ----------------------------- */

        public void SetState<T>() where T : IState
        {
            var newState = FindStateOfType<T>();
            SetState(newState);
        }

        public void SetState(IState newState)
        {
            if (newState == null)
            {
                Debug.LogError($"SetState failed: newState is null in state machine \"{base.name}\".");
                return;
            }
            else
            {
                currentState?.OnStateExit(this);
                currentState = newState;
                gameObject.name = defaultName + " - " + currentState;
                currentStateTime = 0f;
                currentState.OnStateEnter(this);
            }
        }

        public bool IsInState<T>() where T : IState
        {
            return IsInState(typeof(T));
        }

        public bool IsInState(System.Type type)
        {
            if (currentState != null)
            {
                return this.currentState.GetType() == type;
            }
            else
            {
                return false;
            }
        }

        public bool IsInAnyState(params System.Type[] types)
        {
            foreach(var type in types)
            {
                if(IsInState(type))
                {
                    return true;
                }
            }
            return false;
        }

        /* ----------------------------- Private Methods ---------------------------- */

        private void CheckTransitions()
        {
            if (stateMachine == null || stateMachine.Transitions == null)
            {
                return;
            }

            foreach (var transition in stateMachine.Transitions)
            {
                if (transition.Source == currentState)
                {
                    if (transition.Condition != null && transition.Condition.Validate(this))
                    {
                        IState source = transition.Source;
                        IState target = transition.Target;
                        SetState(target);
                        return;
                    }
                }
            }
        }

        private T FindStateOfType<T>() where T : IState
        {
            foreach (var state in stateMachine.States)
            {
                if (state.GetType() == typeof(T))
                {
                    return (T)state;
                }
            }
            Debug.LogError($"FindStateOfType<{typeof(T)}> failed: Unable to find state of type {typeof(T)} in state machine \"{defaultName}\"");
            return default(T);
        }

        /* -------------------------------------------------------------------------- */
    }
}
