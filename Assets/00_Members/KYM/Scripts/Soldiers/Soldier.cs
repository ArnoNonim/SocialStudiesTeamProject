using System;
using System.Collections.Generic;
using _00_Members.JYG._Scripts.UISystem;
using _00_Members.JYG._Scripts.UISystem.Quest;
using _00_Members.KYM.Scripts.Humans;
using _00_Members.KYM.Scripts.Soldiers.DeathEvent;
using _00_Members.KYM.Scripts.Soldiers.Explosions;
using _00_Members.KYM.Scripts.VFX;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class Soldier : AbstractHuman
    {
        [Header("사망 효과")]
        [SerializeField] private GameObject bodyExplosionEffect;
        [SerializeField] private GameObject headExplosionEffect;
        [SerializeField] private GameObject detachedHeadPrefab;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform neckBone;
        [SerializeField] private float spawnedObjectLifetime = 8f;

        [Header("사망 물리력")]
        [SerializeField] private float defaultForce = 12f;
        [SerializeField] private Vector3 defaultLocalForceDirection = Vector3.back;

        [Header("거리별 사망 유형")]
        [SerializeField, Min(0f)] private float bodyExplosionDistance = 1.6f;
        [SerializeField, Min(0f)] private float headExplosionDistance = 3f;

        [Header("거리 기반 분리 파츠 물리력")]
        [SerializeField, Min(0f)] private float closeRangePartForce = 12f;
        [SerializeField, Min(0f)] private float farRangePartForce = 4f;
        
        private Collider[] _rootColliders;
        private bool[] _initialRootColliderStates;
        private Collider[] _originalColliders;
        private bool[] _initialOriginalColliderStates;
        private Rigidbody _rootRigidbody;
        private readonly List<GameObject> _spawnedDeathObjects = new List<GameObject>();
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private bool _initialAnimatorEnabled;

        public SoldierMover Mover { get; private set; }
        public SoldierRenderer Renderer { get; private set; }
        public RagdollController Ragdoll { get; private set; }
        public SoldierDismemberment Dismemberment { get; private set; }
        protected override void Awake()
        {
            base.Awake();

            Mover = GetModule<SoldierMover>();
            Renderer = GetModule<SoldierRenderer>();
            Ragdoll = GetModule<RagdollController>();
            Dismemberment = GetModule<SoldierDismemberment>();
            _rootColliders = GetComponents<Collider>();
            _initialRootColliderStates = new bool[_rootColliders.Length];
            for (int i = 0; i < _rootColliders.Length; i++)
            {
                _initialRootColliderStates[i] = _rootColliders[i].enabled;
            }

            _originalColliders = GetComponentsInChildren<Collider>(true);
            _initialOriginalColliderStates = new bool[_originalColliders.Length];
            for (int i = 0; i < _originalColliders.Length; i++)
            {
                _initialOriginalColliderStates[i] = _originalColliders[i].enabled;
            }

            _rootRigidbody = GetComponent<Rigidbody>();
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            DisableRootPhysics();
            _initialAnimatorEnabled = Renderer?.Animator != null && Renderer.Animator.enabled;

            if (headBone == null && Renderer?.Animator != null && Renderer.Animator.isHuman)
            {
                headBone = Renderer.Animator.GetBoneTransform(HumanBodyBones.Head);
            }

            if (neckBone == null && Renderer?.Animator != null && Renderer.Animator.isHuman)
            {
                neckBone = Renderer.Animator.GetBoneTransform(HumanBodyBones.Neck);
            }

            if (neckBone == null && headBone != null)
            {
                neckBone = headBone.parent;
            }
        }

        public override void Revive()
        {
            ClearSpawnedDeathObjects();

            DisableRootPhysics();

            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;

            Ragdoll?.ResetRagdoll();
            Renderer?.RestoreVisibility();

            if (Renderer?.Animator != null)
            {
                Renderer.Animator.enabled = true;
                Renderer.Animator.Rebind();
                Renderer.Animator.Update(0f);
                Renderer.Animator.enabled = _initialAnimatorEnabled;
            }

            for (int i = 0; i < _rootColliders.Length; i++)
            {
                _rootColliders[i].enabled = _initialRootColliderStates[i];
            }

            for (int i = 0; i < _originalColliders.Length; i++)
            {
                if (_originalColliders[i] != null)
                {
                    _originalColliders[i].enabled = _initialOriginalColliderStates[i];
                }
            }

            DisableRootPhysics();

            Mover?.ResetMovement(transform.position);
            base.Revive();
        }

        protected override void HandleFatalDamage(HumanDamage damage)
        {
            Vector3 direction = damage.Direction.sqrMagnitude > 0f
                ? damage.Direction
                : transform.TransformDirection(defaultLocalForceDirection);
            ExecuteDeath(
                DeathType.Ragdoll,
                damage.HitPoint,
                direction,
                damage.Force,
                null);
        }

        protected override void HandleDistanceDeath(float distance, HumanDamage damage)
        {
            Vector3 direction = damage.Direction.sqrMagnitude > 0f
                ? damage.Direction
                : GetDistanceDeathDirection();

            DeathType distanceDeathType;
            if (distance <= Mathf.Max(0f, bodyExplosionDistance))
            {
                distanceDeathType = DeathType.BodyExplosion;
            }
            else if (distance <= Mathf.Max(bodyExplosionDistance, headExplosionDistance))
            {
                distanceDeathType = DeathType.HeadExplosion;
            }
            else
            {
                distanceDeathType = DeathType.Ragdoll;
            }

            ExecuteDeath(
                distanceDeathType,
                damage.HitPoint,
                direction,
                EvaluateDistancePartForce(distance),
                null);
        }

        private Vector3 GetDistanceDeathDirection()
        {
            Vector3 localDirection = defaultLocalForceDirection.sqrMagnitude > 0f
                ? defaultLocalForceDirection.normalized
                : Vector3.forward * 0.2f + Vector3.up;
            return transform.TransformDirection(localDirection).normalized;
        }

        private float EvaluateDistancePartForce(float distance)
        {
            float forceRange = Mathf.Max(bodyExplosionDistance, headExplosionDistance, 0.01f);
            float distance01 = Mathf.Clamp01(distance / forceRange);
            return Mathf.Lerp(closeRangePartForce, farRangePartForce, distance01);
        }

        public void Die(DeathType deathType)
        {
            Vector3 hitPoint = headBone != null
                ? headBone.position
                : transform.position + Vector3.up;
            Vector3 forceDirection = transform.TransformDirection(defaultLocalForceDirection);
            Die(deathType, hitPoint, forceDirection, defaultForce);
        }

        public void Die(DeathType deathType, Vector3 hitPoint, Vector3 forceDirection, float force)
        {
            DieInternal(deathType, hitPoint, forceDirection, force, null);
        }

        public void DieFromExplosion(
            DeathType deathType,
            ExplosionContext context,
            Vector3 hitPoint)
        {
            Vector3 forceDirection = hitPoint - context.Center;
            if (forceDirection.sqrMagnitude < 0.001f)
            {
                forceDirection = transform.up;
            }

            DieInternal(
                deathType,
                hitPoint,
                forceDirection.normalized,
                context.EvaluateForceAt(hitPoint),
                context);
        }

        private void DieInternal(
            DeathType deathType,
            Vector3 hitPoint,
            Vector3 forceDirection,
            float force,
            ExplosionContext? explosionContext)
        {
            if (!TryBeginDeath())
            {
                return;
            }

            ExecuteDeath(deathType, hitPoint, forceDirection, force, explosionContext);
        }

        private void ExecuteDeath(
            DeathType deathType,
            Vector3 hitPoint,
            Vector3 forceDirection,
            float force,
            ExplosionContext? explosionContext)
        {
            StopGameplayBody();
            Quaternion effectRotation = forceDirection.sqrMagnitude > 0f
                ? Quaternion.LookRotation(forceDirection.normalized, Vector3.up)
                : transform.rotation;

            switch (deathType)
            {
                case DeathType.Ragdoll:
                    if (explosionContext.HasValue)
                    {
                        Ragdoll?.EnableExplosionRagdoll(explosionContext.Value);
                    }
                    else
                    {
                        Ragdoll?.EnableRagdoll(hitPoint, forceDirection, force);
                    }
                    break;

                case DeathType.BodyExplosion:
                    Vector3 victimBodyCenter = transform.position + transform.up;
                    Vector3 bodyExplosionForceOrigin = explosionContext?.Center ?? victimBodyCenter;
                    if (Dismemberment != null)
                    {
                        _spawnedDeathObjects.AddRange(Dismemberment.Explode(
                            bodyExplosionForceOrigin,
                            force,
                            spawnedObjectLifetime,
                            false));
                    }

                    Vector3 bodyHeadPosition = headBone != null ? headBone.position : hitPoint;
                    Vector3 bodyHeadDirection = bodyHeadPosition - bodyExplosionForceOrigin + transform.up * 0.35f;
                    GameObject bodyExplosionHead = SpawnDetachedHead(
                        bodyHeadPosition,
                        bodyHeadDirection,
                        force * 1.2f);
                    AttachDetachedHeadBleeding(bodyExplosionHead);
                    DisableOriginalColliders();

                    if (Renderer?.Animator != null)
                    {
                        Renderer.Animator.enabled = false;
                    }

                    Renderer?.SetVisible(false);
                    SpawnEffect(
                        bodyExplosionEffect,
                        victimBodyCenter,
                        effectRotation,
                        BloodBurstMode.RadialExplosion);
                    break;

                case DeathType.HeadExplosion:
                    Renderer?.SetHeadVisible(false);
                    Vector3 neckPosition = neckBone != null ? neckBone.position : hitPoint;
                    Quaternion upwardRotation = Quaternion.LookRotation(transform.up, transform.forward);
                    SpawnEffect(
                        headExplosionEffect != null ? headExplosionEffect : bodyExplosionEffect,
                        neckPosition,
                        upwardRotation,
                        BloodBurstMode.Directional);
                    GameObject detachedHead = SpawnDetachedHead(hitPoint, forceDirection, force);
                    AttachHeadExplosionBleeding(detachedHead, neckPosition);
                    if (explosionContext.HasValue)
                    {
                        Ragdoll?.EnableExplosionRagdoll(explosionContext.Value);
                    }
                    else
                    {
                        Ragdoll?.EnableRagdoll(hitPoint, forceDirection, force);
                    }
                    Ragdoll?.DisableHeadPhysics();
                    break;
            }
        }

        private void StopGameplayBody()
        {
            Mover?.Stop();

            foreach (Collider rootCollider in _rootColliders)
            {
                rootCollider.enabled = false;
            }

            DisableRootPhysics();
        }

        private void DisableRootPhysics()
        {
            if (_rootRigidbody == null)
            {
                return;
            }

            if (!_rootRigidbody.isKinematic)
            {
                _rootRigidbody.linearVelocity = Vector3.zero;
                _rootRigidbody.angularVelocity = Vector3.zero;
            }

            _rootRigidbody.useGravity = false;
            _rootRigidbody.isKinematic = true;
        }

        private void DisableOriginalColliders()
        {
            foreach (Collider originalCollider in _originalColliders)
            {
                if (originalCollider != null)
                {
                    originalCollider.enabled = false;
                }
            }
        }

        private GameObject SpawnDetachedHead(Vector3 spawnPosition, Vector3 forceDirection, float force)
        {
            GameObject detachedHead;
            if (detachedHeadPrefab != null)
            {
                detachedHead = Instantiate(detachedHeadPrefab, spawnPosition, transform.rotation);
                detachedHead.SetActive(true);
            }
            else
            {
                detachedHead = CreateDetachedHeadFromCurrentVisual(spawnPosition);
                if (detachedHead == null)
                {
                    return null;
                }
            }

            AlignDetachedHeadVisual(detachedHead, spawnPosition);
            _spawnedDeathObjects.Add(detachedHead);

            Rigidbody detachedRigidbody = detachedHead.GetComponent<Rigidbody>();
            if (detachedRigidbody != null)
            {
                detachedRigidbody.isKinematic = false;
                detachedRigidbody.AddForce(forceDirection.normalized * force, ForceMode.Impulse);
            }

            Destroy(detachedHead, spawnedObjectLifetime);
            return detachedHead;
        }

        private GameObject CreateDetachedHeadFromCurrentVisual(Vector3 spawnPosition)
        {
            if (Renderer?.Animator == null || headBone == null)
            {
                return null;
            }

            GameObject detachedHead = new GameObject($"{name}_DetachedHead");
            detachedHead.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

            Vector3 cutPosition = neckBone != null
                ? neckBone.position
                : headBone.position - transform.up * 0.15f;
            Vector3 headDirection = headBone.position - cutPosition;
            if (headDirection.sqrMagnitude < 0.0001f)
            {
                headDirection = transform.up;
            }
            headDirection.Normalize();

            Vector3 localCutPosition = detachedHead.transform.InverseTransformPoint(cutPosition);
            Vector3 localHeadDirection = detachedHead.transform.InverseTransformDirection(headDirection).normalized;
            Vector3 localHeadPosition = detachedHead.transform.InverseTransformPoint(headBone.position);
            float headRadius = Mathf.Max(0.22f, Vector3.Distance(headBone.position, cutPosition) * 2.5f);
            SkinnedMeshRenderer[] sourceRenderers =
                Renderer.Animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            bool hasVisual = false;
            bool hasBounds = false;
            Bounds combinedLocalBounds = default;
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                SkinnedMeshRenderer sourceRenderer = sourceRenderers[i];
                string lowerName = sourceRenderer.name.ToLowerInvariant();
                bool cropAtNeck = lowerName.Contains("body");
                bool isHeadAccessory = lowerName.Contains("head") ||
                                       lowerName.Contains("hair") ||
                                       lowerName.Contains("eye") ||
                                       lowerName.Contains("lash") ||
                                       lowerName.Contains("face");
                if (!cropAtNeck && !isHeadAccessory)
                {
                    continue;
                }

                if (!TryCreateDetachedMesh(
                        sourceRenderer,
                        detachedHead.transform,
                        localCutPosition,
                        localHeadDirection,
                        localHeadPosition,
                        headRadius,
                        cropAtNeck,
                        out Mesh detachedMesh,
                        out Bounds meshBounds))
                {
                    continue;
                }

                GameObject visualPart = new GameObject(sourceRenderer.name);
                visualPart.transform.SetParent(detachedHead.transform, false);
                MeshFilter meshFilter = visualPart.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = visualPart.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = detachedMesh;
                meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                meshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                meshRenderer.receiveShadows = sourceRenderer.receiveShadows;

                Destroy(detachedMesh, spawnedObjectLifetime);
                if (!hasBounds)
                {
                    combinedLocalBounds = meshBounds;
                    hasBounds = true;
                }
                else
                {
                    combinedLocalBounds.Encapsulate(meshBounds);
                }
                hasVisual = true;
            }

            if (!hasVisual)
            {
                Destroy(detachedHead);
                return null;
            }

            if (hasBounds)
            {
                SphereCollider headCollider = detachedHead.AddComponent<SphereCollider>();
                headCollider.center = combinedLocalBounds.center;
                headCollider.radius = Mathf.Max(0.06f, combinedLocalBounds.extents.magnitude);
            }

            Rigidbody headRigidbody = detachedHead.AddComponent<Rigidbody>();
            headRigidbody.mass = 0.8f;
            headRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            headRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            AttachCurrentFaceMosaic(detachedHead.transform);
            return detachedHead;
        }

        private static bool TryCreateDetachedMesh(
            SkinnedMeshRenderer sourceRenderer,
            Transform detachedRoot,
            Vector3 localCutPosition,
            Vector3 localHeadDirection,
            Vector3 localHeadPosition,
            float headRadius,
            bool cropAtNeck,
            out Mesh detachedMesh,
            out Bounds usedBounds)
        {
            Mesh bakedMesh = new Mesh();
            sourceRenderer.BakeMesh(bakedMesh);
            Vector3[] sourceVertices = bakedMesh.vertices;
            if (sourceVertices.Length == 0)
            {
                Destroy(bakedMesh);
                detachedMesh = null;
                usedBounds = default;
                return false;
            }

            Matrix4x4 toDetachedSpace = detachedRoot.worldToLocalMatrix * sourceRenderer.transform.localToWorldMatrix;
            Vector3[] detachedVertices = new Vector3[sourceVertices.Length];
            for (int i = 0; i < sourceVertices.Length; i++)
            {
                detachedVertices[i] = toDetachedSpace.MultiplyPoint3x4(sourceVertices[i]);
            }

            detachedMesh = new Mesh
            {
                name = $"{sourceRenderer.name}_DetachedHeadMesh",
                indexFormat = bakedMesh.indexFormat,
                vertices = detachedVertices,
                subMeshCount = bakedMesh.subMeshCount
            };

            Vector3[] sourceNormals = bakedMesh.normals;
            if (sourceNormals.Length == sourceVertices.Length)
            {
                Matrix4x4 normalMatrix = toDetachedSpace.inverse.transpose;
                Vector3[] detachedNormals = new Vector3[sourceNormals.Length];
                for (int i = 0; i < sourceNormals.Length; i++)
                {
                    detachedNormals[i] = normalMatrix.MultiplyVector(sourceNormals[i]).normalized;
                }
                detachedMesh.normals = detachedNormals;
            }

            Vector4[] sourceTangents = bakedMesh.tangents;
            if (sourceTangents.Length == sourceVertices.Length)
            {
                Vector4[] detachedTangents = new Vector4[sourceTangents.Length];
                for (int i = 0; i < sourceTangents.Length; i++)
                {
                    Vector3 tangent = toDetachedSpace.MultiplyVector(sourceTangents[i]).normalized;
                    detachedTangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, sourceTangents[i].w);
                }
                detachedMesh.tangents = detachedTangents;
            }

            detachedMesh.uv = bakedMesh.uv;
            detachedMesh.uv2 = bakedMesh.uv2;
            detachedMesh.colors = bakedMesh.colors;

            bool hasTriangle = false;
            bool hasUsedBounds = false;
            usedBounds = default;
            for (int subMesh = 0; subMesh < bakedMesh.subMeshCount; subMesh++)
            {
                int[] sourceTriangles = bakedMesh.GetTriangles(subMesh);
                List<int> keptTriangles = new List<int>(sourceTriangles.Length);
                for (int triangle = 0; triangle < sourceTriangles.Length; triangle += 3)
                {
                    int a = sourceTriangles[triangle];
                    int b = sourceTriangles[triangle + 1];
                    int c = sourceTriangles[triangle + 2];
                    Vector3 triangleCenter = (detachedVertices[a] + detachedVertices[b] + detachedVertices[c]) / 3f;
                    bool isAboveNeck =
                        Vector3.Dot(triangleCenter - localCutPosition, localHeadDirection) >= -0.015f;
                    bool isNearHead =
                        (triangleCenter - localHeadPosition).sqrMagnitude <= headRadius * headRadius;
                    bool keepTriangle = !cropAtNeck || (isAboveNeck && isNearHead);
                    if (!keepTriangle)
                    {
                        continue;
                    }

                    keptTriangles.Add(a);
                    keptTriangles.Add(b);
                    keptTriangles.Add(c);
                    EncapsulateVertex(detachedVertices[a], ref usedBounds, ref hasUsedBounds);
                    EncapsulateVertex(detachedVertices[b], ref usedBounds, ref hasUsedBounds);
                    EncapsulateVertex(detachedVertices[c], ref usedBounds, ref hasUsedBounds);
                    hasTriangle = true;
                }
                detachedMesh.SetTriangles(keptTriangles, subMesh, false);
            }

            Destroy(bakedMesh);
            if (!hasTriangle)
            {
                Destroy(detachedMesh);
                detachedMesh = null;
                return false;
            }

            detachedMesh.bounds = usedBounds;
            return true;
        }

        private static void EncapsulateVertex(Vector3 vertex, ref Bounds bounds, ref bool initialized)
        {
            if (!initialized)
            {
                bounds = new Bounds(vertex, Vector3.zero);
                initialized = true;
                return;
            }

            bounds.Encapsulate(vertex);
        }

        private void AttachCurrentFaceMosaic(Transform detachedHead)
        {
            if (headBone == null)
            {
                return;
            }

            ParticleSystem sourceParticle = headBone.GetComponentInChildren<ParticleSystem>(true);
            if (sourceParticle == null)
            {
                return;
            }

            Transform mosaicRoot = sourceParticle.transform;
            while (mosaicRoot.parent != null && mosaicRoot.parent != headBone)
            {
                mosaicRoot = mosaicRoot.parent;
            }

            GameObject mosaic = Instantiate(mosaicRoot.gameObject, mosaicRoot.position, mosaicRoot.rotation);
            mosaic.transform.SetParent(detachedHead, true);
            mosaic.SetActive(true);
            foreach (ParticleSystem particle in mosaic.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Play();
            }
        }

        private void AttachHeadExplosionBleeding(GameObject detachedHead, Vector3 neckPosition)
        {
            if (Dismemberment == null)
            {
                return;
            }

            Transform neckParent = neckBone != null ? neckBone : transform;
            GameObject neckLeak = Dismemberment.AttachPersistentLeak(
                neckParent,
                neckPosition,
                transform.up);
            if (neckLeak != null)
            {
                _spawnedDeathObjects.Add(neckLeak);
            }

            if (detachedHead == null || !TryGetRendererBounds(detachedHead, out Bounds detachedBounds))
            {
                return;
            }

            AttachDetachedHeadBleeding(detachedHead, detachedBounds);
        }

        private void AttachDetachedHeadBleeding(GameObject detachedHead)
        {
            if (detachedHead == null || !TryGetRendererBounds(detachedHead, out Bounds detachedBounds))
            {
                return;
            }

            AttachDetachedHeadBleeding(detachedHead, detachedBounds);
        }

        private void AttachDetachedHeadBleeding(GameObject detachedHead, Bounds detachedBounds)
        {
            if (Dismemberment == null)
            {
                return;
            }

            Vector3 detachedCutPosition = new Vector3(
                detachedBounds.center.x,
                detachedBounds.min.y,
                detachedBounds.center.z);
            Dismemberment.AttachPersistentLeak(
                detachedHead.transform,
                detachedCutPosition,
                -transform.up);
        }

        private static void AlignDetachedHeadVisual(GameObject detachedHead, Vector3 targetPosition)
        {
            if (!TryGetRendererBounds(detachedHead, out Bounds visualBounds))
            {
                return;
            }

            detachedHead.transform.position += targetPosition - visualBounds.center;
        }

        private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>(true);
            Renderer firstVisualRenderer = null;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (!(targetRenderers[i] is ParticleSystemRenderer))
                {
                    firstVisualRenderer = targetRenderers[i];
                    break;
                }
            }

            if (firstVisualRenderer == null)
            {
                bounds = default;
                return false;
            }

            bounds = firstVisualRenderer.bounds;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer != firstVisualRenderer && !(targetRenderer is ParticleSystemRenderer))
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            return true;
        }

        private void SpawnEffect(
            GameObject effectPrefab,
            Vector3 position,
            Quaternion rotation,
            BloodBurstMode mode)
        {
            if (effectPrefab == null)
            {
                return;
            }

            GameObject effect = Instantiate(effectPrefab, position, rotation);
            effect.SetActive(true);
            _spawnedDeathObjects.Add(effect);

            BloodBurstVfx bloodBurst = effect.GetComponent<BloodBurstVfx>();
            if (bloodBurst != null)
            {
                bloodBurst.Play(mode);
            }
            else
            {
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Play();
                }
            }

            Destroy(effect, spawnedObjectLifetime);
        }

        private void ClearSpawnedDeathObjects()
        {
            foreach (GameObject spawnedObject in _spawnedDeathObjects)
            {
                if (spawnedObject != null)
                {
                    Destroy(spawnedObject);
                }
            }

            _spawnedDeathObjects.Clear();
        }
    }
}
