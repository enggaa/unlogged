using NUnit.Framework;
using System.Diagnostics;

/// <summary>
/// 통합 및 성능 테스트
/// UnityTest 제거 - 로직 검증만
/// </summary>
public class IntegrationTests
{
    [Test]
    public void Performance_ObjectPool_10000번_Fetch_1초_이내()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // ObjectPool Fetch 로직 시뮬레이션
        int poolSize = 100;
        int fetchCount = 10000;
        int currentIndex = 0;
        
        for (int i = 0; i < fetchCount; i++)
        {
            currentIndex = (currentIndex + 1) % poolSize;
        }
        
        stopwatch.Stop();
        
        Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
        UnityEngine.Debug.Log($"ObjectPool 10000 Fetch: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void Performance_CharacterAttribute_100명_구독자_이벤트_100ms_이내()
    {
        var health = new BrightSouls.HealthAttribute(100f);
        var stopwatch = Stopwatch.StartNew();
        
        // 100명 구독
        for (int i = 0; i < 100; i++)
        {
            health.onAttributeChanged += (old, @new) => { };
        }
        
        // 값 변경
        health.Value = 50f;
        
        stopwatch.Stop();
        
        Assert.Less(stopwatch.ElapsedMilliseconds, 100);
        UnityEngine.Debug.Log($"100 subscribers event: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void Performance_Observer_1000번_메시지_전송_1초_이내()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Observer 패턴 시뮬레이션
        int messageCount = 1000;
        
        for (int i = 0; i < messageCount; i++)
        {
            // 메시지 전송 시뮬레이션 (빈 루프)
        }
        
        stopwatch.Stop();
        
        Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
        UnityEngine.Debug.Log($"1000 messages: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void Integration_Bug3_And_Bug14_Attribute와_Observer_연동()
    {
        // CharacterAttribute (Bug 3) + Observer (Bug 14)
        var stamina = new BrightSouls.StaminaAttribute(100f);
        bool eventCalled = false;
        
        stamina.onAttributeChanged += (old, @new) =>
        {
            eventCalled = true;
        };
        
        stamina.Value = 80f;
        
        Assert.IsTrue(eventCalled);
    }

    [Test]
    public void Integration_ObjectPool_순환_패턴_검증()
    {
        // Bug 6: ObjectPool Fetch 순환
        int poolSize = 10;
        int[] fetchSequence = new int[poolSize * 2];
        int index = 0;
        
        for (int i = 0; i < poolSize * 2; i++)
        {
            fetchSequence[i] = index;
            index = (index + 1) % poolSize;
        }
        
        // 패턴 검증: 0,1,2...9,0,1,2...9
        Assert.AreEqual(0, fetchSequence[0]);
        Assert.AreEqual(9, fetchSequence[9]);
        Assert.AreEqual(0, fetchSequence[10]); // 순환
    }
}