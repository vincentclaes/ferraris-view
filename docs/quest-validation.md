# Quest 3 acceptance

Latest package: **13 September 2026**, `builds/Winksele1775.apk`, **79,044,403 bytes** (75.38 MiB). SHA-256: `b12fd8af06806cb5b99a9bdb862325a6d8dc2b568f3cb6f994a64f8dd1087c38`. Unity 6000.6.0f1 built Android ARM64 with OpenXR, Vulkan, minimum API 29 and target API 36. APK Signature Scheme v2 verifies. The manifest contains VR launch/headtracking declarations and Meta Quest supported-device metadata.

`bash scripts/unity.sh quest` now rebuilds generated Android staging/compiled output and runs `scripts/verify-quest.py`. The first package contained 100 numbered stale copies (156,619,994 bytes). Cleaning only Gradle staging exposed duplicate `mscorlib` assemblies in the Android compilation cache; recreating both generated Android directories fixed the failure. The final package has no numbered copies or duplicate ZIP entries, contains the required ARM64 Unity/IL2CPP/OpenXR libraries, and embeds byte-identical world, terrain and five discovery JSON files. The world contains the 53 reviewed building symbols. Receipt: `artifacts/quest-package-validation.json`; build log: `artifacts/unity-quest.log`.

Hardware status: ADB found no attached device on 13 September. This package has **not** been installed or tested on the headset. The earlier simple prototype was installed and launched on 12 September; Vincent confirmed map → world → movement → B return, and a short sample reported 72–73 FPS. Those results do not validate the current graphics, polygon colliders or discovery interfaces. Human tableaux remain unimplemented; the story currently provides text and waypoints.

Connect an unlocked Quest 3 over USB with developer mode enabled and accept USB debugging inside the headset. Run the `quest-device.sh` commands in the README. Keep the headset's normal boundary active and begin seated or standing in a clear space.

1. Launch from the installed app or the device script. The Ferraris map should appear ahead. Turn your head and confirm stable, correct stereo tracking in both eyes.
2. Use the left stick to pan after zooming with the right stick. Aim the right controller ray at a recognizable road junction and press its trigger.
3. Confirm the world opens at that junction, with terrain, dirt roads, buildings and orchard trees visible. Check the ground height feels plausible and the camera follows head movement.
4. Walk with the left stick and turn with the right stick. Turning should happen in 30-degree steps. Confirm movement follows gaze, buildings block passage, and you remain supported by the terrain.
5. Press B to return to the map. Confirm the selected marker corresponds to the entry point. Repeat from a field and a building symbol; building selections may move to a nearby free spawn within 25 metres.
6. Remove and replace the headset, then return from the system menu. Check tracking, input and the current mode recover. Note discomfort or unexpected camera movement.
7. Profile a development build in Unity Profiler over the Android connection for at least five minutes, including the village and tree clusters. Record achieved refresh rate, frame-time spikes and sustained CPU/GPU timing. At 72 FPS the frame budget is approximately 13.9 ms. A desktop FPS reading or successful build is not a Quest performance result.

Record the date, Quest model/OS, Git commit, APK SHA-256, tracking/control results, measured performance and any errors from `bash scripts/quest-device.sh logs`. Distinguish the confirmed basic journey and short 72 Hz performance sample from longer stress, resume and comfort testing.
