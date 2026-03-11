using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Helpers;
using Snog.InteractionSystem.Runtime.Helpers.QuickActions;

namespace Snog.InteractionSystem.Editor.Inspectors
{
    [CustomEditor(typeof(BaseInteractable), true)]
    [CanEditMultipleObjects]
    public class BaseInteractableEditor : UnityEditor.Editor
    {
        private SerializedProperty spPrompt;
        private SerializedProperty spIsEnabled;
        private SerializedProperty spOnInteract;
        private SerializedProperty spInteractionMode;
        private SerializedProperty spOverrideHold;
        private SerializedProperty spHoldDuration;
        private SerializedProperty spCooldown;
        private SerializedProperty spMaxUses;

        private void OnEnable()
        {
            spPrompt          = serializedObject.FindProperty("prompt");
            spIsEnabled       = serializedObject.FindProperty("isEnabled");
            spOnInteract      = serializedObject.FindProperty("onInteract");
            spInteractionMode = serializedObject.FindProperty("interactionMode");
            spOverrideHold    = serializedObject.FindProperty("overrideHoldDuration");
            spHoldDuration    = serializedObject.FindProperty("holdDurationSeconds");
            spCooldown        = serializedObject.FindProperty("cooldown");
            spMaxUses         = serializedObject.FindProperty("maxUses");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Core
            EditorGUILayout.PropertyField(spPrompt);
            EditorGUILayout.PropertyField(spIsEnabled);

            EditorGUILayout.Space(4);

            // Interaction mode — FIX: compare by enum value, not fragile int index
            EditorGUILayout.PropertyField(spInteractionMode);
            var currentMode = (InteractionMode)spInteractionMode.intValue;
            if (currentMode == InteractionMode.Hold)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(spOverrideHold,
                        new GUIContent("Override Hold Duration",
                            "If checked, this object uses its own hold duration instead of the global setting in InteractionSettings."));
                    if (spOverrideHold.boolValue)
                        EditorGUILayout.PropertyField(spHoldDuration, new GUIContent("Hold Duration (s)"));
                }
            }

            EditorGUILayout.Space(4);

            // Cooldown & uses
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Cooldown & Uses", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(spCooldown, new GUIContent("Cooldown (s)",
                    "Seconds before this interactable can be used again. 0 = no cooldown."));

                EditorGUILayout.PropertyField(spMaxUses, new GUIContent("Max Uses",
                    "Maximum number of times this interactable can be triggered. -1 = unlimited."));

                bool hasCooldown = spCooldown.floatValue > 0f;
                bool hasMaxUses  = spMaxUses.intValue >= 0;

                if (!hasCooldown && !hasMaxUses)
                    EditorGUILayout.LabelField("Unlimited uses, no cooldown.", EditorStyles.miniLabel);
                else if (hasCooldown && !hasMaxUses)
                    EditorGUILayout.LabelField($"Unlimited uses, {spCooldown.floatValue:0.0}s cooldown between each.", EditorStyles.miniLabel);
                else if (!hasCooldown && hasMaxUses)
                    EditorGUILayout.LabelField($"Up to {spMaxUses.intValue} use(s), then permanently disabled.", EditorStyles.miniLabel);
                else
                    EditorGUILayout.LabelField($"Up to {spMaxUses.intValue} use(s) with {spCooldown.floatValue:0.0}s cooldown.", EditorStyles.miniLabel);

                // Runtime status during play mode
                if (Application.isPlaying && targets.Length == 1)
                {
                    var it = (BaseInteractable)target;
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);
                        if (hasCooldown)
                            EditorGUILayout.LabelField("Cooldown Remaining", $"{it.CooldownRemaining:0.00}s");
                        if (hasMaxUses)
                            EditorGUILayout.LabelField("Uses Remaining", it.UsesRemaining.ToString());
                    }
                    Repaint();
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(spOnInteract);

            // Quick actions
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Add Quick Actions", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "One-click setup: adds the component and wires it to onInteract.",
                    EditorStyles.miniLabel);

                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("▶  Play Audio"))       AddAction<OnInteract_PlayAudio>(SetupPlayAudio);
                    if (GUILayout.Button("⚙  Animator Trigger")) AddAction<OnInteract_AnimatorTrigger>(SetupAnimatorTrigger);
                    if (GUILayout.Button("✚  Set Active"))       AddAction<OnInteract_SetActive>(SetupSetActive);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("⇄  Toggle Active"))    AddAction<OnInteract_ToggleActive>(null);
                    if (GUILayout.Button("▣  Debug Log"))        AddAction<OnInteract_DebugLog>(null);
                    if (GUILayout.Button("∅  Invoke Event"))     AddAction<OnInteract_InvokeEvent>(null);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Generic action helper ─────────────────────────────────────────────────

        private void AddAction<T>(System.Action<BaseInteractable, T> setup) where T : Component
        {
            foreach (Object obj in targets)
            {
                var interactable = obj as BaseInteractable;
                if (interactable == null) continue;

                Undo.RecordObject(interactable, $"Add {typeof(T).Name}");

                var action = interactable.gameObject.GetComponent<T>();
                if (action == null)
                    action = Undo.AddComponent<T>(interactable.gameObject);

                setup?.Invoke(interactable, action);
                ConnectToEvent(interactable.OnInteractEvent, action, GetMethodName<T>());

                EditorUtility.SetDirty(interactable);
                EditorUtility.SetDirty(action);

                if (PrefabUtility.IsPartOfPrefabInstance(interactable))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(interactable);
            }
        }

        // ── Per-type setup ────────────────────────────────────────────────────────

        private void SetupPlayAudio(BaseInteractable interactable, OnInteract_PlayAudio action)
        {
            if (action.target != null) return;
            var src = interactable.gameObject.GetComponent<AudioSource>()
                   ?? Undo.AddComponent<AudioSource>(interactable.gameObject);
            Undo.RecordObject(action, "Configure Play Audio");
            action.target = src;
        }

        private void SetupAnimatorTrigger(BaseInteractable interactable, OnInteract_AnimatorTrigger action)
        {
            if (action.animator == null)
            {
                var anim = interactable.gameObject.GetComponent<Animator>()
                        ?? Undo.AddComponent<Animator>(interactable.gameObject);
                Undo.RecordObject(action, "Configure Animator Trigger");
                action.animator = anim;
            }
            if (string.IsNullOrEmpty(action.triggerName))
                action.triggerName = "OnInteract";
        }

        private void SetupSetActive(BaseInteractable interactable, OnInteract_SetActive action)
        {
            Undo.RecordObject(action, "Configure Set Active");
            if (action.targets == null || action.targets.Length == 0)
                action.targets = new GameObject[] { interactable.gameObject };
            action.activeValue = false;
        }

        // ── UnityEvent wiring ─────────────────────────────────────────────────────

        private static string GetMethodName<T>()
        {
            if (typeof(T) == typeof(OnInteract_PlayAudio))       return nameof(OnInteract_PlayAudio.Play);
            if (typeof(T) == typeof(OnInteract_AnimatorTrigger)) return nameof(OnInteract_AnimatorTrigger.Fire);
            if (typeof(T) == typeof(OnInteract_SetActive))       return nameof(OnInteract_SetActive.Apply);
            if (typeof(T) == typeof(OnInteract_ToggleActive))    return nameof(OnInteract_ToggleActive.Apply);
            if (typeof(T) == typeof(OnInteract_DebugLog))        return nameof(OnInteract_DebugLog.Log);
            if (typeof(T) == typeof(OnInteract_InvokeEvent))     return nameof(OnInteract_InvokeEvent.Invoke);
            return "Apply";
        }

        private static void ConnectToEvent(UnityEvent evt, Object target, string methodName)
        {
            if (evt == null || target == null) return;

            int count = evt.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
                if (evt.GetPersistentTarget(i) == target && evt.GetPersistentMethodName(i) == methodName)
                    return;

            var methodInfo = target.GetType().GetMethod(
                methodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (methodInfo == null) return;

            var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, methodInfo);
            UnityEventTools.AddPersistentListener(evt, action);
        }
    }
}
