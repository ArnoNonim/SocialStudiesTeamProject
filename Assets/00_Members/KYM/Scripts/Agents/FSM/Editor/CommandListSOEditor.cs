using System.IO;
using System.Linq;
using _00_Members.KYM.Scripts.CoreSystems;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00_Members.KYM.Scripts.Agents.FSM.Editor
{
    [CustomEditor(typeof(CommandListSO))]
    public class CommandListSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            editorView.CloneTree(root);

            root.Q<Button>("GenerateButton").clicked += HandleGenerateEnumClick;
            
            return root;
        }

        private void HandleGenerateEnumClick()
        {
            CommandListSO listData = target as CommandListSO;
            
            Debug.Assert(listData != null, "Target data is null check editor");
            
            int index = 0;
            string enumString = string.Join(",", listData.commandList.Select(so =>
            {
                so.commandIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.commandName} = {index++}";
            }));
            
            string code = string.Format(CodeFormat.EnumFormat,"_00_Members.KYM.Scripts.Agents.FSM", listData.commandEnumName, enumString);

            string scriptPath = AssetDatabase.GetAssetPath( MonoScript.FromScriptableObject(this));
            string directoryName = Path.GetDirectoryName(scriptPath);
            Debug.Assert(directoryName != null, "Parent directory is null");
            
            DirectoryInfo parentDirectory = Directory.GetParent(directoryName);
            Debug.Assert(parentDirectory != null, "Parent directory is null");
            
            string path = parentDirectory.FullName;
            File.WriteAllText($"{path}/{listData.commandEnumName}.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}