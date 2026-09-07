using Unity.Cinemachine;
using UnityEngine;

namespace _00_Members.PTY.Scripts
{
    /// <summary>
    /// 수류탄/폭발 FX 풀을 소유하는 씬 스코프 매니저.
    /// static이 아니라 씬에 실제로 배치된 오브젝트이므로,
    /// 씬 전환 시 유니티가 이 오브젝트를 파괴하면서 풀도 함께 정리됨.
    /// -> 다음 씬으로 죽은 레퍼런스가 넘어갈 일이 없음.
    /// </summary>
    public class GrenadePoolManager : MonoBehaviour
    {
        public static GrenadePoolManager Instance { get; private set; }

        [SerializeField] private DropGrenade grenadePrefab;
        [SerializeField] private Transform grenadePoolFolder;
        [SerializeField] private GrenadeExplosionFX fxPrefab;
        [SerializeField] private Transform fxPoolFolder;

        private PoolManager<DropGrenade> _grenadePool;
        private PoolManager<GrenadeExplosionFX> _fxPool;

        private void Awake()
        {
            // DontDestroyOnLoad 걸지 않음 - 이 매니저는 "현재 씬 전용"으로 의도된 것.
            // 씬이 바뀌면 이 오브젝트 자체가 사라지는 게 핵심.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("GrenadePoolManager가 씬에 두 개 이상 존재함. 하나만 둬야 함.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _grenadePool = new PoolManager<DropGrenade>(grenadePrefab, grenadePoolFolder);
            _fxPool = new PoolManager<GrenadeExplosionFX>(fxPrefab, fxPoolFolder);
        }

        private void OnDestroy()
        {
            if (Instance != this) return; // 위에서 중복으로 즉시 파괴된 인스턴스면 정리할 것도 없음

            _grenadePool?.Clear();
            _fxPool?.Clear();
            Instance = null;
        }

        public DropGrenade SpawnGrenade(Vector3 pos, Quaternion rot)
        {
            var g = _grenadePool.Get();
            g.transform.SetPositionAndRotation(pos, rot);
            return g;
        }

        public void ReleaseGrenade(DropGrenade instance) => _grenadePool.Release(instance);

        // TODO 해결됨: GrenadeExplosionFX 소스 확인 완료, 실제 Play 호출로 채움.
        public GrenadeExplosionFX SpawnFX(Vector3 pos, Material mat, AudioClip clip, CinemachineImpulseSource impulseSource)
        {
            var fx = _fxPool.Get();
            fx.Play(pos, mat, clip, impulseSource); // 포지션 세팅까지 Play 내부에서 처리함
            return fx;
        }

        public void ReleaseFX(GrenadeExplosionFX instance) => _fxPool.Release(instance);
    }
}