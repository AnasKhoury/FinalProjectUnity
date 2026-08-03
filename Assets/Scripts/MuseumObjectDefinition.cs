using System;
using UnityEngine;

[Serializable]
public sealed class MuseumObjectDefinition
{
    [Tooltip("Name shown in the mobile interaction panel.")]
    public string displayName;

    [Tooltip("Must match the name of the AR Foundation reference image.")]
    public string referenceImageName;

    [Tooltip("Must match a label in Teachable Machine labels.txt.")]
    public string recognitionLabel;

    [TextArea]
    [Tooltip("Short text shown after the object is recognized.")]
    public string mobileDescription;

    [TextArea(3, 8)]
    [Tooltip("Written English explanation shown above the mobile interaction buttons.")]
    public string englishExplanationText;

    [Tooltip("The optimized mobile prefab for this museum object.")]
    public GameObject prefab;

    [Tooltip("English audio explanation played from the mobile audio button.")]
    public AudioClip englishAudioClip;

    [TextArea(3, 8)]
    [Tooltip("Written Hebrew explanation shown when Hebrew support is used.")]
    public string hebrewExplanationText;

    [Tooltip("Hebrew audio explanation played from the mobile Hebrew audio button.")]
    public AudioClip hebrewAudioClip;

    [Tooltip("Arabic audio explanation played when Arabic is selected.")]
    public AudioClip arabicAudioClip;

    [Tooltip("Local offset from the tracked image or anchor.")]
    public Vector3 localOffset = new Vector3(0f, 0.15f, 0f);

    [Tooltip("Local Euler rotation applied after spawning.")]
    public Vector3 localEulerAngles;

    [Tooltip("Mobile-safe scale for the spawned object.")]
    public Vector3 localScale = Vector3.one;
}
