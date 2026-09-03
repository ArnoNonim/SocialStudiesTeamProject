#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace InfinityPBR.BOTD.EditorTools
{
    public sealed class BOTDUrpCleanupWindow : EditorWindow
    {
        private const string RootFolder = "Assets/BOTD_Standard Shader";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const string UrpTerrainLitShaderName = "Universal Render Pipeline/Terrain/Lit";

        private static readonly string[] FoliageTokens =
        {
            "grass", "plant", "bush", "shrub", "fern", "clover", "meadow",
            "foliage", "leaf", "leaves", "branches", "billboard", "pinegroundscatter"
        };

        private static readonly string[] FoliageExcludedPathTokens =
        {
            "/terraintextures/", "/art/terrain/", "/cliffs/", "/rocks/", "/ground/", "/wood/"
        };

        private static readonly string[] WoodTokens =
        {
            "trunk", "bark", "stump", "wood", "log", "root", "branch", "stick"
        };

        private static readonly string[] RockTokens =
        {
            "rock", "cliff", "stone", "boulder", "passagecave", "granite", "sandstone"
        };

        private static readonly string[] LegacyKeywords =
        {
            "_DISABLE_DBUFFER",
            "_DOUBLESIDED_ON",
            "_MASKMAP",
            "_MATERIAL_FEATURE_TRANSMISSION",
            "_NORMALMAP_TANGENT_SPACE",
            "_THICKNESSMAP",
            "_ANIM_SINGLE_PIVOT_COLOR",
            "_ANIM_HIERARCHY_PIVOT",
            "_ANIM_HIERARCHY_PIVOT_COLOR",
            "_ENABLE_TERRAIN_MODE",
            "_HEIGHT_BASED_BLEND",
            "_LAYEREDLIT_4_LAYERS"
        };

        [Serializable]
        private struct ScanResult
        {
            public int Total;
            public int UrpLit;
            public int Foliage;
            public int EmissionEnabled;
            public int TransparentFoliage;
            public int ReflectiveFoliage;
            public int ReflectiveWood;
            public int ReflectiveRock;
            public int ReflectiveEnvironment;
            public int WrongTerrainShader;
            public int LegacyKeywordMaterials;
        }

        private ScanResult _result;
        private bool _hasScanned;
        private Vector2 _scroll;

        [MenuItem("Tools/Render/BOTD URP Cleanup")]
        private static void Open()
        {
            BOTDUrpCleanupWindow window = GetWindow<BOTDUrpCleanupWindow>();
            window.titleContent = new GUIContent("BOTD URP Cleanup");
            window.minSize = new Vector2(470f, 430f);
            window.Show();
        }

        private void OnEnable()
        {
            Scan();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("BOTD URP 변환 후 정리", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"대상 폴더: {RootFolder}\n" +
                "이 폴더 밖의 Material과 Scene은 수정하지 않습니다.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            DrawScanResult();

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("다시 검사", GUILayout.Height(30f)))
                    Scan();

                if (GUILayout.Button("대상 폴더 선택", GUILayout.Height(30f)))
                {
                    UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RootFolder);
                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                }
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("개별 수정", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Terrain을 깨끗한 URP Terrain/Lit으로 재구성", GUILayout.Height(34f)))
                RunWithConfirmation(FixTerrainMaterials, "Terrain 전체 정리");

            if (GUILayout.Button("2. 식생을 Opaque + Alpha Clipping으로 정리", GUILayout.Height(34f)))
                RunWithConfirmation(FixFoliageMaterials, "식생 Material 정리");

            if (GUILayout.Button("3. BOTD Lit Material의 Emission 끄기", GUILayout.Height(34f)))
                RunWithConfirmation(DisableAccidentalEmission, "Emission 정리");

            if (GUILayout.Button("4. 나무/줄기의 보라색 환경 반사 정리", GUILayout.Height(34f)))
                RunWithConfirmation(FixWoodReflectionMaterials, "나무/줄기 반사 정리");

            if (GUILayout.Button("5. 바위/절벽의 보라색 환경 반사 정리", GUILayout.Height(34f)))
                RunWithConfirmation(FixRockReflectionMaterials, "바위/절벽 반사 정리");

            GUI.backgroundColor = new Color(0.72f, 0.82f, 1f);
            if (GUILayout.Button("6. 모든 BOTD 환경 Material의 보라색 반사 일괄 정리", GUILayout.Height(40f)))
                RunWithConfirmation(FixAllEnvironmentReflectionMaterials, "전체 환경 반사 정리");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(
                "권장 수정은 Terrain, 식생 Surface, Emission, 레거시 키워드를 한 번에 정리합니다. " +
                "각 Material은 Undo에 기록되며 실행 직후에는 Ctrl+Z로 되돌릴 수 있습니다.",
                MessageType.Warning);

            GUI.backgroundColor = new Color(0.55f, 0.9f, 0.65f);
            if (GUILayout.Button("권장 수정 모두 적용", GUILayout.Height(44f)))
                RunRecommendedFixes();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "Terrain 정리는 현재 열린 BOTD Scene의 Terrain이 오래된 Reflection Probe를 사용하지 않도록 함께 설정합니다. " +
                "구형 Post Processing과 전체 Lighting 재베이크는 별도로 처리해야 합니다.",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawScanResult()
        {
            EditorGUILayout.LabelField("검사 결과", EditorStyles.boldLabel);
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("아직 검사하지 않았습니다.", MessageType.None);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawCount("전체 Material", _result.Total);
                DrawCount("URP Lit", _result.UrpLit);
                DrawCount("식생 후보", _result.Foliage);
                DrawCount("Emission 켜짐", _result.EmissionEnabled);
                DrawCount("Transparent 식생", _result.TransparentFoliage);
                DrawCount("금속성/반사가 과한 식생", _result.ReflectiveFoliage);
                DrawCount("반사가 과한 나무/줄기", _result.ReflectiveWood);
                DrawCount("반사가 과한 바위/절벽", _result.ReflectiveRock);
                DrawCount("반사 정리가 필요한 전체 환경", _result.ReflectiveEnvironment);
                DrawCount("잘못된 Terrain Shader", _result.WrongTerrainShader);
                DrawCount("레거시 키워드 잔존", _result.LegacyKeywordMaterials);
            }
        }

        private static void DrawCount(string label, int count)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label);
                EditorGUILayout.LabelField(count.ToString(), EditorStyles.boldLabel, GUILayout.Width(70f));
            }
        }

        private void Scan()
        {
            ScanResult result = default;
            foreach (Material material in CollectMaterials())
            {
                result.Total++;
                string path = AssetDatabase.GetAssetPath(material);
                bool isUrpLit = IsShader(material, UrpLitShaderName);
                bool isFoliage = IsFoliage(path);
                bool isWood = IsWood(path);
                bool isRock = IsRock(path);

                if (isUrpLit)
                    result.UrpLit++;
                if (isFoliage && isUrpLit)
                    result.Foliage++;
                if (isUrpLit && material.IsKeywordEnabled("_EMISSION"))
                    result.EmissionEnabled++;
                if (isFoliage && isUrpLit && GetFloat(material, "_Surface") > 0.5f)
                    result.TransparentFoliage++;
                if (isFoliage && isUrpLit &&
                    (GetFloat(material, "_Metallic") > 0.05f || GetFloat(material, "_Smoothness") > 0.5f))
                {
                    result.ReflectiveFoliage++;
                }
                if (isWood && isUrpLit &&
                    (GetFloat(material, "_Metallic") > 0.05f ||
                     GetFloat(material, "_Smoothness") > 0.35f ||
                     GetFloat(material, "_EnvironmentReflections") > 0.5f))
                {
                    result.ReflectiveWood++;
                }
                if (isRock && isUrpLit &&
                    (GetFloat(material, "_Metallic") > 0.05f ||
                     GetFloat(material, "_Smoothness") > 0.4f ||
                     GetFloat(material, "_EnvironmentReflections") > 0.5f))
                {
                    result.ReflectiveRock++;
                }
                if (isUrpLit && NeedsEnvironmentReflectionCleanup(material))
                    result.ReflectiveEnvironment++;
                if (IsTerrainMaterial(path, material) && !IsShader(material, UrpTerrainLitShaderName))
                    result.WrongTerrainShader++;
                if (isUrpLit && HasLegacyKeyword(material))
                    result.LegacyKeywordMaterials++;
            }

            _result = result;
            _hasScanned = true;
            Repaint();
        }

        private void RunRecommendedFixes()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "BOTD 권장 수정",
                "BOTD_Standard Shader 폴더의 Material을 다음과 같이 수정합니다.\n\n" +
                "- Terrain: URP Terrain/Lit\n" +
                "- 식생: Opaque + Alpha Clipping + 양면 렌더링\n" +
                "- 식생: Metallic/Smoothness와 환경 반사 정상화\n" +
                "- 나무/줄기: 보라색 환경 반사 차단\n" +
                "- 바위/절벽: 보라색 환경 반사 차단\n" +
                "- 바닥을 포함한 모든 BOTD 환경 Material 반사 정리\n" +
                "- Lit Material: Emission 끄기\n" +
                "- 알려진 HDRP/BOTD 레거시 키워드 제거\n\n" +
                "계속할까요?",
                "적용",
                "취소");
            if (!confirmed)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("BOTD URP Recommended Cleanup");
            int changed = 0;
            try
            {
                changed += FixTerrainMaterials();
                changed += FixFoliageMaterials();
                changed += DisableAccidentalEmission();
                changed += FixAllEnvironmentReflectionMaterials();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
                EditorUtility.ClearProgressBar();
            }

            Finish(changed, "BOTD 권장 수정");
        }

        private void RunWithConfirmation(Func<int> action, string title)
        {
            if (!EditorUtility.DisplayDialog(title, $"{RootFolder} 아래의 대상 Material을 수정할까요?", "적용", "취소"))
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(title);
            int changed;
            try
            {
                changed = action();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
                EditorUtility.ClearProgressBar();
            }

            Finish(changed, title);
        }

        private void Finish(int changed, string title)
        {
            AssetDatabase.SaveAssets();
            Scan();
            Debug.Log($"[{title}] Material {changed}개를 수정했습니다.");
            EditorUtility.DisplayDialog(title, $"Material {changed}개를 수정했습니다.", "확인");
        }

        private static int FixTerrainMaterials()
        {
            Shader terrainShader = Shader.Find(UrpTerrainLitShaderName);
            if (terrainShader == null)
            {
                Debug.LogError($"Shader를 찾지 못했습니다: {UrpTerrainLitShaderName}");
                return 0;
            }

            int changed = 0;
            foreach (Material material in CollectMaterials())
            {
                string path = AssetDatabase.GetAssetPath(material);
                if (!IsTerrainMaterial(path, material))
                    continue;

                Undo.RecordObject(material, "Fix BOTD Terrain Material");
                string materialName = material.name;
                Material cleanMaterial = new Material(terrainShader)
                {
                    name = materialName,
                    renderQueue = (int)RenderQueue.Geometry,
                    enableInstancing = true
                };
                SetFloat(cleanMaterial, "_EnableInstancedPerPixelNormal", 1f);
                cleanMaterial.SetOverrideTag("RenderType", "Opaque");
                EditorUtility.CopySerialized(cleanMaterial, material);
                material.name = materialName;
                UnityEngine.Object.DestroyImmediate(cleanMaterial);
                EditorUtility.SetDirty(material);
                changed++;
            }

            changed += FixTerrainLayers();
            changed += DisableTerrainReflectionProbesInOpenBotdScenes();

            return changed;
        }

        private static int FixTerrainLayers()
        {
            int changed = 0;
            string[] guids = AssetDatabase.FindAssets("t:TerrainLayer", new[] { RootFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer == null)
                    continue;

                bool needsCleanup = layer.metallic > 0.0001f ||
                                    layer.smoothness > 0.0001f ||
                                    layer.maskMapTexture != null ||
                                    layer.specular.maxColorComponent > 0.0001f ||
                                    layer.diffuseRemapMin != Vector4.zero ||
                                    layer.diffuseRemapMax != Vector4.one;
                if (!needsCleanup)
                    continue;

                Undo.RecordObject(layer, "Fix BOTD Terrain Layer");
                layer.metallic = 0f;
                layer.smoothness = 0f;
                layer.maskMapTexture = null;
                layer.specular = Color.black;
                layer.diffuseRemapMin = Vector4.zero;
                layer.diffuseRemapMax = Vector4.one;
                layer.maskMapRemapMin = Vector4.zero;
                layer.maskMapRemapMax = Vector4.one;
                EditorUtility.SetDirty(layer);
                changed++;
            }

            return changed;
        }

        private static int DisableTerrainReflectionProbesInOpenBotdScenes()
        {
            int changed = 0;
            Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Terrain terrain in terrains)
            {
                Scene scene = terrain.gameObject.scene;
                string scenePath = scene.path.Replace('\\', '/');
                if (!scene.IsValid() || !scenePath.StartsWith(RootFolder + "/", StringComparison.OrdinalIgnoreCase) ||
                    terrain.reflectionProbeUsage == ReflectionProbeUsage.Off)
                    continue;

                Undo.RecordObject(terrain, "Disable BOTD Terrain Reflection Probes");
                terrain.reflectionProbeUsage = ReflectionProbeUsage.Off;
                EditorUtility.SetDirty(terrain);
                EditorSceneManager.MarkSceneDirty(scene);
                changed++;
            }

            return changed;
        }

        private static int FixFoliageMaterials()
        {
            int changed = 0;
            List<Material> materials = CollectMaterials();
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                string path = AssetDatabase.GetAssetPath(material);
                if (!IsShader(material, UrpLitShaderName) || !IsFoliage(path))
                    continue;

                EditorUtility.DisplayProgressBar("BOTD 식생 정리", path, (float)i / materials.Count);
                Undo.RecordObject(material, "Fix BOTD Foliage Material");

                SetFloat(material, "_Surface", 0f);
                SetFloat(material, "_AlphaClip", 1f);
                SetFloat(material, "_Cull", (float)CullMode.Off);
                SetFloat(material, "_ZWrite", 1f);
                SetFloat(material, "_SrcBlend", (float)BlendMode.One);
                SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
                SetFloat(material, "_SrcBlendAlpha", (float)BlendMode.One);
                SetFloat(material, "_DstBlendAlpha", (float)BlendMode.Zero);
                SetFloat(material, "_WorkflowMode", 1f);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_Smoothness", Mathf.Min(GetFloat(material, "_Smoothness"), 0.3f));
                SetFloat(material, "_SpecularHighlights", 0f);
                SetFloat(material, "_EnvironmentReflections", 0f);

                if (material.HasProperty("_MetallicGlossMap"))
                    material.SetTexture("_MetallicGlossMap", null);
                if (material.HasProperty("_SpecGlossMap"))
                    material.SetTexture("_SpecGlossMap", null);

                if (material.HasProperty("_Cutoff"))
                {
                    float cutoff = Mathf.Clamp(material.GetFloat("_Cutoff"), 0.1f, 0.9f);
                    material.SetFloat("_Cutoff", cutoff);
                }

                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.DisableKeyword("_ALPHAMODULATE_ON");
                material.DisableKeyword("_METALLICSPECGLOSSMAP");
                material.DisableKeyword("_SPECGLOSSMAP");
                material.DisableKeyword("_SPECULAR_SETUP");
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                material.EnableKeyword("_ALPHATEST_ON");
                RemoveLegacyKeywords(material);

                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
                material.SetShaderPassEnabled("ShadowCaster", true);
                material.SetShaderPassEnabled("DepthOnly", true);
                material.enableInstancing = true;
                material.doubleSidedGI = true;
                EditorUtility.SetDirty(material);
                changed++;
            }

            return changed;
        }

        private static int DisableAccidentalEmission()
        {
            int changed = 0;
            foreach (Material material in CollectMaterials())
            {
                if (!IsShader(material, UrpLitShaderName))
                    continue;

                bool keywordEnabled = material.IsKeywordEnabled("_EMISSION");
                bool colorIsBlack = !material.HasProperty("_EmissionColor") ||
                                    material.GetColor("_EmissionColor").maxColorComponent <= 0.0001f;
                bool hasLegacyKeywords = HasLegacyKeyword(material);
                if (!keywordEnabled && colorIsBlack && !hasLegacyKeywords)
                    continue;

                Undo.RecordObject(material, "Disable BOTD Accidental Emission");
                material.DisableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", Color.black);
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                RemoveLegacyKeywords(material);
                EditorUtility.SetDirty(material);
                changed++;
            }

            return changed;
        }

        private static int FixWoodReflectionMaterials()
        {
            int changed = 0;
            List<Material> materials = CollectMaterials();
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                string path = AssetDatabase.GetAssetPath(material);
                if (!IsShader(material, UrpLitShaderName) || !IsWood(path))
                    continue;

                EditorUtility.DisplayProgressBar("BOTD 나무 반사 정리", path, (float)i / materials.Count);
                Undo.RecordObject(material, "Fix BOTD Wood Reflection");

                SetFloat(material, "_WorkflowMode", 1f);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_Smoothness", Mathf.Min(GetFloat(material, "_Smoothness"), 0.25f));
                SetFloat(material, "_EnvironmentReflections", 0f);

                if (material.HasProperty("_MetallicGlossMap"))
                    material.SetTexture("_MetallicGlossMap", null);

                material.DisableKeyword("_METALLICSPECGLOSSMAP");
                material.DisableKeyword("_SPECULAR_SETUP");
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                RemoveLegacyKeywords(material);
                EditorUtility.SetDirty(material);
                changed++;
            }

            return changed;
        }

        private static int FixRockReflectionMaterials()
        {
            int changed = 0;
            List<Material> materials = CollectMaterials();
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                string path = AssetDatabase.GetAssetPath(material);
                if (!IsShader(material, UrpLitShaderName) || !IsRock(path))
                    continue;

                EditorUtility.DisplayProgressBar("BOTD 바위 반사 정리", path, (float)i / materials.Count);
                Undo.RecordObject(material, "Fix BOTD Rock Reflection");

                SetFloat(material, "_WorkflowMode", 1f);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_Smoothness", Mathf.Min(GetFloat(material, "_Smoothness"), 0.35f));
                SetFloat(material, "_EnvironmentReflections", 0f);

                if (material.HasProperty("_MetallicGlossMap"))
                    material.SetTexture("_MetallicGlossMap", null);

                material.DisableKeyword("_METALLICSPECGLOSSMAP");
                material.DisableKeyword("_SPECULAR_SETUP");
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                RemoveLegacyKeywords(material);
                EditorUtility.SetDirty(material);
                changed++;
            }

            return changed;
        }

        private static int FixAllEnvironmentReflectionMaterials()
        {
            int changed = 0;
            List<Material> materials = CollectMaterials();
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                string path = AssetDatabase.GetAssetPath(material);
                if (!IsShader(material, UrpLitShaderName) || !NeedsEnvironmentReflectionCleanup(material))
                    continue;

                EditorUtility.DisplayProgressBar("BOTD 전체 환경 반사 정리", path, (float)i / materials.Count);
                Undo.RecordObject(material, "Fix All BOTD Environment Reflections");

                SetFloat(material, "_WorkflowMode", 1f);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_Smoothness", Mathf.Min(GetFloat(material, "_Smoothness"), 0.35f));
                SetFloat(material, "_EnvironmentReflections", 0f);

                if (material.HasProperty("_MetallicGlossMap"))
                    material.SetTexture("_MetallicGlossMap", null);
                if (material.HasProperty("_SpecGlossMap"))
                    material.SetTexture("_SpecGlossMap", null);

                material.DisableKeyword("_METALLICSPECGLOSSMAP");
                material.DisableKeyword("_SPECGLOSSMAP");
                material.DisableKeyword("_SPECULAR_SETUP");
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                RemoveLegacyKeywords(material);
                EditorUtility.SetDirty(material);
                changed++;
            }

            return changed;
        }

        private static List<Material> CollectMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { RootFolder });
            List<Material> materials = new List<Material>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                    materials.Add(material);
            }

            materials.Sort((left, right) => string.CompareOrdinal(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right)));
            return materials;
        }

        private static bool IsFoliage(string path)
        {
            string normalized = path.Replace('\\', '/').ToLowerInvariant();
            if (FoliageExcludedPathTokens.Any(normalized.Contains))
                return false;

            return FoliageTokens.Any(normalized.Contains);
        }

        private static bool IsTerrainMaterial(string path, Material material)
        {
            string normalized = path.Replace('\\', '/');
            return material.name.Equals("TerrainLayeredLit", StringComparison.OrdinalIgnoreCase) ||
                   normalized.IndexOf("/Art/Terrain/", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   material.name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsWood(string path)
        {
            string normalized = path.Replace('\\', '/').ToLowerInvariant();
            return WoodTokens.Any(normalized.Contains);
        }

        private static bool IsRock(string path)
        {
            string normalized = path.Replace('\\', '/').ToLowerInvariant();
            return RockTokens.Any(normalized.Contains);
        }

        private static bool IsShader(Material material, string shaderName)
        {
            return material != null && material.shader != null && material.shader.name == shaderName;
        }

        private static float GetFloat(Material material, string property)
        {
            return material.HasProperty(property) ? material.GetFloat(property) : 0f;
        }

        private static bool NeedsEnvironmentReflectionCleanup(Material material)
        {
            return GetFloat(material, "_Metallic") > 0.0001f ||
                   GetFloat(material, "_Smoothness") > 0.35f ||
                   GetFloat(material, "_EnvironmentReflections") > 0.5f ||
                   material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") ||
                   material.IsKeywordEnabled("_SPECGLOSSMAP") ||
                   !material.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF");
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static bool HasLegacyKeyword(Material material)
        {
            return material.shaderKeywords.Any(IsLegacyKeyword);
        }

        private static void RemoveLegacyKeywords(Material material)
        {
            foreach (string keyword in material.shaderKeywords.Where(IsLegacyKeyword).ToArray())
                material.DisableKeyword(keyword);
        }

        private static bool IsLegacyKeyword(string keyword)
        {
            if (LegacyKeywords.Contains(keyword))
                return true;

            return keyword.StartsWith("_BENTNORMALMAP", StringComparison.Ordinal) ||
                   keyword.StartsWith("_ANIM_", StringComparison.Ordinal) ||
                   keyword.StartsWith("_HEIGHTMAP", StringComparison.Ordinal) ||
                   keyword.StartsWith("_MASKMAP", StringComparison.Ordinal) ||
                   keyword.StartsWith("_NORMALMAP_TANGENT_SPACE", StringComparison.Ordinal);
        }
    }
}
#endif
