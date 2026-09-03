using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace _00_Members.PTY.Scripts
{
    public class GrenadeExplosionFX : MonoBehaviour, IPoolable
    {
        [SerializeField] private ParticleSystem explosion;
        [SerializeField] private ParticleSystemRenderer explosionRenderer;
        [SerializeField] private AudioSource sndSource;
        [SerializeField] private ExplodeLight exLgt;

        private static PoolManager<GrenadeExplosionFX> _pool;
        private Coroutine _releaseRoutine;

        public static void Initialize(GrenadeExplosionFX prefab, Transform poolFolder, int defaultCapacity = 8, int maxSize = 32)
        {
            _pool ??= new PoolManager<GrenadeExplosionFX>(prefab, poolFolder, defaultCapacity, maxSize);
        }

        public static void Play(Vector3 position, Material mat, AudioClip clip, CinemachineImpulseSource impulseSource)
        {
            var fx = _pool.Get();
            if (fx == null || fx.explosionRenderer == null)
            {
                Debug.LogWarning("[GrenadeExplosionFX] 풀에서 파괴된 인스턴스가 꺼내짐. Stop Action 세팅 확인 필요.");
                return;
            }

            fx.transform.SetPositionAndRotation(position, Quaternion.identity);
            fx.explosionRenderer.material = mat;
            fx.sndSource.clip = clip;
            fx.exLgt.transform.position = position;
            fx.exLgt.Play();
            fx.explosion.Play();
            fx.sndSource.Play();
            impulseSource.GenerateImpulse();

            if (fx._releaseRoutine != null) fx.StopCoroutine(fx._releaseRoutine);
            fx._releaseRoutine = fx.StartCoroutine(fx.ReleaseAfter(fx.explosion.main.duration));
        }

        private IEnumerator ReleaseAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            _pool.Release(this);
        }

        public void OnSpawned() { }   // Play()에서 바로 세팅하니 여긴 비워둠
        public void OnDespawned() { } // SetActive(false)는 PoolManager가 알아서 함
    }
}