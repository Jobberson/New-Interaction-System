using System;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableScaffolderWindow : EditorWindow
{
    private const string WindowTitle = "Interactable Scaffolder (Extended)";

    private enum Preset
    {
        Door,
        Pickup,
        Lever,
        Readable,
        Button,
        SceneLoader,
        SavePoint,
        Custom // falls back to a minimal BaseInteractable
    }

    private enum ColliderType
    {
        None,
        Box,
        Sphere,
        Capsule,
        MeshConvex
    }

    // UI
    private Preset preset = Preset.Door;
    private string className = "DoorInteractable";
    private string nameSpace = "";

    // Generation options
    private bool inheritBase = true;
    private bool generateCustomEditorStub = false;
    private bool includeUnityEventHook = true;

    // Paths
    private string scriptsFolder = "Assets/Scripts/Interaction/Generated";
    private string prefabsFolder = "Assets/InteractionSystem/Prefabs";
    private string materialsFolder = "Assets/InteractionSystem/Materials";

    // Prefab options
    private bool createPrefab = true;
    private ColliderType prefabCollider = ColliderType.Box;
    private bool assignInteractableLayer = true;

    // Material options
    private bool createSampleMaterial = true;
    private Color sampleColor = new Color(0.75f, 0.7f, 0.6f);

    // Prompt default (used by templates)
    private string defaultPrompt = "Interact";

    [MenuItem("Tools/Interaction/Interactable Scaffolder (Extended)")]
    public static void Open()
    {
        var w = GetWindow<InteractableScaffolderWindow>(true, WindowTitle, true);
        w.minSize = new Vector2(640, 540);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
        var newPreset = (Preset)EditorGUILayout.EnumPopup("Type", preset);
        if (newPreset != preset)
        {
            preset = newPreset;
            className = GetSuggestedClassName(preset);
            defaultPrompt = GetSuggestedPrompt(preset);
            prefabCollider = GetSuggestedCollider(preset);
            sampleColor = GetSuggestedColor(preset);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Script", EditorStyles.boldLabel);
        className = EditorGUILayout.TextField("Class Name", className);
        nameSpace = EditorGUILayout.TextField("Namespace (optional)", nameSpace);
        inheritBase = EditorGUILayout.ToggleLeft("Inherit BaseInteractable", inheritBase);
        includeUnityEventHook = EditorGUILayout.ToggleLeft("Include UnityEvent onInteract Hook", includeUnityEventHook);
        generateCustomEditorStub = EditorGUILayout.ToggleLeft("Generate Custom Editor Stub", generateCustomEditorStub);
        defaultPrompt = EditorGUILayout.TextField("Default Prompt", defaultPrompt);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output Folders", EditorStyles.boldLabel);
        scriptsFolder = FolderField("Scripts Folder", scriptsFolder);
        prefabsFolder = FolderField("Prefabs Folder", prefabsFolder);
        materialsFolder = FolderField("Materials Folder", materialsFolder);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Prefab Options", EditorStyles.boldLabel);
        createPrefab = EditorGUILayout.ToggleLeft("Create Matching Prefab", createPrefab);
        using (new EditorGUI.DisabledScope(!createPrefab))
        {
            prefabCollider = (ColliderType)EditorGUILayout.EnumPopup("Collider", prefabCollider);
            assignInteractableLayer = EditorGUILayout.ToggleLeft("Assign 'Interactable' Layer", assignInteractableLayer);
            createSampleMaterial = EditorGUILayout.ToggleLeft("Create Sample Material", createSampleMaterial);
            if (createSampleMaterial)
            {
                sampleColor = EditorGUILayout.ColorField("Sample Color", sampleColor);
            }
        }

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(!IsInputValid(out string reason)))
        {
            if (GUILayout.Button("Create", GUILayout.Height(36)))
            {
                CreateAssets();
            }
        }
        if (!IsInputValid(out string msg))
        {
            EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox("The scaffolder creates a script template tailored to the preset, optionally a prefab with collider, sample material, and assigns the Interactable layer.", MessageType.Info);
    }

    private string FolderField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        if (GUILayout.Button("Browse...", GUILayout.Width(90)))
        {
            BrowseForProjectFolder(ref value);
        }
        EditorGUILayout.EndHorizontal();
        return value;
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

    private bool IsInputValid(out string reason)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            reason = "Class Name cannot be empty.";
            return false;
        }
        if (!IsValidIdentifier(className))
        {
            reason = "Class Name must be a valid C# identifier.";
            return false;
        }
        if (!scriptsFolder.StartsWith("Assets"))
        {
            reason = "Scripts Folder must be inside 'Assets'.";
            return false;
        }
        if (createPrefab && !prefabsFolder.StartsWith("Assets"))
        {
            reason = "Prefabs Folder must be inside 'Assets'.";
            return false;
        }
        reason = "OK";
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
        EnsureFolder(scriptsFolder);
        if (createPrefab)
        {
            EnsureFolder(prefabsFolder);
            if (createSampleMaterial)
                EnsureFolder(materialsFolder);
        }

        // Create script
        string scriptPath = Path.Combine(scriptsFolder, $"{className}.cs").Replace("\\", "/");
        if (File.Exists(scriptPath))
        {
            EditorUtility.DisplayDialog(WindowTitle, $"A script named '{className}.cs' already exists at:\n{scriptPath}", "OK");
            return;
        }

        string code = GenerateCodeForPreset();
        File.WriteAllText(scriptPath, code, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(scriptPath);

        // Optional: Custom Editor stub
        if (generateCustomEditorStub)
        {
            string editorPath = Path.Combine(scriptsFolder, $"{className}_Editor.cs").Replace("\\", "/");
            string editorCode = GenerateCustomEditorStubCode();
            File.WriteAllText(editorPath, editorCode, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(editorPath);
        }

        // Prefab and material
        string createdPrefabPath = null;
        if (createPrefab)
        {
            createdPrefabPath = CreatePresetPrefab();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select script
        var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        Selection.activeObject = scriptAsset;

        string msg = $"Created script: {scriptPath}";
        if (!string.IsNullOrEmpty(createdPrefabPath))
        {
            msg += $"\nCreated prefab: {createdPrefabPath}";
        }
        EditorUtility.DisplayDialog(WindowTitle, msg, "Nice");
    }

    private string GenerateCodeForPreset()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        if (preset == Preset.SceneLoader) sb.AppendLine("using UnityEngine.SceneManagement;");
        if (includeUnityEventHook) sb.AppendLine("using UnityEngine.Events;");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine($"namespace {nameSpace}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"    [AddComponentMenu(\"Interaction/{className}\")]");
        sb.AppendLine($"    public class {className} : {(inheritBase ? "BaseInteractable" : "MonoBehaviour")}");
        sb.AppendLine("    {");

        // Fields per preset
        switch (preset)
        {
            case Preset.Door:
                sb.AppendLine("        [Header(\"Door\")]");
                sb.AppendLine("        [SerializeField] private Transform doorPivot;");
                sb.AppendLine("        [SerializeField] private float openAngle = 90f;");
                sb.AppendLine("        [SerializeField] private float speed = 6f;");
                sb.AppendLine("        private bool isOpen = false;");
                sb.AppendLine("        private Quaternion closedRot;");
                sb.AppendLine("        private Quaternion openRot;");
                sb.AppendLine();
                break;

            case Preset.Pickup:
                sb.AppendLine("        [Header(\"Pickup\")]");
                sb.AppendLine("        [SerializeField] private string itemId = \"Item\";");
                sb.AppendLine("        [SerializeField] private int amount = 1;");
                sb.AppendLine();
                break;

            case Preset.Lever:
                sb.AppendLine("        [Header(\"Lever\")]");
                sb.AppendLine("        [SerializeField] private Animator animator;");
                sb.AppendLine("        [SerializeField] private string triggerName = \"Pull\";");
                sb.AppendLine("        private bool isOn = false;");
                sb.AppendLine();
                break;

            case Preset.Readable:
                sb.AppendLine("        [Header(\"Readable\")]");
                sb.AppendLine("        [TextArea] [SerializeField] private string text = \"Lorem ipsum\";");
                sb.AppendLine();
                break;

            case Preset.Button:
                sb.AppendLine("        [Header(\"Button\")]");
                sb.AppendLine("        [SerializeField] private AudioSource clickSfx;");
                sb.AppendLine("        private bool pressed = false;");
                sb.AppendLine();
                break;

            case Preset.SceneLoader:
                sb.AppendLine("        [Header(\"Scene Loader\")]");
                sb.AppendLine("        [SerializeField] private string sceneName = \"SampleScene\";");
                sb.AppendLine("        [SerializeField] private bool loadAdditively = false;");
                sb.AppendLine();
                break;

            case Preset.SavePoint:
                sb.AppendLine("        [Header(\"Save Point\")]");
                sb.AppendLine("        [SerializeField] private string saveId = \"save_point_01\";");
                sb.AppendLine();
                break;

            case Preset.Custom:
            default:
                sb.AppendLine("        [Header(\"Custom\")]");
                sb.AppendLine("        [SerializeField] private bool toggleState = false;");
                sb.AppendLine();
                break;
        }

        if (includeUnityEventHook)
        {
            sb.AppendLine("        [Header(\"Events\")]");
            sb.AppendLine("        [SerializeField] private UnityEvent onInteract;");
            sb.AppendLine();
        }

        // Awake/Start
        sb.AppendLine("        private void Awake()");
        sb.AppendLine("        {");
        switch (preset)
        {
            case Preset.Door:
                sb.AppendLine("            if (doorPivot == null) doorPivot = transform;");
                sb.AppendLine("            closedRot = doorPivot.localRotation;");
                sb.AppendLine("            openRot = Quaternion.Euler(doorPivot.localEulerAngles + new Vector3(0f, openAngle, 0f));");
                break;
            case Preset.Lever:
                sb.AppendLine("            if (animator == null) animator = GetComponent<Animator>();");
                break;
            default:
                sb.AppendLine("            // Initialize here if needed");
                break;
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // Update (for Door smoothing)
        if (preset == Preset.Door)
        {
            sb.AppendLine("        private void Update()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (doorPivot == null) return;");
            sb.AppendLine("            Quaternion target = isOpen ? openRot : closedRot;");
            sb.AppendLine("            doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, Time.deltaTime * speed);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Interact method(s)
        if (inheritBase)
        {
            sb.AppendLine("        protected override void PerformInteraction(GameObject interactor)");
            sb.AppendLine("        {");
            switch (preset)
            {
                case Preset.Door:
                    sb.AppendLine("            isOpen = !isOpen;");
                    break;
                case Preset.Pickup:
                    sb.AppendLine("            Debug.Log($\"Picked up {amount} x {itemId}\");");
                    sb.AppendLine("            Destroy(gameObject);");
                    break;
                case Preset.Lever:
                    sb.AppendLine("            isOn = !isOn;");
                    sb.AppendLine("            if (animator != null && !string.IsNullOrEmpty(triggerName))");
                    sb.AppendLine("            {");
                    sb.AppendLine("                animator.SetTrigger(triggerName);");
                    sb.AppendLine("            }");
                    break;
                case Preset.Readable:
                    sb.AppendLine("            Debug.Log($\"Read: {text}\");");
                    break;
                case Preset.Button:
                    sb.AppendLine("            pressed = true;");
                    sb.AppendLine("            if (clickSfx != null) clickSfx.Play();");
                    break;
                case Preset.SceneLoader:
                    sb.AppendLine("            if (!string.IsNullOrEmpty(sceneName))");
                    sb.AppendLine("            {");
                    sb.AppendLine("                var mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;");
                    sb.AppendLine("                SceneManager.LoadSceneAsync(sceneName, mode);");
                    sb.AppendLine("            }");
                    break;
                case Preset.SavePoint:
                    sb.AppendLine("            Debug.Log($\"Save requested at {saveId}\");");
                    break;
                case Preset.Custom:
                default:
                    sb.AppendLine("            toggleState = !toggleState;");
                    sb.AppendLine("            Debug.Log($\"[" + className + "] Interacted by {interactor.name}. State: {toggleState}\");");
                    break;
            }
            if (includeUnityEventHook)
            {
                sb.AppendLine("            onInteract?.Invoke();");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("        public void Interact(GameObject interactor)");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.Log($\"[" + className + "] Interacted by {interactor.name}\");");
            if (includeUnityEventHook)
            {
                sb.AppendLine("            onInteract?.Invoke();");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Prompt override for nice UX
        if (inheritBase)
        {
            sb.AppendLine("        public override string GetInteractionPrompt()");
            sb.AppendLine("        {");
            switch (preset)
            {
                case Preset.Door:
                    sb.AppendLine("            return isOpen ? \"Close\" : \"Open\";");
                    break;
                case Preset.Pickup:
                    sb.AppendLine("            return $\"Pick up {itemId}\";");
                    break;
                case Preset.Lever:
                    sb.AppendLine("            return \"Use\";");
                    break;
                case Preset.Readable:
                    sb.AppendLine("            return \"Read\";");
                    break;
                case Preset.Button:
                    sb.AppendLine("            return \"Press\";");
                    break;
                case Preset.SceneLoader:
                    sb.AppendLine("            return \"Enter\";");
                    break;
                case Preset.SavePoint:
                    sb.AppendLine("            return \"Save\";");
                    break;
                default:
                    sb.AppendLine($"            return \"{Escape(defaultPrompt)}\";");
                    break;
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine("}");
        }
        return sb.ToString();
    }

    private string GenerateCustomEditorStubCode()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("using UnityEngine;");
        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine($"namespace {nameSpace}");
            sb.AppendLine("{");
            sb.AppendLine($"    [CustomEditor(typeof({className}))]");
            sb.AppendLine($"    public class {className}_Editor : Editor");
            sb.AppendLine("    {");
            sb.AppendLine("        public override void OnInspectorGUI()");
            sb.AppendLine("        {");
            sb.AppendLine("            DrawDefaultInspector();");
            sb.AppendLine("            EditorGUILayout.HelpBox(\"This is a stub CustomEditor. You can extend it to add helpful tooling.\", MessageType.Info);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"[CustomEditor(typeof({className}))]");
            sb.AppendLine($"public class {className}_Editor : Editor");
            sb.AppendLine("{");
            sb.AppendLine("    public override void OnInspectorGUI()");
            sb.AppendLine("    {");
            sb.AppendLine("        DrawDefaultInspector();");
            sb.AppendLine("        EditorGUILayout.HelpBox(\"This is a stub CustomEditor. You can extend it to add helpful tooling.\", MessageType.Info);");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        return sb.ToString();
    }

    private string CreatePresetPrefab()
    {
        // Root GO
        var go = new GameObject(className);

        // Collider
        AddCollider(go, prefabCollider);

        // Layer
        if (assignInteractableLayer)
        {
            int layerIndex = EnsureLayer("Interactable");
            if (layerIndex >= 0) go.layer = layerIndex;
        }

        // For Door preset, add Pivot child to rotate
        if (preset == Preset.Door)
        {
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(go.transform, false);
            pivot.transform.localPosition = Vector3.zero;
        }

        // Attach the component
        var scriptType = FindTypeByName(string.IsNullOrWhiteSpace(nameSpace) ? className : $"{nameSpace}.{className}");
        if (scriptType == null)
        {
            // Script may not be compiled yet; leave as-is
        }
        else
        {
            go.AddComponent(scriptType);
        }

        string outPath = AssetDatabase.GenerateUniqueAssetPath($"{prefabsFolder}/{className}.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, outPath);
        GameObject.DestroyImmediate(go);

        return outPath;
    }

    private void AddCollider(GameObject go, ColliderType type)
    {
        switch (type)
        {
            case ColliderType.Box:
                if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
                break;
            case ColliderType.Sphere:
                if (go.GetComponent<Collider>() == null) go.AddComponent<SphereCollider>();
                break;
            case ColliderType.Capsule:
                if (go.GetComponent<Collider>() == null) go.AddComponent<CapsuleCollider>();
                break;
            case ColliderType.MeshConvex:
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf == null)
                    {
                        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        var mesh = cube.GetComponent<MeshFilter>().sharedMesh;
                        GameObject.DestroyImmediate(cube);
                        mf = go.AddComponent<MeshFilter>();
                        var mr = go.AddComponent<MeshRenderer>();
                        mf.sharedMesh = mesh;
                    }
                    var mc = go.AddComponent<MeshCollider>();
                    mc.convex = true;
                    break;
                }
            case ColliderType.None:
            default:
                break;
        }
    }

    private int EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < layersProp.arraySize; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (sp != null && sp.stringValue == layerName)
            {
                return i;
            }
        }

        for (int i = 8; i < 32; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (sp != null && string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning($"[Scaffolder] Could not create layer '{layerName}'. Please add it manually.");
        return LayerMask.NameToLayer(layerName);
    }

    private static string GetSuggestedClassName(Preset p)
    {
        switch (p)
        {
            case Preset.Door: return "DoorInteractable";
            case Preset.Pickup: return "PickupInteractable";
            case Preset.Lever: return "LeverInteractable";
            case Preset.Readable: return "ReadableInteractable";
            case Preset.Button: return "ButtonInteractable";
            case Preset.SceneLoader: return "SceneLoaderInteractable";
            case Preset.SavePoint: return "SavePointInteractable";
            default: return "NewInteractable";
        }
    }

    private static string GetSuggestedPrompt(Preset p)
    {
        switch (p)
        {
            case Preset.Door: return "Open";
            case Preset.Pickup: return "Pick up";
            case Preset.Lever: return "Use";
            case Preset.Readable: return "Read";
            case Preset.Button: return "Press";
            case Preset.SceneLoader: return "Enter";
            case Preset.SavePoint: return "Save";
            default: return "Interact";
        }
    }

    private static ColliderType GetSuggestedCollider(Preset p)
    {
        switch (p)
        {
            case Preset.Pickup: return ColliderType.Sphere;
            case Preset.Door: return ColliderType.Box;
            case Preset.Lever: return ColliderType.Box;
            case Preset.Readable: return ColliderType.Box;
            case Preset.Button: return ColliderType.Box;
            case Preset.SceneLoader: return ColliderType.Box;
            case Preset.SavePoint: return ColliderType.Box;
            default: return ColliderType.Box;
        }
    }

    private static Color GetSuggestedColor(Preset p)
    {
        switch (p)
        {
            case Preset.Pickup: return new Color(0.9f, 0.85f, 0.3f);
            case Preset.Door: return new Color(0.5f, 0.3f, 0.15f);
            case Preset.Lever: return new Color(0.5f, 0.5f, 0.5f);
            case Preset.Readable: return new Color(0.8f, 0.8f, 0.9f);
            case Preset.Button: return new Color(0.7f, 0.2f, 0.2f);
            case Preset.SceneLoader: return new Color(0.2f, 0.5f, 0.7f);
            case Preset.SavePoint: return new Color(0.2f, 0.7f, 0.2f);
            default: return new Color(0.75f, 0.7f, 0.6f);
        }
    }

    private static string Escape(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }

    private static void EnsureFolder(string projectPath)
    {
        if (AssetDatabase.IsValidFolder(projectPath))
            return;

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

    private static Type FindTypeByName(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }
}
