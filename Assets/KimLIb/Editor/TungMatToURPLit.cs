#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KimLIb.Editor
{
    public static class TungMatToURPLit
    {
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        private readonly struct TextureValue
        {
            public readonly bool Found;
            public readonly Texture Texture;
            public readonly Vector2 Scale;
            public readonly Vector2 Offset;

            public TextureValue(bool found, Texture texture, Vector2 scale, Vector2 offset)
            {
                Found = found;
                Texture = texture;
                Scale = scale;
                Offset = offset;
            }
        }

        private sealed class SavedMaterialProperties
        {
            private readonly Dictionary<string, TextureValue> _textures =
                new Dictionary<string, TextureValue>();
            private readonly Dictionary<string, float> _floats =
                new Dictionary<string, float>();
            private readonly Dictionary<string, Color> _colors =
                new Dictionary<string, Color>();

            public SavedMaterialProperties(Material material)
            {
                SerializedObject serializedMaterial = new SerializedObject(material);
                ReadTextures(serializedMaterial.FindProperty("m_SavedProperties.m_TexEnvs"));
                ReadFloats(serializedMaterial.FindProperty("m_SavedProperties.m_Floats"));
                ReadFloats(serializedMaterial.FindProperty("m_SavedProperties.m_Ints"));
                ReadColors(serializedMaterial.FindProperty("m_SavedProperties.m_Colors"));
            }

            public bool TryGetTexture(string name, out TextureValue value) =>
                _textures.TryGetValue(name, out value);

            public bool TryGetFloat(string name, out float value) =>
                _floats.TryGetValue(name, out value);

            public bool TryGetColor(string name, out Color value) =>
                _colors.TryGetValue(name, out value);

            private void ReadTextures(SerializedProperty properties)
            {
                if (properties == null || !properties.isArray)
                    return;

                for (int i = 0; i < properties.arraySize; i++)
                {
                    SerializedProperty entry = properties.GetArrayElementAtIndex(i);
                    SerializedProperty nameProperty = entry.FindPropertyRelative("first");
                    SerializedProperty valueProperty = entry.FindPropertyRelative("second");
                    if (nameProperty == null || valueProperty == null)
                        continue;

                    SerializedProperty textureProperty = valueProperty.FindPropertyRelative("m_Texture");
                    SerializedProperty scaleProperty = valueProperty.FindPropertyRelative("m_Scale");
                    SerializedProperty offsetProperty = valueProperty.FindPropertyRelative("m_Offset");
                    string propertyName = nameProperty.stringValue;
                    _textures[propertyName] = new TextureValue(
                        true,
                        textureProperty?.objectReferenceValue as Texture,
                        scaleProperty?.vector2Value ?? Vector2.one,
                        offsetProperty?.vector2Value ?? Vector2.zero);
                }
            }

            private void ReadFloats(SerializedProperty properties)
            {
                if (properties == null || !properties.isArray)
                    return;

                for (int i = 0; i < properties.arraySize; i++)
                {
                    SerializedProperty entry = properties.GetArrayElementAtIndex(i);
                    SerializedProperty nameProperty = entry.FindPropertyRelative("first");
                    SerializedProperty valueProperty = entry.FindPropertyRelative("second");
                    if (nameProperty == null || valueProperty == null)
                        continue;

                    float value = valueProperty.propertyType == SerializedPropertyType.Integer
                        ? valueProperty.intValue
                        : valueProperty.floatValue;
                    _floats[nameProperty.stringValue] = value;
                }
            }

            private void ReadColors(SerializedProperty properties)
            {
                if (properties == null || !properties.isArray)
                    return;

                for (int i = 0; i < properties.arraySize; i++)
                {
                    SerializedProperty entry = properties.GetArrayElementAtIndex(i);
                    SerializedProperty nameProperty = entry.FindPropertyRelative("first");
                    SerializedProperty valueProperty = entry.FindPropertyRelative("second");
                    if (nameProperty != null && valueProperty != null)
                        _colors[nameProperty.stringValue] = valueProperty.colorValue;
                }
            }
        }

        [MenuItem("Tools/Render/Convert Selected Materials Or Folders To URP Lit")]
        private static void ConvertSelected()
        {
            Shader urpLit = Shader.Find(UrpLitShaderName);
            if (urpLit == null)
            {
                Debug.LogError("URP Lit Shader를 찾지 못했습니다.");
                return;
            }

            List<Material> foundMaterials = CollectSelectedMaterials();
            if (foundMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "URP Lit 변환",
                    "Project 창에서 Material이나 검사할 폴더를 선택해주세요.",
                    "확인");
                return;
            }

            List<Material> conversionTargets = new List<Material>();
            int protectedCount = 0;
            foreach (Material material in foundMaterials)
            {
                if (ShouldConvertToUrpLit(material))
                    conversionTargets.Add(material);
                else
                    protectedCount++;
            }

            if (conversionTargets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "URP Lit 변환",
                    $"Material {foundMaterials.Count}개를 검사했지만 변환할 레거시 Material이 없습니다.",
                    "확인");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "URP Lit 일괄 변환",
                $"Material {foundMaterials.Count}개를 검사했습니다.\n\n" +
                $"변환 대상: {conversionTargets.Count}개\n" +
                $"이미 최신이거나 전용 Shader라 보호됨: {protectedCount}개\n\n" +
                "선택한 폴더의 하위 폴더까지 변환합니다. 계속할까요?",
                "변환",
                "취소");
            if (!confirmed)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Convert Materials To URP Lit");
            int convertedCount = 0;
            try
            {
                for (int i = 0; i < conversionTargets.Count; i++)
                {
                    Material material = conversionTargets[i];
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    EditorUtility.DisplayProgressBar(
                        "URP Lit 변환 중",
                        materialPath,
                        (float)i / conversionTargets.Count);
                    Convert(material, urpLit);
                    convertedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(undoGroup);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Material {foundMaterials.Count}개 검사 완료: " +
                $"URP Lit 변환 {convertedCount}개, 보호/건너뜀 {protectedCount}개.");
        }

        [MenuItem("Assets/Convert Materials In Selected Folders To URP Lit", false, 2000)]
        private static void ConvertFromProjectWindow()
        {
            ConvertSelected();
        }

        private static List<Material> CollectSelectedMaterials()
        {
            HashSet<string> materialPaths = new HashSet<string>();
            HashSet<string> selectedFolders = new HashSet<string>();

            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AddSelectedPath(path, materialPaths, selectedFolders);
            }

            // Some Unity Project Browser layouts do not populate assetGUIDs for the active item.
            foreach (Object selected in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                AddSelectedPath(path, materialPaths, selectedFolders);
            }

            if (selectedFolders.Count > 0)
            {
                string[] materialGuids = AssetDatabase.FindAssets(
                    "t:Material",
                    new List<string>(selectedFolders).ToArray());
                foreach (string guid in materialGuids)
                    materialPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            List<Material> materials = new List<Material>(materialPaths.Count);
            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                    materials.Add(material);
            }

            materials.Sort((left, right) => string.CompareOrdinal(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right)));
            return materials;
        }

        private static void AddSelectedPath(
            string path,
            HashSet<string> materialPaths,
            HashSet<string> selectedFolders)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (AssetDatabase.IsValidFolder(path))
            {
                selectedFolders.Add(path);
                return;
            }

            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Material))
                materialPaths.Add(path);
        }

        private static bool ShouldConvertToUrpLit(Material material)
        {
            if (material == null)
                return false;

            Shader shader = material.shader;
            if (shader == null || !shader.isSupported)
                return true;

            string shaderName = shader.name;
            if (string.IsNullOrEmpty(shaderName) || shaderName == "Hidden/InternalErrorShader")
                return true;

            if (shaderName == UrpLitShaderName ||
                shaderName.StartsWith("Universal Render Pipeline/") ||
                shaderName.StartsWith("Shader Graphs/"))
            {
                return false;
            }

            return !IsProtectedSpecialPurposeShader(shaderName);
        }

        private static bool IsProtectedSpecialPurposeShader(string shaderName)
        {
            return shaderName.StartsWith("UI/") ||
                   shaderName.StartsWith("Sprites/") ||
                   shaderName.StartsWith("TextMeshPro/") ||
                   shaderName.StartsWith("Particles/") ||
                   shaderName.StartsWith("Mobile/Particles/") ||
                   shaderName.StartsWith("Skybox/") ||
                   shaderName.StartsWith("Hidden/") ||
                   shaderName.StartsWith("Nature/Terrain/") ||
                   shaderName.StartsWith("Terrain/") ||
                   shaderName.Contains("Decal");
        }

        private static void Convert(Material material, Shader urpLit)
        {
            Undo.RecordObject(material, "Convert Material To URP Lit");

            SavedMaterialProperties saved = new SavedMaterialProperties(material);
            string sourceShaderName = material.shader != null ? material.shader.name : string.Empty;
            bool sourceIsUrpLit = sourceShaderName == UrpLitShaderName;
            string renderType = material.GetTag("RenderType", false, string.Empty);
            int renderQueue = material.renderQueue;
            bool enableInstancing = material.enableInstancing;
            bool doubleSidedGi = material.doubleSidedGI;
            MaterialGlobalIlluminationFlags giFlags = material.globalIlluminationFlags;

            TextureValue baseMap = GetTexture(material, saved,
                "_BaseMap", "_MainTex", "_Albedo", "_ColorMap", "_DiffuseMap");
            Color baseColor = GetColor(material, saved, Color.white,
                "_BaseColor", "_Color", "_TintColor");
            TextureValue normalMap = GetTexture(material, saved,
                "_BumpMap", "_NormalMap", "_NormalTex", "_Normal");
            float normalScale = GetFloat(material, saved, 1f,
                "_BumpScale", "_NormalScale", "_NormalIntensity");
            TextureValue metallicMap = GetTexture(material, saved,
                "_MetallicGlossMap", "_MetallicMap", "_MetalnessMap");
            float metallic = GetFloat(material, saved, 0f, "_Metallic", "_Metalness");
            float smoothness = GetFloat(material, saved, 0.5f,
                "_Smoothness", "_Glossiness", "_GlossMapScale");
            TextureValue specularMap = GetTexture(material, saved,
                "_SpecGlossMap", "_SpecularMap");
            Color specularColor = GetColor(material, saved, new Color(0.2f, 0.2f, 0.2f),
                "_SpecColor", "_SpecularColor");
            TextureValue occlusionMap = GetTexture(material, saved,
                "_OcclusionMap", "_AOMap", "_AmbientOcclusionMap");
            float occlusionStrength = GetFloat(material, saved, 1f,
                "_OcclusionStrength", "_AOStrength");
            TextureValue emissionMap = GetTexture(material, saved,
                "_EmissionMap", "_EmissiveMap");
            Color emissionColor = GetColor(material, saved, Color.black,
                "_EmissionColor", "_EmissiveColor");
            TextureValue heightMap = GetTexture(material, saved,
                "_ParallaxMap", "_HeightMap");
            float parallax = GetFloat(material, saved, 0.005f,
                "_Parallax", "_HeightAmplitude");
            TextureValue detailMask = GetTexture(material, saved, "_DetailMask");
            TextureValue detailAlbedo = GetTexture(material, saved,
                "_DetailAlbedoMap", "_DetailMap");
            TextureValue detailNormal = GetTexture(material, saved,
                "_DetailNormalMap", "_DetailNormal");
            float detailAlbedoScale = GetFloat(material, saved, 1f,
                "_DetailAlbedoMapScale", "_DetailAlbedoScale");
            float detailNormalScale = GetFloat(material, saved, 1f,
                "_DetailNormalMapScale", "_DetailNormalScale");
            float cutoff = GetFloat(material, saved, 0.5f,
                "_Cutoff", "_AlphaCutoff", "_OpacityThreshold");
            float cull = GetFloat(material, saved, (float)CullMode.Back, "_Cull", "_CullMode");
            float sourceMode = GetFloat(material, saved, 0f, "_Mode");
            float sourceSurface = GetFloat(material, saved, 0f, "_Surface");
            float sourceAlphaClip = GetFloat(material, saved, 0f, "_AlphaClip");
            float sourceBlend = GetFloat(material, saved, 0f, "_Blend");

            bool alphaClip = sourceIsUrpLit
                ? sourceAlphaClip > 0.5f
                : sourceMode == 1f || renderType == "TransparentCutout";
            bool transparent = sourceIsUrpLit
                ? sourceSurface > 0.5f
                : sourceMode >= 2f || renderType == "Transparent";

            material.shader = urpLit;
            MaterialEditor.ApplyMaterialPropertyDrawers(material);

            SetTexture(material, "_BaseMap", baseMap);
            SetTexture(material, "_MainTex", baseMap);
            SetColor(material, "_BaseColor", baseColor);
            SetColor(material, "_Color", baseColor);
            SetTexture(material, "_BumpMap", normalMap);
            SetFloat(material, "_BumpScale", normalScale);
            SetTexture(material, "_MetallicGlossMap", metallicMap);
            SetFloat(material, "_Metallic", metallic);
            SetFloat(material, "_Smoothness", smoothness);
            SetTexture(material, "_SpecGlossMap", specularMap);
            SetColor(material, "_SpecColor", specularColor);
            SetTexture(material, "_OcclusionMap", occlusionMap);
            SetFloat(material, "_OcclusionStrength", occlusionStrength);
            SetTexture(material, "_EmissionMap", emissionMap);
            SetColor(material, "_EmissionColor", emissionColor);
            SetTexture(material, "_ParallaxMap", heightMap);
            SetFloat(material, "_Parallax", parallax);
            SetTexture(material, "_DetailMask", detailMask);
            SetTexture(material, "_DetailAlbedoMap", detailAlbedo);
            SetTexture(material, "_DetailNormalMap", detailNormal);
            SetFloat(material, "_DetailAlbedoMapScale", detailAlbedoScale);
            SetFloat(material, "_DetailNormalMapScale", detailNormalScale);
            SetFloat(material, "_Cutoff", cutoff);
            SetFloat(material, "_Cull", cull);
            SetFloat(material, "_Blend", sourceBlend);

            bool useSpecularWorkflow = specularMap.Texture != null && metallicMap.Texture == null;
            SetFloat(material, "_WorkflowMode", useSpecularWorkflow ? 0f : 1f);
            ConfigureSurface(material, transparent, alphaClip, renderQueue);
            ConfigureKeywords(
                material,
                normalMap.Texture != null,
                metallicMap.Texture != null,
                useSpecularWorkflow && specularMap.Texture != null,
                emissionMap.Texture != null || emissionColor.maxColorComponent > 0f,
                heightMap.Texture != null,
                detailAlbedo.Texture != null || detailNormal.Texture != null);

            material.enableInstancing = enableInstancing;
            material.doubleSidedGI = doubleSidedGi;
            material.globalIlluminationFlags = giFlags;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureSurface(
            Material material,
            bool transparent,
            bool alphaClip,
            int renderQueue)
        {
            SetFloat(material, "_Surface", transparent ? 1f : 0f);
            SetFloat(material, "_AlphaClip", alphaClip ? 1f : 0f);
            SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparent);
            SetKeyword(material, "_ALPHATEST_ON", alphaClip);

            if (transparent)
            {
                SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_SrcBlendAlpha", (float)BlendMode.One);
                SetFloat(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                SetFloat(material, "_SrcBlend", (float)BlendMode.One);
                SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
                SetFloat(material, "_SrcBlendAlpha", (float)BlendMode.One);
                SetFloat(material, "_DstBlendAlpha", (float)BlendMode.Zero);
                SetFloat(material, "_ZWrite", 1f);
                material.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
            }

            if (renderQueue >= 0)
            {
                material.renderQueue = renderQueue;
            }
            else
            {
                material.renderQueue = transparent
                    ? (int)RenderQueue.Transparent
                    : alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            }
        }

        private static void ConfigureKeywords(
            Material material,
            bool hasNormalMap,
            bool hasMetallicMap,
            bool hasSpecularMap,
            bool hasEmission,
            bool hasHeightMap,
            bool hasDetailMap)
        {
            SetKeyword(material, "_NORMALMAP", hasNormalMap);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", hasMetallicMap);
            SetKeyword(material, "_SPECGLOSSMAP", hasSpecularMap);
            SetKeyword(material, "_EMISSION", hasEmission);
            SetKeyword(material, "_PARALLAXMAP", hasHeightMap);
            SetKeyword(material, "_DETAIL_MULX2", hasDetailMap);
        }

        private static TextureValue GetTexture(
            Material material,
            SavedMaterialProperties saved,
            params string[] names)
        {
            TextureValue firstFound = default;
            foreach (string name in names)
            {
                if (material.HasProperty(name))
                {
                    TextureValue current = new TextureValue(
                        true,
                        material.GetTexture(name),
                        material.GetTextureScale(name),
                        material.GetTextureOffset(name));
                    if (current.Texture != null)
                        return current;
                    if (!firstFound.Found)
                        firstFound = current;
                }

                if (!saved.TryGetTexture(name, out TextureValue serialized))
                    continue;

                if (serialized.Texture != null)
                    return serialized;
                if (!firstFound.Found)
                    firstFound = serialized;
            }

            return firstFound;
        }

        private static float GetFloat(
            Material material,
            SavedMaterialProperties saved,
            float fallback,
            params string[] names)
        {
            foreach (string name in names)
            {
                if (material.HasProperty(name))
                    return material.GetFloat(name);
            }

            foreach (string name in names)
            {
                if (saved.TryGetFloat(name, out float value))
                    return value;
            }

            return fallback;
        }

        private static Color GetColor(
            Material material,
            SavedMaterialProperties saved,
            Color fallback,
            params string[] names)
        {
            foreach (string name in names)
            {
                if (material.HasProperty(name))
                    return material.GetColor(name);
            }

            foreach (string name in names)
            {
                if (saved.TryGetColor(name, out Color value))
                    return value;
            }

            return fallback;
        }

        private static void SetTexture(Material material, string name, TextureValue value)
        {
            if (!value.Found || !material.HasProperty(name))
                return;

            material.SetTexture(name, value.Texture);
            material.SetTextureScale(name, value.Scale);
            material.SetTextureOffset(name, value.Offset);
        }

        private static void SetFloat(Material material, string name, float value)
        {
            if (material.HasProperty(name))
                material.SetFloat(name, value);
        }

        private static void SetColor(Material material, string name, Color value)
        {
            if (material.HasProperty(name))
                material.SetColor(name, value);
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
#endif
