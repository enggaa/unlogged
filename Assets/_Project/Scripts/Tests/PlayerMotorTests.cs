using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using BrightSouls.Gameplay;
using BrightSouls;

namespace Tests
{
    /// <summary>
    /// Bug 9, 10: PlayerMotor 입력 처리 테스트
    /// Bug 9: ClampMovementInput에서 음수 입력이 0으로 밀려 왼쪽/뒤 이동 불가
    /// Bug 10: Move 입력의 canceled 이벤트 미구독으로 키 놓아도 계속 이동
    /// </summary>
    public class PlayerMotorTests
    {
        private GameObject playerObj;
        private Player player;
        private PlayerMotor motor;
        private MockPlayerInput input;

        [SetUp]
        public void SetUp()
        {
            // Player GameObject 생성
            playerObj = new GameObject("TestPlayer");
            
            // 필수 컴포넌트들 추가
            var controller = playerObj.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.5f;

            // Mock 컴포넌트들 추가
            input = playerObj.AddComponent<MockPlayerInput>();
            var stateController = playerObj.AddComponent<MockPlayerStateController>();
            
            // Player 컴포넌트는 나중에 추가 (의존성 때문에)
            player = playerObj.AddComponent<Player>();
            motor = playerObj.AddComponent<PlayerMotor>();

            // PlayerMotor reflection으로 player 필드 설정
            var playerField = typeof(PlayerMotor)
                .GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerField.SetValue(motor, player);

            var charControllerField = typeof(PlayerMotor)
                .GetField("charController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            charControllerField.SetValue(motor, controller);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObj);
        }

        [Test]
        public void Bug9_음수_X입력이_정상_처리됨()
        {
            // Given: ClampMovementInput 메서드 접근
            var method = typeof(PlayerMotor).GetMethod("ClampMovementInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // When: 왼쪽 입력 (-1, 0) 클램핑
            var leftInput = new Vector2(-1f, 0f);
            var result = (Vector2)method.Invoke(motor, new object[] { leftInput });

            // Then: X값이 -1로 유지되어야 함
            Assert.AreEqual(-1f, result.x, 0.01f, 
                "Bug 수정 후: 음수 X입력(-1)이 0으로 변환되지 않아야 합니다.");
        }

        [Test]
        public void Bug9_음수_Y입력이_정상_처리됨()
        {
            // Given: ClampMovementInput 메서드
            var method = typeof(PlayerMotor).GetMethod("ClampMovementInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // When: 뒤쪽 입력 (0, -1) 클램핑
            var backInput = new Vector2(0f, -1f);
            var result = (Vector2)method.Invoke(motor, new object[] { backInput });

            // Then: Y값이 -1로 유지되어야 함
            Assert.AreEqual(-1f, result.y, 0.01f, 
                "Bug 수정 후: 음수 Y입력(-1)이 0으로 변환되지 않아야 합니다.");
        }

        [Test]
        public void Bug9_대각선_음수_입력_정상_처리()
        {
            // Given: ClampMovementInput 메서드
            var method = typeof(PlayerMotor).GetMethod("ClampMovementInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // When: 왼쪽 뒤 대각선 입력 (-0.7, -0.7)
            var diagonalInput = new Vector2(-0.7f, -0.7f);
            var result = (Vector2)method.Invoke(motor, new object[] { diagonalInput });

            // Then: 두 값 모두 음수로 유지되어야 함
            Assert.Less(result.x, 0f, "X값은 음수여야 합니다.");
            Assert.Less(result.y, 0f, "Y값은 음수여야 합니다.");
        }

        [Test]
        public void Bug9_데드존_처리_정상_작동()
        {
            // Given: ClampMovementInput 메서드
            var method = typeof(PlayerMotor).GetMethod("ClampMovementInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // When: 작은 값 입력 (0.05, -0.05)
            var smallInput = new Vector2(0.05f, -0.05f);
            var result = (Vector2)method.Invoke(motor, new object[] { smallInput });

            // Then: 데드존 처리로 0이 되어야 함 (양수/음수 무관)
            Assert.AreEqual(0f, result.x, 0.01f, "작은 양수 입력은 데드존으로 0이 되어야 합니다.");
            Assert.AreEqual(0f, result.y, 0.01f, "작은 음수 입력도 데드존으로 0이 되어야 합니다.");
        }

        [Test]
        public void Bug9_크기_1_초과_입력_정규화()
        {
            // Given: ClampMovementInput 메서드
            var method = typeof(PlayerMotor).GetMethod("ClampMovementInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // When: 크기가 1 초과인 입력 (1.5, 1.5)
            var largeInput = new Vector2(1.5f, 1.5f);
            var result = (Vector2)method.Invoke(motor, new object[] { largeInput });

            // Then: 정규화되어 크기가 1이 되어야 함
            Assert.LessOrEqual(result.magnitude, 1.01f, 
                "크기가 1 초과인 입력은 정규화되어야 합니다.");
            Assert.GreaterOrEqual(result.magnitude, 0.99f, 
                "정규화 후 크기는 약 1이어야 합니다.");
        }

        [Test]
        public void Bug9_정상_범위_입력_유지()
        {
            // Given: ClampMovementInput 메서드
            var method = typeof(PlayerMotor).GetMethod("ClampMovementInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // When: 정상 범위 입력 (0.5, -0.5)
            var normalInput = new Vector2(0.5f, -0.5f);
            var result = (Vector2)method.Invoke(motor, new object[] { normalInput });

            // Then: 값이 그대로 유지되어야 함
            Assert.AreEqual(0.5f, result.x, 0.01f, "정상 범위의 양수 X는 유지되어야 합니다.");
            Assert.AreEqual(-0.5f, result.y, 0.01f, "정상 범위의 음수 Y는 유지되어야 합니다.");
        }

        [Test]
        public void Bug10_MoveCommand가_CanExecute_정상_작동()
        {
            // Given: MoveCommand 생성
            var moveCommand = new MoveCommand(player);

            // When: CanExecute 체크
            bool canExecute = moveCommand.CanExecute();

            // Then: player가 살아있고 dodging/staggered 아니면 true
            // (초기 상태이므로 true여야 함)
            Assert.IsTrue(canExecute, "초기 상태에서 MoveCommand는 실행 가능해야 합니다.");
        }

        // Bug 10은 Input System의 canceled 이벤트 구독 테스트
        // 실제 Input System을 Mock하기 어려우므로 통합 테스트로 대체 권장
        [Test]
        public void Bug10_Move_Execute로_Vector2_Zero_전달_가능()
        {
            // Given: MoveCommand
            var moveCommand = new MoveCommand(player);

            // When: Vector2.zero 전달
            // Then: 예외 없이 실행되어야 함
            Assert.DoesNotThrow(() =>
            {
                moveCommand.Execute(Vector2.zero);
            }, "Bug 수정 후: canceled 이벤트로 Vector2.zero를 전달해도 예외가 없어야 합니다.");
        }
    }

    /// <summary>
    /// Mock PlayerInput for testing
    /// </summary>
    public class MockPlayerInput : MonoBehaviour
    {
        public UnityEngine.InputSystem.PlayerInput currentActionMap { get; private set; }

        private void Awake()
        {
            // Minimal setup for testing
        }
    }

    /// <summary>
    /// Mock PlayerStateController for testing
    /// </summary>
    public class MockPlayerStateController : MonoBehaviour
    {
        public bool IsDead => false;
        public bool IsDodging => false;
        public bool IsStaggered => false;
    }
}
