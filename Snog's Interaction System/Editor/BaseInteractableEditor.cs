using UnityEditor;
using UnityEngine;
using UnityEditor.Events;

[CustomEditor(typeof(BaseInteractable), true)]
public class BaseInteractableEditor : Editor
{
    private SerializedProperty spPrompt;
    private SerializedProperty spIsEnabled;
    private SerializedProperty spOnInteract;

    // Optional per-object mode fields if you adopted the override earlier
    private SerializedProperty spInteractionMode;
    private SerializedProperty spOverrideHold;
    private SerializedProperty spHoldSeconds;

    private bool foldPrompt = true;
    private bool foldMode = true;
    private bool foldRequirements = false;
    private bool foldActions = true;

    private void OnEnable()
    {
        spPrompt = serializedObject.FindProperty("prompt");
        spIsEnabled = serializedObject.FindProperty("isEnabled");
        spOnInteract = serializedObject.FindProperty("onInteract");

        spInteractionMode = serializedObject.FindProperty("interactionMode");
        spOverrideHold = serializedObject.FindProperty("overrideHoldDuration");
        spHoldSeconds = serializedObject.FindProperty("holdDurationSeconds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Prompt
        foldPrompt = EditorGUILayout.BeginFoldoutHeaderGroup(foldPrompt, "Prompt");
        if (foldPrompt)
        {
            EditorGUILayout.PropertyField(spPrompt);
            EditorGUILayout.PropertyField(spIsEnabled);
            EditorGUILayout.HelpBox("Prompt is the action label used to build the HUD message (e.g., \"Open\"). You can override GetInteractionPrompt() for dynamic text.", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // Mode
        if (spInteractionMode != null)
        {
            foldMode = EditorGUILayout.BeginFoldoutHeaderGroup(foldMode, "Interaction Mode");
            if (foldMode)
            {
                EditorGUILayout.PropertyField(spInteractionMode);
                using (new EditorGUI.DisabledScope(!spOverrideHold.boolValue))
                {
                    // shows even disabled so user sees both fields
                }
                EditorGUILayout.PropertyField(spOverrideHold, new GUIContent("Override Hold Duration"));
                if (spOverrideHold.boolValue)
                {
                    EditorGUILayout.PropertyField(spHoldSeconds, new GUIContent("Hold Duration (s)"));
                }
                EditorGUILayout.HelpBox("Choose Press, Hold, or InheritFromInteractor. If Hold is selected, you can override the duration here.", MessageType.None);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);
        }

        // Requirements (placeholder area for your future Requirement SOs)
        foldRequirements = EditorGUILayout.BeginFoldoutHeaderGroup(foldRequirements, "Requirements");
        if (foldRequirements)
        {
            EditorGUILayout.HelpBox("Add your Requirement components or ScriptableObjects here (e.g., HasItem, PowerOn, etc.). Then check them inside CanInteract().", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // Actions (UnityEvent)
        foldActions = EditorGUILayout.BeginFoldoutHeaderGroup(foldActions, "Actions (onInteract)");
        if (foldActions)
        {
            EditorGUILayout.PropertyField(spOnInteract);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Add Common Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Play Audio"))
                {
                    AddPlayAudioAction((BaseInteractable)target);
                }
                if (GUILayout.Button("⚙ Animator Trigger"))
                {
                    AddAnimatorTriggerAction((BaseInteractable)target);
                }
                if (GUILayout.Button("✚ Set Active"))
                {
                    AddSetActiveAction((BaseInteractable)target);
                }
            }

            EditorGUILayout.HelpBox("Quick buttons add tiny helper components and bind their methods to onInteract, so designers can create behavior without code.", MessageType.None);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void AddPlayAudioAction(BaseInteractable t)
    {
        var go = t.gameObject;
        var action = go.GetComponent<OnInteract_PlayAudio>();
        if (action == null) action = Undo.AddComponent<OnInteract_PlayAudio>(go);

        var src = go.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(go);
        action.target = src;

        // Bind event
        var ev = spOnInteract;
        if (ev != null)
        {
            UnityEventTools.AddPersistentListener((t as BaseInteractable).GetType()
                .GetField("onInteract", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(t) as UnityEngine.Events.UnityEvent, action.Play);
            EditorUtility.SetDirty(t);
        }
    }

    private void AddAnimatorTriggerAction(BaseInteractable t)
    {
        var go = t.gameObject;
        var action = go.GetComponent<OnInteract_AnimatorTrigger>();
        if (action == null) action = Undo.AddComponent<OnInteract_AnimatorTrigger>(go);

        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = Undo.AddComponent<Animator>(go);
        action.animator = anim;
        if (string.IsNullOrEmpty(action.triggerName)) action.triggerName = "OnInteract";

        // Bind event
        var ev = spOnInteract;
        if (ev != null)
        {
            UnityEventTools.AddPersistentListener((t as BaseInteractable).GetType()
                .GetField("onInteract", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(t) as UnityEngine.Events.UnityEvent, action.Fire);
            EditorUtility.SetDirty(t);
        }
    }

    private void AddSetActiveAction(BaseInteractable t)
    {
        var go = t.gameObject;
        var action = go.GetComponent<OnInteract_SetActive>();
        if (action == null) action = Undo.AddComponent<OnInteract_SetActive>(go);

        if (action.targets == null || action.targets.Length == 0)
        {
            action.targets = new GameObject[] { go };
        }
        action.activeValue = true;

        // Bind event
        var ev = spOnInteract;
        if (ev != null)
        {
            UnityEventTools.AddPersistentListener((t as BaseInteractable).GetType()
                .GetField("onInteract", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(t) as UnityEngine.Events.UnityEvent, action.Apply);
            EditorUtility.SetDirty(t);
        }
    }
}
