# Moonwitch RX changelog

## 2.1.0-pr.2-rx1

- Based on Kenji-NX Android 2.1.0-pr.2 commit
  `6c1c05dc5d2d17a65263d878a0ed0ea9fb00f202`.
- Saves settings when leaving with Android's back button or back gesture.
- Uses one set of defaults in the Settings UI and the emulation runtime.
- Enables the performance-oriented ARM64 defaults: NCE, PPTC, Macro HLE,
  shader cache, threaded rendering, and handheld mode.
- Removes per-frame preference loading, MIUI broadcasts, refresh-rate changes,
  and repeated Adreno turbo calls.
- Sends the Android loading-screen callback once on the first rendered frame
  instead of crossing the C#/JNI boundary on every frame.
- Keeps saved NCE/PPTC values when games are started from Android shortcuts.
- Caches virtual-controller preferences instead of rereading every preference on
  each analog-stick event.
- Polls the emulated controller at 250 Hz instead of 1,000 Hz, reducing native
  input-loop overhead while remaining faster than the display and sensors.
- Makes performance-mode transitions idempotent and lifecycle-aware.
- Uses the independent `com.moonait.moonwitchrx` application ID.
- Removes the package-ID impersonation used by the upstream optimized flavor.
- Builds a dedicated ARMv8.2-A APK for devices such as Snapdragon 7+ Gen 2.

Moonwitch RX retains the upstream MIT license and attribution.
