using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using BrightSouls.Gameplay;
using BrightSouls.AI;

namespace BrightSouls.Testing
{
    /// <summary>
    /// 런타임에서 프리팹이 올바르게 설정되었는지 검증하고 수정하는 헬퍼
    /// </summary>
    public class PrefabSetupHelper : MonoBehaviour
    {
        [Header("Validation")]
        [SerializeField] private bool autoFixMissingComponents = true;
        [SerializeField] private bool validateOnStart = true;
        
        private void Start()
        {
            if (validateOnStart)
            {
                ValidateSetup();
            }
        }
        
        /// <summary>
        /// 플레이어 또는 AI 프리팹의 설정을 검증
        /// </summary>
        public void ValidateSetup()
        {
            var player = GetComponent<Player>();
            var aiCharacter = GetComponent<AICharacter>();
            
            if (player != null)
            {
                ValidatePlayerSetup(player);
            }
            else if (aiCharacter != null)
            {
                ValidateAISetup(aiCharacter);
            }
        }
        
        private void ValidatePlayerSetup(Player player)
        {
            Debug.Log($"Validating Player: {player.name}");

            // Input System 체크
            if (player.Input == null && autoFixMissingComponents)
            {
                Debug.LogWarning("PlayerInput missing - adding component");
                gameObject.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            }

            // InputActionAsset 체크
            if (player.Input != null && player.Input.actions == null)
            {
                Debug.LogError("PlayerInput has no InputActionAsset assigned! Input will not work. " +
                    "Assign InputSystem_Actions to the PlayerInput component on the player prefab.");
            }
            
            // Animator 체크
            if (player.Anim == null)
            {
                Debug.LogWarning("Animator not found on Player!");
            }
            
            // Motor 체크
            if (player.Motor == null)
            {
                Debug.LogWarning("PlayerMotor not found!");
            }
            
            // Combat Controller 체크
            if (player.Combat == null)
            {
                Debug.LogWarning("PlayerCombatController not found!");
            }
            
            // Attributes 체크
            if (player.Attributes == null)
            {
                Debug.LogWarning("PlayerAttributeContainer not found!");
            }
            
            Debug.Log("Player validation complete");
        }
        
        private void ValidateAISetup(AICharacter ai)
        {
            Debug.Log($"Validating AI: {ai.name}");
            
            // NavMeshAgent 체크
            var navAgent = ai.NavAgent;
            if (navAgent == null && autoFixMissingComponents)
            {
                Debug.LogWarning("NavMeshAgent missing - adding component");
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
            
            if (navAgent != null)
            {
                // NavMeshAgent 기본 설정
                navAgent.speed = 3.5f;
                navAgent.angularSpeed = 120f;
                navAgent.acceleration = 8f;
                navAgent.stoppingDistance = 1.5f;
                navAgent.autoBraking = true;
            }
            
            // Animator 체크
            if (ai.AIAnimator == null)
            {
                Debug.LogWarning("Animator not found on AI!");
            }
            
            Debug.Log("AI validation complete");
        }
    }
}