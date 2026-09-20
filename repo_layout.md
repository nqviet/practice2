# Repo Layout Plan

## 1. Conventions 

| # | Rule | Example |
|---|---|---|
| 1 | **Art is bucketed by asset type at the top level**, not by feature | `Sprites/`, `Materials/`, `Particles/`, `Mesh/`, `Texture2D/`, `Animations/`, `Audios/`, `Shaders/` |
| 2 | **Third-party is publisher-namespaced and never mixed with ours** | `Plugins/Demigiant/DOTween`, `Plugins/Sirenix`, `Plugins/Dreamteck/Splines`, `Sprites/GUI Pro-SuperCasual` |
| 3 | **Scripts split by role, not by feature** — `Core` (shared types) / `Runtime` (behaviours) / `Editor` (tools) / `Utils` (statics) | `Scripts/Core/GameEnums.cs`, `Scripts/Utils/Singleton.cs`, `Scripts/Editor/GridEditorWindow.cs` |
| 4 | **Authored content (SOs) is separated from code and from scenes** | `GameData/{Configs,Blocks,Balls,Levels,VFX}/`; the *type* (`LevelDefinition.cs`) lives in `Scripts/Runtime`. `Resources/` is package-mandated only — no gameplay content |
| 5 | **Render-pipeline / global config is centralized in `Settings/`** | `Settings/{UniversalRP,Renderer2D,DefaultVolumeProfile,Volume_Gameplay}` |
| 6 | **Runtime-spawned prefabs are flat-ish and few**; scene files are few and flat | `Prefabs/` = one folder per family (§2), `Scenes/` = 4 flat files |

## 2. Proposed `Assets/` layout

```
Assets/
├── Animations/                      # clips + controllers only (motion is mostly DOTween in code)
│   ├── Board/                       #   board intro, level-complete stagger
│   └── UI/                          #   popup transitions, button states
├── Audios/
│   ├── Music/
│   ├── SFX/{Ball,Block,Cannon,UI}/
│   └── Mixers/                      #   AudioMixer_Master.mixer → Master/Music/SFX/UI
├── GameData/                        # ← SO INSTANCES. All authored content lives here.
│   ├── Configs/                     #   GameplayConfig, AdConfig, AnalyticsConfig
│   ├── Blocks/                      #   BlockDefinition_Brick / _Steel / _Red / _Blue / _Yellow
│   ├── Balls/                       #   BallSkin_Red / _Blue / _Yellow / _White
│   ├── Levels/                      #   LevelDefinition_01, LevelPack_XX
│   └── VFX/                         #   VFXDefinition_Break_Red, _ImpactSpark, _MuzzleFlash
├── Materials/{Blocks,Balls,FX,UI}/  #   FX = additive/glow; 2D needs very few
├── Particles/                       # FX source assets + purchased FX packs
│   ├── Textures/
│   └── Hyper Casual FX/             #   third-party pack, kept intact for clean upgrades
├── Plugins/                         # non-UPM third-party only (publisher-namespaced)
│   └── Demigiant/DOTween/           #   UPM pkgs (Cinemachine, Addressables, IAP) stay in Packages/
├── Prefabs/
│   ├── Arena/                       #   Walls, ReturnLine, ArenaRig (GridOrigin is a scene object)
│   ├── Blocks/                      #   Block_Base + colour variants
│   ├── Balls/                       #   Ball_Base + skins
│   ├── Cannon/                      #   CannonRoot, Cannon_Base, Cannon_Head, MuzzlePoint
│   ├── Aim/                         #   AimReticle, SightLine (no AimDot — nothing is ever drawn ahead of the ball)
│   ├── FX/                          #   FX_Break_*, FX_MuzzleFlash, FX_ImpactSpark, TrailRun
│   ├── UI/                          #   HUD pieces: legend, BallPicker, Btn_*, AdBannerAnchor
│   └── Popups/                      #   Popup_Settings, Popup_LevelComplete, Popup_Shop
│                                    #   (restart is instant — there is no confirm dialog and no fail popup)
├── Resources/                       # ONLY package-mandated files. No gameplay content.
│   ├── DOTweenSettings.asset
│   └── (TMP settings if relocated)
├── Scenes/
│   ├── 00_Boot.unity
│   ├── 01_MainMenu.unity
│   ├── 02_Gameplay.unity           #   replaces SampleScene.unity
│   └── 99_Dev_BoardSandbox.unity    #   board authoring / validator target
├── Scripts/
│   ├── Core/                        # GameEnums (GameColor, BlockKind, GameState, SfxId), UIBase,
│   │                                #   IState, EventBus, ServiceLocator, Layers, SortingLayers,
│   │                                #   GameConstants   (no scene MonoBehaviours)
│   ├── Runtime/
│   │   ├── Bootstrap/               #   GameBootstrap, service registration
│   │   ├── GameFlow/                #   GameManager (FSM), SceneLoader, States/ (IState impls — no Lost)
│   │   ├── Board/                   #   BoardState, LevelController, Block, GridMath,
│   │                                #   GridAuthoring, ChainSolver, LevelValidator (pure, no Editor deps)
│   │   ├── Cannon/                  #   AimController, CannonController, SightLine
│   │   ├── Ball/                    #   Ball, BallStream (single-ball lifecycle — no budget, no cadence),
│   │                                #   BallTrail, BallPicker
│   │   ├── Objectives/              #   ObjectiveTracker (brick-clear win only — there is no fail state)
│   │   ├── Services/                #   PoolService, VFXService, AudioService, SaveService,
│   │                                #   AnalyticsService, AdController, IAPService, CameraShaker,
│   │                                #   CameraFitter (aspect / banner camera contract)
│   │   └── UI/                      #   UIManager, UIHud, UISettings, UIWin, SafeAreaRect
│   │                                #   (no UILose — the level cannot be failed)
│   ├── Editor/                      # LevelValidatorWindow (coverage+access), BoardGizmos,
│   │                                #   GridAuthoringTools, SyncCameraTool, BuildScript
│   │   └── Builders/                #   ProjectSetup, SceneBuilder, PrefabBuilder, SpriteForge,
│   │                                #   LevelAssetBuilder — every Unity object under Assets/ is
│   │                                #   created by an idempotent editor method, never hand-edited YAML
│   └── Utils/                       # Singleton, MathUtils, Extensions, Common
├── Settings/                        # ← all render/global config (example parity)
│   ├── UniversalRP.asset
│   ├── Renderer2D.asset
│   ├── UniversalRenderPipelineGlobalSettings.asset
│   ├── DefaultVolumeProfile.asset
│   ├── Volume_Gameplay.asset        #   Bloom + Tonemapping + Vignette + Color Adjustments
│   └── Scenes/                      #   scene template assets (already present)
├── Shaders/
│   ├── Graph/                       #   ShaderGraph: Additive, Glow, Dissolve, TrailFade
│   └── Include/                     #   .hlsl shared functions
├── Sprites/
│   ├── Blocks/  Balls/  Cannon/  Arena/  FX/  UI/
│   └── GUI Pro-SuperCasual/         #   third-party UI pack, untouched subtree
├── SpriteAtlases/                   # ← added: mobile draw-call control
│   ├── Atlas_Gameplay.spriteatlas
│   └── Atlas_UI.spriteatlas
├── Tests/
│   ├── EditMode/                    #   level validator, grid math, chain-reaction solver
│   └── PlayMode/                    #   constant-speed reflection, pool integrity
├── TextMesh Pro/                    #   TMP essentials (unchanged)
├── Texture2D/                       #   non-sprite textures: glow ramps, noise, gradients
├── AddressableAssetsData/           #   auto-created; .bin ignored by existing .gitignore
├── InputSystem_Actions.inputactions #   stays at Assets root (example parity)
└── UniversalRenderPipelineGlobalSettings.asset  # → moves into Settings/
```
