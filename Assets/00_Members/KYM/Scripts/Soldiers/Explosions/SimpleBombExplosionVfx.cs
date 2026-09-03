using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions
{
    public sealed class SimpleBombExplosionVfx : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float scale = 1f;
        [SerializeField] private Color fireColor = new Color(1f, 0.24f, 0.025f, 1f);
        [SerializeField] private Color smokeColor = new Color(0.2f, 0.18f, 0.16f, 0.72f);

        private readonly List<Material> _runtimeMaterials = new List<Material>();
        private Texture2D _softParticleTexture;
        private Light _flashLight;
        private float _lightStartIntensity;
        private float _elapsed;

        private void Awake()
        {
            _softParticleTexture = CreateSoftParticleTexture();
            BuildEffect();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_flashLight != null)
            {
                float fade = 1f - Mathf.Clamp01(_elapsed / 0.18f);
                _flashLight.intensity = _lightStartIntensity * fade * fade;
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _runtimeMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            if (_softParticleTexture != null)
            {
                Destroy(_softParticleTexture);
            }
        }

        private void BuildEffect()
        {
            Material flashMaterial = CreateParticleMaterial(new Color(1f, 0.75f, 0.28f, 1f), true);
            Material fireMaterial = CreateParticleMaterial(fireColor, true);
            Material smokeMaterial = CreateParticleMaterial(smokeColor, false);
            Material debrisMaterial = CreateParticleMaterial(new Color(0.18f, 0.12f, 0.08f, 1f), false);

            CreateBurst(
                "Flash",
                flashMaterial,
                2,
                new Vector2(0f, 0.4f),
                new Vector2(4.5f, 6.5f),
                new Vector2(0.08f, 0.14f),
                0f,
                false);
            CreateBurst(
                "Fireball",
                fireMaterial,
                34,
                new Vector2(4f, 9f),
                new Vector2(0.35f, 1.1f),
                new Vector2(0.22f, 0.5f),
                -0.05f,
                true);
            CreateBurst(
                "Dust",
                smokeMaterial,
                52,
                new Vector2(2.5f, 7f),
                new Vector2(0.7f, 1.8f),
                new Vector2(1.1f, 2.4f),
                -0.08f,
                true);
            CreateBurst(
                "Debris",
                debrisMaterial,
                42,
                new Vector2(6f, 14f),
                new Vector2(0.045f, 0.14f),
                new Vector2(0.65f, 1.5f),
                1.4f,
                false);

            GameObject lightObject = new GameObject("ExplosionLight");
            lightObject.transform.SetParent(transform, false);
            _flashLight = lightObject.AddComponent<Light>();
            _flashLight.type = LightType.Point;
            _flashLight.color = new Color(1f, 0.34f, 0.08f);
            _flashLight.range = 14f * scale;
            _flashLight.intensity = 2200f * scale;
            _flashLight.shadows = LightShadows.None;
            _lightStartIntensity = _flashLight.intensity;
        }

        private void CreateBurst(
            string systemName,
            Material material,
            short count,
            Vector2 speed,
            Vector2 size,
            Vector2 lifetime,
            float gravity,
            bool useNoise)
        {
            GameObject systemObject = new GameObject(systemName);
            systemObject.SetActive(false);
            systemObject.transform.SetParent(transform, false);
            ParticleSystem system = systemObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x * scale, speed.y * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x * scale, size.y * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.maxParticles = count + 4;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f * scale;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.82f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = colorGradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

            if (useNoise)
            {
                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = 0.65f * scale;
                noise.frequency = 0.55f;
                noise.scrollSpeed = 0.5f;
            }

            ParticleSystemRenderer particleRenderer = systemObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.material = material;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            systemObject.SetActive(true);
            system.Play(true);
        }

        private Material CreateParticleMaterial(Color color, bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                name = "Runtime Bomb Particle",
                mainTexture = _softParticleTexture,
                color = color,
                renderQueue = 3000
            };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", additive ? (float)BlendMode.SrcAlpha : (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _runtimeMaterials.Add(material);
            return material;
        }

        private static Texture2D CreateSoftParticleTexture()
        {
            const int resolution = 32;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "Runtime Soft Particle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 uv = new Vector2(
                        (x + 0.5f) / resolution * 2f - 1f,
                        (y + 0.5f) / resolution * 2f - 1f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - uv.magnitude), 1.8f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
