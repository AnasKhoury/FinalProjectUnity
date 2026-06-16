using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class MuseumRealObjectRecognitionController : MonoBehaviour
{
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private TouchObjectManipulator touchManipulator;
    [SerializeField] private MuseumObjectDefinition[] museumObjects;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string modelPath = "TeachableMachine/model_unquant.tflite";
    [SerializeField] private string labelsPath = "TeachableMachine/labels.txt";
    [SerializeField] private float confidenceThreshold = 0.88f;
    [SerializeField] private float minimumScoreMargin = 0.18f;
    [SerializeField] private float verificationSeconds = 4.0f;
    [SerializeField] private float recognitionCooldownSeconds = 0.75f;
    [SerializeField] private float spawnDistanceMeters = 1.6f;
    [SerializeField] private float defaultModelScaleMultiplier = 0.25f;
    [Range(0.35f, 1f)]
    [SerializeField] private float centerCropFraction = 0.65f;
    [SerializeField] private bool showBuiltInDebugOverlay;

    public event Action<string, float> RecognitionUpdated;
    public event Action<string> RecognitionDebugUpdated;
    public event Action<MuseumObjectDefinition, Transform> ObjectRecognized;
    public event Action<bool> AudioPlaybackChanged;
    public string CurrentAudioLanguage { get; private set; } = string.Empty;

    private readonly Dictionary<string, MuseumObjectDefinition> definitionsByLabel = new();
    private float nextRecognitionTime;
    private GameObject activeObject;
    private MuseumObjectDefinition activeDefinition;
    private Interpreter interpreter;
    private string[] labels = Array.Empty<string>();
    private int inputWidth = 224;
    private int inputHeight = 224;
    private float[] floatInput;
    private byte[] byteInput;
    private sbyte[] sbyteInput;
    private float[] floatOutput;
    private byte[] byteOutput;
    private sbyte[] sbyteOutput;
    private Texture2D cameraTexture;
    private Interpreter.DataType inputType;
    private Interpreter.DataType outputType;
    private bool modelReady;
    private bool isProcessingFrame;
    private string lastDebugText = "Starting real object recognition...";
    private GUIStyle debugStyle;
    private string candidateLabel = string.Empty;
    private float candidateStartedAt;
    private float candidateConfidence;
    private float candidateMargin;
    private bool finalRecognitionLocked;
    private string finalLockedLabel = string.Empty;
    private bool wasAudioPlaying;

    private void Awake()
    {
        if (cameraManager == null)
        {
            cameraManager = FindAnyObjectByType<ARCameraManager>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        definitionsByLabel.Clear();
        foreach (MuseumObjectDefinition definition in museumObjects)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.recognitionLabel))
            {
                continue;
            }

            definitionsByLabel[definition.recognitionLabel] = definition;
        }

        InitializeModel();
    }

    private void Start()
    {
        if (cameraManager == null)
        {
            UpdateDebug("No ARCameraManager found in scene.\nRun Museum AR > Initialize Android AR Project.");
            RecognitionUpdated?.Invoke("Camera manager missing", 0f);
            return;
        }

        RecognitionUpdated?.Invoke(modelReady ? "Scanning camera..." : lastDebugText, 0f);
        RecognitionDebugUpdated?.Invoke(lastDebugText);
    }

    private void OnEnable()
    {
        if (cameraManager == null)
        {
            cameraManager = FindAnyObjectByType<ARCameraManager>();
        }

        if (cameraManager != null)
        {
            cameraManager.frameReceived += OnCameraFrameReceived;
        }

        RecognitionUpdated?.Invoke(modelReady ? "Scanning camera..." : lastDebugText, 0f);
        RecognitionDebugUpdated?.Invoke(lastDebugText);
    }

    private void OnDisable()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnDestroy()
    {
        StopActiveObjectAudio();
        interpreter?.Dispose();
        if (cameraTexture != null)
        {
            Destroy(cameraTexture);
        }
    }

    private void OnGUI()
    {
        if (!showBuiltInDebugOverlay)
        {
            return;
        }

        debugStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(28, Screen.height / 42),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            wordWrap = true
        };

        Rect backgroundRect = new(18f, 18f, Screen.width - 36f, Mathf.Min(Screen.height * 0.46f, 520f));
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string status = modelReady ? "TEACHABLE MACHINE: RUNNING" : "TEACHABLE MACHINE: NOT READY";
        GUI.Label(
            new Rect(backgroundRect.x + 22f, backgroundRect.y + 18f, backgroundRect.width - 44f, backgroundRect.height - 36f),
            $"{status}\n{lastDebugText}",
            debugStyle);
    }

    private void Update()
    {
        if (audioSource == null)
        {
            return;
        }

        bool isPlaying = audioSource.isPlaying;
        if (wasAudioPlaying != isPlaying)
        {
            wasAudioPlaying = isPlaying;
            if (!isPlaying)
            {
                CurrentAudioLanguage = string.Empty;
            }

            AudioPlaybackChanged?.Invoke(isPlaying);
        }
    }

    public bool IsDebugOverlayVisible()
    {
        return showBuiltInDebugOverlay;
    }

    public void SetDebugOverlayVisible(bool visible)
    {
        showBuiltInDebugOverlay = visible;
        RecognitionDebugUpdated?.Invoke(lastDebugText);
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!modelReady || isProcessingFrame || finalRecognitionLocked)
        {
            return;
        }

        if (Time.unscaledTime < nextRecognitionTime)
        {
            return;
        }

        nextRecognitionTime = Time.unscaledTime + recognitionCooldownSeconds;
        RunRecognitionOnLatestCpuImage();
    }

    private void InitializeModel()
    {
        try
        {
            labels = ParseLabels(Encoding.UTF8.GetString(LoadStreamingAssetBytes(labelsPath)));

            InterpreterOptions options = new()
            {
                threads = 2
            };
            interpreter = new Interpreter(LoadStreamingAssetBytes(modelPath), options);
            interpreter.AllocateTensors();

            Interpreter.TensorInfo inputInfo = interpreter.GetInputTensorInfo(0);
            inputType = inputInfo.type;
            if (inputInfo.shape.Length >= 4)
            {
                inputHeight = inputInfo.shape[1];
                inputWidth = inputInfo.shape[2];
            }

            Interpreter.TensorInfo outputInfo = interpreter.GetOutputTensorInfo(0);
            outputType = outputInfo.type;
            int outputLength = Math.Max(labels.Length, GetTensorElementCount(outputInfo.shape));

            floatInput = new float[inputWidth * inputHeight * 3];
            byteInput = new byte[floatInput.Length];
            sbyteInput = new sbyte[floatInput.Length];
            floatOutput = new float[outputLength];
            byteOutput = new byte[outputLength];
            sbyteOutput = new sbyte[outputLength];
            cameraTexture = new Texture2D(inputWidth, inputHeight, TextureFormat.RGBA32, false);

            modelReady = true;
            UpdateDebug($"Model ready: {inputWidth}x{inputHeight}, {labels.Length} labels\nWaiting for camera frame...");
            RecognitionUpdated?.Invoke("Scanning camera...", 0f);
        }
        catch (Exception exception)
        {
            modelReady = false;
            UpdateDebug($"Model load failed:\n{exception.Message}");
            RecognitionUpdated?.Invoke($"Model load failed: {exception.Message}", 0f);
            Debug.LogException(exception);
        }
    }

    private void RunRecognitionOnLatestCpuImage()
    {
        if (cameraManager == null || !cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            RecognitionUpdated?.Invoke("Waiting for camera image", 0f);
            UpdateDebug("Waiting for AR camera CPU image...");
            return;
        }

        isProcessingFrame = true;
        try
        {
            XRCpuImage.ConversionParams conversionParams = new(
                image,
                TextureFormat.RGBA32,
                XRCpuImage.Transformation.MirrorY);

            conversionParams.inputRect = GetCenterCropRect(image.width, image.height);
            conversionParams.outputDimensions = new Vector2Int(inputWidth, inputHeight);
            image.Convert(conversionParams, cameraTexture.GetRawTextureData<byte>());
            cameraTexture.Apply();

            Color32[] pixels = cameraTexture.GetPixels32();
            FillInputTensor(pixels);

            switch (inputType)
            {
                case Interpreter.DataType.Float32:
                    interpreter.SetInputTensorData(0, floatInput);
                    break;
                case Interpreter.DataType.UInt8:
                    interpreter.SetInputTensorData(0, byteInput);
                    break;
                case Interpreter.DataType.Int8:
                    interpreter.SetInputTensorData(0, sbyteInput);
                    break;
                default:
                    RecognitionUpdated?.Invoke($"Unsupported input type: {inputType}", 0f);
                    return;
            }

            interpreter.Invoke();
            float[] scores = ReadOutputScores();
            int bestIndex = 0;
            float bestScore = scores.Length > 0 ? scores[0] : 0f;
            int secondIndex = -1;
            float secondScore = 0f;
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > bestScore)
                {
                    secondScore = bestScore;
                    secondIndex = bestIndex;
                    bestScore = scores[i];
                    bestIndex = i;
                }
                else if (secondIndex < 0 || scores[i] > secondScore)
                {
                    secondScore = scores[i];
                    secondIndex = i;
                }
            }

            string label = bestIndex < labels.Length ? labels[bestIndex] : $"Class {bestIndex}";
            string secondLabel = secondIndex >= 0 && secondIndex < labels.Length ? labels[secondIndex] : "None";
            float scoreMargin = bestScore - secondScore;
            UpdateVerificationState(label, bestScore, scoreMargin);
            UpdateDebug(BuildScoreDebugText(scores, label, bestScore, secondLabel, secondScore, scoreMargin));
            ApplyRecognition(label, bestScore);
        }
        catch (Exception exception)
        {
            RecognitionUpdated?.Invoke($"Recognition error: {exception.Message}", 0f);
            UpdateDebug($"Recognition error:\n{exception.Message}");
            Debug.LogException(exception);
        }
        finally
        {
            image.Dispose();
            isProcessingFrame = false;
        }
    }

    public void ApplyRecognition(string label, float confidence)
    {
        RecognitionUpdated?.Invoke(label, confidence);
        if (finalRecognitionLocked || confidence < confidenceThreshold)
        {
            return;
        }

        if (!definitionsByLabel.TryGetValue(label, out MuseumObjectDefinition definition))
        {
            return;
        }

        if (!IsCandidateVerified(label))
        {
            return;
        }

        finalRecognitionLocked = true;
        finalLockedLabel = label;
        activeDefinition = definition;
        SpawnOrUpdateActiveObject(definition);
        if (activeObject != null)
        {
            activeObject.SetActive(false);
        }

        ObjectRecognized?.Invoke(definition, activeObject != null ? activeObject.transform : null);
        UpdateDebug($"FINAL LOCKED: {label}\nVerified for {verificationSeconds:0.0}s at {confidence:P0}\nUse Reset to scan again.");
    }

    private void UpdateDebug(string message)
    {
        lastDebugText = message;
        RecognitionDebugUpdated?.Invoke(lastDebugText);
    }

    private string BuildScoreDebugText(float[] scores, string bestLabel, float bestScore, string secondLabel, float secondScore, float scoreMargin)
    {
        StringBuilder builder = new();
        if (finalRecognitionLocked)
        {
            builder.Append("FINAL LOCKED: ");
            builder.Append(finalLockedLabel);
            builder.Append("\nUse Reset to scan again.\n");
        }
        else
        {
            bool canVerify = bestScore >= confidenceThreshold
                && scoreMargin >= minimumScoreMargin
                && definitionsByLabel.ContainsKey(bestLabel);
            builder.Append("Verifying best object: ");
            builder.Append(GetVerificationProgressSeconds(bestLabel).ToString("0.0"));
            builder.Append("/");
            builder.Append(verificationSeconds.ToString("0.0"));
            builder.Append("s\n");
            if (!canVerify)
            {
                builder.Append("Move closer and center the object\n");
            }
        }

        builder.Append("Scanning real object\n");
        builder.Append("Best: ");
        builder.Append(bestLabel);
        builder.Append(" ");
        builder.Append(Mathf.RoundToInt(bestScore * 100f));
        builder.Append("%\n");
        builder.Append("Second: ");
        builder.Append(secondLabel);
        builder.Append(" ");
        builder.Append(Mathf.RoundToInt(secondScore * 100f));
        builder.Append("% | Gap: ");
        builder.Append(Mathf.RoundToInt(scoreMargin * 100f));
        builder.Append("%\n");
        builder.Append("Need: ");
        builder.Append(Mathf.RoundToInt(confidenceThreshold * 100f));
        builder.Append("% + ");
        builder.Append(Mathf.RoundToInt(minimumScoreMargin * 100f));
        builder.Append("% gap\n");

        for (int i = 0; i < labels.Length; i++)
        {
            string label = labels[i];
            float score = i < scores.Length ? scores[i] : 0f;
            builder.Append(label);
            builder.Append(": ");
            builder.Append(Mathf.RoundToInt(score * 100f));
            builder.Append("%");
            if (i < labels.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private void UpdateVerificationState(string label, float confidence, float scoreMargin)
    {
        if (confidence < confidenceThreshold
            || scoreMargin < minimumScoreMargin
            || !definitionsByLabel.ContainsKey(label))
        {
            candidateLabel = string.Empty;
            candidateStartedAt = 0f;
            candidateConfidence = 0f;
            candidateMargin = 0f;
            return;
        }

        if (!string.Equals(candidateLabel, label, StringComparison.Ordinal))
        {
            candidateLabel = label;
            candidateStartedAt = Time.unscaledTime;
        }

        candidateConfidence = confidence;
        candidateMargin = scoreMargin;
    }

    private bool IsCandidateVerified(string label)
    {
        return string.Equals(candidateLabel, label, StringComparison.Ordinal)
            && candidateConfidence >= confidenceThreshold
            && candidateMargin >= minimumScoreMargin
            && Time.unscaledTime - candidateStartedAt >= verificationSeconds;
    }

    private float GetVerificationProgressSeconds(string label)
    {
        if (!string.Equals(candidateLabel, label, StringComparison.Ordinal))
        {
            return 0f;
        }

        return Mathf.Clamp(Time.unscaledTime - candidateStartedAt, 0f, verificationSeconds);
    }

    public void ToggleActiveObject()
    {
        if (activeObject == null)
        {
            return;
        }

        activeObject.SetActive(!activeObject.activeSelf);
        if (activeObject.activeSelf && activeDefinition != null)
        {
            PlaceInFrontOfCamera(activeObject.transform, activeDefinition);
        }

        if (!activeObject.activeSelf)
        {
            StopActiveObjectAudio();
        }
    }

    public void ResetActiveObjectTransform()
    {
        StopActiveObjectAudio();
        finalRecognitionLocked = false;
        finalLockedLabel = string.Empty;
        candidateLabel = string.Empty;
        candidateStartedAt = 0f;
        candidateConfidence = 0f;
        candidateMargin = 0f;
        RecognitionUpdated?.Invoke("Scanning camera...", 0f);
        UpdateDebug("Recognition reset.\nScanning camera...");

        if (activeObject != null)
        {
            activeObject.SetActive(false);
        }
    }

    public bool HasActiveObjectAudio()
    {
        return finalRecognitionLocked && activeDefinition != null && activeDefinition.englishAudioClip != null;
    }

    public bool HasActiveObjectHebrewAudio()
    {
        return finalRecognitionLocked && activeDefinition != null && activeDefinition.hebrewAudioClip != null;
    }

    public bool IsAudioPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }

    public void ToggleActiveObjectAudio()
    {
        ToggleActiveObjectAudioClip(activeDefinition != null ? activeDefinition.englishAudioClip : null, "English");
    }

    public void ToggleActiveObjectHebrewAudio()
    {
        ToggleActiveObjectAudioClip(activeDefinition != null ? activeDefinition.hebrewAudioClip : null, "Hebrew");
    }

    private void ToggleActiveObjectAudioClip(AudioClip clip, string languageName)
    {
        if (!finalRecognitionLocked || clip == null || audioSource == null)
        {
            RecognitionUpdated?.Invoke("Lock an object before playing audio", 0f);
            AudioPlaybackChanged?.Invoke(false);
            return;
        }

        if (audioSource.isPlaying)
        {
            if (audioSource.clip == clip)
            {
                StopActiveObjectAudio();
                return;
            }

            audioSource.Stop();
        }

        audioSource.clip = clip;
        CurrentAudioLanguage = languageName;
        audioSource.Play();
        wasAudioPlaying = true;
        RecognitionUpdated?.Invoke($"Playing {languageName} audio: {activeDefinition.displayName}", 0f);
        AudioPlaybackChanged?.Invoke(true);
    }

    public void StopActiveObjectAudio()
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        CurrentAudioLanguage = string.Empty;
        wasAudioPlaying = false;
        AudioPlaybackChanged?.Invoke(false);
    }

    private void SpawnOrUpdateActiveObject(MuseumObjectDefinition definition)
    {
        if (activeDefinition != definition)
        {
            StopActiveObjectAudio();
        }

        if (definition.prefab == null)
        {
            touchManipulator?.ClearTarget(activeObject != null ? activeObject.transform : null);
            activeObject = null;
            return;
        }

        if (activeObject == null || !activeObject.name.StartsWith(definition.recognitionLabel, StringComparison.Ordinal))
        {
            if (activeObject != null)
            {
                Destroy(activeObject);
            }

            activeObject = Instantiate(definition.prefab);
            activeObject.name = $"{definition.recognitionLabel}_RecognizedObject";
        }

        PlaceInFrontOfCamera(activeObject.transform, definition);
        touchManipulator?.SetTarget(activeObject.transform);
    }

    private void PlaceInFrontOfCamera(Transform target, MuseumObjectDefinition definition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        target.SetPositionAndRotation(
            cameraTransform.position + cameraTransform.forward * spawnDistanceMeters + definition.localOffset,
            Quaternion.LookRotation(cameraTransform.forward, Vector3.up) * Quaternion.Euler(definition.localEulerAngles));
        target.localScale = Vector3.Scale(definition.localScale, Vector3.one * defaultModelScaleMultiplier);
    }

    private void FillInputTensor(Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            int offset = i * 3;
            Color32 pixel = pixels[i];
            floatInput[offset] = pixel.r / 255f;
            floatInput[offset + 1] = pixel.g / 255f;
            floatInput[offset + 2] = pixel.b / 255f;
            byteInput[offset] = pixel.r;
            byteInput[offset + 1] = pixel.g;
            byteInput[offset + 2] = pixel.b;
            sbyteInput[offset] = unchecked((sbyte)(pixel.r - 128));
            sbyteInput[offset + 1] = unchecked((sbyte)(pixel.g - 128));
            sbyteInput[offset + 2] = unchecked((sbyte)(pixel.b - 128));
        }
    }

    private RectInt GetCenterCropRect(int imageWidth, int imageHeight)
    {
        int shorterSide = Mathf.Min(imageWidth, imageHeight);
        int cropSize = Mathf.Clamp(Mathf.RoundToInt(shorterSide * centerCropFraction), 1, shorterSide);
        int x = Mathf.Max(0, (imageWidth - cropSize) / 2);
        int y = Mathf.Max(0, (imageHeight - cropSize) / 2);
        return new RectInt(x, y, cropSize, cropSize);
    }

    private float[] ReadOutputScores()
    {
        int labelCount = labels.Length;
        float[] scores = new float[labelCount];
        switch (outputType)
        {
            case Interpreter.DataType.Float32:
                interpreter.GetOutputTensorData(0, floatOutput);
                Array.Copy(floatOutput, scores, Math.Min(labelCount, floatOutput.Length));
                break;
            case Interpreter.DataType.UInt8:
                interpreter.GetOutputTensorData(0, byteOutput);
                for (int i = 0; i < labelCount && i < byteOutput.Length; i++)
                {
                    scores[i] = byteOutput[i] / 255f;
                }
                break;
            case Interpreter.DataType.Int8:
                interpreter.GetOutputTensorData(0, sbyteOutput);
                for (int i = 0; i < labelCount && i < sbyteOutput.Length; i++)
                {
                    scores[i] = (sbyteOutput[i] + 128) / 255f;
                }
                break;
            default:
                RecognitionUpdated?.Invoke($"Unsupported output type: {outputType}", 0f);
                break;
        }

        return scores;
    }

    private static int GetTensorElementCount(int[] shape)
    {
        int count = 1;
        foreach (int dimension in shape)
        {
            count *= Math.Max(1, dimension);
        }

        return count;
    }

    private static string[] ParseLabels(string labelsText)
    {
        string[] lines = labelsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string[] parsed = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            int firstSpace = line.IndexOf(' ');
            parsed[i] = firstSpace >= 0 && int.TryParse(line[..firstSpace], out _)
                ? line[(firstSpace + 1)..].Trim()
                : line;
        }

        return parsed;
    }

    private static byte[] LoadStreamingAssetBytes(string relativePath)
    {
        string path = Path.Combine(Application.streamingAssetsPath, relativePath);
        string uri = Application.platform == RuntimePlatform.Android ? path : $"file://{path}";
        using UnityWebRequest request = UnityWebRequest.Get(uri);
        request.SendWebRequest();
        while (!request.isDone)
        {
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new FileNotFoundException($"Failed to load StreamingAsset: {relativePath}. {request.error}");
        }

        return request.downloadHandler.data;
    }
}
