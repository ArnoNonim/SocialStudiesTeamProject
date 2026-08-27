using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace _00_Members.PTY.Scripts
{
    public class StabilizeGrenade : MonoBehaviour
    {
        public ParticleSystem explosion;
        public Material[] explosionMats;
        public AudioSource exSndSource;
        public AudioClip[] exSndClips;
        public ExplodeLight exLgt;
        public CinemachineImpulseSource impulseSource;
        private Rigidbody _rb;

        [Header("회전 세팅")] [Tooltip("수류탄이 방향을 잡는 속도입니다. 값이 클수록 빠르게 바닥을 향합니다.")]
        public float rotationSpeed = 2.0f;

        private bool _isExplodable;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();

            _rb.linearDamping = 0.5f;
            _rb.angularDamping = 0.5f;

            StartCoroutine(Timer());
        }

        private IEnumerator Timer()
        {
            yield return new WaitForSeconds(0.5f);
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
            if (_isExplodable)
            {
                ParticleSystem clone = Instantiate(explosion, collision.contacts[0].point + new Vector3(0, 2, 0), Quaternion.identity);
                AudioSource sndClone = Instantiate(exSndSource, Vector3.zero, Quaternion.identity);
                exLgt.transform.position = collision.contacts[0].point;
                exLgt.Play();
                sndClone.clip = exSndClips[Random.Range(0, exSndClips.Length)];
                clone.GetComponent<ParticleSystemRenderer>().material = explosionMats[Random.Range(0, explosionMats.Length)];
                clone.Play();
                sndClone.Play();
                impulseSource.GenerateImpulse();
                Destroy(gameObject);
            }
        }
    }
}