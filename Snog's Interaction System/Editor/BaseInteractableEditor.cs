using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Snog.InteractionSystem.Editor
{
    [CustomEditor(typeof(BaseInteractable), true)]
    [CanEditMultipleObjects]
    public class BaseInteractableEditor : Editor
    {
        private SerializedProperty spPrompt;
        private SerializedProperty spIsEnabled;
        private SerializedProperty spOnInteract;

        private void OnEnable()
        {
            spPrompt = serializedObject.FindProperty("prompt");
            spIsEnabled = serializedObject.FindProperty("isEnabled");
            spOnInteract = serializedObject.FindProperty("onInteract");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spPrompt);
            EditorGUILayout.PropertyField(spIsEnabled);
            EditorGUILayout.PropertyField(spOnInteract);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add Common Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Play Audio"))
                {
                    AddPlayAudioAction();
                }

                if (GUILayout.Button("⚙ Animator Trigger"))
                {
                    AddAnimatorTriggerAction();
                }

                if (GUILayout.Button("✚ Set Active"))
                {
                    AddSetActiveAction();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddPlayAudioAction()
        {
            ForEachTargetInteractable(
                (BaseInteractable interactable) =>
                {
                    GameObject go = interactable.gameObject;

                    var action = go.GetComponent<OnInteract_PlayAudio>();

                    if (action == null)
                    {
                        action = Undo.AddComponent<OnInteract_PlayAudio>(go);
                    }

                    var src = go.GetComponent<AudioSource>();

                    if (src == null)
                    {
                        src = Undo.AddComponent<AudioSource>(go);
                    }

                    Undo.RecordObject(action, "Configure Play Audio Action");
                    action.target = src;

                    AddPersistentListenerIfMissing(
                        interactable.OnInteractEvent,
                        action,
                        nameof(OnInteract_PlayAudio.Play),
                        action.Play
                    );

                    MarkDirtyAndPrefab(interactable, action);
                }
            );
        }

        private void AddAnimatorTriggerAction()
        {
            ForEachTargetInteractable(
                (BaseInteractable interactable) =>
                {
                    GameObject go = interactable.gameObject;

                    var action = go.GetComponent<OnInteract_AnimatorTrigger>();

                    if (action == null)
                    {
                        action = Undo.AddComponent<OnInteract_AnimatorTrigger>(go);
                    }

                    var anim = go.GetComponent<Animator>();

                    if (anim == null)
                    {
                        anim = Undo.AddComponent<Animator>(go);
                    }

                    Undo.RecordObject(action, "Configure Animator Trigger Action");
                    action.animator = anim;

                    if (string.IsNullOrEmpty(action.triggerName))
                    {
                        action.triggerName = "OnInteract";
                    }

                    AddPersistentListenerIfMissing(
                        interactable.OnInteractEvent,
                        action,
                        nameof(OnInteract_AnimatorTrigger.Fire),
                        action.Fire
                    );

                    MarkDirtyAndPrefab(interactable, action);
                }
            );
        }

        private void AddSetActiveAction()
        {
            ForEachTargetInteractable(
                (BaseInteractable interactable) =>
                {
                    GameObject go = interactable.gameObject;

                    var action = go.GetComponent<OnInteract_SetActive>();

                    if (action == null)
                    {
                        action = Undo.AddComponent<OnInteract_SetActive>(go);
                    }

                    Undo.RecordObject(action, "Configure Set Active Action");

                    if (action.targets == null || action.targets.Length == 0)
                    {
                        action.targets = new GameObject[] { go };
                    }

                    action.activeValue = true;

                    AddPersistentListenerIfMissing(
                        interactable.OnInteractEvent,
                        action,
                        nameof(OnInteract_SetActive.Apply),
                        action.Apply
                    );

                    MarkDirtyAndPrefab(interactable, action);
                }
            );
        }

        private void ForEachTargetInteractable(System.Action<BaseInteractable> perTarget)
        {
            foreach (Object o in targets)
            {
                var interactable = o as BaseInteractable;

                if (interactable == null)
                {
                    continue;
                }

                Undo.RecordObject(interactable, "Add Interact Action");
                perTarget.Invoke(interactable);
            }
        }

        private void AddPersistentListenerIfMissing(UnityEvent evt, Object target, string methodName, UnityAction call)
        {
            if (evt == null)
            {
                return;
            }

            if (HasPersistentListener(evt, target, methodName))
            {
                return;
            }

            UnityEventTools.AddPersistentListener(evt, call);
        }

        private bool HasPersistentListener(UnityEvent evt, Object target, string methodName)
        {
            int count = evt.GetPersistentEventCount();

            for (int i = 0; i < count; i++)
            {
                Object t = evt.GetPersistentTarget(i);
                string m = evt.GetPersistentMethodName(i);

                if (t == target && m == methodName)
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkDirtyAndPrefab(Object a, Object b)
        {
            EditorUtility.SetDirty(a);
            EditorUtility.SetDirty(b);

            if (PrefabUtility.IsPartOfPrefabInstance(a))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(a);
            }

            if (PrefabUtility.IsPartOfPrefabInstance(b))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(b);
            }
        }
    }
}