using System.Collections.Generic;
using _00_Members.KYM.Scripts.Soldiers.DeathEvent;
using _00_Members.KYM.Scripts.VFX;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class Soldier : ModuleOwner
    {
        [Header("Death Effects")]
        [SerializeField] private GameObject bodyExplosionEffect;
        [SerializeField] private GameObject headExplosionEffect;
        [SerializeField] private GameObject detachedHeadPrefab;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform neckBone;
        [SerializeField] private float spawnedObjectLifetime = 8f;

        [Header("Death Force")]
        [SerializeField] private float defaultForce = 12f;
        [SerializeField] private Vector3 defaultLocalForceDirection = Vector3.back;

        private Collider[] _rootColliders;
        private bool[] _initialRootColliderStates;
        private Collider[] _originalColliders;
        private bool[] _initialOriginalColliderStates;
        private Rigidbody _rootRigidbody;
        private readonly List<GameObject> _spawnedDeathObjects = new List<GameObject>();
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private bool _initialRootKinematic;
        private bool _initialRootUseGravity;
        private bool _initialAnimatorEnabled;

        public SoldierMover Mover { get; private set; }
        public SoldierRenderer Renderer { get; private set; }
        public RagdollController Ragdoll { get; private set; }
        public SoldierDismemberment Dismemberment { get; private set; }
        public bool IsDead { get; private set; }

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
            _initialRootKinematic = _rootRigidbody != null && _rootRigidbody.isKinematic;
            _initialRootUseGravity = _rootRigidbody != null && _rootRigidbody.useGravity;
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

        public void Revive()
        {
            ClearSpawnedDeathObjects();

            if (_rootRigidbody != null)
            {
                _rootRigidbody.linearVelocity = Vector3.zero;
                _rootRigidbody.angularVelocity = Vector3.zero;
                _rootRigidbody.isKinematic = true;
            }

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

            if (_rootRigidbody != null)
            {
                _rootRigidbody.useGravity = _initialRootUseGravity;
                _rootRigidbody.isKinematic = _initialRootKinematic;
            }

            Mover?.ResetMovement(transform.position);
            IsDead = false;
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
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            StopGameplayBody();
            Quaternion effectRotation = forceDirection.sqrMagnitude > 0f
                ? Quaternion.LookRotation(forceDirection.normalized, Vector3.up)
                : transform.rotation;

            switch (deathType)
            {
                case DeathType.Ragdoll:
                    Ragdoll?.EnableRagdoll(hitPoint, forceDirection, force);
                    break;

                case DeathType.BodyExplosion:
                    Vector3 bodyExplosionCenter = transform.position + transform.up;
                    if (Dismemberment != null)
                    {
                        _spawnedDeathObjects.AddRange(Dismemberment.Explode(
                            bodyExplosionCenter,
                            force,
                            spawnedObjectLifetime,
                            false));
                    }

                    Vector3 bodyHeadPosition = headBone != null ? headBone.position : hitPoint;
                    Vector3 bodyHeadDirection = bodyHeadPosition - bodyExplosionCenter + transform.up * 0.35f;
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
                        bodyExplosionCenter,
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
                    Ragdoll?.EnableRagdoll(hitPoint, forceDirection, force);
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

            if (_rootRigidbody != null)
            {
                _rootRigidbody.linearVelocity = Vector3.zero;
                _rootRigidbody.angularVelocity = Vector3.zero;
                _rootRigidbody.isKinematic = true;
            }
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
            if (detachedHeadPrefab == null)
            {
                return null;
            }

            GameObject detachedHead = Instantiate(detachedHeadPrefab, spawnPosition, transform.rotation);
            detachedHead.SetActive(true);
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
            if (targetRenderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = targetRenderers[0].bounds;
            for (int i = 1; i < targetRenderers.Length; i++)
            {
                bounds.Encapsulate(targetRenderers[i].bounds);
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
