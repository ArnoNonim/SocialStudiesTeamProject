using System;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Humans
{
    public abstract class AbstractHuman : ModuleOwner, IDamageable
    {
        [Header("Human Vitality")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        public event Action<HumanDamage> Damaged;
        public event Action Died;
        public event Action Revived;

        public bool IsDead { get; private set; }
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;

        public virtual Vector3 DamageSamplePoint
        {
            get
            {
                Animator animator = GetComponentInChildren<Animator>(true);
                if (animator != null && animator.isHuman)
                {
                    Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                    if (chest != null)
                    {
                        return chest.position;
                    }
                }

                return transform.position + transform.up;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            RestoreLife(false);
        }

        public virtual void ApplyDamage(HumanDamage damage)
        {
            if (IsDead || damage.Amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            Damaged?.Invoke(damage);
            if (currentHealth <= 0f && TryBeginDeath())
            {
                HandleFatalDamage(damage);
            }
        }

        public virtual void Revive()
        {
            RestoreLife(true);
        }

        protected bool TryBeginDeath()
        {
            if (IsDead)
            {
                return false;
            }

            IsDead = true;
            currentHealth = 0f;
            Died?.Invoke();
            return true;
        }

        protected abstract void HandleFatalDamage(HumanDamage damage);

        private void RestoreLife(bool notify)
        {
            IsDead = false;
            currentHealth = maxHealth;
            if (notify)
            {
                Revived?.Invoke();
            }
        }
    }
}
