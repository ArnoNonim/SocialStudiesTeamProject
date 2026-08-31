using _00_Members.KYM.Scripts.Soldiers;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Humans.Effects
{
    public sealed class HumanCorpseBleedingModule : MonoBehaviour, IModule
    {
        [SerializeField] private Material bloodMaterial;
        [SerializeField, Min(0.5f)] private float bleedingDuration = 18f;
        [SerializeField, Min(0f)] private float startDelay = 0.12f;
        [SerializeField, Min(0f)] private float pressureDuration = 3.8f;

        private AbstractHuman _human;
        private RagdollController _ragdoll;
        private Animator _animator;
        private HumanDamage _lastDamage;
        private GameObject _effectRoot;
        private bool _hasDamage;
        private bool _pending;
        private float _startTime;

        public void Initialize(ModuleOwner owner)
        {
            _human = owner as AbstractHuman;
            _ragdoll = owner.GetModule<RagdollController>();
            _animator = owner.GetComponentInChildren<Animator>(true);
            if (_human == null)
            {
                enabled = false;
                return;
            }

            _human.Damaged += OnDamaged;
            _human.Died += OnDied;
            _human.Revived += OnRevived;
        }

        private void OnDamaged(HumanDamage damage)
        {
            _lastDamage = damage;
            _hasDamage = true;
        }

        private void OnDied()
        {
            CleanupEffect();
            _pending = true;
            _startTime = Time.time + startDelay;
        }

        private void Update()
        {
            if (!_pending || Time.time < _startTime)
            {
                return;
            }

            // Body explosions already own their cut-surface effects. Waiting for
            // an active ragdoll prevents a duplicate emitter on the hidden body.
            if (_ragdoll != null && !_ragdoll.IsRagdollActive)
            {
                if (Time.time - _startTime > 0.5f)
                {
                    _pending = false;
                }

                return;
            }

            _pending = false;
            CreateBleedingEffect();
        }

        private void CreateBleedingEffect()
        {
            if (bloodMaterial == null)
            {
                return;
            }

            Vector3 woundPoint = _hasDamage
                ? _lastDamage.HitPoint
                : _human.DamageSamplePoint;
            Transform anchor = FindClosestBodyAnchor(woundPoint);
            Vector3 outward = _hasDamage && _lastDamage.Direction.sqrMagnitude > 0f
                ? -_lastDamage.Direction
                : _human.transform.up;

            _effectRoot = new GameObject("CorpseBleeding");
            _effectRoot.layer = _human.gameObject.layer;
            _effectRoot.transform.SetPositionAndRotation(
                woundPoint,
                SafeLookRotation(outward));
            _effectRoot.transform.SetParent(anchor, true);

            CreatePressurePulse(_effectRoot.transform);
            CreateGravityDrips(_effectRoot.transform);
            Destroy(_effectRoot, bleedingDuration + 2f);
        }

        private Transform FindClosestBodyAnchor(Vector3 point)
        {
            if (_animator == null)
            {
                return _human.transform;
            }

            Rigidbody[] bodies = _animator.GetComponentsInChildren<Rigidbody>(true);
            Transform closest = _animator.transform;
            float closestSqrDistance = float.MaxValue;
            foreach (Rigidbody body in bodies)
            {
                float sqrDistance = (body.worldCenterOfMass - point).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closest = body.transform;
            }

            return closest;
        }

        private void CreatePressurePulse(Transform parent)
        {
            GameObject child = new GameObject("PressurePulse");
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);

            ParticleSystem system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = Mathf.Max(0.5f, pressureDuration);
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.58f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.38f, 1.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.042f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.3f, 0.004f, 0.006f, 0.96f),
                new Color(0.07f, 0.001f, 0.002f, 0.9f));
            main.gravityModifier = 1.15f;
            main.maxParticles = 84;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 10, 15),
                new ParticleSystem.Burst(0.2f, 7, 11),
                new ParticleSystem.Burst(0.52f, 5, 9),
                new ParticleSystem.Burst(1.05f, 4, 7),
                new ParticleSystem.Burst(1.8f, 3, 5),
                new ParticleSystem.Burst(2.7f, 2, 4)
            });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 17f;
            shape.radius = 0.025f;

            ConfigureCollisionAndNoise(system, 0.22f, 0.12f);
            ConfigureTextureSheet(system, 8, 11);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = bloodMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.7f;
            renderer.velocityScale = 0.2f;
            system.Play();
        }

        private void CreateGravityDrips(Transform parent)
        {
            GameObject child = new GameObject("GravityDrips");
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);

            ParticleSystem system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = bleedingDuration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.034f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.24f, 0.002f, 0.004f, 0.95f),
                new Color(0.055f, 0.001f, 0.001f, 0.9f));
            main.gravityModifier = 1.9f;
            main.maxParticles = 112;

            ParticleSystem.EmissionModule emission = system.emission;
            AnimationCurve taper = new AnimationCurve(
                new Keyframe(0f, 9f),
                new Keyframe(0.18f, 6.5f),
                new Keyframe(0.55f, 3.2f),
                new Keyframe(1f, 0f));
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f, taper);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.025f;
            shape.radiusThickness = 1f;

            ConfigureCollisionAndNoise(system, 0.38f, 0.055f);
            ConfigureTextureSheet(system, 8, 11);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = bloodMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.45f;
            renderer.velocityScale = 0.08f;
            system.Play();
        }

        private static void ConfigureCollisionAndNoise(
            ParticleSystem system,
            float dampen,
            float noiseStrength)
        {
            ParticleSystem.CollisionModule collision = system.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.dampen = dampen;
            collision.bounce = 0.01f;
            collision.lifetimeLoss = 0.18f;
            collision.radiusScale = 0.22f;
            collision.enableDynamicColliders = false;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.strength = noiseStrength;
            noise.frequency = 0.75f;
            noise.damping = true;
        }

        private static void ConfigureTextureSheet(ParticleSystem system, int firstFrame, int lastFrame)
        {
            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.numTilesX = 4;
            sheet.numTilesY = 4;
            float minFrame = Mathf.Clamp(firstFrame, 0, 15) / 16f;
            float maxFrame = (Mathf.Clamp(lastFrame, firstFrame, 15) + 0.999f) / 16f;
            sheet.startFrame = new ParticleSystem.MinMaxCurve(minFrame, maxFrame);
            sheet.frameOverTime = 0f;
            sheet.cycleCount = 1;
        }

        private static Quaternion SafeLookRotation(Vector3 forward)
        {
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.up;
            }

            Vector3 up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.95f
                ? Vector3.forward
                : Vector3.up;
            return Quaternion.LookRotation(forward.normalized, up);
        }

        private void OnRevived()
        {
            _pending = false;
            _hasDamage = false;
            CleanupEffect();
        }

        private void CleanupEffect()
        {
            if (_effectRoot != null)
            {
                Destroy(_effectRoot);
                _effectRoot = null;
            }
        }

        private void OnDestroy()
        {
            if (_human == null)
            {
                return;
            }

            _human.Damaged -= OnDamaged;
            _human.Died -= OnDied;
            _human.Revived -= OnRevived;
        }
    }
}
