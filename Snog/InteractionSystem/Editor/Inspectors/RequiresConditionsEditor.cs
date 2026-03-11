using UnityEditor;
using UnityEngine;
using Snog.InteractionSystem.Runtime.Conditions;

namespace Snog.InteractionSystem.Editor.Inspectors
{
    [CustomEditor(typeof(RequiresConditions))]
    public class RequiresConditionsEditor : UnityEditor.Editor
    {
        private SerializedProperty spConditions;
        private SerializedProperty spRequireAll;

        private void OnEnable()
        {
            spConditions = serializedObject.FindProperty("conditions");
            spRequireAll = serializedObject.FindProperty("requireAll");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spRequireAll,
                new GUIContent("Logic",
                    "AND: all conditions must pass.\nOR: any single condition passing is enough."));

            // Replace the label with AND / OR phrasing
            string logicLabel = spRequireAll.boolValue
                ? "All conditions must pass (AND)"
                : "Any condition passing is enough (OR)";
            EditorGUILayout.LabelField(logicLabel, EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(spConditions, true);

            // Live evaluation status during play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                var rc = (RequiresConditions)target;

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Live Condition Status", EditorStyles.boldLabel);

                    var conditions = rc.Conditions;
                    if (conditions == null || conditions.Length == 0)
                    {
                        EditorGUILayout.LabelField("No conditions — always passes.", EditorStyles.miniLabel);
                    }
                    else
                    {
                        for (int i = 0; i < conditions.Length; i++)
                        {
                            var condition = conditions[i];
                            if (condition == null)
                            {
                                EditorGUILayout.LabelField($"[{i}] (null)", EditorStyles.miniLabel);
                                continue;
                            }

                            bool passes = condition.Evaluate(rc.gameObject);
                            string icon = passes ? "✓" : "✗";
                            Color prev  = GUI.color;
                            GUI.color   = passes ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                            EditorGUILayout.LabelField($"{icon}  {condition.name}");
                            GUI.color = prev;
                        }

                        EditorGUILayout.Space(2);
                        bool overall = rc.Evaluate(rc.gameObject);
                        EditorGUILayout.LabelField(
                            overall ? "▶ Overall: PASSES" : "▶ Overall: BLOCKED",
                            overall ? EditorStyles.boldLabel : EditorStyles.boldLabel);
                    }
                }

                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
