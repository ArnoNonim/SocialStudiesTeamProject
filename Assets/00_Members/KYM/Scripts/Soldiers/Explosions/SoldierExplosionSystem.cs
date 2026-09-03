using System.Collections.Generic;
using _00_Members.KYM.Scripts.Humans;
using _00_Members.KYM.Scripts.Soldiers.DeathEvent;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions
{
    public static class SoldierExplosionSystem
    {
        public static int Detonate(
            Vector3 center,
            ExplosionProfile profile,
            GameObject source = null)
        {
            if (profile == null)
            {
                Debug.LogWarning("Explosion ignored because no ExplosionProfile was provided.");
                return 0;
            }

            Collider[] hits = Physics.OverlapSphere(
                center,
                profile.Radius,
                profile.AffectedLayers,
                QueryTriggerInteraction.Ignore);
            HashSet<AbstractHuman> processedHumans = new HashSet<AbstractHuman>();
            int killedCount = 0;

            foreach (Collider hit in hits)
            {
                AbstractHuman human = hit.GetComponentInParent<AbstractHuman>();
                if (human == null || human.IsDead || !processedHumans.Add(human))
                {
                    continue;
                }

                Vector3 samplePoint = human.DamageSamplePoint;
                float distance = Vector3.Distance(center, samplePoint);
                float exposure = IsCovered(center, samplePoint, human, source, profile.ObstructionLayers)
                    ? profile.CoveredExposure
                    : 1f;
                float effectiveLethalRadius = profile.LethalRadius * Mathf.Sqrt(exposure);
                if (distance > effectiveLethalRadius)
                {
                    continue;
                }

                ExplosionContext context = new ExplosionContext(
                    center,
                    profile.Radius,
                    profile.Force,
                    profile.UpwardModifier,
                    profile.ForceFalloffExponent,
                    exposure,
                    source);

                if (human is Soldier soldier)
                {
                    DeathType deathType = ResolveDeathType(distance, exposure, profile);
                    soldier.DieFromExplosion(deathType, context, samplePoint);
                }
                else
                {
                    Vector3 direction = samplePoint - center;
                    human.ApplyDamage(new HumanDamage(
                        float.MaxValue,
                        samplePoint,
                        direction,
                        context.EvaluateForceAt(samplePoint),
                        source));
                }

                killedCount++;
            }

            return killedCount;
        }

        private static DeathType ResolveDeathType(
            float distance,
            float exposure,
            ExplosionProfile profile)
        {
            if (distance <= profile.BodyExplosionRadius * exposure)
            {
                return DeathType.BodyExplosion;
            }

            if (Random.value <= profile.HeadExplosionChance * exposure)
            {
                return DeathType.HeadExplosion;
            }

            return DeathType.Ragdoll;
        }

        private static bool IsCovered(
            Vector3 center,
            Vector3 target,
            AbstractHuman human,
            GameObject source,
            LayerMask obstructionLayers)
        {
            Vector3 offset = target - center;
            float distance = offset.magnitude;
            if (distance <= 0.2f)
            {
                return false;
            }

            Vector3 direction = offset / distance;
            Vector3 rayStart = center + direction * 0.15f;
            RaycastHit[] obstructionHits = Physics.RaycastAll(
                rayStart,
                direction,
                distance - 0.15f,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit obstructionHit in obstructionHits)
            {
                Transform hitTransform = obstructionHit.transform;
                if (hitTransform == null || hitTransform.IsChildOf(human.transform))
                {
                    continue;
                }

                if (source != null && hitTransform.IsChildOf(source.transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
