using UnityEngine;

namespace _00_Members.KYM.Scripts.Humans
{
    public readonly struct HumanDamage
    {
        public HumanDamage(
            float amount,
            Vector3 hitPoint,
            Vector3 direction,
            float force = 0f,
            GameObject source = null)
        {
            Amount = Mathf.Max(0f, amount);
            HitPoint = hitPoint;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
            Force = Mathf.Max(0f, force);
            Source = source;
        }

        public float Amount { get; }
        public Vector3 HitPoint { get; }
        public Vector3 Direction { get; }
        public float Force { get; }
        public GameObject Source { get; }
    }
}
