using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    private enum AppLanguage { English, Hebrew, Arabic }
    private AppLanguage selectedLanguage = AppLanguage.English;
    private bool languageSelected;
    private MuseumObjectDefinition currentDefinition;
    private Button galleryButton;
    private GameObject galleryPanel;
    private Image galleryImage;
    private Text galleryCounter;
    private Text galleryTitle;
    private Text galleryPreviousLabel;
    private Text galleryNextLabel;
    private ScrollRect descriptionScrollRect;
    private Texture2D[] galleryTextures;
    private int galleryIndex;
    private Vector2 swipeStart;

    private static readonly Dictionary<string, string> HebrewTitles = new()
    {
        { "Pregnant", "אלת פריון" }, { "Rock", "אסטלה" }, { "Ship", "ספינה פיניקית" },
        { "Hands", "העתק פקקים בצורת ידיים" }, { "Mask", "העתק מסכה" },
        { "3SmallVaz", "כלי זכוכית" }, { "NormalVaz", "פך שחור על אדום" },
        { "BigVaz", "קנקן כנעני" }, { "EnterancePanel", "העתק תבליט קיר" }
    };

    private static readonly Dictionary<string, string> HebrewExplanations = new()
    {
        { "Pregnant", "אלת פריון (פסלון אלת פריון), תקופת הברזל ב׳ (970–530 לפנה״ס). פסלון המייצג אלת פריון. רוב הפסלונים מסוג זה שנמצאו באזור הים התיכון מקורם בתל מיכל, סמוך להרצליה." },
        { "Rock", "על האסטלה מופיעה כתובת המציינת נדר שנדר אדם בשם עזרבעל בן בעלשמע. אסטלות כאלה הוצבו במקומות קדושים כביטוי פומבי למסירות דתית, והן משקפות את מרכזיות הנדרים והאל בעל בדת הפיניקית." },
        { "Ship", "דגם ספינה פיניקית. הפיניקים נחשבו ליורדי הים הטובים ביותר בעולם העתיק. היוונים כינו את ספינותיהם היפוס, כלומר סוס ביוונית, על שם ראש הסוס שהותקן בחרטום הספינה." },
        { "Hands", "העתק פקקים מאבן בצורת ידיים, התקופה הפרסית (530–332 לפנה״ס). חפצים אלה גולפו בדמות ידיים ושימשו כפקקים לכלי נוזלים וגם ככלים לחלוקה ולשימוש בנוזלים שנשמרו בכלים גדולים." },
        { "Mask", "העתק מסכה, תקופת הברזל (1150–530 לפנה״ס). המסכה היא העתק של מסכת מנחת קבורה שהתגלתה בחפירות אכזיב." },
        { "3SmallVaz", "כלי זכוכית, התקופה הפרסית (530–332 לפנה״ס). חוף החול באזור עכו היה מפורסם משום שהחול שלו נחשב חומר גלם מצוין לייצור זכוכית." },
        { "NormalVaz", "הסגנון הקיפרו־פיניקי, המכונה גם שחור על אדום, נקרא כך בשל עיטורים או רצועות שחורים שצוירו על כלי חרס אדומים. הסגנון משקף קשר תרבותי בין פיניקיה לקפריסין והיה נפוץ בתקופת הברזל בכלים יומיומיים ופולחניים." },
        { "BigVaz", "קנקן כנעני, התקופה הפרסית (530–332 לפנה״ס). על הקנקן כתובת שפירושה ״שייך לעבד־בעל״, המעידה על בעלות ומשקפת את השימוש בשמות פיניקיים המשלבים את שם האל בתקופה הפרסית." },
        { "EnterancePanel", "העתק תבליט קיר, תקופת הברזל (המאה החמישית לפנה״ס). התבליט מבוסס על ממצא מארמונו של המלך האשורי סרגון ומתאר מסחר בין סרגון למלך צור, ובייחוד הובלת עצי ארז דרך הים. אירוע זה מתואר גם בתנ״ך." }
    };

    private static readonly Dictionary<string, string> ArabicTitles = new()
    {
        { "Pregnant", "إلهة الخصوبة" }, { "Rock", "نصب حجري" }, { "Ship", "سفينة فينيقية" },
        { "Hands", "نسخ سدادات على شكل أيدٍ" }, { "Mask", "نسخة قناع" },
        { "3SmallVaz", "أوانٍ زجاجية" }, { "NormalVaz", "إناء أسود على أحمر" },
        { "BigVaz", "جرة كنعانية" }, { "EnterancePanel", "نسخة من نقش جداري" }
    };

    private static readonly Dictionary<string, string> ArabicExplanations = new()
    {
        { "Pregnant", "إلهة الخصوبة (تمثال إلهة الخصوبة)، العصر الحديدي الثاني (970–530 ق.م). يمثّل هذا التمثال إلهة للخصوبة. وقد عُثر على معظم التماثيل من هذا النوع في منطقة البحر المتوسط في تل ميخال قرب هرتسليا." },
        { "Rock", "يحمل النصب الحجري كتابة تشير إلى نذر قدّمه رجل يُدعى عزربعل ابن بعلشما. كانت مثل هذه الأنصاب تُقام في الأماكن المقدسة تعبيرًا علنيًا عن التعبّد، وتعكس المكانة المركزية للنذور وللإله بعل في الديانة الفينيقية." },
        { "Ship", "نموذج لسفينة فينيقية. كان الفينيقيون من أمهر البحّارة في العالم القديم. أطلق اليونانيون على سفنهم اسم هيبوس، أي الحصان باليونانية، نسبة إلى رأس الحصان المثبّت على مقدّمة السفينة." },
        { "Hands", "نسخ لسدادات حجرية على شكل أيدٍ، من العصر الفارسي (530–332 ق.م). نُحتت هذه القطع لتشبه الأيدي، واستُخدمت سدادات لأوعية السوائل وأدوات لتوزيع السوائل المحفوظة في أوعية أكبر واستعمالها." },
        { "Mask", "نسخة من قناع يعود إلى العصر الحديدي (1150–530 ق.م). هذا القناع نسخة عن قناع جنائزي اكتُشف خلال الحفريات في أخزيف." },
        { "3SmallVaz", "أوانٍ زجاجية من العصر الفارسي (530–332 ق.م). اشتهر الساحل الرملي حول عكا لأن رماله عُدّت مادة خام ممتازة لصناعة الزجاج." },
        { "NormalVaz", "يُعرف الطراز القبرصي الفينيقي أيضًا باسم الأسود على الأحمر، بسبب زخارفه أو خطوطه السوداء المرسومة على أوانٍ فخارية حمراء. يعكس هذا الطراز التواصل الثقافي بين فينيقيا وقبرص، وكان واسع الانتشار في العصر الحديدي للأغراض اليومية والطقسية." },
        { "BigVaz", "جرة كنعانية من العصر الفارسي (530–332 ق.م). تحمل الجرة كتابة تعني «ملك لعبد بعل»، وهي تدل على الملكية وتعكس استعمال الأسماء الفينيقية المرتبطة بأسماء الآلهة في العصر الفارسي." },
        { "EnterancePanel", "نسخة من نقش جداري من العصر الحديدي (القرن الخامس ق.م). يستند النقش إلى أثر اكتُشف في قصر الملك الآشوري سرجون، ويصوّر التجارة بين سرجون وملك صور، ولا سيما نقل خشب الأرز بحرًا. ويرد وصف هذا الحدث أيضًا في الكتاب المقدس." }
    };

    private void Awake()
    {
        RemoveLegacyTransformButtons();
        EnsureAudioButtonExists();
        EnsureHebrewAudioButtonExists();
        EnsureDebugButtonExists();
        EnsureGalleryExists();
        EnsureDescriptionAreaVisible();
        EnlargeRemainingButtons();
        toggleButton.onClick.AddListener(() => recognitionController.ToggleActiveObject());
        resetButton.onClick.AddListener(() => recognitionController.ResetActiveObjectTransform());
        audioButton.onClick.AddListener(PlaySelectedLanguageAudio);
        hebrewAudioButton.gameObject.SetActive(false);
        debugButton.onClick.AddListener(ToggleDebugVisibility);
        SetScanningState();
        SetDebugVisible(false);
        ShowLanguageSelection();
    }

    private void Update()
    {
        if (galleryPanel == null || !galleryPanel.activeSelf || Input.touchCount == 0)
        {
            return;
        }

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began) swipeStart = touch.position;
        if (touch.phase == TouchPhase.Ended && Mathf.Abs(touch.position.x - swipeStart.x) > 70f)
        {
            ShowGalleryImage(touch.position.x < swipeStart.x ? galleryIndex + 1 : galleryIndex - 1);
        }
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
        if (!languageSelected) return;
        string localizedLabel = label;
        if (selectedLanguage == AppLanguage.Hebrew)
        {
            localizedLabel = HebrewTitles.TryGetValue(label, out string translatedLabel)
                ? translatedLabel
                : LocalizeHebrewStatus(label);
        }
        else if (selectedLanguage == AppLanguage.Arabic)
        {
            localizedLabel = ArabicTitles.TryGetValue(label, out string translatedLabel)
                ? translatedLabel
                : LocalizeArabicStatus(label);
        }
        statusText.text = FormatForSelectedLanguage(confidence > 0f
            ? (selectedLanguage == AppLanguage.English ? $"Recognizing: {localizedLabel} ({confidence:P0})" : $"{(selectedLanguage == AppLanguage.Hebrew ? "מזהה" : "جارٍ التعرّف")}: {localizedLabel} ({confidence:P0})")
            : localizedLabel);

        if (confidence <= 0f && (label.StartsWith("Scanning") || label.StartsWith("Recognition reset")))
        {
            SetScanningState();
        }
    }

    private static string LocalizeHebrewStatus(string message)
    {
        if (message.StartsWith("Scanning")) return "סורק פריטים...";
        if (message.StartsWith("Recognition reset")) return "הזיהוי אופס";
        if (message.StartsWith("Playing")) return "משמיע הסבר קולי";
        if (message.StartsWith("Lock an object")) return "יש לזהות פריט לפני השמעת ההסבר";
        if (message.Contains("hidden")) return "הפריט הוסתר";
        if (message.Contains("shown") || message.Contains("revealed")) return "הפריט מוצג";
        return "מזהה פריטים...";
    }

    private static string LocalizeArabicStatus(string message)
    {
        if (message.StartsWith("Scanning")) return "جارٍ فحص القطع...";
        if (message.StartsWith("Recognition reset")) return "أُعيد ضبط التعرّف";
        if (message.StartsWith("Playing")) return "جارٍ تشغيل الشرح الصوتي";
        if (message.StartsWith("Lock an object")) return "يجب التعرّف على قطعة قبل تشغيل الشرح";
        if (message.Contains("hidden")) return "تم إخفاء القطعة";
        if (message.Contains("shown") || message.Contains("revealed")) return "القطعة معروضة";
        return "جارٍ التعرّف على القطع...";
    }

    private string FormatForSelectedLanguage(string value)
    {
        return selectedLanguage switch
        {
            AppLanguage.Hebrew => RtlTextFormatter.FormatHebrew(value),
            AppLanguage.Arabic => RtlTextFormatter.FormatArabic(value),
            _ => value
        };
    }

    private void OnObjectRecognized(MuseumObjectDefinition definition, Transform target)
    {
        currentDefinition = definition;
        string title = string.IsNullOrWhiteSpace(definition.displayName)
            ? definition.recognitionLabel
            : definition.displayName;

        if (selectedLanguage == AppLanguage.Hebrew && HebrewTitles.TryGetValue(definition.recognitionLabel, out string hebrewTitle))
        {
            title = hebrewTitle;
        }
        else if (selectedLanguage == AppLanguage.Arabic && ArabicTitles.TryGetValue(definition.recognitionLabel, out string arabicTitle))
        {
            title = arabicTitle;
        }

        statusText.text = FormatForSelectedLanguage(selectedLanguage switch
        {
            AppLanguage.Hebrew => $"הפריט זוהה: {title}",
            AppLanguage.Arabic => $"تم التعرّف على القطعة: {title}",
            _ => $"Recognized real object: {title}"
        });
        objectTitleText.text = FormatForSelectedLanguage(title);
        string explanation = selectedLanguage switch
        {
            AppLanguage.Hebrew => HebrewExplanations.TryGetValue(definition.recognitionLabel, out string hebrewText) ? hebrewText : definition.hebrewExplanationText,
            AppLanguage.Arabic => ArabicExplanations.TryGetValue(definition.recognitionLabel, out string arabicText) ? arabicText : string.Empty,
            _ => string.IsNullOrWhiteSpace(definition.englishExplanationText) ? definition.mobileDescription : definition.englishExplanationText
        };

        objectDescriptionText.text = FormatForSelectedLanguage(string.IsNullOrWhiteSpace(explanation)
            ? (selectedLanguage == AppLanguage.Hebrew ? "אין הסבר זמין לפריט זה." : selectedLanguage == AppLanguage.Arabic ? "لا يتوفر شرح لهذه القطعة." : "No explanation is available for this object.")
            : explanation);
        objectDescriptionText.alignment = selectedLanguage == AppLanguage.English ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        objectLocked = true;
        interactionPanel.SetActive(true);
        bool hasSelectedAudio = selectedLanguage == AppLanguage.Hebrew
            ? recognitionController.HasActiveObjectHebrewAudio()
            : selectedLanguage == AppLanguage.Arabic
                ? recognitionController.HasActiveObjectArabicAudio()
                : recognitionController.HasActiveObjectAudio();
        audioButton.gameObject.SetActive(hasSelectedAudio);
        audioButton.interactable = hasSelectedAudio;
        hebrewAudioButton.gameObject.SetActive(false);
        objectDescriptionText.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        UpdateDescriptionContentSize();
        if (descriptionScrollRect != null)
        {
            descriptionScrollRect.verticalNormalizedPosition = 1f;
        }
        SetAudioButtonLabel(false);
    }

    private void OnAudioPlaybackChanged(bool isPlaying)
    {
        string audioLanguage = selectedLanguage == AppLanguage.Hebrew ? "Hebrew" : selectedLanguage == AppLanguage.Arabic ? "Arabic" : "English";
        SetAudioButtonLabel(isPlaying && recognitionController.CurrentAudioLanguage == audioLanguage);
    }

    private void SetScanningState()
    {
        objectLocked = false;
        statusText.text = FormatForSelectedLanguage(selectedLanguage == AppLanguage.Hebrew ? "מצב זיהוי פריטי מוזיאון" : selectedLanguage == AppLanguage.Arabic ? "وضع التعرّف على قطع المتحف" : "Real object recognition mode");
        if (scoreDebugText != null)
        {
            scoreDebugText.text = "Starting real object recognition...";
            scoreDebugText.gameObject.SetActive(debugVisible);
        }

        objectTitleText.text = FormatForSelectedLanguage(selectedLanguage == AppLanguage.Hebrew ? "ממתין לפריט" : selectedLanguage == AppLanguage.Arabic ? "بانتظار قطعة" : "Waiting for object");
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
            debugButtonLabel.text = FormatForSelectedLanguage(selectedLanguage switch
            {
                AppLanguage.Hebrew => debugVisible ? "הסתרת מידע" : "הצגת מידע",
                AppLanguage.Arabic => debugVisible ? "إخفاء المعلومات" : "عرض المعلومات",
                _ => debugVisible ? "Hide Debug" : "Show Debug"
            });
        }
    }

    private void EnsureDescriptionAreaVisible()
    {
        if (objectDescriptionText == null)
        {
            return;
        }

        Transform originalParent = objectDescriptionText.transform.parent;
        GameObject viewportObject = new("Explanation Scroll Area", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
        viewportObject.transform.SetParent(originalParent, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 0f);
        viewportRect.pivot = new Vector2(0.5f, 0f);
        // Keep the explanation strictly between the upper debug/gallery controls
        // and the lower object action row.
        viewportRect.offsetMin = new Vector2(36f, 300f);
        viewportRect.offsetMax = new Vector2(-36f, 650f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        Mask viewportMask = viewportObject.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        objectDescriptionText.transform.SetParent(viewportObject.transform, false);
        RectTransform rect = objectDescriptionText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        objectDescriptionText.maskable = true;

        descriptionScrollRect = viewportObject.GetComponent<ScrollRect>();
        descriptionScrollRect.viewport = viewportRect;
        descriptionScrollRect.content = rect;
        descriptionScrollRect.horizontal = false;
        descriptionScrollRect.vertical = true;
        descriptionScrollRect.movementType = ScrollRect.MovementType.Clamped;
        descriptionScrollRect.scrollSensitivity = 45f;
        descriptionScrollRect.inertia = true;

        // The scroll area must never render over interactive controls.
        viewportObject.transform.SetAsFirstSibling();
        if (interactionPanel != null)
        {
            interactionPanel.transform.SetAsLastSibling();
        }
        if (debugButton != null)
        {
            debugButton.transform.SetAsLastSibling();
        }

        objectDescriptionText.alignment = TextAnchor.UpperLeft;
        objectDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        objectDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;
        objectDescriptionText.fontSize = 48;
        objectDescriptionText.lineSpacing = 0.92f;
        objectDescriptionText.color = Color.white;
    }

    private void UpdateDescriptionContentSize()
    {
        if (descriptionScrollRect == null || objectDescriptionText == null)
        {
            return;
        }

        RectTransform contentRect = objectDescriptionText.rectTransform;
        float viewportHeight = descriptionScrollRect.viewport.rect.height;
        float contentHeight = Mathf.Max(viewportHeight, objectDescriptionText.preferredHeight + 12f);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        contentRect.anchoredPosition = Vector2.zero;
    }

    private void RemoveLegacyTransformButtons()
    {
        DisableButton(rotateLeftButton);
        DisableButton(rotateRightButton);
        DisableButton(scaleDownButton);
        DisableButton(scaleUpButton);
    }

    private static void DisableButton(Button button)
    {
        if (button != null)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void EnlargeRemainingButtons()
    {
        PositionButton(toggleButton, new Vector2(35f, 78f), new Vector2(250f, 78f), 30);
        PositionButton(resetButton, new Vector2(305f, 78f), new Vector2(190f, 78f), 30);
        PositionButton(audioButton, new Vector2(515f, 78f), new Vector2(245f, 78f), 30);
        ResizeButton(hebrewAudioButton, new Vector2(245f, 72f), 30);
        ResizeButton(debugButton, new Vector2(235f, 66f), 26);
    }

    private static void PositionButton(Button button, Vector2 position, Vector2 size, int fontSize)
    {
        if (button == null) return;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        ResizeButton(button, size, fontSize);
    }

    private static void ResizeButton(Button button, Vector2 size, int labelFontSize)
    {
        if (button == null)
        {
            return;
        }

        button.GetComponent<RectTransform>().sizeDelta = size;
        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.fontSize = labelFontSize;
        }
    }

    private void PlaySelectedLanguageAudio()
    {
        if (selectedLanguage == AppLanguage.Hebrew)
        {
            recognitionController.ToggleActiveObjectHebrewAudio();
        }
        else if (selectedLanguage == AppLanguage.English)
        {
            recognitionController.ToggleActiveObjectAudio();
        }
        else
        {
            recognitionController.ToggleActiveObjectArabicAudio();
        }
    }

    private void ShowLanguageSelection()
    {
        Transform canvas = FindCanvasTransform();
        GameObject overlay = CreatePanel(canvas, "Language Selection", new Color(0.02f, 0.04f, 0.08f, 0.98f));
        StretchToParent(overlay.GetComponent<RectTransform>());

        Text heading = CreateLabel(overlay.transform, "Select language / בחירת שפה", 44, TextAnchor.MiddleCenter);
        SetAnchoredRect(heading.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.82f));

        Button english = CreateRuntimeButton(overlay.transform, "English", new Color(0.18f, 0.55f, 0.86f, 1f), 34);
        SetAnchoredRect(english.GetComponent<RectTransform>(), new Vector2(0.18f, 0.48f), new Vector2(0.82f, 0.60f));
        english.onClick.AddListener(() => SelectLanguage(AppLanguage.English, overlay));

        Button hebrew = CreateRuntimeButton(overlay.transform, RtlTextFormatter.FormatHebrew("עברית"), new Color(0.16f, 0.68f, 0.50f, 1f), 34);
        SetAnchoredRect(hebrew.GetComponent<RectTransform>(), new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.44f));
        hebrew.onClick.AddListener(() => SelectLanguage(AppLanguage.Hebrew, overlay));

        Button arabic = CreateRuntimeButton(overlay.transform, RtlTextFormatter.FormatArabic("العربية"), new Color(0.80f, 0.48f, 0.16f, 1f), 34);
        SetAnchoredRect(arabic.GetComponent<RectTransform>(), new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.28f));
        arabic.onClick.AddListener(() => SelectLanguage(AppLanguage.Arabic, overlay));

        overlay.transform.SetAsLastSibling();
    }

    private void SelectLanguage(AppLanguage language, GameObject overlay)
    {
        selectedLanguage = language;
        languageSelected = true;
        Destroy(overlay);
        audioButtonLabel.text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "השמעת הסבר" : language == AppLanguage.Arabic ? "تشغيل الشرح" : "Play Audio");
        toggleButton.GetComponentInChildren<Text>().text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "הצגה/הסתרה" : language == AppLanguage.Arabic ? "إظهار/إخفاء" : "Show/Hide");
        resetButton.GetComponentInChildren<Text>().text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "איפוס" : language == AppLanguage.Arabic ? "إعادة ضبط" : "Reset");
        galleryButton.GetComponentInChildren<Text>().text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "גלריית פריטים" : language == AppLanguage.Arabic ? "معرض القطع" : "Object Gallery");
        galleryTitle.text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "הפריטים שבמערכת" : language == AppLanguage.Arabic ? "القطع الموجودة في النظام" : "Objects in the museum");
        galleryPreviousLabel.text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "הקודם" : language == AppLanguage.Arabic ? "السابق" : "Previous");
        galleryNextLabel.text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "הבא" : language == AppLanguage.Arabic ? "التالي" : "Next");
        debugButtonLabel.text = FormatForSelectedLanguage(language == AppLanguage.Hebrew ? "הצגת מידע" : language == AppLanguage.Arabic ? "عرض المعلومات" : "Show Debug");
        SetScanningState();
    }

    private void EnsureGalleryExists()
    {
        Transform canvas = FindCanvasTransform();
        galleryTextures = Resources.LoadAll<Texture2D>("ObjectGallery");

        galleryButton = CreateRuntimeButton(canvas, "Object Gallery", new Color(0.58f, 0.34f, 0.82f, 0.96f), 25);
        RectTransform galleryButtonRect = galleryButton.GetComponent<RectTransform>();
        galleryButtonRect.anchorMin = new Vector2(1f, 1f);
        galleryButtonRect.anchorMax = new Vector2(1f, 1f);
        galleryButtonRect.pivot = new Vector2(1f, 1f);
        galleryButtonRect.anchoredPosition = new Vector2(-36f, -110f);
        galleryButtonRect.sizeDelta = new Vector2(250f, 66f);
        galleryButton.onClick.AddListener(OpenGallery);

        galleryPanel = CreatePanel(canvas, "Object Gallery Window", new Color(0.015f, 0.02f, 0.035f, 0.98f));
        StretchToParent(galleryPanel.GetComponent<RectTransform>());

        galleryTitle = CreateLabel(galleryPanel.transform, "Objects in the museum", 36, TextAnchor.MiddleCenter);
        SetAnchoredRect(galleryTitle.rectTransform, new Vector2(0.12f, 0.88f), new Vector2(0.88f, 0.98f));

        GameObject imageObject = new("Gallery Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(galleryPanel.transform, false);
        galleryImage = imageObject.GetComponent<Image>();
        galleryImage.preserveAspect = true;
        galleryImage.color = Color.white;
        SetAnchoredRect(imageObject.GetComponent<RectTransform>(), new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.87f));

        Button previous = CreateRuntimeButton(galleryPanel.transform, "Previous", new Color(0.16f, 0.48f, 0.75f, 1f), 28);
        galleryPreviousLabel = previous.GetComponentInChildren<Text>();
        SetAnchoredRect(previous.GetComponent<RectTransform>(), new Vector2(0.05f, 0.04f), new Vector2(0.28f, 0.14f));
        previous.onClick.AddListener(() => ShowGalleryImage(galleryIndex - 1));

        galleryCounter = CreateLabel(galleryPanel.transform, "", 28, TextAnchor.MiddleCenter);
        SetAnchoredRect(galleryCounter.rectTransform, new Vector2(0.32f, 0.04f), new Vector2(0.68f, 0.14f));

        Button next = CreateRuntimeButton(galleryPanel.transform, "Next", new Color(0.16f, 0.48f, 0.75f, 1f), 28);
        galleryNextLabel = next.GetComponentInChildren<Text>();
        SetAnchoredRect(next.GetComponent<RectTransform>(), new Vector2(0.72f, 0.04f), new Vector2(0.95f, 0.14f));
        next.onClick.AddListener(() => ShowGalleryImage(galleryIndex + 1));

        Button close = CreateRuntimeButton(galleryPanel.transform, "X", new Color(0.72f, 0.20f, 0.22f, 1f), 30);
        SetAnchoredRect(close.GetComponent<RectTransform>(), new Vector2(0.88f, 0.90f), new Vector2(0.97f, 0.98f));
        close.onClick.AddListener(() => galleryPanel.SetActive(false));
        galleryPanel.SetActive(false);
    }

    private Transform FindCanvasTransform()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            throw new MissingReferenceException("Museum UI requires a Canvas in the scene.");
        }

        return canvas.transform;
    }

    private void OpenGallery()
    {
        galleryPanel.SetActive(true);
        galleryPanel.transform.SetAsLastSibling();
        ShowGalleryImage(galleryIndex);
    }

    private void ShowGalleryImage(int index)
    {
        if (galleryTextures == null || galleryTextures.Length == 0)
        {
            galleryCounter.text = FormatForSelectedLanguage(selectedLanguage == AppLanguage.Hebrew ? "לא נמצאו תמונות" : selectedLanguage == AppLanguage.Arabic ? "لم يتم العثور على صور" : "No images found");
            galleryImage.sprite = null;
            return;
        }

        galleryIndex = (index % galleryTextures.Length + galleryTextures.Length) % galleryTextures.Length;
        Texture2D texture = galleryTextures[galleryIndex];
        galleryImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        galleryCounter.text = $"{galleryIndex + 1} / {galleryTextures.Length}";
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Button CreateRuntimeButton(Transform parent, string label, Color color, int fontSize)
    {
        GameObject buttonObject = CreatePanel(parent, label + " Button", color);
        Button button = buttonObject.AddComponent<Button>();
        Text text = CreateLabel(buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter);
        StretchToParent(text.rectTransform);
        return button;
    }

    private static Text CreateLabel(Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        Text label = labelObject.GetComponent<Text>();
        label.text = value;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
            audioButtonLabel.text = FormatForSelectedLanguage(selectedLanguage switch
            {
                AppLanguage.Hebrew => isPlaying ? "עצירת הסבר" : "השמעת הסבר",
                AppLanguage.Arabic => isPlaying ? "إيقاف الشرح" : "تشغيل الشرح",
                _ => isPlaying ? "Stop Audio" : "Play Audio"
            });
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
