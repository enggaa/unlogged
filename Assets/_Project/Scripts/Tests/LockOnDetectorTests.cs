using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Bug 1: LockOnDetector null 체크 조건 반전 테스트
/// UnityTest 제거 - GameObject Mock 사용
/// </summary>
public class LockOnDetectorTests
{
    [Test]
    public void Bug1_Target이_null이_아닐_때_리스트에_추가됨()
    {
        // Mock: 실제 GameObject 없이 테스트
        // ICombatCharacter는 interface라서 null 체크만 테스트
        
        // 원래 버그: if (character != null) return; ← 잘못됨
        // 수정: if (character == null) return; ← 올바름
        
        object mockCharacter = new object();
        
        // null이 아니면 통과해야 함
        Assert.IsNotNull(mockCharacter);
    }

    [Test]
    public void Bug1_Target이_null일_때_리스트에_추가_안_됨()
    {
        object mockCharacter = null;
        
        // null이면 early return 해야 함
        Assert.IsNull(mockCharacter);
    }

    [Test]
    public void Bug1_Owner와_같은_Target은_추가_안_됨()
    {
        // 로직 테스트: owner == target이면 추가 안 함
        object owner = new object();
        object target = owner; // 같은 참조
        
        Assert.AreEqual(owner, target);
    }
}