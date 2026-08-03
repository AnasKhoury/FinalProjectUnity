using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.ARFoundation;

public static class MuseumAndroidProjectSetup
{
    private const string ScenePath = "Assets/Scenes/MuseumAndroidAR.unity";
    private const string ApkPath = "Builds/MuseumAR.apk";
    private const string BuildRequestPath = "Builds/build-apk.request";
    private const string ARCoreLoaderTypeName = "UnityEngine.XR.ARCore.ARCoreLoader";
    private const string AutoPrepareSessionKey = "SA_MUSEUM_ANDROID_AR_AUTO_PREPARED";
    private const string TeachableMachineAssetFolder = "Assets/ML/TeachableMachine";
    private const string TeachableMachineStreamingFolder = "Assets/StreamingAssets/TeachableMachine";
    private const string SoundFolder = "Assets/Sounds";
    private const string MuseumObjectsFolder = "Assets/Prefabs/MuseumObjects";
    private static readonly ObjectRecognitionDefinition[] ObjectDefinitions =
    {
        new("DEA GRAVIDA", "Pregnant", "Fertility goddess figurine. Recognized from the real museum object.", "DEA GRAVIDA (Fertility Goddess Figurine), Iron Age II (970-530 BCE). A figurine representing a fertility goddess. Most figurines of this type found around the Mediterranean region come from Tel Michal, near Herzliya.", "1781630285984181577zvxgw78a-voicemaker.in-speech.mp3", "DEA GRAVIDA.mp3", "Meshy_AI_Ancient_Figurine_Scul_1222165842_texture.glb"),
        new("Rock", "Rock", "Carved rock artifact. Recognized from the real museum object.", "On a stele, there is an inscription indicating a vow made by a man named Azirbal, son of Baalshama. Such stelae were set up in sacred places as public expressions of devotion, reflecting the central role of vows and the god Baal in Phoenician religious life.", "hajarfotemthlt.mp3", "אסטלה.mp3", "Meshy_AI_Ancient_Artifact_Frag_0105112753_texture.glb"),
        new("Ship", "Ship", "Museum ship model. Recognized from the real museum object.", "Phoenician ship model. The Phoenicians were the finest seafarers of the ancient world. The Greeks called their ships hippos (Greek for \"horse\"), after the horse's head that was mounted on the prow of the ship.", "sfenefenekem.mp3", "דגם ספינה פיניקית.mp3", "Meshy_AI_i_want_the_object_wit_1222163443_texture.glb"),
        new("Replica Stone Hand-Shaped Stoppers", "Hands", "Stone hand-shaped stoppers. Recognized from the real museum object.", "Replica Stone Hand-Shaped Stoppers, Persian Period (530-332 BCE). These stone objects were carved to resemble hands. They were used as stoppers for liquid containers and also served as tools for distributing and using liquids stored in larger vessels.", "Gloves.mp3", "העתק כפות.mp3", "Meshy_AI_Ancient_Artifact_Disp_0105112819_texture.glb"),
        new("Replica Mask", "Mask", "Replica mask. Recognized from the real museum object.", "Replica Mask, Iron Age (1150-530 BCE). This mask is a replica of a burial offering mask discovered during excavations at Achziv.", "mask.mp3", "העתק מסכה.mp3", "Meshy_AI_Ancient_Terracotta_Ma_0105112802_texture.glb"),
        new("Glass Vessels", "3SmallVaz", "Glass vessels. Recognized from the real museum object.", "Glass Vessels, Persian Period (530-332 BCE). The sandy coast around Acre was famous because its sand was considered an excellent raw material for glass production.", "threevessels.mp3", "כלי זכוכית.mp3", "Meshy_AI_3of_0105112743_texture.glb"),
        new("Normal Vase", "NormalVaz", "Cypro-Phoenician black-on-red vessel. Recognized from the real museum object.", "The Cypro-Phoenician style, also known as black-on-red, refers to its characteristic color scheme: black-painted bands or motifs applied to red ceramic vessels. This style reflects cultural interaction between Phoenicia and Cyprus and was widely used in the Iron Age for both everyday and ritual pottery.", "jarrahamra.mp3", "פך.mp3", "Meshy_AI_Ancient_Artifact_Exhi_1222131107_texture.glb"),
        new("Big Vase", "BigVaz", "Canaanite jar. Recognized from the real museum object.", "Canaanite jar, Persian period, 530-332 BCE. The jar bears an inscription reading \"belonging to Abd-Baal,\" indicating ownership and reflecting the use of Phoenician theophoric names during the Persian period.", "jarrakbere.mp3", "קנקן כנעני.mp3", "Meshy_AI_Ancient_Ceramic_Vesse_0105112811_texture.glb"),
        new("Replica of a Wall Relief", "EnterancePanel", "Replica wall relief. Recognized from the real museum object.", "Replica of a Wall Relief, Iron Age (5th Century BCE). This relief is based on a relief discovered in the palace of the Assyrian king Sargon. It depicts trade between Sargon and the king of Tyre, specifically the transport of cedar wood by sea. This event is also described in the Bible.", "room.mp3", "רפליקה של.mp3", "Meshy_AI_Ancient_Voyage_Carvin_0105112735_texture.glb")
    };

    [MenuItem("Museum AR/Initialize Android AR Project")]
    public static void Initialize()
    {
        ConfigureAndroidPlayer();
        ConfigureAndroidXR();
        ConfigureTeachableMachineAssets();
        CreateScene();
        ConfigureAndroidBuildAndRun();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Museum Android AR project initialized for Teachable Machine real-object recognition. Add model.tflite and labels.txt to Assets/ML/TeachableMachine, then assign four mobile prefabs.");
    }

    // Command-line entry point used to produce a directly installable test APK.
    public static void BuildAndroidApk()
    {
        ConfigureAndroidPlayer();
        ConfigureAndroidXR();
        ConfigureTeachableMachineAssets();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        string outputDirectory = Path.GetDirectoryName(ApkPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        BuildPlayerOptions options = new()
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new BuildFailedException($"Android APK build failed: {report.summary.result}");
        }

        Debug.Log($"Android APK created at {Path.GetFullPath(ApkPath)}");
    }

    // Assigns Arabic recordings that have been supplied so far without building an APK.
    public static void ConfigureArabicAudioAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MuseumRealObjectRecognitionController controller = UnityEngine.Object.FindAnyObjectByType<MuseumRealObjectRecognitionController>();
        if (controller == null)
        {
            throw new MissingReferenceException("Museum recognition controller was not found in the scene.");
        }

        SerializedObject serializedController = new(controller);
        SerializedProperty objects = serializedController.FindProperty("museumObjects");
        string[,] suppliedClips =
        {
            { "BigVaz", "BigVaz_Arabic.mp3" },
            { "Mask", "Mask_Arabic.mp3" },
            { "Ship", "Ship_Arabic.mp3" },
            { "3SmallVaz", "GlassVessels_Arabic.mp3" },
            { "Hands", "Hands_Arabic.mp3" },
            { "NormalVaz", "NormalVaz_Arabic.mp3" },
            { "Pregnant", "DEAGRAVIDA_Arabic.mp3" },
            { "Rock", "Rock_Arabic.mp3" },
            { "EnterancePanel", "EnterancePanel_Arabic.mp3" }
        };

        int assignedCount = 0;
        for (int clipIndex = 0; clipIndex < suppliedClips.GetLength(0); clipIndex++)
        {
            string recognitionLabel = suppliedClips[clipIndex, 0];
            string fileName = suppliedClips[clipIndex, 1];
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{fileName}");
            if (clip == null)
            {
                continue;
            }

            for (int i = 0; i < objects.arraySize; i++)
            {
                SerializedProperty entry = objects.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("recognitionLabel").stringValue != recognitionLabel)
                {
                    continue;
                }

                entry.FindPropertyRelative("arabicAudioClip").objectReferenceValue = clip;
                assignedCount++;
                break;
            }
        }

        if (assignedCount == 0)
        {
            throw new FileNotFoundException("No supplied Arabic MP3 files could be imported and assigned.");
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Assigned {assignedCount} supplied Arabic audio clip(s). No APK was built.");
    }

    [InitializeOnLoadMethod]
    private static void ProcessOneTimeApkBuildRequest()
    {
        if (!File.Exists(BuildRequestPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            File.Delete(BuildRequestPath);
            BuildAndroidApk();
        };
    }

    [InitializeOnLoadMethod]
    private static void AutoPrepareAndroidSettings()
    {
        if (SessionState.GetBool(AutoPrepareSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoPrepareSessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ConfigureAndroidPlayer();
            ConfigureAndroidXR();
            ConfigureTeachableMachineAssets();
            ConfigureAndroidBuildAndRun();
            AssetDatabase.SaveAssets();
            Debug.Log("SA Museum Android AR settings auto-prepared for Build And Run.");
        };
    }

    [MenuItem("Museum AR/Prepare Android Build And Run")]
    public static void PrepareAndroidBuildAndRun()
    {
        ConfigureAndroidPlayer();
        ConfigureAndroidXR();
        ConfigureTeachableMachineAssets();
        ConfigureAndroidBuildAndRun();
        AssetDatabase.SaveAssets();
        Debug.Log("Android Build And Run settings are ready. Connect an Android ARCore phone with USB debugging enabled, then use File > Build Settings > Build And Run.");
    }

    [MenuItem("Museum AR/Assign English Audio Clips")]
    public static void AssignEnglishAudioClips()
    {
        MuseumRealObjectRecognitionController controller = UnityEngine.Object.FindAnyObjectByType<MuseumRealObjectRecognitionController>();
        if (controller == null)
        {
            Debug.LogError("No MuseumRealObjectRecognitionController found in the open scene. Open Assets/Scenes/MuseumAndroidAR.unity first.");
            return;
        }

        SerializedObject serializedController = new(controller);
        SerializedProperty museumObjectsProperty = serializedController.FindProperty("museumObjects");
        for (int i = 0; i < museumObjectsProperty.arraySize; i++)
        {
            SerializedProperty entry = museumObjectsProperty.GetArrayElementAtIndex(i);
            string recognitionLabel = entry.FindPropertyRelative("recognitionLabel").stringValue;
            ObjectRecognitionDefinition definition = FindDefinition(recognitionLabel);
            if (string.IsNullOrEmpty(definition.RecognitionLabel))
            {
                continue;
            }

            AudioClip clip = string.IsNullOrEmpty(definition.AudioFileName)
                ? null
                : AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{definition.AudioFileName}");
            AudioClip hebrewClip = string.IsNullOrEmpty(definition.HebrewAudioFileName)
                ? null
                : AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{definition.HebrewAudioFileName}");
            entry.FindPropertyRelative("englishAudioClip").objectReferenceValue = clip;
            entry.FindPropertyRelative("englishExplanationText").stringValue = LoadExplanationTranscript(definition);
            entry.FindPropertyRelative("hebrewAudioClip").objectReferenceValue = hebrewClip;
            if (!string.IsNullOrEmpty(definition.AudioFileName) && clip == null)
            {
                Debug.LogWarning($"Audio clip not found for {recognitionLabel}: {SoundFolder}/{definition.AudioFileName}");
            }
            if (!string.IsNullOrEmpty(definition.HebrewAudioFileName) && hebrewClip == null)
            {
                Debug.LogWarning($"Hebrew audio clip not found for {recognitionLabel}: {SoundFolder}/{definition.HebrewAudioFileName}");
            }
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        Debug.Log("English audio clips assigned to museum object definitions.");
    }

    [MenuItem("Museum AR/Sync Scene Objects From Teachable Machine Labels")]
    public static void SyncSceneObjectsFromTeachableMachineLabels()
    {
        MuseumRealObjectRecognitionController controller = UnityEngine.Object.FindAnyObjectByType<MuseumRealObjectRecognitionController>();
        if (controller == null)
        {
            Debug.LogError("No MuseumRealObjectRecognitionController found in the open scene. Open Assets/Scenes/MuseumAndroidAR.unity first.");
            return;
        }

        string labelsAssetPath = $"{TeachableMachineAssetFolder}/labels.txt";
        if (!File.Exists(labelsAssetPath))
        {
            Debug.LogError($"Could not find labels file: {labelsAssetPath}");
            return;
        }

        string[] labels = ParseTeachableMachineLabels(File.ReadAllText(labelsAssetPath));
        if (labels.Length == 0)
        {
            Debug.LogError($"No labels found in {labelsAssetPath}");
            return;
        }

        SerializedObject serializedController = new(controller);
        SerializedProperty museumObjectsProperty = serializedController.FindProperty("museumObjects");
        Dictionary<string, SceneObjectDefinitionData> existingByLabel = new(StringComparer.Ordinal);
        for (int i = 0; i < museumObjectsProperty.arraySize; i++)
        {
            SceneObjectDefinitionData data = ReadSceneObjectDefinition(museumObjectsProperty.GetArrayElementAtIndex(i));
            if (!string.IsNullOrWhiteSpace(data.RecognitionLabel))
            {
                existingByLabel[data.RecognitionLabel] = data;
            }
        }

        museumObjectsProperty.arraySize = labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            string label = labels[i];
            ObjectRecognitionDefinition defaultDefinition = FindDefinition(label);
            SceneObjectDefinitionData data = existingByLabel.TryGetValue(label, out SceneObjectDefinitionData existing)
                ? existing
                : SceneObjectDefinitionData.FromLabel(label);

            if (!string.IsNullOrEmpty(defaultDefinition.RecognitionLabel))
            {
                data.DisplayName = defaultDefinition.DisplayName;
                data.MobileDescription = defaultDefinition.Description;
                data.EnglishExplanationText = LoadExplanationTranscript(defaultDefinition);
                data.EnglishAudioClip ??= AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{defaultDefinition.AudioFileName}");
                data.HebrewAudioClip ??= AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{defaultDefinition.HebrewAudioFileName}");
                data.Prefab ??= LoadKnownModelPrefab(defaultDefinition);
            }

            WriteSceneObjectDefinition(museumObjectsProperty.GetArrayElementAtIndex(i), data);
        }

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        Debug.Log($"Synced {labels.Length} Teachable Machine labels into the scene. Existing prefab/audio assignments were preserved when labels matched.");
    }

    private static void ConfigureAndroidPlayer()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.productName = "SA Museum AR";
        PlayerSettings.companyName = "SA Museum";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.samuseum.androidar");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.ARCoreEnabled = true;
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SplashScreen.show = false;
    }

    private static void ConfigureAndroidBuildAndRun()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static void ConfigureTeachableMachineAssets()
    {
        Directory.CreateDirectory(TeachableMachineStreamingFolder);

        CopyAssetIfExists($"{TeachableMachineAssetFolder}/model_unquant.tflite", $"{TeachableMachineStreamingFolder}/model_unquant.tflite");
        CopyAssetIfExists($"{TeachableMachineAssetFolder}/labels.txt", $"{TeachableMachineStreamingFolder}/labels.txt");
        AssetDatabase.ImportAsset(TeachableMachineStreamingFolder, ImportAssetOptions.ImportRecursive);
    }

    private static void CopyAssetIfExists(string sourcePath, string destinationPath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"Missing Teachable Machine file: {sourcePath}");
            return;
        }

        File.Copy(sourcePath, destinationPath, true);
    }

    private static void ConfigureAndroidXR()
    {
        object buildTargetSettings = GetOrCreateXRGeneralSettingsPerBuildTarget();
        if (buildTargetSettings == null)
        {
            Debug.LogError("Could not access XR Plug-in Management settings. Open Project Settings > XR Plug-in Management > Android and enable ARCore manually.");
            return;
        }

        if (!HasAndroidXRManagerSettings(buildTargetSettings))
        {
            InvokeXRSettingsMethod(buildTargetSettings, "CreateDefaultManagerSettingsForBuildTarget", BuildTargetGroup.Android);
        }

        XRManagerSettings managerSettings = GetAndroidXRManagerSettings(buildTargetSettings);
        if (managerSettings == null)
        {
            Debug.LogError("Could not create Android XR Manager settings.");
            return;
        }

        bool hasARCore = false;
        foreach (XRLoader loader in managerSettings.activeLoaders)
        {
            if (loader != null && loader.GetType().FullName == ARCoreLoaderTypeName)
            {
                hasARCore = true;
                break;
            }
        }

        if (!hasARCore)
        {
            bool assigned = XRPackageMetadataStore.AssignLoader(managerSettings, ARCoreLoaderTypeName, BuildTargetGroup.Android);
            if (!assigned)
            {
                Debug.LogError("Failed to assign ARCore loader for Android. Open Project Settings > XR Plug-in Management > Android and enable ARCore manually.");
                return;
            }
        }

        EditorUtility.SetDirty(managerSettings);
        EditorUtility.SetDirty((UnityEngine.Object)buildTargetSettings);
        Debug.Log("Android XR Plug-in Management is configured with ARCore.");
    }

    private static object GetOrCreateXRGeneralSettingsPerBuildTarget()
    {
        Type settingsType = Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
        if (settingsType == null)
        {
            return null;
        }

        MethodInfo getOrCreate = settingsType.GetMethod("GetOrCreate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return getOrCreate?.Invoke(null, null);
    }

    private static bool HasAndroidXRManagerSettings(object buildTargetSettings)
    {
        object result = InvokeXRSettingsMethod(buildTargetSettings, "HasManagerSettingsForBuildTarget", BuildTargetGroup.Android);
        return result is bool hasSettings && hasSettings;
    }

    private static XRManagerSettings GetAndroidXRManagerSettings(object buildTargetSettings)
    {
        object result = InvokeXRSettingsMethod(buildTargetSettings, "ManagerSettingsForBuildTarget", BuildTargetGroup.Android);
        return result as XRManagerSettings;
    }

    private static object InvokeXRSettingsMethod(object target, string methodName, BuildTargetGroup buildTargetGroup)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return method?.Invoke(target, new object[] { buildTargetGroup });
    }

    private static void CreateScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject sessionObject = new("AR Session");
        sessionObject.AddComponent<ARSession>();
        sessionObject.AddComponent<ARInputManager>();

        GameObject originObject = new("XR Origin");
        XROrigin origin = originObject.AddComponent<XROrigin>();
        TouchObjectManipulator manipulator = originObject.AddComponent<TouchObjectManipulator>();
        MuseumRealObjectRecognitionController recognitionController = originObject.AddComponent<MuseumRealObjectRecognitionController>();

        SerializedObject serializedRecognitionController = new(recognitionController);
        serializedRecognitionController.FindProperty("touchManipulator").objectReferenceValue = manipulator;
        serializedRecognitionController.FindProperty("modelPath").stringValue = "TeachableMachine/model_unquant.tflite";
        serializedRecognitionController.FindProperty("labelsPath").stringValue = "TeachableMachine/labels.txt";
        SerializedProperty museumObjectsProperty = serializedRecognitionController.FindProperty("museumObjects");
        museumObjectsProperty.arraySize = ObjectDefinitions.Length;
        for (int i = 0; i < ObjectDefinitions.Length; i++)
        {
            ObjectRecognitionDefinition definition = ObjectDefinitions[i];
            SerializedProperty entry = museumObjectsProperty.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("displayName").stringValue = definition.DisplayName;
            entry.FindPropertyRelative("referenceImageName").stringValue = string.Empty;
            entry.FindPropertyRelative("recognitionLabel").stringValue = definition.RecognitionLabel;
            entry.FindPropertyRelative("mobileDescription").stringValue = definition.Description;
            entry.FindPropertyRelative("englishExplanationText").stringValue = LoadExplanationTranscript(definition);
            entry.FindPropertyRelative("englishAudioClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{definition.AudioFileName}");
            entry.FindPropertyRelative("hebrewAudioClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{definition.HebrewAudioFileName}");
            entry.FindPropertyRelative("prefab").objectReferenceValue = LoadKnownModelPrefab(definition);
            entry.FindPropertyRelative("localOffset").vector3Value = new Vector3(0f, 0.15f, 0f);
            entry.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
            entry.FindPropertyRelative("localScale").vector3Value = Vector3.one;
        }
        serializedRecognitionController.ApplyModifiedPropertiesWithoutUndo();

        GameObject cameraObject = new("AR Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(originObject.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 30f;
        cameraObject.AddComponent<AudioListener>();
        ARCameraManager cameraManager = cameraObject.AddComponent<ARCameraManager>();
        cameraObject.AddComponent<ARCameraBackground>();
        origin.Camera = camera;

        serializedRecognitionController = new SerializedObject(recognitionController);
        serializedRecognitionController.FindProperty("cameraManager").objectReferenceValue = cameraManager;
        serializedRecognitionController.ApplyModifiedPropertiesWithoutUndo();

        GameObject lightObject = new("Mobile Preview Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        CreateMobileInteractionUi(recognitionController, manipulator);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static void CreateMobileInteractionUi(
        MuseumRealObjectRecognitionController recognitionController,
        TouchObjectManipulator manipulator)
    {
        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new("Mobile Interaction Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rootPanel = CreatePanel(canvasObject.transform, "Bottom Object Interaction Panel", new Color(0.04f, 0.05f, 0.06f, 0.82f));
        RectTransform rootRect = rootPanel.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.sizeDelta = new Vector2(0f, 760f);
        rootRect.anchoredPosition = Vector2.zero;

        GameObject statusText = CreateText(rootPanel.transform, "Status Text", "Real object recognition mode", 40, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(statusText, new Vector2(36f, -34f), new Vector2(-36f, -86f));

        GameObject scoreDebugText = CreateText(rootPanel.transform, "Score Debug Text", "Starting real object recognition...", 30, FontStyle.Bold, TextAnchor.UpperLeft);
        Text scoreDebug = scoreDebugText.GetComponent<Text>();
        scoreDebug.horizontalOverflow = HorizontalWrapMode.Wrap;
        scoreDebug.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(scoreDebugText, new Vector2(36f, -92f), new Vector2(-36f, -288f));

        GameObject titleText = CreateText(rootPanel.transform, "Object Title Text", "Waiting for object", 42, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(titleText, new Vector2(36f, -300f), new Vector2(-36f, -356f));

        GameObject descriptionText = CreateText(rootPanel.transform, "Object Description Text", "Point the phone camera at the real museum object.", 28, FontStyle.Normal, TextAnchor.UpperLeft);
        Text description = descriptionText.GetComponent<Text>();
        description.horizontalOverflow = HorizontalWrapMode.Wrap;
        description.verticalOverflow = VerticalWrapMode.Truncate;
        description.fontSize = 48;
        description.lineSpacing = 0.92f;
        SetRect(descriptionText, new Vector2(36f, -362f), new Vector2(-36f, -606f));

        GameObject interactionPanel = CreatePanel(rootPanel.transform, "Touch Interaction Buttons", new Color(0f, 0f, 0f, 0f));
        RectTransform interactionRect = interactionPanel.GetComponent<RectTransform>();
        interactionRect.anchorMin = new Vector2(0f, 0f);
        interactionRect.anchorMax = new Vector2(1f, 0f);
        interactionRect.pivot = new Vector2(0.5f, 0f);
        interactionRect.sizeDelta = new Vector2(0f, 160f);
        interactionRect.anchoredPosition = new Vector2(0f, 18f);

        Button toggleButton = CreateButton(interactionPanel.transform, "Toggle Object Button", "Show/Hide", new Vector2(35f, 68f), new Vector2(285f, 146f));
        Button resetButton = CreateButton(interactionPanel.transform, "Reset Button", "Reset", new Vector2(305f, 68f), new Vector2(495f, 146f));

        GameObject uiControllerObject = new("Mobile UI Controller");
        MuseumMobileUIController uiController = uiControllerObject.AddComponent<MuseumMobileUIController>();
        SerializedObject serializedUi = new(uiController);
        serializedUi.FindProperty("recognitionController").objectReferenceValue = recognitionController;
        serializedUi.FindProperty("touchManipulator").objectReferenceValue = manipulator;
        serializedUi.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<Text>();
        serializedUi.FindProperty("scoreDebugText").objectReferenceValue = scoreDebugText.GetComponent<Text>();
        serializedUi.FindProperty("objectTitleText").objectReferenceValue = titleText.GetComponent<Text>();
        serializedUi.FindProperty("objectDescriptionText").objectReferenceValue = descriptionText.GetComponent<Text>();
        serializedUi.FindProperty("interactionPanel").objectReferenceValue = interactionPanel;
        serializedUi.FindProperty("toggleButton").objectReferenceValue = toggleButton;
        serializedUi.FindProperty("resetButton").objectReferenceValue = resetButton;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static GameObject CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = alignment;
        return textObject;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.92f, 0.74f, 0.25f, 0.95f));
        SetRect(buttonObject, min, max);
        Button button = buttonObject.AddComponent<Button>();

        GameObject labelObject = CreateText(buttonObject.transform, "Label", label, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelObject.GetComponent<Text>().color = new Color(0.05f, 0.05f, 0.05f);
        return button;
    }

    private static void SetRect(GameObject gameObject, Vector2 min, Vector2 max)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = min;
        rect.sizeDelta = max - min;
    }

    private readonly struct ObjectRecognitionDefinition
    {
        public ObjectRecognitionDefinition(string displayName, string recognitionLabel, string description, string explanationText, string audioFileName, string hebrewAudioFileName, string modelFileName)
        {
            DisplayName = displayName;
            RecognitionLabel = recognitionLabel;
            Description = description;
            ExplanationText = explanationText;
            AudioFileName = audioFileName;
            HebrewAudioFileName = hebrewAudioFileName;
            ModelFileName = modelFileName;
        }

        public string DisplayName { get; }
        public string RecognitionLabel { get; }
        public string Description { get; }
        public string ExplanationText { get; }
        public string AudioFileName { get; }
        public string HebrewAudioFileName { get; }
        public string ModelFileName { get; }
    }

    private static ObjectRecognitionDefinition FindDefinition(string recognitionLabel)
    {
        foreach (ObjectRecognitionDefinition definition in ObjectDefinitions)
        {
            if (string.Equals(definition.RecognitionLabel, recognitionLabel, StringComparison.Ordinal))
            {
                return definition;
            }
        }

        return default;
    }

    private static string LoadExplanationTranscript(ObjectRecognitionDefinition definition)
    {
        string transcriptPath = $"{SoundFolder}/{Path.GetFileNameWithoutExtension(definition.AudioFileName)}.txt";
        TextAsset transcript = AssetDatabase.LoadAssetAtPath<TextAsset>(transcriptPath);
        if (transcript != null && !string.IsNullOrWhiteSpace(transcript.text))
        {
            return transcript.text.Trim();
        }

        return definition.ExplanationText;
    }

    private static GameObject LoadKnownModelPrefab(ObjectRecognitionDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.ModelFileName))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>($"{MuseumObjectsFolder}/{definition.ModelFileName}");
    }

    private static string[] ParseTeachableMachineLabels(string labelsText)
    {
        string[] lines = labelsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> labels = new();
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            int firstSpace = line.IndexOf(' ');
            string label = firstSpace >= 0 && int.TryParse(line[..firstSpace], out _)
                ? line[(firstSpace + 1)..].Trim()
                : line;

            if (!string.IsNullOrWhiteSpace(label))
            {
                labels.Add(label);
            }
        }

        return labels.ToArray();
    }

    private static SceneObjectDefinitionData ReadSceneObjectDefinition(SerializedProperty entry)
    {
        return new SceneObjectDefinitionData
        {
            DisplayName = entry.FindPropertyRelative("displayName").stringValue,
            ReferenceImageName = entry.FindPropertyRelative("referenceImageName").stringValue,
            RecognitionLabel = entry.FindPropertyRelative("recognitionLabel").stringValue,
            MobileDescription = entry.FindPropertyRelative("mobileDescription").stringValue,
            EnglishExplanationText = entry.FindPropertyRelative("englishExplanationText").stringValue,
            Prefab = entry.FindPropertyRelative("prefab").objectReferenceValue as GameObject,
            EnglishAudioClip = entry.FindPropertyRelative("englishAudioClip").objectReferenceValue as AudioClip,
            HebrewAudioClip = entry.FindPropertyRelative("hebrewAudioClip").objectReferenceValue as AudioClip,
            LocalOffset = entry.FindPropertyRelative("localOffset").vector3Value,
            LocalEulerAngles = entry.FindPropertyRelative("localEulerAngles").vector3Value,
            LocalScale = entry.FindPropertyRelative("localScale").vector3Value
        };
    }

    private static void WriteSceneObjectDefinition(SerializedProperty entry, SceneObjectDefinitionData data)
    {
        entry.FindPropertyRelative("displayName").stringValue = data.DisplayName;
        entry.FindPropertyRelative("referenceImageName").stringValue = data.ReferenceImageName;
        entry.FindPropertyRelative("recognitionLabel").stringValue = data.RecognitionLabel;
        entry.FindPropertyRelative("mobileDescription").stringValue = data.MobileDescription;
        entry.FindPropertyRelative("englishExplanationText").stringValue = data.EnglishExplanationText;
        entry.FindPropertyRelative("prefab").objectReferenceValue = data.Prefab;
        entry.FindPropertyRelative("englishAudioClip").objectReferenceValue = data.EnglishAudioClip;
        entry.FindPropertyRelative("hebrewAudioClip").objectReferenceValue = data.HebrewAudioClip;
        entry.FindPropertyRelative("localOffset").vector3Value = data.LocalOffset;
        entry.FindPropertyRelative("localEulerAngles").vector3Value = data.LocalEulerAngles;
        entry.FindPropertyRelative("localScale").vector3Value = data.LocalScale == Vector3.zero ? Vector3.one : data.LocalScale;
    }

    private sealed class SceneObjectDefinitionData
    {
        public string DisplayName;
        public string ReferenceImageName;
        public string RecognitionLabel;
        public string MobileDescription;
        public string EnglishExplanationText;
        public GameObject Prefab;
        public AudioClip EnglishAudioClip;
        public AudioClip HebrewAudioClip;
        public Vector3 LocalOffset = new(0f, 0.15f, 0f);
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale = Vector3.one;

        public static SceneObjectDefinitionData FromLabel(string label)
        {
            return new SceneObjectDefinitionData
            {
                DisplayName = label,
                RecognitionLabel = label,
                MobileDescription = $"{label}. Recognized from the real museum object.",
                EnglishExplanationText = $"{label}. Add the written explanation here.",
                LocalOffset = new Vector3(0f, 0.15f, 0f),
                LocalEulerAngles = Vector3.zero,
                LocalScale = Vector3.one
            };
        }
    }
}
