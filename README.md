# SA Museum Android AR

Android phone version of the SA Museum AR experience. The app uses the phone camera, AR Foundation/ARCore, and a Teachable Machine TensorFlow Lite image model to recognize real museum objects, then lets the visitor reveal and interact with matching 3D scans, read explanations, and play English or Hebrew audio.

## Current Features

- Real object recognition from the Android phone camera.
- Teachable Machine / TensorFlow Lite model support.
- 9 museum object labels:
  - `Pregnant` / DEA GRAVIDA
  - `Rock`
  - `Ship`
  - `Hands`
  - `Mask`
  - `3SmallVaz`
  - `NormalVaz`
  - `BigVaz`
  - `EnterancePanel`
- 3D model reveal after object lock.
- Mobile interaction buttons:
  - `Show/Hide`
  - `Reset`
  - rotate left/right
  - scale down/up
- English and Hebrew audio buttons, shown only after an object is locked.
- Text explanation panel above the interaction buttons.
- `Show Debug` button for recognition percentages, best match, score gap, and locked object status.
- Debug block is hidden by default for museum use.

## Unity Version

Use Unity:

```text
6000.2.13f1
```

Open this folder as a Unity project:

```text
FinalProjectUnity
```

Main scene:

```text
Assets/Scenes/MuseumAndroidAR.unity
```

## Important Packages

The project uses:

- AR Foundation
- Google ARCore XR Plugin
- TensorFlow Lite Unity package
- glTFast for `.glb` 3D models
- Unity UI
- Burst

Packages are defined in:

```text
Packages/manifest.json
```

## Teachable Machine Model

The active model files are here:

```text
Assets/ML/TeachableMachine/model_unquant.tflite
Assets/ML/TeachableMachine/labels.txt
```

If you train a newer model:

1. Export from Teachable Machine as `TensorFlow Lite`.
2. Replace `model_unquant.tflite`.
3. Replace `labels.txt`.
4. In Unity, run:

```text
Museum AR > Sync Scene Objects From Teachable Machine Labels
```

5. Check the `Museum Real Object Recognition Controller` on the scene object and assign any missing prefabs/audio.

## Android Build Steps

1. Open `Assets/Scenes/MuseumAndroidAR.unity`.
2. Connect an ARCore-supported Android phone by USB.
3. Enable USB debugging on the phone.
4. In Unity, run:

```text
Museum AR > Prepare Android Build And Run
```

5. Go to:

```text
File > Build Profiles / Build Settings
```

6. Select Android.
7. Press `Build And Run`.

Do not run `Museum AR > Initialize Android AR Project` unless you intentionally want to recreate the base scene, because that can reset scene assignments.

## Testing In The Museum

1. Start the app on the phone.
2. Point the camera at one real object.
3. Keep the object centered and close enough for a few seconds.
4. Wait until the app locks the recognized object.
5. Use `Show/Hide` to reveal the 3D model.
6. Use rotate/scale buttons to inspect it.
7. Use `Play Audio` or `Hebrew` for explanations.
8. Use `Reset` to hide the current object and scan again.

For testing recognition quality, press `Show Debug`. The debug panel shows:

- best object prediction
- second prediction
- percentage gap
- required confidence and gap
- all class percentages
- final locked object

Hide the debug panel before normal museum presentation.

## Notes For GitHub

This repository intentionally excludes generated Unity folders such as:

- `Library`
- `Temp`
- `Logs`
- `Obj`
- build outputs / APK files
- Burst debug output folders

Unity will recreate those folders when the project is opened.
