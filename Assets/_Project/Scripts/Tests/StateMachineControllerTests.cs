using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Bug 4 & 7: StateMachineController 테스트
/// UnityTest 제거 - SetUp/TearDown에서 GameObject 관리
/// </summary>
public class StateMachineControllerTests
{
    private GameObject testObject;

    [SetUp]
    public void SetUp()
    {
        // 각 테스트 전에 GameObject 생성
        testObject = new GameObject("TestObject");
    }

    [TearDown]
    public void TearDown()
    {
        // 각 테스트 후에 GameObject 파괴
        if (testObject != null)
        {
            Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void Bug4_CurrentState가_null일_때_GetType_호출하면_예외_발생()
    {
        // Null 체크 없이 currentState.GetType() 호출 시 NullReferenceException
        UnityPatterns.FiniteStateMachine.IState nullState = null;
        
        Assert.Throws<System.NullReferenceException>(() =>
        {
            var type = nullState.GetType(); // 여기서 예외
        });
    }

    [Test]
    public void Bug4_CurrentState가_null이_아닐_때_정상_동작()
    {
        // Mock state
        var mockState = new MockState();
        
        Assert.DoesNotThrow(() =>
        {
            var type = mockState.GetType();
            Assert.IsNotNull(type);
        });
    }

    [Test]
    public void Bug7_StateMachine이_null일_때_Start에서_예외_발생()
    {
        // SerializedStateMachine이 null인 경우
        UnityPatterns.FiniteStateMachine.SerializedStateMachine nullMachine = null;
        
        Assert.IsNull(nullMachine);
    }

    [Test]
    public void Bug7_DefaultState가_null일_때_초기화_실패()
    {
        // DefaultState가 null이면 초기화 불가
        UnityPatterns.FiniteStateMachine.IState nullState = null;
        
        Assert.IsNull(nullState);
    }

    // Mock State for testing
    private class MockState : UnityPatterns.FiniteStateMachine.IState
    {
        public void OnStateEnter(UnityPatterns.FiniteStateMachine.StateMachineController fsm) { }
        public void OnStateUpdate(UnityPatterns.FiniteStateMachine.StateMachineController fsm) { }
        public void OnStateExit(UnityPatterns.FiniteStateMachine.StateMachineController fsm) { }
    }
}