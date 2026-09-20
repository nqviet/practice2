# Recommended Unity 6 stack

| Area                  | Technology / dependency                                    | Purpose                                                    |
| --------------------- | ---------------------------------------------------------- | ---------------------------------------------------------- |
| Engine                | **Unity 6 LTS + C#**                                       | Core runtime                                               |
| Rendering             | **URP + 2D Renderer**                                      | Sprites, glow, bloom, lightweight mobile rendering         |
| Physics               | **Unity Physics 2D / Box2D**                               | Ball collision, ricochets, triggers                        |
| Input                 | **Unity Input System**                                     | Drag/aim/release and touch input                           |
| UI                    | **uGUI + TextMeshPro**                                     | HUD, level screen, settings, menus                         |
| Animation             | **DOTween** (free tier; **not on the Unity registry** — OpenUPM scoped registry or a vendored Asset Store import; route still open) | Block hits, UI transitions, cannon recoil, screen feedback |
| Camera                | **Cinemachine** (3.x for Unity 6 — `Unity.Cinemachine` namespace) | Camera shake/impulse on block destruction                  |
| Content               | **ScriptableObjects**                                      | Block definitions, ball parameters, level configs          |
| Asset loading         | **Addressables**                                           | Level packs, sprites, audio, remote/additional content     |
| Audio                 | **Unity AudioMixer** initially                             | SFX/music mixing and settings                              |
| Persistence           | **JSON + Application.persistentDataPath**                  | Local progress/settings                                    |
| Ads                   | **Unity LevelPlay / mediation SDK**                        | Banner, interstitial and rewarded ads                      |
| IAP                   | **Unity IAP**                                              | Remove-ads / purchases                                     |
| Analytics             | **Unity Analytics or Firebase Analytics**                  | Funnel, level and retention telemetry                      |
| Crash reporting       | **Firebase Crashlytics**                                   | Mobile production diagnostics                              |
| Dependency management | **Unity Package Manager + scoped registries where needed** | Reproducible dependencies                                  |
| Tests                 | **Unity Test Framework**                                   | Gameplay logic/edit-mode tests                             |
