using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Snog.InteractionSystem.Editor.Windows
{
    public class InteractionSetupWizard : EditorWindow
    {
        private const string WindowTitle = "Interaction Setup Wizard";

        // Default folders
        private string rootFolder = "Assets/Snog/InteractionSystem";
        private string runtimeFolder = "Assets/Snog/InteractionSystem/Runtime";
        private string editorFolder = "Assets/Snog/InteractionSystem/Editor";
        private string resourcesFolder = "Assets/Snog/InteractionSystem/Resources";
        private string prefabsFolder = "Assets/Snog/InteractionSystem/Prefabs";
        private string scenesFolder = "Assets/Snog/InteractionSystem/Samples/Demo";

        // Asset names
        private string settingsAssetName = "InteractionSettings.asset";
        private string quickstartPrefabName = "Interaction_Starter.prefab";
        private string inputActionsAssetName = "InteractionInput.inputactions";
        private string inputActionReferenceName = "Interact_InputActionReference.asset";

        // Options
        private bool preferTMP = true;
        private bool useNewInputSystemIfAvailable = true;
        private bool createOrUpdateSettings = true;
        private bool createQuickStartPrefab = true;
        private bool createDemoScene = true;
        private bool showAdvanced = false;

        // Detected capabilities
        private bool tmpAvailable;
        private bool inputSystemAvailable;

        // Cached assets
        private InteractionSettings cachedSettings;

        [MenuItem("Tools/Interaction/Setup Wizard")]
        public static void Open()
        {
            var w = GetWindow<InteractionSetupWizard>(true, WindowTitle, true);
            w.minSize = new Vector2(600, 520);
            w.Show();
        }

        private void OnEnable()
        {
            DetectPackages();
            LoadOrFindSettings();
        }

        private void DetectPackages()
        {
            // TMP presence via reflection
            tmpAvailable = GetTypeByName("TMPro.TextMeshProUGUI") != null;

            // New Input System presence via reflection (type check)
            inputSystemAvailable = GetTypeByName("UnityEngine.InputSystem.InputAction") != null;

            // Default: prefer TMP if available
            preferTMP = tmpAvailable;
        }

        private static Type GetTypeByName(string qualifiedName)
        {
            // Try fast path if assembly qualified
            var type = Type.GetType(qualifiedName);
            if (type != null)
                return type;

            // Search all assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = asm.GetType(qualifiedName);
                    if (type != null)
                        return type;
                }
                catch { /* ignore reflection issues */ }
            }
            return null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interaction Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This will create a ready-to-use Interaction Settings asset, a QuickStart prefab with a PlayerInteractor + Prompt UI, " +
                "and an optional Demo Scene with an example interactable. You can press Play right away.",
                MessageType.Info
            );

            DrawCapabilityRow();

            EditorGUILayout.Space();
            DrawFolders();

            EditorGUILayout.Space();
            DrawOptions();

            EditorGUILayout.Space();
            DrawValidation();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(false))
            {
                if (GUILayout.Button("Create / Update Assets", GUILayout.Height(40)))
                {
                    CreateOrUpdate();
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Demo Scene (if created)"))
            {
                var path = $"{scenesFolder}/Interaction_Demo.unity";
                if (File.Exists(path))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(WindowTitle, "Demo scene not found. Run the wizard to create it.", "OK");
                }
            }

            EditorGUILayout.Space();
            DrawTips();
        }

        private void DrawCapabilityRow()
        {
            EditorGUILayout.BeginHorizontal();
            StatusLabel("TextMeshPro", tmpAvailable ? "Detected" : "Not found", tmpAvailable);
            GUILayout.Space(12);
            StatusLabel("New Input System", inputSystemAvailable ? "Detected" : "Not found", inputSystemAvailable);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Let user override TMP preference only if available
            using (new EditorGUI.DisabledScope(!tmpAvailable))
            {
                preferTMP = EditorGUILayout.ToggleLeft(new GUIContent("Use TextMeshPro for Prompt UI (fallback to UGUI if not available)"), preferTMP);
            }

            // New Input System preference (only if available)
            using (new EditorGUI.DisabledScope(!inputSystemAvailable))
            {
                useNewInputSystemIfAvailable = EditorGUILayout.ToggleLeft(
                    new GUIContent("Use New Input System if available (fallback to Legacy Input)"),
                    useNewInputSystemIfAvailable
                );
            }
        }

        private void DrawFolders()
        {
            EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);

            rootFolder = FolderField("Root Folder", rootFolder);
            runtimeFolder = FolderField("Runtime Scripts", runtimeFolder);
            editorFolder = FolderField("Editor Scripts", editorFolder);
            resourcesFolder = FolderField("Resources", resourcesFolder);
            prefabsFolder = FolderField("Prefabs", prefabsFolder);
            scenesFolder = FolderField("Demo Scenes", scenesFolder);
        }

        private void DrawOptions()
        {
            EditorGUILayout.LabelField("Assets to Create", EditorStyles.boldLabel);
            createOrUpdateSettings = EditorGUILayout.ToggleLeft("Create/Update InteractionSettings.asset", createOrUpdateSettings);
            createQuickStartPrefab = EditorGUILayout.ToggleLeft("Create QuickStart Prefab (Interaction_Starter)", createQuickStartPrefab);
            createDemoScene = EditorGUILayout.ToggleLeft("Create Demo Scene (Interaction_Demo.unity)", createDemoScene);

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                settingsAssetName = EditorGUILayout.TextField("Settings Asset Name", settingsAssetName);
                quickstartPrefabName = EditorGUILayout.TextField("QuickStart Prefab Name", quickstartPrefabName);
                inputActionsAssetName = EditorGUILayout.TextField("Input Actions Asset Name", inputActionsAssetName);
                inputActionReferenceName = EditorGUILayout.TextField("InputActionReference Name", inputActionReferenceName);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            bool playerInteractorExists = GetTypeByName("PlayerInteractor") != null;
            bool baseInteractableExists = GetTypeByName("BaseInteractable") != null;
            bool promptUIExistsUGUI = GetTypeByName("InteractionPromptUI") != null;
            bool promptUITMPExists = GetTypeByName("InteractionPromptUI_TMP") != null || promptUIExistsUGUI; // we can use UGUI variant

            RowCheck("PlayerInteractor script", playerInteractorExists);
            RowCheck("BaseInteractable script", baseInteractableExists);
            RowCheck("Prompt UI script", promptUIExistsUGUI || promptUITMPExists);

            if (!(playerInteractorExists && baseInteractableExists && (promptUIExistsUGUI || promptUITMPExists)))
            {
                EditorGUILayout.HelpBox(
                    "Missing one or more required scripts. Please make sure you imported the Interaction System runtime scripts first.",
                    MessageType.Warning
                );
            }
        }

        private void DrawTips()
        {
            EditorGUILayout.HelpBox(
                "Tip: The QuickStart prefab works standalone. Drop it into any scene and press Play. " +
                "The Demo Scene includes a floor, a light, and an example interactable for instant testing.",
                MessageType.None
            );
        }

        private void StatusLabel(string label, string state, bool ok)
        {
            var color = ok ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.8f, 0.3f, 0.2f);
            var prev = GUI.color;
            GUI.color = color;
            GUILayout.Label($"●", GUILayout.Width(16));
            GUI.color = prev;

            GUILayout.Label($"{label}: {state}");
        }

        private void RowCheck(string label, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            StatusLabel(label, ok ? "OK" : "Missing", ok);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
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

        private void CreateOrUpdate()
        {
            try
            {
                EnsureFolder(rootFolder);
                EnsureFolder(runtimeFolder);
                EnsureFolder(editorFolder);
                EnsureFolder(resourcesFolder);
                EnsureFolder(prefabsFolder);
                EnsureFolder(scenesFolder);

                // Ensure Interactable layer exists and set mask in settings
                int interactableLayer = EnsureLayer("Interactable");

                // Settings
                if (createOrUpdateSettings)
                {
                    cachedSettings = CreateOrFindSettings(resourcesFolder, settingsAssetName);
                    // Apply sane defaults
                    cachedSettings.interactDistance = 3.5f;
                    cachedSettings.sphereRadius = 0.06f;
                    cachedSettings.interactKey = KeyCode.E;
                    cachedSettings.holdToInteract = false;
                    cachedSettings.holdDuration = 0.5f;
                    cachedSettings.pressPromptFormat = "Press {0} to {1}";
                    cachedSettings.holdPromptFormat = "Hold {0} to {1}";

                    // Assign mask to include "Interactable" layer (and Default)
                    var mask = (1 << 0) | (1 << interactableLayer);
                    cachedSettings.interactableMask = mask;

                    EditorUtility.SetDirty(cachedSettings);
                }

                // QuickStart Prefab
                string quickstartPath = null;
                if (createQuickStartPrefab)
                {
                    quickstartPath = BuildQuickStartPrefab(prefabsFolder, quickstartPrefabName, preferTMP, cachedSettings, useNewInputSystemIfAvailable && inputSystemAvailable);
                }

                // Input System assets (optional)
                if (useNewInputSystemIfAvailable && inputSystemAvailable)
                {
                    CreateInputAssetsIfMissing(resourcesFolder, inputActionsAssetName, inputActionReferenceName, quickstartPath);
                }

                // Demo Scene
                if (createDemoScene)
                {
                    CreateDemoScene(scenesFolder, quickstartPath, interactableLayer);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(WindowTitle, "Setup completed! You can now drag 'Interaction_Starter' into your scene or open the Demo Scene and press Play.", "Nice");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InteractionSetupWizard] Error: {ex}");
                EditorUtility.DisplayDialog(WindowTitle, $"Error during setup:\n{ex.Message}", "OK");
            }
        }

        // ---------- Helpers ----------

        private void EnsureFolder(string projectPath)
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

        private InteractionSettings CreateOrFindSettings(string folder, string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(InteractionSettings)}");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var found = AssetDatabase.LoadAssetAtPath<InteractionSettings>(path);
                if (found != null)
                    return found;
            }

            var asset = ScriptableObject.CreateInstance<InteractionSettings>();
            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{assetName}");
            AssetDatabase.CreateAsset(asset, outPath);
            return asset;
        }

        private void LoadOrFindSettings()
        {
            cachedSettings = null;
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(InteractionSettings)}");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                cachedSettings = AssetDatabase.LoadAssetAtPath<InteractionSettings>(path);
            }
        }

        private string BuildQuickStartPrefab(string folder, string prefabName, bool useTMP, InteractionSettings settings, bool hookNewInput)
        {
            // Create GO hierarchy
            var root = new GameObject("Interaction_Starter");
            var cameraGO = new GameObject("PlayerCamera");
            cameraGO.transform.SetParent(root.transform, worldPositionStays: false);
            cameraGO.transform.localPosition = Vector3.zero;
            cameraGO.transform.localRotation = Quaternion.identity;

            var cam = cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();

            // Canvas
            var canvasGO = new GameObject("HUD_Canvas");
            canvasGO.transform.SetParent(root.transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem (for UI navigation)
            var es = new GameObject("EventSystem");
            es.transform.SetParent(root.transform, false);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Prompt container
            var promptGO = new GameObject("InteractionPromptUI");
            promptGO.transform.SetParent(canvasGO.transform, false);
            var cg = promptGO.AddComponent<CanvasGroup>();

            // Background ring
            var holdRing = new GameObject("HoldRing");
            holdRing.transform.SetParent(promptGO.transform, false);
            var ringImg = holdRing.AddComponent<Image>();
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillOrigin = (int)Image.Origin360.Top;
            ringImg.fillClockwise = false;
            ringImg.color = new Color(1f, 1f, 1f, 0.75f);
            ringImg.fillAmount = 0f;
            var ringRect = ringImg.rectTransform;
            ringRect.sizeDelta = new Vector2(48, 48);
            ringRect.anchoredPosition = new Vector2(0f, -60f);
            ringRect.anchorMin = new Vector2(0.5f, 0f);
            ringRect.anchorMax = new Vector2(0.5f, 0f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);

            // Prompt text (TMP or UGUI)
            Component promptTextComp = null;
            if (useTMP && tmpAvailable)
            {
                // Create TMP text via reflection to avoid hard dependency at compile time
                var tmpType = GetTypeByName("TMPro.TextMeshProUGUI");
                var tmpGO = new GameObject("Text_TMP");
                tmpGO.transform.SetParent(promptGO.transform, false);
                promptTextComp = tmpGO.AddComponent(tmpType);

                // Set defaults via reflection
                var rect = (tmpGO.transform as RectTransform);
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -60f);
                rect.sizeDelta = new Vector2(600f, 60f);

                tmpType.GetProperty("alignment")?.SetValue(promptTextComp, Enum.Parse(GetTypeByName("TMPro.TextAlignmentOptions"), "Center"));
                tmpType.GetProperty("fontSize")?.SetValue(promptTextComp, 24f);
                tmpType.GetProperty("text")?.SetValue(promptTextComp, "");
                (promptTextComp as Behaviour).enabled = true;
            }
            else
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(promptGO.transform, false);
                var text = textGO.AddComponent<Text>();
                var rect = text.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -60f);
                rect.sizeDelta = new Vector2(600f, 60f);
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 24;
                text.text = "";
                text.color = Color.white;
                promptTextComp = text;
            }

            // Attach the Prompt UI component (UGUI version by default)
            Component promptUIComp = null;
            Type promptUIType = GetTypeByName("InteractionPromptUI");
            if (promptUIType == null && tmpAvailable)
            {
                // If user only has TMP prompt script, try that
                promptUIType = GetTypeByName("InteractionPromptUI_TMP");
            }
            if (promptUIType == null)
            {
                Debug.LogWarning("[Setup Wizard] Couldn't find InteractionPromptUI script. Leaving the object for user to assign later.");
            }
            else
            {
                promptUIComp = promptGO.AddComponent(promptUIType);

                // Assign fields via reflection: promptText, holdFillImage, fadeSpeed
                var textField = promptUIType.GetField("promptText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (textField != null)
                {
                    textField.SetValue(promptUIComp, promptTextComp);
                }
                var holdField = promptUIType.GetField("holdFillImage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (holdField != null)
                {
                    holdField.SetValue(promptUIComp, ringImg);
                }
            }

            // Crosshair
            var crosshairGO = new GameObject("Crosshair");
            crosshairGO.transform.SetParent(canvasGO.transform, false);

            var crossImg = crosshairGO.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.75f);

            // Optional: ensure it actually renders by default
            crossImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            crossImg.type = Image.Type.Simple;

            var cr = crossImg.rectTransform;
            cr.anchorMin = new Vector2(0.5f, 0.5f);
            cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = new Vector2(8f, 8f);

            // Wire crosshair into InteractionPromptUI (reflection)
            if (promptUIType != null && promptUIComp != null)
            {
                var crosshairField = promptUIType.GetField(
                    "crosshairImage",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                if (crosshairField != null)
                {
                    crosshairField.SetValue(promptUIComp, crossImg);
                }
            }

            // PlayerInteractor
            var interactorType = GetTypeByName("PlayerInteractor");
            if (interactorType == null)
            {
                Debug.LogError("[Setup Wizard] PlayerInteractor type not found. Make sure runtime scripts are imported.");
            }
            else
            {
                var interactor = root.AddComponent(interactorType);

                // Assign camera
                var camField = interactorType.GetField("playerCamera", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                camField?.SetValue(interactor, cam);

                // Assign prompt UI
                var promptField = interactorType.GetField("promptUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                promptField?.SetValue(interactor, promptUIComp);

                // Assign settings
                var settingsField = interactorType.GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                settingsField?.SetValue(interactor, settings);

                // If using New Input System, we'll assign InputActionReference later (after we create it)
            }

            // Save prefab
            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{prefabName}");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            GameObject.DestroyImmediate(root);

            return outPath;
        }

        private void CreateInputAssetsIfMissing(string folder, string inputAssetName, string referenceName, string quickstartPrefabPath)
        {
    #if ENABLE_INPUT_SYSTEM
            // Create InputActionAsset if missing
            string assetSearch = $"t:UnityEngine.InputSystem.InputActionAsset {Path.GetFileNameWithoutExtension(inputAssetName)}";
            string[] guids = AssetDatabase.FindAssets(assetSearch);
            UnityEngine.InputSystem.InputActionAsset inputAsset = null;

            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
            }
            else
            {
                inputAsset = ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputActionAsset>();
                var map = inputAsset.AddActionMap("Gameplay");
                var interact = map.AddAction("Interact", UnityEngine.InputSystem.InputActionType.Button);
                interact.AddBinding("<Keyboard>/e");
                interact.AddBinding("<Gamepad>/buttonSouth");

                string outPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{inputAssetName}");
                AssetDatabase.CreateAsset(inputAsset, outPath);
            }

            // Create InputActionReference for Interact
            var gameplay = inputAsset.actionMaps.FirstOrDefault(m => m.name == "Gameplay");
            var interactAction = gameplay?.FindAction("Interact");
            if (interactAction == null)
            {
                Debug.LogWarning("[Setup Wizard] Could not find or create 'Gameplay/Interact' action.");
                return;
            }

            UnityEngine.InputSystem.InputActionReference iaRef = null;
            string refSearch = $"t:UnityEngine.InputSystem.InputActionReference {Path.GetFileNameWithoutExtension(referenceName)}";
            var refGuids = AssetDatabase.FindAssets(refSearch);
            if (refGuids != null && refGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(refGuids[0]);
                iaRef = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionReference>(path);
            }
            else
            {
                iaRef = UnityEngine.InputSystem.InputActionReference.Create(interactAction);
                string refPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{referenceName}");
                AssetDatabase.CreateAsset(iaRef, refPath);
            }

            // Assign to QuickStart prefab's PlayerInteractor
            if (!string.IsNullOrEmpty(quickstartPrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(quickstartPrefabPath);
                if (prefab != null)
                {
                    var interactorType = GetTypeByName("PlayerInteractor");
                    var interactor = prefab.GetComponent(interactorType);
                    if (interactor != null)
                    {
                        var actionField = interactorType.GetField("interactAction", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (actionField != null)
                        {
                            actionField.SetValue(interactor, iaRef);
                            EditorUtility.SetDirty(prefab);
                            PrefabUtility.SavePrefabAsset(prefab);
                        }
                    }
                }
            }
    #else
            Debug.Log("[Setup Wizard] New Input System not enabled for compilation (ENABLE_INPUT_SYSTEM undefined). Skipping Input Actions setup.");
    #endif
        }

        private void CreateDemoScene(string folder, string quickstartPrefabPath, int interactableLayer)
        {
            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3, 1, 3);

            // Starter prefab
            if (!string.IsNullOrEmpty(quickstartPrefabPath))
            {
                var starterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(quickstartPrefabPath);
                if (starterPrefab != null)
                {
                    var starter = (GameObject)PrefabUtility.InstantiatePrefab(starterPrefab);
                    starter.transform.position = new Vector3(0, 1.7f, -4f);
                    starter.transform.rotation = Quaternion.identity;
                }
            }

            // A simple example interactable (Door-like)
            var example = GameObject.CreatePrimitive(PrimitiveType.Cube);
            example.name = "ExampleDoor";
            example.transform.position = new Vector3(0, 1, 2.5f);
            example.transform.localScale = new Vector3(1.2f, 2.2f, 0.2f);
            example.layer = interactableLayer;

            // Add a hinge child to rotate
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(example.transform, false);
            pivot.transform.localPosition = new Vector3(-0.5f, -1.0f, 0f);

            // Add DoorInteractable if available, else PickupInteractable, else BaseInteractable mock
            var doorType = GetTypeByName("DoorInteractable");
            var pickupType = GetTypeByName("PickupInteractable");
            var baseType = GetTypeByName("BaseInteractable");

            Component toAttach = null;
            if (doorType != null)
            {
                toAttach = example.AddComponent(doorType);
                // Try assign pivot if field exists
                var field = doorType.GetField("doorPivot", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                field?.SetValue(toAttach, pivot.transform);
            }
            else if (pickupType != null)
            {
                toAttach = example.AddComponent(pickupType);
            }
            else if (baseType != null)
            {
                toAttach = example.AddComponent(baseType);
            }

            // Save scene
            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Interaction_Demo.unity");
            EditorSceneManager.SaveScene(scene, outPath, true);
        }

        private int EnsureLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            // If already exists, return its index
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                var sp = layersProp.GetArrayElementAtIndex(i);
                if (sp != null && sp.stringValue == layerName)
                {
                    return i;
                }
            }

            // Try to add at first empty slot from 8 to 31 (user layers)
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

            Debug.LogWarning($"[Setup Wizard] Could not create layer '{layerName}'. Please add it manually.");
            return LayerMask.NameToLayer(layerName); // may be -1
        }
    }
}