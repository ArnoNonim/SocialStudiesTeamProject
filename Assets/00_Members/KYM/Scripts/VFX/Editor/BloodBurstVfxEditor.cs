using UnityEditor;
using UnityEngine;

namespace _00_Members.KYM.Scripts.VFX.Editor
{
    [CustomEditor(typeof(BloodBurstVfx))]
    public class BloodBurstVfxEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BloodBurstVfx bloodBurst = (BloodBurstVfx)target;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview"))
                {
                    bloodBurst.Preview();
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Stop"))
                {
                    bloodBurst.StopPreview();
                    SceneView.RepaintAll();
                }
            }
        }
    }
}
