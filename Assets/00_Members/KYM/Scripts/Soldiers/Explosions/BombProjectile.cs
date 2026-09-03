using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(SoldierBombExplosion))]
    public sealed class BombProjectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float armingDelay = 0.12f;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 1f;
        [SerializeField, Min(0f)] private float maximumLifetime = 20f;

        private SoldierBombExplosion _explosion;
        private float _armedTime;

        private void Awake()
        {
            _explosion = GetComponent<SoldierBombExplosion>();
            Rigidbody body = GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnEnable()
        {
            _armedTime = Time.time + armingDelay;
            if (maximumLifetime > 0f)
            {
                Invoke(nameof(Detonate), maximumLifetime);
            }
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time < _armedTime || collision.relativeVelocity.magnitude < minimumImpactSpeed)
            {
                return;
            }

            Detonate();
        }

        public void Detonate()
        {
            CancelInvoke();
            _explosion?.Detonate();
        }
    }
}
