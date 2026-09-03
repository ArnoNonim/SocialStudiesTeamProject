using UnityEditor;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Explosions.Editor
{
    public static class BombPrefabBuilder
    {
        private const string RootFolder = "Assets/00_Members/KYM/01.Graphics/VFX/Bomb";
        private const string BombPrefabPath = RootFolder + "/DroneBomb_Sphere.prefab";
        private const string EffectPrefabPath = RootFolder + "/SimpleBombExplosion.prefab";
        private const string BombMaterialPath = RootFolder + "/MAT_DroneBomb.mat";

        [InitializeOnLoadMethod]
        private static void ScheduleInitialBuild()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("Tools/KYM/Rebuild Bomb Prefabs")]
        public static void RebuildPrefabs()
        {
            BuildPrefabs();
            Debug.Log($"Bomb prefabs rebuilt at {RootFolder}.");
        }

        private static void BuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath) != null)
            {
                return;
            }

            BuildPrefabs();
        }

        private static void BuildPrefabs()
        {
            EnsureFolder("Assets/00_Members/KYM/01.Graphics/VFX", "Bomb");

            GameObject effectPrefab = BuildEffectPrefab();
            Material bombMaterial = GetOrCreateBombMaterial();
            BuildBombPrefab(effectPrefab, bombMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject BuildEffectPrefab()
        {
            GameObject effectRoot = new GameObject("SimpleBombExplosion");
            effectRoot.AddComponent<SimpleBombExplosionVfx>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(effectRoot, EffectPrefabPath);
            Object.DestroyImmediate(effectRoot);
            return prefab;
        }

        private static Material GetOrCreateBombMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BombMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader) { name = "MAT_DroneBomb" };
                AssetDatabase.CreateAsset(material, BombMaterialPath);
            }

            Color bombColor = new Color(0.12f, 0.14f, 0.11f, 1f);
            material.color = bombColor;
            material.SetColor("_BaseColor", bombColor);
            material.SetFloat("_Metallic", 0.72f);
            material.SetFloat("_Smoothness", 0.32f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildBombPrefab(GameObject effectPrefab, Material bombMaterial)
        {
            GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bomb.name = "DroneBomb_Sphere";
            bomb.transform.localScale = Vector3.one * 0.35f;

            MeshRenderer meshRenderer = bomb.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = bombMaterial;

            Rigidbody rigidbody = bomb.AddComponent<Rigidbody>();
            rigidbody.mass = 8f;
            rigidbody.useGravity = true;
            rigidbody.linearDamping = 0.02f;
            rigidbody.angularDamping = 0.16f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            SoldierBombExplosion explosion = bomb.AddComponent<SoldierBombExplosion>();
            SerializedObject serializedExplosion = new SerializedObject(explosion);
            serializedExplosion.FindProperty("explosionEffectPrefab").objectReferenceValue = effectPrefab;
            serializedExplosion.FindProperty("effectLifetime").floatValue = 4f;
            serializedExplosion.FindProperty("destroyBombAfterDetonation").boolValue = true;
            serializedExplosion.ApplyModifiedPropertiesWithoutUndo();

            bomb.AddComponent<BombProjectile>();
            PrefabUtility.SaveAsPrefabAsset(bomb, BombPrefabPath);
            Object.DestroyImmediate(bomb);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
