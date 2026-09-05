using System;
using System.Collections.Generic;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class SoldierDismemberment : MonoBehaviour, IModule
    {
        private enum BodySection
        {
            Torso,
            Head,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg
        }

        [SerializeField] private float separationForceMultiplier = 2.4f;
        [SerializeField] private float upwardBias = 0.22f;
        [SerializeField] private float randomDirection = 0.28f;
        [SerializeField] private float angularImpulse = 8f;
        [SerializeField] private float partAngularDamping = 1.6f;
        [SerializeField] private float maxPartAngularVelocity = 7f;

        [Header("Detached Part Colliders")]
        [SerializeField, Range(0.1f, 1f)] private float limbColliderRadiusScale = 0.22f;
        [SerializeField, Range(0.1f, 1f)] private float limbColliderLengthScale = 0.65f;
        [SerializeField, Range(0.1f, 1f)] private float bodyColliderScale = 0.52f;

        [Header("Bleeding")]
        [SerializeField] private Material bloodMaterial;
        [SerializeField] private float bleedingDuration = 2.8f;

        private readonly Dictionary<BodySection, Transform> _sectionRoots =
            new Dictionary<BodySection, Transform>();

        private SkinnedMeshRenderer[] _skinnedRenderers;
        private Transform _ownerTransform;

        public void Initialize(ModuleOwner owner)
        {
            _ownerTransform = owner.transform;
            _skinnedRenderers = owner.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            Animator animator = owner.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman)
            {
                return;
            }

            AddSectionRoot(BodySection.Head, animator.GetBoneTransform(HumanBodyBones.Head));
            AddSectionRoot(BodySection.LeftArm, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
            AddSectionRoot(BodySection.RightArm, animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
            AddSectionRoot(BodySection.LeftLeg, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
            AddSectionRoot(BodySection.RightLeg, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
        }

        public List<GameObject> Explode(
            Vector3 explosionCenter,
            float force,
            float lifetime,
            bool includeGeneratedHead = true)
        {
            Dictionary<BodySection, GameObject> sectionObjects = new Dictionary<BodySection, GameObject>();
            List<Mesh> generatedMeshes = new List<Mesh>();

            foreach (SkinnedMeshRenderer sourceRenderer in _skinnedRenderers)
            {
                if (sourceRenderer == null || sourceRenderer.sharedMesh == null || !sourceRenderer.enabled)
                {
                    continue;
                }

                BakeRendererSections(sourceRenderer, sectionObjects, generatedMeshes, includeGeneratedHead);
            }

            List<GameObject> spawnedParts = new List<GameObject>(sectionObjects.Count);
            foreach (KeyValuePair<BodySection, GameObject> section in sectionObjects)
            {
                if (TryFinalizeSection(section.Key, section.Value, explosionCenter, force))
                {
                    spawnedParts.Add(section.Value);
                    Destroy(section.Value, lifetime);
                }
                else
                {
                    Destroy(section.Value);
                }
            }

            IgnorePartToPartCollisions(spawnedParts);

            foreach (Mesh generatedMesh in generatedMeshes)
            {
                Destroy(generatedMesh, lifetime);
            }

            return spawnedParts;
        }

        public GameObject AttachPersistentLeak(Transform parent, Vector3 worldPosition, Vector3 outward)
        {
            if (parent == null || bloodMaterial == null)
            {
                return null;
            }

            return CreateBleedingEmitter(parent, worldPosition, outward);
        }

        private void BakeRendererSections(
            SkinnedMeshRenderer sourceRenderer,
            Dictionary<BodySection, GameObject> sectionObjects,
            List<Mesh> generatedMeshes,
            bool includeGeneratedHead)
        {
            Mesh bakedMesh = new Mesh { name = sourceRenderer.name + "_Baked" };
            sourceRenderer.BakeMesh(bakedMesh);

            BodySection[] vertexSections = ResolveVertexSections(sourceRenderer, bakedMesh.vertexCount);
            int subMeshCount = bakedMesh.subMeshCount;
            Dictionary<BodySection, List<int>[]> sectionTriangles = CreateTriangleLists(subMeshCount);

            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                int[] triangles = bakedMesh.GetTriangles(subMesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    BodySection section = ResolveTriangleSection(
                        vertexSections[triangles[i]],
                        vertexSections[triangles[i + 1]],
                        vertexSections[triangles[i + 2]]);

                    sectionTriangles[section][subMesh].Add(triangles[i]);
                    sectionTriangles[section][subMesh].Add(triangles[i + 1]);
                    sectionTriangles[section][subMesh].Add(triangles[i + 2]);
                }
            }

            foreach (KeyValuePair<BodySection, List<int>[]> section in sectionTriangles)
            {
                if (!includeGeneratedHead && section.Key == BodySection.Head)
                {
                    continue;
                }

                if (!HasTriangles(section.Value))
                {
                    continue;
                }

                Mesh sectionMesh = Instantiate(bakedMesh);
                sectionMesh.name = sourceRenderer.name + "_" + section.Key;
                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    sectionMesh.SetTriangles(section.Value[subMesh], subMesh, false);
                }

                // The cloned mesh still contains every vertex from the baked body even though
                // only this section's triangles are rendered. RecalculateBounds() would include
                // those unused vertices and pull the detached part's pivot/collider toward the
                // full body's center. Build a tight bound from referenced vertices instead.
                sectionMesh.bounds = CalculateUsedVertexBounds(sectionMesh, section.Value);
                generatedMeshes.Add(sectionMesh);

                GameObject sectionRoot = GetOrCreateSectionRoot(section.Key, sectionObjects);
                CreateMeshChild(sectionRoot, sourceRenderer, sectionMesh);
            }

            Destroy(bakedMesh);
        }

        private BodySection[] ResolveVertexSections(SkinnedMeshRenderer renderer, int vertexCount)
        {
            BodySection[] sections = new BodySection[vertexCount];
            BoneWeight[] boneWeights = renderer.sharedMesh.boneWeights;
            Transform[] bones = renderer.bones;

            if (boneWeights.Length != vertexCount)
            {
                return sections;
            }

            for (int i = 0; i < vertexCount; i++)
            {
                int boneIndex = GetDominantBoneIndex(boneWeights[i]);
                sections[i] = boneIndex >= 0 && boneIndex < bones.Length
                    ? ResolveBoneSection(bones[boneIndex])
                    : BodySection.Torso;
            }

            return sections;
        }

        private BodySection ResolveBoneSection(Transform bone)
        {
            if (bone == null)
            {
                return BodySection.Torso;
            }

            foreach (KeyValuePair<BodySection, Transform> sectionRoot in _sectionRoots)
            {
                if (sectionRoot.Value != null && bone.IsChildOf(sectionRoot.Value))
                {
                    return sectionRoot.Key;
                }
            }

            return BodySection.Torso;
        }

        private static int GetDominantBoneIndex(BoneWeight weight)
        {
            int boneIndex = weight.boneIndex0;
            float boneWeight = weight.weight0;

            if (weight.weight1 > boneWeight)
            {
                boneIndex = weight.boneIndex1;
                boneWeight = weight.weight1;
            }

            if (weight.weight2 > boneWeight)
            {
                boneIndex = weight.boneIndex2;
                boneWeight = weight.weight2;
            }

            if (weight.weight3 > boneWeight)
            {
                boneIndex = weight.boneIndex3;
            }

            return boneIndex;
        }

        private static BodySection ResolveTriangleSection(BodySection first, BodySection second, BodySection third)
        {
            if (first == second || first == third)
            {
                return first;
            }

            return second == third ? second : first;
        }

        private static Dictionary<BodySection, List<int>[]> CreateTriangleLists(int subMeshCount)
        {
            Dictionary<BodySection, List<int>[]> result = new Dictionary<BodySection, List<int>[]>();
            foreach (BodySection section in Enum.GetValues(typeof(BodySection)))
            {
                List<int>[] subMeshes = new List<int>[subMeshCount];
                for (int i = 0; i < subMeshCount; i++)
                {
                    subMeshes[i] = new List<int>();
                }

                result.Add(section, subMeshes);
            }

            return result;
        }

        private static Bounds CalculateUsedVertexBounds(Mesh mesh, List<int>[] subMeshes)
        {
            Vector3[] vertices = mesh.vertices;
            Bounds bounds = default;
            bool hasVertex = false;

            foreach (List<int> triangles in subMeshes)
            {
                foreach (int vertexIndex in triangles)
                {
                    if (vertexIndex < 0 || vertexIndex >= vertices.Length)
                    {
                        continue;
                    }

                    if (!hasVertex)
                    {
                        bounds = new Bounds(vertices[vertexIndex], Vector3.zero);
                        hasVertex = true;
                    }
                    else
                    {
                        bounds.Encapsulate(vertices[vertexIndex]);
                    }
                }
            }

            return hasVertex ? bounds : mesh.bounds;
        }

        private static bool HasTriangles(List<int>[] subMeshes)
        {
            foreach (List<int> triangles in subMeshes)
            {
                if (triangles.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject GetOrCreateSectionRoot(
            BodySection section,
            Dictionary<BodySection, GameObject> sectionObjects)
        {
            if (sectionObjects.TryGetValue(section, out GameObject existingRoot))
            {
                return existingRoot;
            }

            GameObject sectionRoot = new GameObject("Dismembered_" + section);
            sectionRoot.layer = _ownerTransform.gameObject.layer;
            sectionRoot.transform.SetPositionAndRotation(_ownerTransform.position, Quaternion.identity);
            sectionObjects.Add(section, sectionRoot);
            return sectionRoot;
        }

        private static void CreateMeshChild(
            GameObject sectionRoot,
            SkinnedMeshRenderer sourceRenderer,
            Mesh sectionMesh)
        {
            GameObject meshObject = new GameObject(sourceRenderer.name);
            meshObject.layer = sectionRoot.layer;
            meshObject.transform.SetParent(sectionRoot.transform, false);
            meshObject.transform.SetPositionAndRotation(sourceRenderer.transform.position, sourceRenderer.transform.rotation);
            meshObject.transform.localScale = sourceRenderer.transform.lossyScale;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sectionMesh;

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            meshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            meshRenderer.receiveShadows = sourceRenderer.receiveShadows;
        }

        private bool TryFinalizeSection(
            BodySection section,
            GameObject sectionRoot,
            Vector3 explosionCenter,
            float force)
        {
            MeshRenderer[] renderers = sectionRoot.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Transform[] children = new Transform[sectionRoot.transform.childCount];
            Vector3[] childPositions = new Vector3[children.Length];
            Quaternion[] childRotations = new Quaternion[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = sectionRoot.transform.GetChild(i);
                childPositions[i] = children[i].position;
                childRotations[i] = children[i].rotation;
            }

            sectionRoot.transform.position = bounds.center;
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetPositionAndRotation(childPositions[i], childRotations[i]);
            }

            AddSectionCollider(section, sectionRoot, bounds);

            Rigidbody rigidbody = sectionRoot.AddComponent<Rigidbody>();
            rigidbody.mass = section == BodySection.Torso ? 3.5f : 1.35f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.angularDamping = partAngularDamping;
            rigidbody.maxAngularVelocity = maxPartAngularVelocity;
            rigidbody.ResetCenterOfMass();
            rigidbody.ResetInertiaTensor();

            Vector3 outward = bounds.center - explosionCenter;
            if (outward.sqrMagnitude < 0.001f)
            {
                outward = UnityEngine.Random.onUnitSphere;
            }

            outward = (outward.normalized + Vector3.up * upwardBias +
                       UnityEngine.Random.insideUnitSphere * randomDirection).normalized;
            rigidbody.AddForce(outward * force * separationForceMultiplier, ForceMode.Impulse);
            rigidbody.AddTorque(
                UnityEngine.Random.insideUnitSphere * Mathf.Min(angularImpulse, 3.5f),
                ForceMode.Impulse);

            AttachBleedingEmitters(section, sectionRoot, explosionCenter);
            return true;
        }

        private void AddSectionCollider(BodySection section, GameObject sectionRoot, Bounds bounds)
        {
            Vector3 size = bounds.size;
            if (section == BodySection.LeftArm || section == BodySection.RightArm ||
                section == BodySection.LeftLeg || section == BodySection.RightLeg)
            {
                CapsuleCollider capsule = sectionRoot.AddComponent<CapsuleCollider>();
                capsule.direction = GetLongestAxis(size);

                float length = GetAxis(size, capsule.direction);
                float widthA = GetAxis(size, (capsule.direction + 1) % 3);
                float widthB = GetAxis(size, (capsule.direction + 2) % 3);
                capsule.radius = Mathf.Max(
                    Mathf.Min(widthA, widthB) * limbColliderRadiusScale,
                    0.018f);
                capsule.height = Mathf.Max(
                    length * limbColliderLengthScale,
                    capsule.radius * 2f);
                return;
            }

            BoxCollider box = sectionRoot.AddComponent<BoxCollider>();
            box.size = new Vector3(
                Mathf.Max(size.x * bodyColliderScale, 0.025f),
                Mathf.Max(size.y * bodyColliderScale, 0.025f),
                Mathf.Max(size.z * bodyColliderScale, 0.025f));
        }

        private static int GetLongestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z)
            {
                return 0;
            }

            return size.y >= size.z ? 1 : 2;
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            return axis == 0 ? value.x : axis == 1 ? value.y : value.z;
        }

        private static void IgnorePartToPartCollisions(List<GameObject> parts)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                Collider[] firstPartColliders = parts[i].GetComponentsInChildren<Collider>();
                for (int j = i + 1; j < parts.Count; j++)
                {
                    Collider[] secondPartColliders = parts[j].GetComponentsInChildren<Collider>();
                    foreach (Collider firstCollider in firstPartColliders)
                    {
                        foreach (Collider secondCollider in secondPartColliders)
                        {
                            Physics.IgnoreCollision(firstCollider, secondCollider, true);
                        }
                    }
                }
            }
        }

        private void AttachBleedingEmitters(
            BodySection section,
            GameObject sectionRoot,
            Vector3 explosionCenter)
        {
            if (bloodMaterial == null)
            {
                return;
            }

            if (section == BodySection.Torso)
            {
                foreach (KeyValuePair<BodySection, Transform> cutPoint in _sectionRoots)
                {
                    Vector3 outward = cutPoint.Value.position - explosionCenter;
                    CreateBleedingEmitter(sectionRoot.transform, cutPoint.Value.position, outward);
                }

                return;
            }

            if (_sectionRoots.TryGetValue(section, out Transform sectionBone))
            {
                Vector3 outward = sectionBone.position - explosionCenter;
                CreateBleedingEmitter(sectionRoot.transform, sectionBone.position, outward);
            }
        }

        private GameObject CreateBleedingEmitter(Transform parent, Vector3 worldPosition, Vector3 outward)
        {
            if (outward.sqrMagnitude < 0.001f)
            {
                outward = Vector3.up;
            }

            Vector3 upReference = Mathf.Abs(Vector3.Dot(outward.normalized, Vector3.up)) > 0.95f
                ? Vector3.forward
                : Vector3.up;

            GameObject emitterRoot = new GameObject("CutBleeding");
            emitterRoot.layer = parent.gameObject.layer;
            emitterRoot.transform.SetPositionAndRotation(
                worldPosition,
                Quaternion.LookRotation(outward.normalized, upReference));
            emitterRoot.transform.SetParent(parent, true);

            CreatePressureJet(emitterRoot.transform);
            CreateStumpCover(emitterRoot.transform);
            CreateGravityDrips(emitterRoot.transform);
            return emitterRoot;
        }

        private void CreateStumpCover(Transform parent)
        {
            GameObject coverObject = new GameObject("StumpCover");
            coverObject.layer = parent.gameObject.layer;
            coverObject.transform.SetParent(parent, false);

            ParticleSystem system = coverObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.25f, 0.002f, 0.004f, 0.98f),
                new Color(0.055f, 0.001f, 0.001f, 0.96f));
            main.gravityModifier = 0.08f;
            main.maxParticles = 52;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(14f, 22f);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 13, 20) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.045f;
            shape.radiusThickness = 1f;

            ConfigureBleedingColor(system, 0.98f);
            ConfigureBleedingNoise(system, 0.015f, 0.055f);
            ConfigureBleedingTexture(system);

            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.72f),
                new Keyframe(0.16f, 1.15f),
                new Keyframe(1f, 0.82f));
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.material = bloodMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 1f;

            system.Play();
        }

        private void CreatePressureJet(Transform parent)
        {
            GameObject jetObject = new GameObject("PressureJet");
            jetObject.layer = parent.gameObject.layer;
            jetObject.transform.SetParent(parent, false);

            ParticleSystem system = jetObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = bleedingDuration * 0.55f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.052f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.32f, 0.004f, 0.006f, 0.95f),
                new Color(0.09f, 0.001f, 0.002f, 0.92f));
            main.gravityModifier = 0.75f;
            main.maxParticles = 56;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 8, 12),
                new ParticleSystem.Burst(0.16f, 5, 9),
                new ParticleSystem.Burst(0.38f, 4, 7),
                new ParticleSystem.Burst(0.75f, 3, 5)
            });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 13f;
            shape.radius = 0.022f;
            shape.radiusThickness = 1f;

            ConfigureBleedingColor(system, 0.95f);
            ConfigureBleedingNoise(system, 0.08f, 0.22f);
            ConfigureBleedingCollision(system, 0.38f);
            ConfigureBleedingTexture(system);

            ParticleSystem.TrailModule trails = system.trails;
            trails.enabled = true;
            trails.ratio = 0.7f;
            trails.lifetime = 0.09f;
            trails.dieWithParticles = true;
            trails.minVertexDistance = 0.015f;
            trails.worldSpace = true;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.material = bloodMaterial;
            renderer.trailMaterial = bloodMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.5f;
            renderer.velocityScale = 0.15f;

            system.Play();
        }

        private void CreateGravityDrips(Transform parent)
        {
            GameObject dripObject = new GameObject("GravityDrips");
            dripObject.layer = parent.gameObject.layer;
            dripObject.transform.SetParent(parent, false);

            ParticleSystem system = dripObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = bleedingDuration;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.014f, 0.042f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.28f, 0.003f, 0.005f, 0.95f),
                new Color(0.065f, 0.001f, 0.001f, 0.92f));
            main.gravityModifier = 1.9f;
            main.maxParticles = 56;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(5f, 9f);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.018f;
            shape.radiusThickness = 1f;

            ConfigureBleedingColor(system, 0.92f);
            ConfigureBleedingNoise(system, 0.025f, 0.08f);
            ConfigureBleedingCollision(system, 0.5f);
            ConfigureBleedingTexture(system);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.material = bloodMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            system.Play();
        }

        private static void ConfigureBleedingColor(ParticleSystem system, float alpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.45f, 0.45f, 0.45f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(alpha, 0f),
                    new GradientAlphaKey(alpha * 0.9f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;
        }

        private static void ConfigureBleedingNoise(ParticleSystem system, float minStrength, float maxStrength)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.strength = new ParticleSystem.MinMaxCurve(minStrength, maxStrength);
            noise.frequency = 0.75f;
            noise.scrollSpeed = 0.25f;
            noise.damping = true;
        }

        private static void ConfigureBleedingCollision(ParticleSystem system, float dampen)
        {
            ParticleSystem.CollisionModule collision = system.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.dampen = dampen;
            collision.bounce = 0.02f;
            collision.lifetimeLoss = 0.16f;
            collision.radiusScale = 0.25f;
            collision.enableDynamicColliders = false;
        }

        private static void ConfigureBleedingTexture(ParticleSystem system)
        {
            ParticleSystem.TextureSheetAnimationModule textureSheet = system.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.numTilesX = 4;
            textureSheet.numTilesY = 4;
            textureSheet.frameOverTime = 0f;
            textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0.5f, 0.749f);
            textureSheet.cycleCount = 1;
        }

        private void AddSectionRoot(BodySection section, Transform root)
        {
            if (root != null)
            {
                _sectionRoots[section] = root;
            }
        }
    }
}
