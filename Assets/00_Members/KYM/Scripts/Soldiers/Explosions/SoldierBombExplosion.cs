using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions
{
    public sealed class SoldierBombExplosion : MonoBehaviour
    {
        [SerializeField] private ExplosionProfile profile = new ExplosionProfile();
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField, Min(0f)] private float effectLifetime = 8f;
        [SerializeField] private bool destroyBombAfterDetonation = true;

        public bool HasDetonated { get; private set; }

        public int Detonate()
        {
            if (HasDetonated)
            {
                return 0;
            }

            HasDetonated = true;
            Vector3 explosionPosition = transform.position;
            int killedCount = SoldierExplosionSystem.Detonate(
                explosionPosition,
                profile,
                gameObject);

            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(
                    explosionEffectPrefab,
                    explosionPosition,
                    Quaternion.identity);
                Destroy(effect, effectLifetime);
            }

            if (explosionClip != null)
            {
                AudioSource.PlayClipAtPoint(explosionClip, explosionPosition);
            }

            if (destroyBombAfterDetonation)
            {
                Destroy(gameObject);
            }

            return killedCount;
        }
    }
}
