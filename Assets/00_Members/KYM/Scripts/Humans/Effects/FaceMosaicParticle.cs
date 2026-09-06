using UnityEngine;
using UnityEngine.Rendering;

namespace _00_Members.KYM.Scripts.Humans.Effects
{
    [DisallowMultipleComponent]
    [AddComponentMenu("KYM/Humans/Face Mosaic Particle")]
    public sealed class FaceMosaicParticle : MonoBehaviour
    {
        private const string MosaicObjectName = "Face Mosaic Particle";
        private const string MosaicShaderName = "KYM/Face Mosaic Particle";

        [Header("Face Tracking")]
        [SerializeField] private Transform head;
        [Tooltip("Offset from the humanoid Head bone. Positive Z normally points toward the face.")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.035f, 0.13f);
        [SerializeField] private Vector2 worldSize = new Vector2(0.3f, 0.38f);

        [Header("Mosaic")]
        [SerializeField, Range(2f, 64f)] private float blockSize = 12f;
        [SerializeField, Range(2f, 32f)] private float colorSteps = 10f;
        [SerializeField, Range(0f, 1f)] private float opacity = 1f;
        [Tooltip("Uses the URP Camera Opaque Texture for a real screen-space mosaic. Disable this when Opaque Texture is unavailable.")]
        [SerializeField] private bool sampleSceneColor = true;
        [SerializeField] private Color fallbackTint = new Color(0.42f, 0.44f, 0.46f, 1f);

        private static readonly int BlockSizeId = Shader.PropertyToID("_BlockSize");
        private static readonly int ColorStepsId = Shader.PropertyToID("_ColorSteps");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int UseSceneColorId = Shader.PropertyToID("_UseSceneColor");
        private static readonly int FallbackTintId = Shader.PropertyToID("_FallbackTint");

        private Transform _mosaicTransform;
        private ParticleSystem _particleSystem;
        private ParticleSystemRenderer _particleRenderer;
        private Material _runtimeMaterial;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            ResolveHead();
            BuildParticle();
        }

        private void OnEnable()
        {
            ResolveHead();
            BuildParticle();
            EmitMosaicParticle();
        }

        private void LateUpdate()
        {
            if (head == null)
            {
                ResolveHead();
            }

            if (_mosaicTransform == null || head == null)
            {
                if (_particleRenderer != null)
                {
                    _particleRenderer.enabled = false;
                }

                return;
            }

            _mosaicTransform.SetPositionAndRotation(head.TransformPoint(localOffset), Quaternion.identity);
            _particleRenderer.enabled = true;
            ApplyMaterialProperties();
        }

        private void OnDisable()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeMaterial);
            }
            else
            {
                DestroyImmediate(_runtimeMaterial);
            }
        }

        private void OnValidate()
        {
            worldSize.x = Mathf.Max(0.01f, worldSize.x);
            worldSize.y = Mathf.Max(0.01f, worldSize.y);
            ApplyMaterialProperties();
        }

        [ContextMenu("Find Humanoid Head")]
        private void ResolveHead()
        {
            if (head != null)
            {
                return;
            }

            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
            }

            if (head == null)
            {
                head = FindHeadByName(transform);
            }
        }

        private void BuildParticle()
        {
            if (_particleSystem != null)
            {
                ApplyMaterialProperties();
                return;
            }

            Transform existing = transform.Find(MosaicObjectName);
            GameObject mosaicObject;
            if (existing != null)
            {
                mosaicObject = existing.gameObject;
            }
            else
            {
                mosaicObject = new GameObject(MosaicObjectName);
                mosaicObject.transform.SetParent(transform, false);
            }

            _mosaicTransform = mosaicObject.transform;
            _particleSystem = mosaicObject.GetComponent<ParticleSystem>();
            if (_particleSystem == null)
            {
                _particleSystem = mosaicObject.AddComponent<ParticleSystem>();
            }

            _particleRenderer = mosaicObject.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleSystem();
            ConfigureRenderer();
            CreateRuntimeMaterial();
            ApplyMaterialProperties();
        }

        private void ConfigureParticleSystem()
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 86400f;
            main.startSpeed = 0f;
            main.startRotation = 0f;
            main.startSize3D = true;
            main.startSizeX = worldSize.x;
            main.startSizeY = worldSize.y;
            main.startSizeZ = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 1;

            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _particleSystem.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = false;
        }

        private void ConfigureRenderer()
        {
            _particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            _particleRenderer.alignment = ParticleSystemRenderSpace.View;
            _particleRenderer.sortMode = ParticleSystemSortMode.Distance;
            _particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _particleRenderer.receiveShadows = false;
            _particleRenderer.lightProbeUsage = LightProbeUsage.Off;
            _particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _particleRenderer.sortingOrder = 50;
        }

        private void CreateRuntimeMaterial()
        {
            if (_runtimeMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find(MosaicShaderName);
            if (shader == null)
            {
                Debug.LogError($"Face mosaic shader '{MosaicShaderName}' was not found.", this);
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = $"{name} Face Mosaic (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            _particleRenderer.sharedMaterial = _runtimeMaterial;
        }

        private void ApplyMaterialProperties()
        {
            if (_particleRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _particleRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(BlockSizeId, blockSize);
            _propertyBlock.SetFloat(ColorStepsId, colorSteps);
            _propertyBlock.SetFloat(OpacityId, opacity);
            _propertyBlock.SetFloat(UseSceneColorId, sampleSceneColor ? 1f : 0f);
            _propertyBlock.SetColor(FallbackTintId, fallbackTint);
            _particleRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void EmitMosaicParticle()
        {
            if (_particleSystem == null)
            {
                return;
            }

            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                startLifetime = 86400f,
                startSize3D = new Vector3(worldSize.x, worldSize.y, 1f),
                startColor = Color.white
            };
            _particleSystem.Emit(emitParams, 1);
            _particleSystem.Play();
        }

        private static Transform FindHeadByName(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                string lowerName = child.name.ToLowerInvariant();
                if (lowerName == "head" || lowerName.EndsWith(":head") || lowerName == "mixamorighead")
                {
                    return child;
                }
            }

            return null;
        }
    }
}
