# Mobile AR setup

EcoSort's AR challenge uses AR Foundation and horizontal-plane hit testing. The same placement contract is backed by ARKit on iOS and ARCore on Android. The Unity Editor and desktop builds keep the existing simulated floor provider.

## Packages and project settings

- Unity Editor: `6000.6.2f1` (the version recorded by `ProjectSettings/ProjectVersion.txt`).
- AR Foundation, ARCore XR Plugin, and ARKit XR Plugin: `6.6.2` (released for Editor `6000.6`).
- XR Plug-in Management: `4.5.3`.
- Android minimum SDK is already API 26; the iOS deployment target is already 15.0.
- iOS camera usage text is configured in Player Settings.
- The Mobile URP renderer receives AR Foundation's AR Background Renderer Feature so the live camera feed renders behind the virtual scene.
- `EcoSort/Configure Android and iOS AR providers` assigns the ARCore loader to Android and ARKit loader to iOS. The editor setup also retries this when the project opens after packages are resolved.

## Runtime flow

On iOS and Android, entering `ARChallengeScene` starts an AR Session, an XR Origin, camera background, horizontal plane detection, and plane hit testing. The player scans a floor or table, taps a detected plane to place the challenge, then drags the recognized waste item into the correct bin. On unsupported platforms and in the Editor, placement remains simulated.

## Build and device acceptance

1. Open `ECOSORT_GAME` in the pinned Unity version and allow Package Manager to resolve the XR dependencies.
2. Confirm the Console reports successful ARCore and ARKit loader configuration. If XR settings were not available during first import, open Project Settings > XR Plug-in Management once and run `EcoSort/Configure Android and iOS AR providers` from the menu.
3. Build an Android development APK for an ARCore-supported Android phone, or configure Unity Build Automation with the iOS signing assets and device provisioning profile for the iPhone.
4. Install and launch on each real device, grant camera access, scan a textured, well-lit horizontal surface, tap a detected plane, and complete the bin-sorting interaction.

The project changes configure the feature but do not prove a device build or AR session has succeeded. ARKit is not available in the iOS Simulator; final acceptance requires physical iOS and Android devices. The iPhone 11 Pro Max supports basic ARKit tracking but does not provide LiDAR-based depth features.
