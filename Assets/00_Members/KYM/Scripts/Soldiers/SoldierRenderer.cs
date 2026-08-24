using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class SoldierRenderer : MonoBehaviour, IModule
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] headRenderers;

        private Renderer[] _allRenderers;
        private bool[] _initialRendererStates;

        public Animator Animator { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            Animator = animator != null
                ? animator
                : GetComponent<Animator>() ?? owner.GetComponentInChildren<Animator>(true);

            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _initialRendererStates = new bool[_allRenderers.Length];
            for (int i = 0; i < _allRenderers.Length; i++)
            {
                _initialRendererStates[i] = _allRenderers[i].enabled;
            }

            if (headRenderers == null || headRenderers.Length == 0)
            {
                headRenderers = System.Array.FindAll(
                    owner.GetComponentsInChildren<Renderer>(true),
                    renderer => IsHeadRenderer(renderer.name));
            }
        }

        public void SetVisible(bool visible)
        {
            foreach (Renderer targetRenderer in _allRenderers)
            {
                targetRenderer.enabled = visible;
            }
        }

        public void RestoreVisibility()
        {
            for (int i = 0; i < _allRenderers.Length; i++)
            {
                if (_allRenderers[i] != null)
                {
                    _allRenderers[i].enabled = _initialRendererStates[i];
                }
            }
        }

        public void SetHeadVisible(bool visible)
        {
            foreach (Renderer targetRenderer in headRenderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = visible;
                }
            }
        }

        private static bool IsHeadRenderer(string rendererName)
        {
            string lowerName = rendererName.ToLowerInvariant();
            return lowerName.Contains("head") ||
                   lowerName.Contains("helmet") ||
                   lowerName.Contains("eye") ||
                   lowerName.Contains("lash") ||
                   lowerName.Contains("refl");
        }
    }
}
