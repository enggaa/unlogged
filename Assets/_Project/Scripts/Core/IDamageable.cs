namespace GameCore
{
    public interface IDamageable
    {
        void TakeDamage(DamageData damageData);
        bool IsDead();
    }
}