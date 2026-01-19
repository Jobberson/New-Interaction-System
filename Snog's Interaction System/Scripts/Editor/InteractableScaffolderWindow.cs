
// Assets/Editor/InteractableScaffolderWindow.cs
// Creates interactable scripts from a template and (optionally) a prefab.
// Allman braces style as requested.

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class InteractableScaffolderWindow : EditorWindow
{
    private const string WindowTitle = "Interactable Scaffolder";
    private const string PendingKey = "InteractableScaffolder_PendingList"; // used by post-script hook

    private string className = "NewInteractable";
    private string targetFolder = "Assets/Scripts/Interaction/Generated";
    private string nameSpace = "";
    private bool inheritBaseInteractable = true;
    private bool implementICustomPrompt = false;
    private bool includeSampleFields = true;
    private bool includeUnityEvent = true;
    private bool overridePromptMethod = true;
    private string defaultPrompt = "Interact";

    private bool createPrefab = true;
    private string prefabFolder = "Assets/Prefabs/Interaction";
    private ColliderType colliderType = ColliderType.Box;
    private bool openScriptAfterCreate = true;

    private enum ColliderType
    {
        None,
        Box,
        Sphere,
        Capsule
    }

    [MenuItem("Tools/Interaction/Interactable Scaffolder")]
    public static void Open()
    {
        var w = GetWindow<InteractableScaffolderWindow>(true, WindowTitle, true);
        w.minSize = new Vector2(520, 420);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Script Settings", EditorStyles.boldLabel);

        className = EditorGUILayout.TextField(new GUIContent("Class Name", "C# type name (e.g., DoorInteractable)"), className);
        nameSpace = EditorGUILayout.TextField(new GUIContent("Namespace (optional)"), nameSpace);

        EditorGUILayout.Space(4);
        inheritBaseInteractable = EditorGUILayout.ToggleLeft(new GUIContent("Inherit BaseInteractable", "If unchecked, inherit MonoBehaviour instead"), inheritBaseInteractable);
        implementICustomPrompt = EditorGUILayout.ToggleLeft(new GUIContent("Implement ICustomPrompt", "Requires you to have the ICustomPrompt + InteractionPromptData types in your project"), implementICustomPrompt);

        includeSampleFields = EditorGUILayout.ToggleLeft(new GUIContent("Include Sample Serialized Fields"), includeSampleFields);
        includeUnityEvent = EditorGUILayout.ToggleLeft(new GUIContent("Include UnityEvent 'onInteract' Hook"), includeUnityEvent);
        overridePromptMethod = EditorGUILayout.ToggleLeft(new GUIContent("Override GetInteractionPrompt"), overridePromptMethod);
        using (new EditorGUI.DisabledScope(!overridePromptMethod))
        {
            defaultPrompt = EditorGUILayout.TextField(new GUIContent("Default Prompt Label"), defaultPrompt);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        targetFolder = EditorGUILayout.TextField(new GUIContent("Script Folder"), targetFolder);
        if (GUILayout.Button("Browse...", GUILayout.Width(90)))
        {
            BrowseForProjectFolder(ref targetFolder);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        createPrefab = EditorGUILayout.ToggleLeft(new GUIContent("Also Create Prefab"), createPrefab, GUILayout.Width(150));
        prefabFolder = EditorGUILayout.TextField(new GUIContent("Prefab Folder"), prefabFolder);
        if (GUILayout.Button("Browse...", GUILayout.Width(90)))
        {
            BrowseForProjectFolder(ref prefabFolder);
        }
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(!createPrefab))
        {
            colliderType = (ColliderType)EditorGUILayout.EnumPopup(new GUIContent("Prefab Collider Type"), colliderType);
        }

        EditorGUILayout.Space(10);
        openScriptAfterCreate = EditorGUILayout.ToggleLeft("Open Script After Create", openScriptAfterCreate);

        EditorGUILayout.Space(12);
        using (new EditorGUI.DisabledScope(!IsInputValid(out _)))
        {
            if (GUILayout.Button("Create Script" + (createPrefab ? " + Prefab" : ""), GUILayout.Height(34)))
            {
                CreateAssets();
            }
        }

        EditorGUILayout.Space();
        DrawHelpBox();
    }

    private void DrawHelpBox()
    {
        var (valid, reason) = IsInputValid(out string msg) ? (true, "") : (false, msg);
        if (!valid)
        {
            EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Generates a C# interactable script from a template.\n" +
                "- If 'Also Create Prefab' is ON, a prefab will be created.\n" +
                "- The component is auto-added to the prefab after the scripts recompile.",
                MessageType.Info
            );
        }
    }

    private bool IsInputValid(out string message)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            message = "Class Name cannot be empty.";
            return false;
        }

        if (!IsValidIdentifier(className))
        {
            message = "Class Name must be a valid C# identifier (letters, digits, underscores; cannot start with a digit).";
            return false;
        }

        if (!targetFolder.StartsWith("Assets"))
        {
            message = "Script Folder must be inside the project (start with 'Assets').";
            return false;
        }

        if (createPrefab && !prefabFolder.StartsWith("Assets"))
        {
            message = "Prefab Folder must be inside the project (start with 'Assets').";
            return false;
        }

        message = "OK";
        return true;
    }

    private static bool IsValidIdentifier(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (!(char.IsLetter(s[0]) || s[0] == '_')) return false;
        for (int i = 1; i < s.Length; i++)
        {
            if (!(char.IsLetterOrDigit(s[i]) || s[i] == '_')) return false;
        }
        return true;
    }

    private void CreateAssets()
    {
        // Ensure folders exist
        EnsureFolder(targetFolder);
        if (createPrefab)
        {
            EnsureFolder(prefabFolder);
        }

        // Create script
        string scriptPath = Path.Combine(targetFolder, $"{className}.cs").Replace("\\", "/");
        if (File.Exists(scriptPath))
        {
            EditorUtility.DisplayDialog(WindowTitle, $"A script named '{className}.cs' already exists at:\n{scriptPath}", "OK");
            return;
        }

        string code = GenerateCode();
        File.WriteAllText(scriptPath, code, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AssetDatabase.ImportAsset(scriptPath);

        // Optionally create prefab (component will be added after compile via post script)
        string createdPrefabPath = null;
        if (createPrefab)
        {
            createdPrefabPath = CreateBasicPrefab(prefabFolder, className, colliderType);
            // Register a pending operation so the post-reload hook can add the component
            RegisterPendingAdd(createdPrefabPath, GetFullTypeName());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select script (and optionally open)
        var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        Selection.activeObject = scriptAsset;

        if (openScriptAfterCreate && scriptAsset != null)
        {
            AssetDatabase.OpenAsset(scriptAsset);
        }

        // Notify
        string msg = $"Created script: {scriptPath}";
        if (createdPrefabPath != null)
        {
            msg += $"\nCreated prefab: {createdPrefabPath}\n(The {className} component will be added after scripts recompile.)";
        }
        EditorUtility.DisplayDialog(WindowTitle, msg, "Nice!");
    }

    private static void EnsureFolder(string projectPath)
    {
        if (AssetDatabase.IsValidFolder(projectPath))
            return;

        // Create nested folders step by step
        string[] parts = projectPath.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
            throw new Exception("Path must start with 'Assets'.");

        string acc = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{acc}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(acc, parts[i]);
            }
            acc = next;
        }
    }

    private void BrowseForProjectFolder(ref string field)
    {
        string absStart = Path.GetFullPath(field.StartsWith("Assets") ? field : "Assets");
        string selectedAbs = EditorUtility.OpenFolderPanel("Select Folder (inside Assets)", absStart, "");
        if (string.IsNullOrEmpty(selectedAbs))
            return;

        string assetsAbs = Path.GetFullPath(Application.dataPath);
        if (!selectedAbs.StartsWith(assetsAbs))
        {
            EditorUtility.DisplayDialog(WindowTitle, "Please pick a folder inside the project 'Assets' directory.", "OK");
            return;
        }

        string rel = "Assets" + selectedAbs.Substring(assetsAbs.Length).Replace("\\", "/");
        field = rel;
    }

    private string GetBaseType()
    {
        return inheritBaseInteractable ? "BaseInteractable" : "MonoBehaviour";
    }

    private string GetFullTypeName()
    {
        if (string.IsNullOrWhiteSpace(nameSpace))
            return className;
        return $"{nameSpace}.{className}";
    }

    private string GenerateCode()
    {
        // Build the class file content
        var sb = new StringBuilder();

        sb.AppendLine("using UnityEngine;");
        if (includeUnityEvent)
        {
            sb.AppendLine("using UnityEngine.Events;");
        }
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine($"namespace {nameSpace}");
            sb.AppendLine("{");
        }

        // AddComponentMenu for convenience
        sb.AppendLine($"    [AddComponentMenu(\"Interaction/{className}\")]");
        sb.AppendLine($"    public class {className} : {GetBaseType()}" + (implementICustomPrompt ? ", ICustomPrompt" : ""));
        sb.AppendLine("    {");

        if (includeSampleFields)
        {
            sb.AppendLine("        [Header(\"Sample\")]");
            sb.AppendLine("        [SerializeField] private bool toggleState = false;");
            if (includeUnityEvent)
            {
                sb.AppendLine("        [SerializeField] private UnityEvent onInteract;");
            }
            sb.AppendLine();
        }

        // Awake/Start optional (empty)
        sb.AppendLine("        private void Awake()");
        sb.AppendLine("        {");
        sb.AppendLine("            // Initialize here if needed");
        sb.AppendLine("        }");
        sb.AppendLine();

        // PerformInteraction override / or placeholder
        if (inheritBaseInteractable)
        {
            sb.AppendLine("        protected override void PerformInteraction(GameObject interactor)");
            sb.AppendLine("        {");
            if (includeUnityEvent)
            {
                sb.AppendLine("            onInteract?.Invoke();");
            }
            sb.AppendLine("            // TODO: Your interaction logic");
            sb.AppendLine("            toggleState = !toggleState;");
            sb.AppendLine("            Debug.Log($\"[" + className + "] Interacted by {interactor.name}. State: {toggleState}\");");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("        // Example method if you choose not to inherit BaseInteractable");
            sb.AppendLine("        public void Interact(GameObject interactor)");
            sb.AppendLine("        {");
            if (includeUnityEvent)
            {
                sb.AppendLine("            onInteract?.Invoke();");
            }
            sb.AppendLine("            Debug.Log($\"[" + className + "] Interacted by {interactor.name}\");");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        if (overridePromptMethod)
        {
            if (inheritBaseInteractable)
            {
                sb.AppendLine("        public override string GetInteractionPrompt()");
                sb.AppendLine("        {");
                sb.AppendLine($"            return \"{EscapeForCode(defaultPrompt)}\";");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("        // If you are not inheriting BaseInteractable,");
                sb.AppendLine("        // ensure your system calls a similar method or uses IInteractable.");
                sb.AppendLine();
            }
        }

        if (implementICustomPrompt)
        {
            sb.AppendLine("        public InteractionPromptData GetPromptData()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new InteractionPromptData");
            sb.AppendLine("            {");
            sb.AppendLine($"                label = \"{EscapeForCode(defaultPrompt)}\",");
            sb.AppendLine("                isFullSentence = false,");
            sb.AppendLine("                showWhenUnavailable = false,");
            sb.AppendLine("                unavailableLabel = null,");
            sb.AppendLine("                icon = null");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("    }"); // class

        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine("}"); // namespace
        }

        return sb.ToString();
    }

    private static string EscapeForCode(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }

    private static string CreateBasicPrefab(string folder, string baseName, ColliderType colType)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.prefab");
        var go = new GameObject(baseName);

        switch (colType)
        {
            case ColliderType.Box:
                go.AddComponent<BoxCollider>();
                break;
            case ColliderType.Sphere:
                go.AddComponent<SphereCollider>();
                break;
            case ColliderType.Capsule:
                go.AddComponent<CapsuleCollider>();
                break;
            case ColliderType.None:
            default:
                break;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        GameObject.DestroyImmediate(go);
        return path;
    }

    private static void RegisterPendingAdd(string prefabPath, string fullTypeName)
    {
        if (string.IsNullOrEmpty(prefabPath) || string.IsNullOrEmpty(fullTypeName))
            return;

        // Append to a semi-colon separated list: "prefabPath|fullTypeName"
        string existing = EditorPrefs.GetString(PendingKey, "");
        string entry = prefabPath + "|" + fullTypeName;
        string updated = string.IsNullOrEmpty(existing) ? entry : (existing + ";" + entry);
        EditorPrefs.SetString(PendingKey, updated);
    }

    // Exposed so the post-processor can reuse the key
    public static string GetPendingKey()
    {
        return PendingKey;
    }
}