using UnityEngine;
using UnityEngine.UI;

public sealed class MuseumMobileUIController : MonoBehaviour
{
    [SerializeField] private MuseumRealObjectRecognitionController recognitionController;
    [SerializeField] private TouchObjectManipulator touchManipulator;
    [SerializeField] private Text statusText;
    [SerializeField] private Text scoreDebugText;
    [SerializeField] private Text objectTitleText;
    [SerializeField] private Text objectDescriptionText;
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button rotateLeftButton;
    [SerializeField] private Button rotateRightButton;
    [SerializeField] private Button scaleDownButton;
    [SerializeField] private Button scaleUpButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Text audioButtonLabel;
    [SerializeField] private Button hebrewAudioButton;
    [SerializeField] private Text hebrewAudioButtonLabel;
    [SerializeField] private Button debugButton;
    [SerializeField] private Text debugButtonLabel;

    private bool objectLocked;
    private bool debugVisible;

    private void Awake()
    {
        EnsureAudioButtonExists();
        EnsureHebrewAudioButtonExists();
        EnsureDebugButtonExists();
        EnsureDescriptionAreaVisible();
        toggleButton.onClick.AddListener(() => recognitionController.ToggleActiveObject());
        resetButton.onClick.AddListener(() => recognitionController.ResetActiveObjectTransform());
        rotateLeftButton.onClick.AddListener(() => touchManipulator.RotateTarget(20f));
        rotateRightButton.onClick.AddListener(() => touchManipulator.RotateTarget(-20f));
        scaleDownButton.onClick.AddListener(() => touchManipulator.ScaleTarget(0.9f));
        scaleUpButton.onClick.AddListener(() => touchManipulator.ScaleTarget(1.1f));
        audioButton.onClick.AddListener(() => recognitionController.ToggleActiveObjectAudio());
        hebrewAudioButton.onClick.AddListener(() => recognitionController.ToggleActiveObjectHebrewAudio());
        debugButton.onClick.AddListener(ToggleDebugVisibility);
        SetScanningState();
        SetDebugVisible(false);
    }

    private void OnEnable()
    {
        recognitionController.RecognitionUpdated += OnRecognitionUpdated;
        recognitionController.RecognitionDebugUpdated += OnRecognitionDebugUpdated;
        recognitionController.ObjectRecognized += OnObjectRecognized;
        recognitionController.AudioPlaybackChanged += OnAudioPlaybackChanged;
    }

    private void OnDisable()
    {
        recognitionController.RecognitionUpdated -= OnRecognitionUpdated;
        recognitionController.RecognitionDebugUpdated -= OnRecognitionDebugUpdated;
        recognitionController.ObjectRecognized -= OnObjectRecognized;
        recognitionController.AudioPlaybackChanged -= OnAudioPlaybackChanged;
    }

    private void OnRecognitionDebugUpdated(string debugText)
    {
        if (scoreDebugText != null)
        {
            scoreDebugText.text = debugText;
            scoreDebugText.gameObject.SetActive(debugVisible);
        }
    }

    private void OnRecognitionUpdated(string label, float confidence)
    {
        statusText.text = confidence > 0f
            ? $"Recognizing: {label} ({confidence:P0})"
            : label;

        if (confidence <= 0f && (label.StartsWith("Scanning") || label.StartsWith("Recognition reset")))
        {
            SetScanningState();
        }
    }

    private void OnObjectRecognized(MuseumObjectDefinition definition, Transform target)
    {
        string title = string.IsNullOrWhiteSpace(definition.displayName)
            ? definition.recognitionLabel
            : definition.displayName;

        statusText.text = $"Recognized real object: {title}";
        objectTitleText.text = title;
        string explanation = string.IsNullOrWhiteSpace(definition.englishExplanationText)
            ? definition.mobileDescription
            : definition.englishExplanationText;

        objectDescriptionText.text = string.IsNullOrWhiteSpace(explanation)
            ? "Use the mobile controls to reveal, rotate, and scale the 3D object."
            : explanation;
        objectLocked = true;
        interactionPanel.SetActive(true);
        audioButton.gameObject.SetActive(recognitionController.HasActiveObjectAudio());
        audioButton.interactable = recognitionController.HasActiveObjectAudio();
        hebrewAudioButton.gameObject.SetActive(recognitionController.HasActiveObjectHebrewAudio());
        hebrewAudioButton.interactable = recognitionController.HasActiveObjectHebrewAudio();
        objectDescriptionText.gameObject.SetActive(true);
        SetAudioButtonLabel(false);
    }

    private void OnAudioPlaybackChanged(bool isPlaying)
    {
        SetAudioButtonLabel(isPlaying && recognitionController.CurrentAudioLanguage == "English");
        SetHebrewAudioButtonLabel(isPlaying && recognitionController.CurrentAudioLanguage == "Hebrew");
    }

    private void SetScanningState()
    {
        objectLocked = false;
        statusText.text = "Real object recognition mode";
        if (scoreDebugText != null)
        {
            scoreDebugText.text = "Starting real object recognition...";
            scoreDebugText.gameObject.SetActive(debugVisible);
        }

        objectTitleText.text = "Waiting for object";
        objectDescriptionText.text = string.Empty;
        objectDescriptionText.gameObject.SetActive(false);
        interactionPanel.SetActive(false);
        if (audioButton != null)
        {
            audioButton.gameObject.SetActive(false);
            audioButton.interactable = false;
            SetAudioButtonLabel(false);
        }

        if (hebrewAudioButton != null)
        {
            hebrewAudioButton.gameObject.SetActive(false);
            hebrewAudioButton.interactable = false;
            SetHebrewAudioButtonLabel(false);
        }
    }

    private void ToggleDebugVisibility()
    {
        SetDebugVisible(!debugVisible);
    }

    private void SetDebugVisible(bool visible)
    {
        debugVisible = visible;
        recognitionController.SetDebugOverlayVisible(debugVisible);

        if (scoreDebugText != null)
        {
            scoreDebugText.gameObject.SetActive(debugVisible);
        }

        if (debugButtonLabel != null)
        {
            debugButtonLabel.text = debugVisible ? "Hide Debug" : "Show Debug";
        }
    }

    private void EnsureDescriptionAreaVisible()
    {
        if (objectDescriptionText == null)
        {
            return;
        }

        RectTransform rect = objectDescriptionText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(36f, 218f);
        rect.offsetMax = new Vector2(-36f, 470f);

        objectDescriptionText.alignment = TextAnchor.UpperLeft;
        objectDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        objectDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        objectDescriptionText.fontSize = 24;
        objectDescriptionText.lineSpacing = 0.92f;
        objectDescriptionText.color = Color.white;
    }

    private void EnsureAudioButtonExists()
    {
        if (audioButton != null)
        {
            if (audioButtonLabel == null)
            {
                audioButtonLabel = audioButton.GetComponentInChildren<Text>();
            }

            return;
        }

        GameObject buttonObject = new("Audio Explanation Button");
        buttonObject.transform.SetParent(interactionPanel.transform, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(35f, 8f);
        rect.sizeDelta = new Vector2(210f, 60f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.68f, 0.95f, 0.95f);
        audioButton = buttonObject.AddComponent<Button>();

        GameObject labelObject = new("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        audioButtonLabel = labelObject.AddComponent<Text>();
        audioButtonLabel.text = "Audio";
        audioButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        audioButtonLabel.fontSize = 28;
        audioButtonLabel.fontStyle = FontStyle.Bold;
        audioButtonLabel.alignment = TextAnchor.MiddleCenter;
        audioButtonLabel.color = Color.white;
        buttonObject.SetActive(false);
    }

    private void EnsureHebrewAudioButtonExists()
    {
        if (hebrewAudioButton != null)
        {
            if (hebrewAudioButtonLabel == null)
            {
                hebrewAudioButtonLabel = hebrewAudioButton.GetComponentInChildren<Text>();
            }

            return;
        }

        GameObject buttonObject = new("Hebrew Audio Explanation Button");
        buttonObject.transform.SetParent(interactionPanel.transform, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(265f, 8f);
        rect.sizeDelta = new Vector2(210f, 60f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.52f, 0.82f, 0.95f);
        hebrewAudioButton = buttonObject.AddComponent<Button>();

        GameObject labelObject = new("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        hebrewAudioButtonLabel = labelObject.AddComponent<Text>();
        hebrewAudioButtonLabel.text = "Hebrew";
        hebrewAudioButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hebrewAudioButtonLabel.fontSize = 28;
        hebrewAudioButtonLabel.fontStyle = FontStyle.Bold;
        hebrewAudioButtonLabel.alignment = TextAnchor.MiddleCenter;
        hebrewAudioButtonLabel.color = Color.white;
        buttonObject.SetActive(false);
    }

    private void EnsureDebugButtonExists()
    {
        if (debugButton != null)
        {
            if (debugButtonLabel == null)
            {
                debugButtonLabel = debugButton.GetComponentInChildren<Text>();
            }

            return;
        }

        Transform parent = interactionPanel != null && interactionPanel.transform.parent != null
            ? interactionPanel.transform.parent
            : transform;

        GameObject buttonObject = new("Debug Toggle Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-36f, -28f);
        rect.sizeDelta = new Vector2(210f, 56f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.18f, 0.22f, 0.94f);
        debugButton = buttonObject.AddComponent<Button>();

        GameObject labelObject = new("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        debugButtonLabel = labelObject.AddComponent<Text>();
        debugButtonLabel.text = "Show Debug";
        debugButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugButtonLabel.fontSize = 24;
        debugButtonLabel.fontStyle = FontStyle.Bold;
        debugButtonLabel.alignment = TextAnchor.MiddleCenter;
        debugButtonLabel.color = Color.white;
    }

    private void SetAudioButtonLabel(bool isPlaying)
    {
        if (audioButtonLabel != null)
        {
            audioButtonLabel.text = isPlaying ? "Stop Audio" : "Play Audio";
        }

        if (audioButton != null && !objectLocked)
        {
            audioButton.gameObject.SetActive(false);
            audioButton.interactable = false;
        }
    }

    private void SetHebrewAudioButtonLabel(bool isPlaying)
    {
        if (hebrewAudioButtonLabel != null)
        {
            hebrewAudioButtonLabel.text = isPlaying ? "Stop Hebrew" : "Hebrew";
        }

        if (hebrewAudioButton != null && !objectLocked)
        {
            hebrewAudioButton.gameObject.SetActive(false);
            hebrewAudioButton.interactable = false;
        }
    }
}
