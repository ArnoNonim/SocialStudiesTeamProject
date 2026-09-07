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

        private Coroutine _releaseRoutine;

        // static _pool / Initialize / Play 전부 제거함.
        // 스폰과 재생은 이제 GrenadePoolManager가 대신 해줌 (Get 해온 인스턴스에 대고 Play 호출).

        /// <summary>
        /// 기존 static Play()의 세팅/재생 로직을 인스턴스 메서드로 그대로 옮김.
        /// GrenadePoolManager.SpawnFX()가 풀에서 Get() 해온 뒤 이걸 호출하는 구조.
        /// </summary>
        public void Play(Vector3 position, Material mat, AudioClip clip, CinemachineImpulseSource impulseSource)
        {
            if (explosionRenderer == null)
            {
                Debug.LogWarning("[GrenadeExplosionFX] explosionRenderer가 null임. Stop Action 세팅 확인 필요.");
                return;
            }

            transform.SetPositionAndRotation(position, Quaternion.identity);
            explosionRenderer.material = mat;
            sndSource.clip = clip;
            exLgt.transform.position = position;
            exLgt.Play();
            explosion.Play();
            sndSource.Play();
            impulseSource.GenerateImpulse();

            if (_releaseRoutine != null) StopCoroutine(_releaseRoutine);
            _releaseRoutine = StartCoroutine(ReleaseAfter(explosion.main.duration));
        }

        private IEnumerator ReleaseAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            GrenadePoolManager.Instance.ReleaseFX(this);
        }

        public void OnSpawned() { }   // Play()에서 바로 세팅하니 여긴 비워둠
        public void OnDespawned()
        {
            // 씬 전환 등으로 강제 정리될 때 코루틴이 남아있으면 끊어줌
            if (_releaseRoutine != null)
            {
                StopCoroutine(_releaseRoutine);
                _releaseRoutine = null;
            }
        }
    }
}