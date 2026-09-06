using System;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions
{
    [Serializable]
    public sealed class ExplosionProfile
    {
        [Header("범위")]
        [SerializeField, Min(0.1f)] private float radius = 8f;
        [SerializeField, Min(0.1f)] private float lethalRadius = 5f;
        [SerializeField, Min(0f)] private float bodyExplosionRadius = 1.6f;

        [Header("폭발력")]
        [SerializeField, Min(0f)] private float force = 18f;
        [SerializeField, Min(0f)] private float upwardModifier = 0.35f;
        [SerializeField, Min(0.1f)] private float forceFalloffExponent = 1.35f;

        [Header("엄폐물 판정")]
        [SerializeField, Range(0f, 1f)] private float coveredExposure = 0.2f;
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private LayerMask obstructionLayers = ~0;

        [Header("사망 연출 분기")]
        [SerializeField, Range(0f, 1f)] private float headExplosionChance = 0.05f;

        public float Radius => Mathf.Max(0.1f, radius);
        public float LethalRadius => Mathf.Clamp(lethalRadius, 0.1f, Radius);
        public float BodyExplosionRadius => Mathf.Clamp(bodyExplosionRadius, 0f, LethalRadius);
        public float Force => Mathf.Max(0f, force);
        public float UpwardModifier => Mathf.Max(0f, upwardModifier);
        public float ForceFalloffExponent => Mathf.Max(0.1f, forceFalloffExponent);
        public float CoveredExposure => Mathf.Clamp01(coveredExposure);
        public LayerMask AffectedLayers => affectedLayers;
        public LayerMask ObstructionLayers => obstructionLayers;
        public float HeadExplosionChance => Mathf.Clamp01(headExplosionChance);
    }
}
