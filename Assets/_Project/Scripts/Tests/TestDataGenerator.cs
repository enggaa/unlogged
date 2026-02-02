using UnityEngine;
using BrightSouls;
using BrightSouls.Gameplay;

namespace BrightSouls.Testing
{
    /// <summary>
    /// 테스트를 위한 ScriptableObject 데이터를 런타임에 생성
    /// </summary>
    public class TestDataGenerator : MonoBehaviour
    {
        public static PlayerAttributeData CreateTestPlayerAttributes()
        {
            var data = ScriptableObject.CreateInstance<PlayerAttributeData>();
            
            // 리플렉션을 통해 private 필드 설정
            var type = typeof(PlayerAttributeData);
            var healthField = type.GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxHealthField = type.GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var staminaField = type.GetField("stamina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxStaminaField = type.GetField("maxStamina", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var poiseField = type.GetField("poise", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxPoiseField = type.GetField("maxPoise", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            healthField?.SetValue(data, 100f);
            maxHealthField?.SetValue(data, 100f);
            staminaField?.SetValue(data, 100f);
            maxStaminaField?.SetValue(data, 100f);
            poiseField?.SetValue(data, 100f);
            maxPoiseField?.SetValue(data, 100f);
            
            return data;
        }
        
        public static PlayerCombatData CreateTestCombatData()
        {
            var data = ScriptableObject.CreateInstance<PlayerCombatData>();
            
            var type = typeof(PlayerCombatData);
            var dodgeStaminaField = type.GetField("dodgeStaminaCost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var blockBreakField = type.GetField("blockBreakDamageModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxBlockAngleField = type.GetField("maximumBlockAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lockOnSpeedField = type.GetField("lockOnbodyRotationLerpSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            dodgeStaminaField?.SetValue(data, 20f);
            blockBreakField?.SetValue(data, 0.2f);
            maxBlockAngleField?.SetValue(data, 100f);
            lockOnSpeedField?.SetValue(data, 7.5f);
            
            return data;
        }
        
        public static PlayerPhysicsData CreateTestPhysicsData()
        {
            var data = ScriptableObject.CreateInstance<PlayerPhysicsData>();
            
            var type = typeof(PlayerPhysicsData);
            var groundLayersField = type.GetField("groundDetectionLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var accelField = type.GetField("accelerationTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var decelField = type.GetField("deccelerationTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fallSpeedField = type.GetField("minimumFallDamageSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fallDamageField = type.GetField("fallDamageMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var blockMoveField = type.GetField("blockingMoveSpeedMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            groundLayersField?.SetValue(data, LayerMask.GetMask("Default"));
            accelField?.SetValue(data, 1f);
            decelField?.SetValue(data, 1f);
            fallSpeedField?.SetValue(data, 15f);
            fallDamageField?.SetValue(data, 3f);
            blockMoveField?.SetValue(data, 0.5f);
            
            return data;
        }
        
        public static WorldPhysicsData CreateTestWorldPhysics()
        {
            var data = ScriptableObject.CreateInstance<WorldPhysicsData>();
            
            var type = typeof(WorldPhysicsData);
            var gravityField = type.GetField("gravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            gravityField?.SetValue(data, new Vector3(0f, -9.81f, 0f));
            
            return data;
        }
    }
}