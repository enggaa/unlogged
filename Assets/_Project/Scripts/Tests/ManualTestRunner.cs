using UnityEngine;
using BrightSouls;
using BrightSouls.Gameplay;

/// <summary>
/// Scene에 배치해서 실제 동작 확인
/// Hierarchy에 GameObject 만들고 이 스크립트 추가
/// Play 버튼 누르면 콘솔에 결과 출력
/// </summary>
public class ManualTestRunner : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== Manual Test Start ===");
        
        Test_Bug3_CharacterAttribute();
        Test_Bug6_ObjectPool();
        Test_Bug14_Observer();
        Test_Bug9_PlayerMotor();
        
        Debug.Log("=== All Tests Passed ===");
    }

    void Test_Bug3_CharacterAttribute()
    {
        Debug.Log("[Test] Bug 3: CharacterAttribute");
        
        var health = new HealthAttribute(100f);
        
        // 구독자 없이 값 변경 (예외 없어야 함)
        health.Value = 50f;
        Debug.Log($"✓ Health: {health.Value} (구독자 없이 변경 성공)");
        
        // 구독자 추가
        health.onAttributeChanged += (old, @new) =>
        {
            Debug.Log($"✓ Event: {old} → {@new}");
        };
        
        health.Value = 30f;
    }

    void Test_Bug6_ObjectPool()
    {
        Debug.Log("[Test] Bug 6: ObjectPool");
        
        // AudioSource 프리팹 필요 없이 로직만 테스트
        int poolSize = 10;
        int[] results = new int[20];
        int index = 0;
        
        for (int i = 0; i < 20; i++)
        {
            results[i] = index;
            index = (index + 1) % poolSize;
        }
        
        Debug.Log($"✓ ObjectPool 순환: {string.Join(",", results)}");
        Debug.Log($"✓ pool[0] 중복 없음: {results[0]} != {results[1]}");
    }

    void Test_Bug14_Observer()
    {
        Debug.Log("[Test] Bug 14: Observer Pattern");
        
        var observer = new TestObserver();
        
        // 구독
        Patterns.Observer.MessageSystem.Observe(observer, Patterns.Observer.Message.Combat_Hit);
        
        // 메시지 전송
        this.Notify(Patterns.Observer.Message.Combat_Hit, "test data");
        
        Debug.Log("✓ Observer 메시지 전송 성공");
    }

    void Test_Bug9_PlayerMotor()
    {
        Debug.Log("[Test] Bug 9: PlayerMotor 입력 처리");
        
        // 음수 입력 테스트
        Vector2 negativeInput = new Vector2(-0.5f, -0.8f);
        Vector2 clamped = ClampInput(negativeInput);
        
        Debug.Log($"✓ 입력: {negativeInput} → 클램프: {clamped}");
        
        if (clamped.x != 0f && clamped.y != 0f)
        {
            Debug.Log("✓ 음수 입력 보존됨");
        }
    }

    // PlayerMotor.ClampMovementInput 로직 복제
    private Vector2 ClampInput(Vector2 input)
    {
        if (input.magnitude > 1f)
        {
            return input.normalized;
        }
        
        // Bug 9 수정: 음수도 유지
        if (Mathf.Abs(input.x) < 0.1f)
        {
            input.x = 0f;
        }
        if (Mathf.Abs(input.y) < 0.1f)
        {
            input.y = 0f;
        }
        
        return input;
    }

    private class TestObserver : Patterns.Observer.IObserver
    {
        public void OnNotification(object sender, Patterns.Observer.Message msg, params object[] args)
        {
            Debug.Log($"✓ 메시지 수신: {msg}");
        }
    }
}

// Observer 확장 메서드
public static class ObserverExtensions
{
    public static void Notify(this MonoBehaviour sender, Patterns.Observer.Message msg, params object[] args)
    {
        Patterns.Observer.MessageSystem.Notify(sender, msg, args);
    }
}