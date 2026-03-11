using System;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Snog.InteractionSystem.Editor.Windows
{
    public class InteractableScaffolderWindow : EditorWindow
    {
        private const string WindowTitle = "Interactable Scaffolder";

        private enum Preset
        {
            Door,
            Pickup,
            Lever,
            Readable,
            Button,
            SceneLoader,
            SavePoint,
            Custom,
        }

        private enum ColliderType
        {
            None,
            Box,
            Sphere,
            Capsule,
            MeshConvex,
        }

        // Script options
        private Preset preset         = Preset.Door;
        private string className      = "DoorInteractable";
        private string nameSpace      = "";
        private bool   inheritBase    = true;
        private bool   includeEvent   = true;
        private bool   genEditorStub  = false;
        private string defaultPrompt  = "Open";

        // Paths
        private string scriptsFolder  = "Assets/Scripts/Interaction/Generated";
        private string prefabsFolder  = "Assets/InteractionSystem/Prefabs";
        private string materialsFolder = "Assets/InteractionSystem/Materials";

        // Prefab options
        private bool         createPrefab         = true;
        private ColliderType prefabCollider        = ColliderType.Box;
        private bool         assignLayer           = true;
        private bool         createMaterial        = true;
        private Color        sampleColor           = new Color(0.75f, 0.7f, 0.6f);

        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Interaction/Interactable Scaffolder")]
        public static void Open()
        {
            var w = GetWindow<InteractableScaffolderWindow>(true, WindowTitle, true);
            w.minSize = new Vector2(620, 560);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            // ── Preset ────────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            var newPreset = (Preset)EditorGUILayout.EnumPopup("Type", preset);
            if (newPreset != preset)
            {
                preset        = newPreset;
                className     = GetSuggestedClassName(preset);
                defaultPrompt = GetSuggestedPrompt(preset);
                prefabCollider = GetSuggestedCollider(preset);
                sampleColor   = GetSuggestedColor(preset);
            }

            // ── Script ────────────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Script", EditorStyles.boldLabel);
            className     = EditorGUILayout.TextField("Class Name", className);
            nameSpace     = EditorGUILayout.TextField("Namespace (optional)", nameSpace);
            inheritBase   = EditorGUILayout.ToggleLeft("Inherit BaseInteractable", inheritBase);
            includeEvent  = EditorGUILayout.ToggleLeft("Include UnityEvent onInteract hook", includeEvent);
            genEditorStub = EditorGUILayout.ToggleLeft("Generate Custom Editor stub", genEditorStub);
            defaultPrompt = EditorGUILayout.TextField("Default Prompt", defaultPrompt);

            // ── Paths ─────────────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Output Folders", EditorStyles.boldLabel);
            scriptsFolder  = FolderField("Scripts Folder",   scriptsFolder);
            prefabsFolder  = FolderField("Prefabs Folder",   prefabsFolder);
            materialsFolder = FolderField("Materials Folder", materialsFolder);

            // ── Prefab ────────────────────────────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Prefab Options", EditorStyles.boldLabel);
            createPrefab = EditorGUILayout.ToggleLeft("Create matching Prefab", createPrefab);
            using (new EditorGUI.DisabledScope(!createPrefab))
            {
                prefabCollider = (ColliderType)EditorGUILayout.EnumPopup("Collider", prefabCollider);
                assignLayer    = EditorGUILayout.ToggleLeft("Assign 'Interactable' layer", assignLayer);
                createMaterial = EditorGUILayout.ToggleLeft("Create sample Material", createMaterial);
                if (createMaterial)
                    sampleColor = EditorGUILayout.ColorField("Sample Color", sampleColor);
            }

            // ── Validation ────────────────────────────────────────────────────────
            EditorGUILayout.Space(10);
            bool valid = IsInputValid(out string reason);
            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button("Create", GUILayout.Height(36)))
                    CreateAssets();
            }
            if (!valid)
                EditorGUILayout.HelpBox(reason, MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Creates a script template tailored to the preset, plus an optional Prefab " +
                "with collider and sample Material. The 'Interactable' layer is created " +
                "automatically if it doesn't exist.",
                MessageType.Info);
        }

        // ── Asset creation ────────────────────────────────────────────────────────

        private void CreateAssets()
        {
            EnsureFolder(scriptsFolder);
            if (createPrefab)
            {
                EnsureFolder(prefabsFolder);
                if (createMaterial) EnsureFolder(materialsFolder);
            }

            string scriptPath = $"{scriptsFolder}/{className}.cs";
            if (File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog(WindowTitle,
                    $"A script named '{className}.cs' already exists at:\n{scriptPath}", "OK");
                return;
            }

            File.WriteAllText(scriptPath, GenerateCode(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(scriptPath);

            if (genEditorStub)
            {
                string editorPath = $"{scriptsFolder}/{className}_Editor.cs";
                File.WriteAllText(editorPath, GenerateEditorStub(), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(editorPath);
            }

            string prefabPath = null;
            if (createPrefab)
                prefabPath = CreatePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

            string msg = $"Script created:\n{scriptPath}";
            if (!string.IsNullOrEmpty(prefabPath))
                msg += $"\n\nPrefab created:\n{prefabPath}";
            EditorUtility.DisplayDialog(WindowTitle, msg, "Done");
        }

        // ── Code generation ───────────────────────────────────────────────────────

        private string GenerateCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            if (preset == Preset.SceneLoader)
                sb.AppendLine("using UnityEngine.SceneManagement;");
            if (includeEvent)
                sb.AppendLine("using UnityEngine.Events;");
            if (inheritBase)
            {
                sb.AppendLine("using Snog.InteractionSystem.Runtime.Helpers;");
                sb.AppendLine("using Snog.InteractionSystem.Runtime.Interfaces;");
            }
            sb.AppendLine();

            bool hasNs = !string.IsNullOrWhiteSpace(nameSpace);
            if (hasNs) { sb.AppendLine($"namespace {nameSpace}"); sb.AppendLine("{"); }

            string indent = hasNs ? "    " : "";
            sb.AppendLine($"{indent}[AddComponentMenu(\"Interaction/{className}\")]");
            sb.AppendLine($"{indent}public class {className} : {(inheritBase ? "BaseInteractable" : "MonoBehaviour")}");
            sb.AppendLine($"{indent}{{");

            // Preset fields
            AppendPresetFields(sb, indent + "    ");

            // UnityEvent hook (only if not inheriting BaseInteractable, which already has one)
            if (includeEvent && !inheritBase)
            {
                sb.AppendLine($"{indent}    [Header(\"Events\")]");
                sb.AppendLine($"{indent}    [SerializeField] private UnityEvent onInteract;");
                sb.AppendLine();
            }

            // Awake
            AppendAwake(sb, indent + "    ");

            // Update (Door only)
            if (preset == Preset.Door)
                AppendDoorUpdate(sb, indent + "    ");

            // Interact method
            AppendInteractMethod(sb, indent + "    ");

            // Prompt override
            if (inheritBase)
                AppendPromptOverride(sb, indent + "    ");

            sb.AppendLine($"{indent}}}");
            if (hasNs) sb.AppendLine("}");
            return sb.ToString();
        }

        private void AppendPresetFields(StringBuilder sb, string ind)
        {
            switch (preset)
            {
                case Preset.Door:
                    sb.AppendLine($"{ind}[Header(\"Door\")]");
                    sb.AppendLine($"{ind}[SerializeField] private Transform doorPivot;");
                    sb.AppendLine($"{ind}[SerializeField] private float openAngle = 90f;");
                    sb.AppendLine($"{ind}[SerializeField] private float speed = 6f;");
                    sb.AppendLine($"{ind}private bool isOpen;");
                    sb.AppendLine($"{ind}private Quaternion closedRot;");
                    sb.AppendLine($"{ind}private Quaternion openRot;");
                    break;
                case Preset.Pickup:
                    sb.AppendLine($"{ind}[Header(\"Pickup\")]");
                    sb.AppendLine($"{ind}[SerializeField] private string itemId = \"Item\";");
                    sb.AppendLine($"{ind}[SerializeField] private int amount = 1;");
                    break;
                case Preset.Lever:
                    sb.AppendLine($"{ind}[Header(\"Lever\")]");
                    sb.AppendLine($"{ind}[SerializeField] private Animator animator;");
                    sb.AppendLine($"{ind}[SerializeField] private string triggerName = \"Pull\";");
                    sb.AppendLine($"{ind}private bool isOn;");
                    break;
                case Preset.Readable:
                    sb.AppendLine($"{ind}[Header(\"Readable\")]");
                    sb.AppendLine($"{ind}[TextArea] [SerializeField] private string text = \"Lorem ipsum...\";");
                    break;
                case Preset.Button:
                    sb.AppendLine($"{ind}[Header(\"Button\")]");
                    sb.AppendLine($"{ind}[SerializeField] private AudioSource clickSfx;");
                    sb.AppendLine($"{ind}private bool pressed;");
                    break;
                case Preset.SceneLoader:
                    sb.AppendLine($"{ind}[Header(\"Scene Loader\")]");
                    sb.AppendLine($"{ind}[SerializeField] private string sceneName = \"SampleScene\";");
                    sb.AppendLine($"{ind}[SerializeField] private bool loadAdditively = false;");
                    break;
                case Preset.SavePoint:
                    sb.AppendLine($"{ind}[Header(\"Save Point\")]");
                    sb.AppendLine($"{ind}[SerializeField] private string saveId = \"save_point_01\";");
                    break;
                default:
                    sb.AppendLine($"{ind}[Header(\"Custom\")]");
                    sb.AppendLine($"{ind}[SerializeField] private bool toggleState;");
                    break;
            }
            sb.AppendLine();
        }

        private void AppendAwake(StringBuilder sb, string ind)
        {
            // When inheriting BaseInteractable, override Awake and call base.Awake()
            // so cooldown/uses state is properly initialised.
            string modifier = inheritBase ? "protected override" : "private";
            sb.AppendLine($"{ind}{modifier} void Awake()");
            sb.AppendLine($"{ind}{{");
            if (inheritBase)
                sb.AppendLine($"{ind}    base.Awake(); // initialises cooldown and use-count state");
            switch (preset)
            {
                case Preset.Door:
                    sb.AppendLine($"{ind}    if (doorPivot == null) doorPivot = transform;");
                    sb.AppendLine($"{ind}    closedRot = doorPivot.localRotation;");
                    sb.AppendLine($"{ind}    openRot   = Quaternion.Euler(doorPivot.localEulerAngles + new Vector3(0f, openAngle, 0f));");
                    break;
                case Preset.Lever:
                    sb.AppendLine($"{ind}    if (animator == null) animator = GetComponent<Animator>();");
                    break;
                default:
                    sb.AppendLine($"{ind}    // Initialise here if needed.");
                    break;
            }
            sb.AppendLine($"{ind}}}");
            sb.AppendLine();
        }

        private void AppendDoorUpdate(StringBuilder sb, string ind)
        {
            sb.AppendLine($"{ind}private void Update()");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    if (doorPivot == null) return;");
            sb.AppendLine($"{ind}    Quaternion target = isOpen ? openRot : closedRot;");
            sb.AppendLine($"{ind}    doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, Time.deltaTime * speed);");
            sb.AppendLine($"{ind}}}");
            sb.AppendLine();
        }

        private void AppendInteractMethod(StringBuilder sb, string ind)
        {
            if (inheritBase)
            {
                // IMPORTANT: No onInteract.Invoke() here — BaseInteractable.Interact() fires it automatically.
                sb.AppendLine($"{ind}/// <summary>");
                sb.AppendLine($"{ind}/// Called by the base class after CanInteract() passes.");
                sb.AppendLine($"{ind}/// The base class fires onInteract automatically — do NOT call it here.");
                sb.AppendLine($"{ind}/// </summary>");
                sb.AppendLine($"{ind}protected override void PerformInteraction(GameObject interactor)");
                sb.AppendLine($"{ind}{{");
                AppendInteractBody(sb, ind + "    ");
                // NO onInteract?.Invoke() — the base class handles it
                sb.AppendLine($"{ind}}}");
            }
            else
            {
                sb.AppendLine($"{ind}public void Interact(GameObject interactor)");
                sb.AppendLine($"{ind}{{");
                sb.AppendLine($"{ind}    Debug.Log($\"[{className}] Interacted by {{interactor.name}}\");");
                if (includeEvent)
                    sb.AppendLine($"{ind}    onInteract?.Invoke();");
                sb.AppendLine($"{ind}}}");
            }
            sb.AppendLine();
        }

        private void AppendInteractBody(StringBuilder sb, string ind)
        {
            switch (preset)
            {
                case Preset.Door:
                    sb.AppendLine($"{ind}isOpen = !isOpen;");
                    break;
                case Preset.Pickup:
                    sb.AppendLine($"{ind}Debug.Log($\"Picked up {{amount}} x {{itemId}}\");");
                    sb.AppendLine($"{ind}Destroy(gameObject);");
                    break;
                case Preset.Lever:
                    sb.AppendLine($"{ind}isOn = !isOn;");
                    sb.AppendLine($"{ind}if (animator != null && !string.IsNullOrEmpty(triggerName))");
                    sb.AppendLine($"{ind}    animator.SetTrigger(triggerName);");
                    break;
                case Preset.Readable:
                    sb.AppendLine($"{ind}Debug.Log($\"Read: {{text}}\");");
                    break;
                case Preset.Button:
                    sb.AppendLine($"{ind}if (pressed) return; // one-shot");
                    sb.AppendLine($"{ind}pressed = true;");
                    sb.AppendLine($"{ind}if (clickSfx != null) clickSfx.Play();");
                    break;
                case Preset.SceneLoader:
                    sb.AppendLine($"{ind}if (!string.IsNullOrEmpty(sceneName))");
                    sb.AppendLine($"{ind}{{");
                    sb.AppendLine($"{ind}    var mode = loadAdditively ? UnityEngine.SceneManagement.LoadSceneMode.Additive");
                    sb.AppendLine($"{ind}                              : UnityEngine.SceneManagement.LoadSceneMode.Single;");
                    sb.AppendLine($"{ind}    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);");
                    sb.AppendLine($"{ind}}}");
                    break;
                case Preset.SavePoint:
                    sb.AppendLine($"{ind}Debug.Log($\"Save requested at {{saveId}}\");");
                    break;
                default:
                    sb.AppendLine($"{ind}toggleState = !toggleState;");
                    sb.AppendLine($"{ind}Debug.Log($\"[{className}] State: {{toggleState}}\");");
                    break;
            }
        }

        private void AppendPromptOverride(StringBuilder sb, string ind)
        {
            sb.AppendLine($"{ind}public override string GetInteractionPrompt()");
            sb.AppendLine($"{ind}{{");
            switch (preset)
            {
                case Preset.Door:
                    sb.AppendLine($"{ind}    return isOpen ? \"Close\" : \"Open\";");
                    break;
                case Preset.Pickup:
                    sb.AppendLine($"{ind}    return $\"Pick up {{itemId}}\";");
                    break;
                case Preset.Lever:
                    sb.AppendLine($"{ind}    return isOn ? \"Turn Off\" : \"Turn On\";");
                    break;
                case Preset.Readable:
                    sb.AppendLine($"{ind}    return \"Read\";");
                    break;
                case Preset.Button:
                    sb.AppendLine($"{ind}    return pressed ? \"(Used)\" : \"Press\";");
                    break;
                case Preset.SceneLoader:
                    sb.AppendLine($"{ind}    return \"Enter\";");
                    break;
                case Preset.SavePoint:
                    sb.AppendLine($"{ind}    return \"Save\";");
                    break;
                default:
                    sb.AppendLine($"{ind}    return \"{Escape(defaultPrompt)}\";");
                    break;
            }
            sb.AppendLine($"{ind}}}");
            sb.AppendLine();
        }

        private string GenerateEditorStub()
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEditor;");
            sb.AppendLine();
            bool hasNs = !string.IsNullOrWhiteSpace(nameSpace);
            if (hasNs) { sb.AppendLine($"namespace {nameSpace}"); sb.AppendLine("{"); }
            string ind = hasNs ? "    " : "";
            sb.AppendLine($"{ind}[CustomEditor(typeof({className}))]");
            sb.AppendLine($"{ind}public class {className}_Editor : Editor");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    public override void OnInspectorGUI()");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        DrawDefaultInspector();");
            sb.AppendLine($"{ind}        // Add custom inspector controls for {className} here.");
            sb.AppendLine($"{ind}    }}");
            sb.AppendLine($"{ind}}}");
            if (hasNs) sb.AppendLine("}");
            return sb.ToString();
        }

        // ── Prefab creation ───────────────────────────────────────────────────────

        private string CreatePrefab()
        {
            var go = new GameObject(className);
            AddCollider(go, prefabCollider);

            if (assignLayer)
            {
                int idx = EnsureLayer("Interactable");
                if (idx >= 0) go.layer = idx;
            }

            if (preset == Preset.Door)
            {
                var pivot = new GameObject("Pivot");
                pivot.transform.SetParent(go.transform, false);
            }

            // Optionally attach the script if it's already compiled
            var scriptType = FindTypeByName(
                string.IsNullOrWhiteSpace(nameSpace) ? className : $"{nameSpace}.{className}");
            if (scriptType != null)
                go.AddComponent(scriptType);

            if (createMaterial)
            {
                string matPath = AssetDatabase.GenerateUniqueAssetPath($"{materialsFolder}/{className}_Sample.mat");
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = sampleColor;
                AssetDatabase.CreateAsset(mat, matPath);

                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;
            }

            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{prefabsFolder}/{className}.prefab");
            PrefabUtility.SaveAsPrefabAsset(go, outPath);
            DestroyImmediate(go);
            return outPath;
        }

        // ── Utility ───────────────────────────────────────────────────────────────

        private string FolderField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("Browse…", GUILayout.Width(80)))
            {
                string abs = EditorUtility.OpenFolderPanel("Select folder (inside Assets)", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    string dataPath = Path.GetFullPath(Application.dataPath);
                    if (abs.StartsWith(dataPath))
                        value = "Assets" + abs.Substring(dataPath.Length).Replace("\\", "/");
                    else
                        EditorUtility.DisplayDialog(WindowTitle, "Please select a folder inside the Assets directory.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private bool IsInputValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(className))       { reason = "Class name cannot be empty.";                   return false; }
            if (!IsValidIdentifier(className))               { reason = "Class name must be a valid C# identifier.";     return false; }
            if (!scriptsFolder.StartsWith("Assets"))         { reason = "Scripts folder must be inside 'Assets'.";       return false; }
            if (createPrefab && !prefabsFolder.StartsWith("Assets")) { reason = "Prefabs folder must be inside 'Assets'."; return false; }
            reason = "OK";
            return true;
        }

        private static bool IsValidIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (!(char.IsLetter(s[0]) || s[0] == '_')) return false;
            for (int i = 1; i < s.Length; i++)
                if (!(char.IsLetterOrDigit(s[i]) || s[i] == '_')) return false;
            return true;
        }

        private void AddCollider(GameObject go, ColliderType type)
        {
            if (type == ColliderType.None) return;
            if (go.GetComponent<Collider>() != null) return;

            switch (type)
            {
                case ColliderType.Box:     go.AddComponent<BoxCollider>();     break;
                case ColliderType.Sphere:  go.AddComponent<SphereCollider>();  break;
                case ColliderType.Capsule: go.AddComponent<CapsuleCollider>(); break;
                case ColliderType.MeshConvex:
                    var mc   = go.AddComponent<MeshCollider>();
                    mc.convex = true;
                    break;
            }
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            for (int i = 8; i < 32; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (sp != null && string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[Scaffolder] Created layer '{layerName}' at index {i}.");
                    return i;
                }
            }

            Debug.LogWarning($"[Scaffolder] Could not create layer '{layerName}' — all user layer slots are full.");
            return -1;
        }

        private static string GetSuggestedClassName(Preset p)
        {
            switch (p)
            {
                case Preset.Door:        return "DoorInteractable";
                case Preset.Pickup:      return "PickupInteractable";
                case Preset.Lever:       return "LeverInteractable";
                case Preset.Readable:    return "ReadableInteractable";
                case Preset.Button:      return "ButtonInteractable";
                case Preset.SceneLoader: return "SceneLoaderInteractable";
                case Preset.SavePoint:   return "SavePointInteractable";
                default:                 return "NewInteractable";
            }
        }

        private static string GetSuggestedPrompt(Preset p)
        {
            switch (p)
            {
                case Preset.Door:        return "Open";
                case Preset.Pickup:      return "Pick up";
                case Preset.Lever:       return "Use";
                case Preset.Readable:    return "Read";
                case Preset.Button:      return "Press";
                case Preset.SceneLoader: return "Enter";
                case Preset.SavePoint:   return "Save";
                default:                 return "Interact";
            }
        }

        private static ColliderType GetSuggestedCollider(Preset p)
        {
            return p == Preset.Pickup ? ColliderType.Sphere : ColliderType.Box;
        }

        private static Color GetSuggestedColor(Preset p)
        {
            switch (p)
            {
                case Preset.Pickup:      return new Color(0.9f,  0.85f, 0.3f);
                case Preset.Door:        return new Color(0.5f,  0.3f,  0.15f);
                case Preset.Lever:       return new Color(0.5f,  0.5f,  0.5f);
                case Preset.Readable:    return new Color(0.8f,  0.8f,  0.9f);
                case Preset.Button:      return new Color(0.7f,  0.2f,  0.2f);
                case Preset.SceneLoader: return new Color(0.2f,  0.5f,  0.7f);
                case Preset.SavePoint:   return new Color(0.2f,  0.7f,  0.2f);
                default:                 return new Color(0.75f, 0.7f,  0.6f);
            }
        }

        private static string Escape(string s) =>
            s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        private static void EnsureFolder(string projectPath)
        {
            if (AssetDatabase.IsValidFolder(projectPath)) return;
            string[] parts = projectPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new Exception("Path must start with 'Assets'.");
            string acc = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{acc}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }

        private static Type FindTypeByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var t = Type.GetType(name);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(name); if (t != null) return t; } catch { }
            }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                catch { continue; }
                if (types == null) continue;
                foreach (var tt in types)
                    if (tt != null && tt.Name == name) return tt;
            }
            return null;
        }
    }
}
