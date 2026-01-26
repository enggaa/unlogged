using UnityEngine;

namespace GameCore
{
    [System.Serializable]
    public class DamageData
    {
        public float damageAmount;
        public DamageType damageType;
        public GameObject attacker;
        public Vector3 hitPoint;
        public Vector3 hitDirection;

        public DamageData(float damage, DamageType type, GameObject source)
        {
            damageAmount = damage;
            damageType = type;
            attacker = source;
        }

        public DamageData(float damage, DamageType type, GameObject source, Vector3 point, Vector3 direction)
        {
            damageAmount = damage;
            damageType = type;
            attacker = source;
            hitPoint = point;
            hitDirection = direction;
        }
    }

    public enum DamageType
    {
        Physical,
        Magic,
        Fire,
        Lightning,
        Dark
    }
}