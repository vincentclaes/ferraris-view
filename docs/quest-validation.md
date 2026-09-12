# Quest 3 acceptance

Status: APK built and signature verified; physical headset acceptance pending.

Connect an unlocked Quest 3 over USB with developer mode enabled and accept USB debugging inside the headset. Run the `quest-device.sh` commands in the README. Keep the headset's normal boundary active and begin seated or standing in a clear space.

1. Launch from the installed app or the device script. The Ferraris map should appear ahead. Turn your head and confirm stable, correct stereo tracking in both eyes.
2. Use the left stick to pan after zooming with the right stick. Aim the right controller ray at a recognizable road junction and press its trigger.
3. Confirm the world opens at that junction, with terrain, dirt roads, buildings and orchard trees visible. Check the ground height feels plausible and the camera follows head movement.
4. Walk with the left stick and turn with the right stick. Turning should happen in 30-degree steps. Confirm movement follows gaze, buildings block passage, and you remain supported by the terrain.
5. Press B to return to the map. Confirm the selected marker corresponds to the entry point. Repeat from a field and a building symbol; building selections may move to a nearby free spawn within 25 metres.
6. Remove and replace the headset, then return from the system menu. Check tracking, input and the current mode recover. Note discomfort or unexpected camera movement.
7. Profile a development build in Unity Profiler over the Android connection for at least five minutes, including the village and tree clusters. Record achieved refresh rate, frame-time spikes and sustained CPU/GPU timing. At 72 FPS the frame budget is approximately 13.9 ms. A desktop FPS reading or successful build is not a Quest performance result.

Record the date, Quest model/OS, Git commit, APK SHA-256, tracking/control results, measured performance and any errors from `bash scripts/quest-device.sh logs`. Until those observations exist, controller tracking, comfort and the 72 FPS target remain unverified.
