using System.Collections;
using _00_Members.KYM.Scripts.Humans;
using Unity.Cinemachine;
using UnityEngine;

namespace _00_Members.PTY.Scripts
{
    public class DropGrenade : MonoBehaviour, IPoolable
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private Material[] explosionMats;
        [SerializeField] private AudioClip[] exSndClips;
        [SerializeField] private Collider col;
        
        public float detectionRadius = 5.0f;
        public LayerMask targetLayer;

        [Header("회전 세팅")]
        [Tooltip("수류탄이 방향을 잡는 속도입니다. 값이 클수록 빠르게 바닥을 향합니다.")]
        public float rotationSpeed = 2.0f;

        private Rigidbody _rb;
        private bool _isExplodable;
        private bool _hasExploded;

        private static PoolManager<DropGrenade> _pool;
        private static readonly WaitForSeconds ArmDelay = new(0.5f);

        private readonly Collider[] _results = new Collider[10];

        public static void Initialize(DropGrenade prefab, Transform poolFolder,
            GrenadeExplosionFX fxPrefab, Transform fxPoolFolder)
        {
            GrenadeExplosionFX.Initialize(fxPrefab, fxPoolFolder);
            _pool ??= new PoolManager<DropGrenade>(prefab, poolFolder);
        }

        public static DropGrenade Spawn(Vector3 pos, Quaternion rot)
        {
            var g = _pool.Get();
            g.transform.SetPositionAndRotation(pos, rot);
            return g;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void OnSpawned()
        {
            _isExplodable = false;
            _hasExploded = false;
            if (col != null) col.enabled = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            StopAllCoroutines();
            StartCoroutine(Timer());
        }

        public void OnDespawned()
        {
            StopAllCoroutines();
        }

        private IEnumerator Timer()
        {
            yield return ArmDelay;
            _isExplodable = true;
        }

        private void FixedUpdate()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.2f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_rb.linearVelocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isExplodable || _hasExploded) return;
            _hasExploded = true;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, _results, targetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                AbstractHuman hitHuman = _results[i].GetComponentInChildren<AbstractHuman>();

                if (hitHuman == null) continue;

                hitHuman.Die(Vector3.Distance(transform.position, hitHuman.transform.position));
                Debug.Log($"병사 {hitHuman.gameObject.name} 킬");
            }
            
            GrenadeExplosionFX.Play(
                collision.contacts[0].point,
                explosionMats[Random.Range(0, explosionMats.Length)],
                exSndClips[Random.Range(0, exSndClips.Length)],
                impulseSource
            );

            if (col != null) col.enabled = false;
            _pool.Release(this);
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}