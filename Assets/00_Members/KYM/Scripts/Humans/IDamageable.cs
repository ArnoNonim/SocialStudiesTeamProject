namespace _00_Members.KYM.Scripts.Humans
{
    public interface IDamageable
    {
        bool IsDead { get; }
        float CurrentHealth { get; }
        void ApplyDamage(HumanDamage damage);
    }
}
