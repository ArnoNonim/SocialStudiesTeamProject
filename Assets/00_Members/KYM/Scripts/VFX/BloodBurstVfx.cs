using UnityEngine;

namespace _00_Members.KYM.Scripts.VFX
{
    public enum BloodBurstMode
    {
        Directional,
        RadialExplosion
    }

    [ExecuteAlways]
    public class BloodBurstVfx : MonoBehaviour
    {
        [SerializeField] private BloodBurstMode previewMode = BloodBurstMode.RadialExplosion;
        [SerializeField] private ParticleSystem coreBurst;
        [SerializeField] private ParticleSystem impactMist;
        [SerializeField] private ParticleSystem spray;
        [SerializeField] private ParticleSystem droplets;

        private readonly Color _freshBlood = new Color(0.34f, 0.005f, 0.008f, 0.95f);
        private readonly Color _darkBlood = new Color(0.075f, 0.002f, 0.003f, 0.9f);

        private void Awake()
        {
            Configure(previewMode);
        }

        private void OnValidate()
        {
            Configure(previewMode);
        }

        [ContextMenu("Preview Blood Burst")]
        public void Preview()
        {
            Play(previewMode);
        }

        public void Play(BloodBurstMode mode)
        {
            Configure(mode);

            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play();
            }
        }

        [ContextMenu("Stop Blood Burst Preview")]
        public void StopPreview()
        {
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Configure(BloodBurstMode mode)
        {
            ResolveParticleSystems();
            ConfigureCoreBurst();
            ConfigureImpactMist();
            ConfigureSpray();
            ConfigureDroplets();

            if (mode == BloodBurstMode.RadialExplosion)
            {
                ConfigureRadialExplosion();
            }
        }

        private void ResolveParticleSystems()
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            coreBurst = coreBurst != null ? coreBurst : GetComponent<ParticleSystem>();

            foreach (ParticleSystem system in systems)
            {
                switch (system.name)
                {
                    case "ImpactMist":
                        impactMist = impactMist != null ? impactMist : system;
                        break;
                    case "Spray":
                        spray = spray != null ? spray : system;
                        break;
                    case "Droplets":
                        droplets = droplets != null ? droplets : system;
                        break;
                }
            }
        }

        private void ConfigureCoreBurst()
        {
            if (coreBurst == null) return;

            ConfigureMain(coreBurst, 0.28f, 0.12f, 0.24f, 1.2f, 3.2f, 0.18f, 0.42f, 0.15f);
            ConfigureBurst(coreBurst, 8, 13);
            ConfigureConeShape(coreBurst, 38f, 0.08f);
            ConfigureColor(coreBurst, _freshBlood, _darkBlood, 0.95f);
            ConfigureSize(coreBurst, 0.35f, 1f, 0.25f);
            ConfigureTextureSheet(coreBurst);
        }

        private void ConfigureImpactMist()
        {
            if (impactMist == null) return;

            ConfigureMain(impactMist, 0.42f, 0.16f, 0.34f, 0.35f, 1.15f, 0.32f, 0.78f, 0.02f);
            ConfigureBurst(impactMist, 12, 18);
            ConfigureConeShape(impactMist, 58f, 0.1f);
            ConfigureColor(impactMist, new Color(0.25f, 0.008f, 0.01f), _darkBlood, 0.55f);
            ConfigureSize(impactMist, 0.25f, 1.1f, 1.35f);
            ConfigureNoise(impactMist, 0.12f, 0.32f, 0.7f);
            ConfigureTextureSheet(impactMist);
        }

        private void ConfigureSpray()
        {
            if (spray == null) return;

            ConfigureMain(spray, 0.72f, 0.3f, 0.68f, 4.5f, 8.5f, 0.045f, 0.12f, 0.75f);
            ConfigureBurst(spray, 18, 28);
            ConfigureConeShape(spray, 16f, 0.035f);
            ConfigureColor(spray, _freshBlood, _darkBlood, 1f);
            ConfigureSize(spray, 0.65f, 1f, 0.4f);
            ConfigureNoise(spray, 0.08f, 0.2f, 0.55f);
            ConfigureCollision(spray, 0.28f);
            ConfigureTextureSheet(spray);

            ParticleSystemRenderer renderer = spray.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 3.2f;
                renderer.velocityScale = 0.18f;
            }
        }

        private void ConfigureDroplets()
        {
            if (droplets == null) return;

            ConfigureMain(droplets, 1.2f, 0.5f, 1.05f, 2.2f, 5.5f, 0.025f, 0.085f, 1.65f);
            ConfigureBurst(droplets, 28, 44);
            ConfigureConeShape(droplets, 48f, 0.075f);
            ConfigureColor(droplets, _freshBlood, _darkBlood, 1f);
            ConfigureSize(droplets, 0.8f, 1f, 0.55f);
            ConfigureNoise(droplets, 0.04f, 0.14f, 0.8f);
            ConfigureCollision(droplets, 0.42f);
            ConfigureTextureSheet(droplets);
        }

        private void ConfigureRadialExplosion()
        {
            ConfigureRadialLayer(coreBurst, 28, 42, 2.8f, 5.8f, 0.18f, 0.38f, 0.28f, 0.66f, 0.1f);
            ConfigureRadialLayer(impactMist, 38, 56, 0.8f, 2.3f, 0.24f, 0.55f, 0.55f, 1.35f, 0.02f);
            ConfigureRadialLayer(spray, 45, 70, 6f, 11f, 0.34f, 0.82f, 0.06f, 0.17f, 0.65f);
            ConfigureRadialLayer(droplets, 62, 90, 3.3f, 8.2f, 0.58f, 1.3f, 0.035f, 0.115f, 1.55f);
        }

        private static void ConfigureRadialLayer(ParticleSystem system, short minCount, short maxCount,
            float minSpeed, float maxSpeed, float minLifetime, float maxLifetime,
            float minSize, float maxSize, float gravity)
        {
            if (system == null)
            {
                return;
            }

            ParticleSystem.MainModule main = system.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.gravityModifier = gravity;
            main.maxParticles = 192;

            ConfigureBurst(system, minCount, maxCount);
            ConfigureSphereShape(system, 0.14f);
        }

        private static void ConfigureMain(ParticleSystem system, float duration, float minLifetime,
            float maxLifetime, float minSpeed, float maxSpeed, float minSize, float maxSize, float gravity)
        {
            ParticleSystem.MainModule main = system.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.gravityModifier = gravity;
            main.maxParticles = 128;
        }

        private static void ConfigureBurst(ParticleSystem system, short minCount, short maxCount)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, minCount, maxCount) });
        }

        private static void ConfigureConeShape(ParticleSystem system, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
        }

        private static void ConfigureSphereShape(ParticleSystem system, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.radiusThickness = 1f;
        }

        private static void ConfigureColor(ParticleSystem system, Color startColor, Color endColor, float startAlpha)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.42f, 0.42f, 0.42f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(startAlpha, 0f),
                    new GradientAlphaKey(startAlpha * 0.9f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;
        }

        private static void ConfigureSize(ParticleSystem system, float start, float middle, float end)
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(0.18f, middle),
                new Keyframe(1f, end));

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void ConfigureNoise(ParticleSystem system, float minStrength, float maxStrength, float frequency)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.strength = new ParticleSystem.MinMaxCurve(minStrength, maxStrength);
            noise.frequency = frequency;
            noise.scrollSpeed = 0.35f;
            noise.damping = true;
        }

        private static void ConfigureCollision(ParticleSystem system, float dampen)
        {
            ParticleSystem.CollisionModule collision = system.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.dampen = dampen;
            collision.bounce = 0.04f;
            collision.lifetimeLoss = 0.18f;
            collision.radiusScale = 0.3f;
            collision.enableDynamicColliders = false;
            collision.maxCollisionShapes = 32;
        }

        private static void ConfigureTextureSheet(ParticleSystem system)
        {
            ParticleSystem.TextureSheetAnimationModule textureSheet = system.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.numTilesX = 4;
            textureSheet.numTilesY = 4;
            textureSheet.frameOverTime = 0f;
            textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);
            textureSheet.cycleCount = 1;
        }
    }
}
