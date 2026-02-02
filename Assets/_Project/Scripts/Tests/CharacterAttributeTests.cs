using NUnit.Framework;
using BrightSouls;

namespace Tests
{
    /// <summary>
    /// Bug 3: CharacterAttribute 이벤트 null 참조 테스트
    /// 증상: onAttributeChanged.Invoke() 호출 시 구독자가 없으면 NullReferenceException 발생
    /// 수정: ?.Invoke()로 null-conditional 연산자 사용
    /// </summary>
    public class CharacterAttributeTests
    {
        [Test]
        public void Bug3_구독자가_없어도_값_변경_시_예외가_발생하지_않음()
        {
            // Given: 구독자가 없는 HealthAttribute
            var health = new HealthAttribute(100f);

            // When: 값을 변경
            // Then: NullReferenceException이 발생하지 않아야 함
            Assert.DoesNotThrow(() => 
            {
                health.Value = 50f;
            }, "Bug 수정 후: 구독자가 없어도 값 변경 시 예외가 발생하지 않아야 합니다.");

            Assert.AreEqual(50f, health.Value, "값이 정상적으로 변경되어야 합니다.");
        }

        [Test]
        public void Bug3_구독자가_있으면_이벤트가_정상적으로_발생함()
        {
            // Given: 구독자가 있는 StaminaAttribute
            var stamina = new StaminaAttribute(100f);
            float oldValue = 0f;
            float newValue = 0f;
            bool eventCalled = false;

            stamina.onAttributeChanged += (old, @new) =>
            {
                oldValue = old;
                newValue = @new;
                eventCalled = true;
            };

            // When: 값을 변경
            stamina.Value = 80f;

            // Then: 이벤트가 발생하고 값이 전달되어야 함
            Assert.IsTrue(eventCalled, "이벤트가 발생해야 합니다.");
            Assert.AreEqual(100f, oldValue, "이전 값이 올바르게 전달되어야 합니다.");
            Assert.AreEqual(80f, newValue, "새 값이 올바르게 전달되어야 합니다.");
        }

        [Test]
        public void Bug3_여러_구독자가_모두_이벤트를_받음()
        {
            // Given: 여러 구독자가 있는 Attribute
            var poise = new PoiseAttribute(100f);
            int callCount = 0;

            poise.onAttributeChanged += (old, @new) => callCount++;
            poise.onAttributeChanged += (old, @new) => callCount++;
            poise.onAttributeChanged += (old, @new) => callCount++;

            // When: 값을 변경
            poise.Value = 50f;

            // Then: 모든 구독자가 이벤트를 받아야 함
            Assert.AreEqual(3, callCount, "3명의 구독자 모두 이벤트를 받아야 합니다.");
        }

        [Test]
        public void Bug3_연속_값_변경_시_매번_이벤트_발생()
        {
            // Given: 구독자가 있는 Attribute
            var health = new HealthAttribute(100f);
            int callCount = 0;

            health.onAttributeChanged += (old, @new) => callCount++;

            // When: 값을 여러 번 변경
            health.Value = 90f;
            health.Value = 80f;
            health.Value = 70f;

            // Then: 매번 이벤트가 발생해야 함
            Assert.AreEqual(3, callCount, "3번의 값 변경에 대해 모두 이벤트가 발생해야 합니다.");
        }

        [Test]
        public void Bug3_StatusAttribute_플래그_연산_시_이벤트_발생()
        {
            // Given: StatusAttribute
            var status = new StatusAttribute();
            int callCount = 0;

            status.onAttributeChanged += (old, @new) => callCount++;

            // When: 플래그 추가/제거
            status.AddStatus(CharacterStatus.Staggered);
            status.AddStatus(CharacterStatus.IFrames);
            status.RemoveStatus(CharacterStatus.Staggered);

            // Then: 각 연산마다 이벤트가 발생해야 함
            Assert.AreEqual(3, callCount, "플래그 연산마다 이벤트가 발생해야 합니다.");
        }
    }
}
