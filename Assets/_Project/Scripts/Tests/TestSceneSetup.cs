using UnityEngine;
using BrightSouls.Gameplay;
using BrightSouls.AI;

namespace BrightSouls.Testing
{
    /// <summary>
    /// 기존 BrightSouls 코드를 활용한 테스트 씬 설정 매니저
    /// 실제 게임 로직은 기존 코드가 처리하며, 이 스크립트는 설정과 초기화만 담당
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Player Setup")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0, 1, 0);
        
        [Header("Enemy Setup")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Vector3[] enemySpawnPositions = new Vector3[]
        {
            new Vector3(5, 0, 5),
            new Vector3(-5, 0, 5),
            new Vector3(0, 0, 10)
        };
        
        [Header("Camera Setup")]
        [SerializeField] private GameObject cameraPrefab;
        
        [Header("Test Environment")]
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Vector3 groundSize = new Vector3(50, 1, 50);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private Player player;
        private AICharacter[] enemies;
        
        private void Start()
        {
            InitializeTestScene();
        }
        
        /// <summary>
        /// 테스트 씬 초기화 - 기존 코드 컴포넌트들을 생성하고 연결
        /// </summary>
        private void InitializeTestScene()
        {
            CreateGround();
            CreatePlayer();
            CreateEnemies();
            SetupCamera();
            
            if (showDebugInfo)
            {
                Debug.Log("=== Test Scene Initialized ===");
                Debug.Log($"Player: {player.name}");
                Debug.Log($"Enemies: {enemies.Length}");
                Debug.Log("Use WASD to move, Mouse to look, Left Click to attack");
            }
        }
        
        /// <summary>
        /// 테스트용 지면 생성
        /// </summary>
        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TestGround";
            ground.transform.localScale = groundSize / 10f;
            ground.transform.position = Vector3.zero;
            
            if (groundMaterial != null)
            {
                ground.GetComponent<Renderer>().material = groundMaterial;
            }
            
            // NavMesh를 위한 레이어 설정
            ground.layer = LayerMask.NameToLayer("Default");
        }
        
        /// <summary>
        /// 플레이어 생성 - 기존 Player 컴포넌트 활용
        /// </summary>
        private void CreatePlayer()
        {
            if (playerPrefab != null)
            {
                GameObject playerObj = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
                player = playerObj.GetComponent<Player>();
                
                if (player == null)
                {
                    Debug.LogError("Player prefab must have Player component!");
                    return;
                }
                
                playerObj.name = "TestPlayer";
                playerObj.tag = "Player";
            }
            else
            {
                Debug.LogError("Player prefab not assigned!");
            }
        }
        
        /// <summary>
        /// 적 AI 생성 - 기존 AICharacter 컴포넌트 활용
        /// </summary>
        private void CreateEnemies()
        {
            if (enemyPrefab == null || player == null)
            {
                Debug.LogError("Enemy prefab or Player not available!");
                return;
            }
            
            enemies = new AICharacter[enemySpawnPositions.Length];
            
            for (int i = 0; i < enemySpawnPositions.Length; i++)
            {
                GameObject enemyObj = Instantiate(enemyPrefab, enemySpawnPositions[i], Quaternion.identity);
                AICharacter enemy = enemyObj.GetComponent<AICharacter>();
                
                if (enemy != null)
                {
                    enemy.name = $"TestEnemy_{i}";
                    enemy.Target = player; // 플레이어를 타겟으로 설정
                    enemies[i] = enemy;
                }
                else
                {
                    Debug.LogError($"Enemy prefab must have AICharacter component!");
                }
            }
        }
        
        /// <summary>
        /// 카메라 설정 - 기존 카메라 시스템 활용
        /// </summary>
        private void SetupCamera()
        {
            if (player == null) return;
            
            // 플레이어의 카메라 디렉터가 자동으로 카메라를 관리
            var cameraDirector = player.GetComponentInChildren<PlayerCameraDirector>();
            if (cameraDirector != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log("Camera Director found and active");
                }
            }
            else
            {
                Debug.LogWarning("PlayerCameraDirector not found on player!");
            }
        }
        
        /// <summary>
        /// 디버그 정보 표시
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugInfo || player == null) return;
            
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.5f));
            style.padding = new RectOffset(10, 10, 10, 10);
            
            string info = $"=== Test Scene Debug Info ===\n\n";
            info += $"Player Health: {player.Health.Value:F0} / {player.MaxHealth.Value:F0}\n";
            info += $"Player Stamina: {player.Stamina.Value:F0} / {player.MaxStamina.Value:F0}\n";
            info += $"Player State: {GetPlayerState()}\n\n";
            info += $"Controls:\n";
            info += $"  WASD - Move\n";
            info += $"  Mouse - Look\n";
            info += $"  Left Click - Light Attack\n";
            info += $"  Right Click - Block\n";
            info += $"  Space - Dodge\n";
            
            GUI.Label(new Rect(10, 10, 300, 250), info, style);
        }
        
        private string GetPlayerState()
        {
            if (player.IsDead) return "Dead";
            if (player.IsAttacking) return "Attacking";
            if (player.IsBlocking) return "Blocking";
            if (player.IsDodging) return "Dodging";
            if (player.IsStaggered) return "Staggered";
            return "Idle/Moving";
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}