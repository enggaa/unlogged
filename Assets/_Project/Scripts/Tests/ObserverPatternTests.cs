using NUnit.Framework;
using Patterns.Observer;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Bug 14: Observer 패턴 Unsubscribe 메서드 없음 테스트
    /// 증상: 구독 해제 기능이 없어 메모리 누수 가능
    /// 수정: Unobserve(), UnobserveAll() 메서드 추가
    /// </summary>
    public class ObserverPatternTests
    {
        private TestObserver observer1;
        private TestObserver observer2;
        private TestObserver observer3;

        [SetUp]
        public void SetUp()
        {
            // MessageSystem 초기화
            MessageSystem.Init();
            
            observer1 = new TestObserver("Observer1");
            observer2 = new TestObserver("Observer2");
            observer3 = new TestObserver("Observer3");
        }

        [TearDown]
        public void TearDown()
        {
            // 모든 구독 해제
            observer1.UnobserveAll();
            observer2.UnobserveAll();
            observer3.UnobserveAll();
        }

        [Test]
        public void Bug14_Observe_후_Notify하면_이벤트_수신()
        {
            // Given: Combat_Hit 메시지 구독
            observer1.Observe(Message.Combat_Hit);

            // When: 메시지 발송
            this.Notify(Message.Combat_Hit, "test_arg");

            // Then: observer1이 메시지를 받아야 함
            Assert.AreEqual(1, observer1.NotificationCount, 
                "구독한 observer가 메시지를 받아야 합니다.");
            Assert.AreEqual(Message.Combat_Hit, observer1.LastMessage);
        }

        [Test]
        public void Bug14_Unobserve_후에는_이벤트_미수신()
        {
            // Given: 구독 후 구독 해제
            observer1.Observe(Message.Combat_Hit);
            observer1.Unobserve(Message.Combat_Hit);

            // When: 메시지 발송
            this.Notify(Message.Combat_Hit);

            // Then: observer1이 메시지를 받지 않아야 함
            Assert.AreEqual(0, observer1.NotificationCount, 
                "Bug 수정 후: Unobserve 후에는 메시지를 받지 않아야 합니다.");
        }

        [Test]
        public void Bug14_여러_Observer가_각자_독립적으로_구독_해제()
        {
            // Given: 3명의 observer가 모두 구독
            observer1.Observe(Message.Combat_Death);
            observer2.Observe(Message.Combat_Death);
            observer3.Observe(Message.Combat_Death);

            // When: observer2만 구독 해제
            observer2.Unobserve(Message.Combat_Death);

            // 메시지 발송
            this.Notify(Message.Combat_Death);

            // Then: observer1, observer3만 받고 observer2는 못 받음
            Assert.AreEqual(1, observer1.NotificationCount, "observer1은 메시지를 받아야 합니다.");
            Assert.AreEqual(0, observer2.NotificationCount, "observer2는 메시지를 받지 않아야 합니다.");
            Assert.AreEqual(1, observer3.NotificationCount, "observer3은 메시지를 받아야 합니다.");
        }

        [Test]
        public void Bug14_UnobserveAll_모든_구독_해제()
        {
            // Given: 여러 메시지 구독
            observer1.Observe(Message.Combat_Hit);
            observer1.Observe(Message.Combat_Death);
            observer1.Observe(Message.Movement_Step);

            // When: 모든 구독 해제
            observer1.UnobserveAll();

            // 각 메시지 발송
            this.Notify(Message.Combat_Hit);
            this.Notify(Message.Combat_Death);
            this.Notify(Message.Movement_Step);

            // Then: 어떤 메시지도 받지 않아야 함
            Assert.AreEqual(0, observer1.NotificationCount, 
                "UnobserveAll 후에는 어떤 메시지도 받지 않아야 합니다.");
        }

        [Test]
        public void Bug14_중복_구독_해제_시_예외_없음()
        {
            // Given: 한 번만 구독
            observer1.Observe(Message.Combat_Hit);

            // When: 여러 번 구독 해제
            // Then: 예외가 발생하지 않아야 함
            Assert.DoesNotThrow(() =>
            {
                observer1.Unobserve(Message.Combat_Hit);
                observer1.Unobserve(Message.Combat_Hit);
                observer1.Unobserve(Message.Combat_Hit);
            }, "중복 구독 해제 시 예외가 발생하지 않아야 합니다.");
        }

        [Test]
        public void Bug14_구독하지_않은_메시지_해제_시_예외_없음()
        {
            // Given: 아무것도 구독하지 않음

            // When: 구독하지 않은 메시지 해제
            // Then: 예외가 발생하지 않아야 함
            Assert.DoesNotThrow(() =>
            {
                observer1.Unobserve(Message.Combat_Hit);
            }, "구독하지 않은 메시지 해제 시 예외가 발생하지 않아야 합니다.");
        }

        [Test]
        public void Bug14_재구독_후_정상_작동()
        {
            // Given: 구독 → 해제 → 재구독
            observer1.Observe(Message.Combat_Dodge);
            observer1.Unobserve(Message.Combat_Dodge);
            observer1.Observe(Message.Combat_Dodge);

            // When: 메시지 발송
            this.Notify(Message.Combat_Dodge);

            // Then: 메시지를 받아야 함
            Assert.AreEqual(1, observer1.NotificationCount, 
                "재구독 후에는 메시지를 다시 받아야 합니다.");
        }

        [Test]
        public void Bug14_메모리_누수_방지_시나리오()
        {
            // Given: 100명의 임시 observer 생성 및 구독
            var tempObservers = new List<TestObserver>();
            for (int i = 0; i < 100; i++)
            {
                var temp = new TestObserver($"Temp{i}");
                temp.Observe(Message.Combat_Hit);
                tempObservers.Add(temp);
            }

            // When: 모두 구독 해제
            foreach (var temp in tempObservers)
            {
                temp.UnobserveAll();
            }

            // 메시지 발송
            this.Notify(Message.Combat_Hit);

            // Then: 아무도 메시지를 받지 않아야 함
            foreach (var temp in tempObservers)
            {
                Assert.AreEqual(0, temp.NotificationCount, 
                    "구독 해제 후 메시지를 받지 않아야 합니다.");
            }
        }
    }

    /// <summary>
    /// 테스트용 Observer 구현
    /// </summary>
    public class TestObserver : IObserver
    {
        public string Name { get; private set; }
        public int NotificationCount { get; private set; }
        public Message LastMessage { get; private set; }
        public object LastSender { get; private set; }
        public object[] LastArgs { get; private set; }

        public TestObserver(string name)
        {
            Name = name;
            NotificationCount = 0;
        }

        public void OnNotification(object sender, Message msg, params object[] args)
        {
            NotificationCount++;
            LastMessage = msg;
            LastSender = sender;
            LastArgs = args;
        }

        public void Reset()
        {
            NotificationCount = 0;
            LastMessage = default;
            LastSender = null;
            LastArgs = null;
        }
    }
}
