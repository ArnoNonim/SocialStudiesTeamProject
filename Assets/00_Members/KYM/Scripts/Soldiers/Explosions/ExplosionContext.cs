using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions
{
    public readonly struct ExplosionContext
    {
        public ExplosionContext(
            Vector3 center,
            float radius,
            float force,
            float upwardModifier,
            float forceFalloffExponent,
            float exposure,
            GameObject source)
        {
            Center = center;
            Radius = radius;
            Force = force;
            UpwardModifier = upwardModifier;
            ForceFalloffExponent = forceFalloffExponent;
            Exposure = exposure;
            Source = source;
        }

        public Vector3 Center { get; }
        public float Radius { get; }
        public float Force { get; }
        public float UpwardModifier { get; }
        public float ForceFalloffExponent { get; }
        public float Exposure { get; }
        public GameObject Source { get; }

        public float EvaluateForceAt(Vector3 worldPosition)
        {
            float distance01 = Mathf.Clamp01(Vector3.Distance(Center, worldPosition) / Radius);
            float falloff = Mathf.Pow(1f - distance01, ForceFalloffExponent);
            return Force * Exposure * falloff;
        }
    }
}
