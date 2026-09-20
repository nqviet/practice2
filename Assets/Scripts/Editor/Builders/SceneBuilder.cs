using System.Collections.Generic;
using System.IO;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Bootstrap;
using Game.Runtime.Cannon;
using Game.Runtime.GameFlow;
using Game.Runtime.Objectives;
using Game.Runtime.Services;
using Game.Runtime.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    public static class SceneBuilder
    {
        private static readonly string[] s_BuildScenePaths = new[]
        {
            "Assets/Scenes/00_Boot.unity",
            "Assets/Scenes/01_MainMenu.unity",
            "Assets/Scenes/02_Gameplay.unity"
        };

        private const string SandboxScenePath = "Assets/Scenes/99_Dev_BoardSandbox.unity";

        // Canvas units at 1080 wide = 100 per world unit; keep in sync with GameplayConfig.CannonBottomPad
        private const float BottomBarHeight = 120f;

        [MenuItem("Tools/Block Breaker/Run Scene Builder", false, 104)]
        public static void Run()
        {
            Debug.Log("[SceneBuilder] Building scene hierarchy...");

            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
            }

            // Remove legacy SampleScene if present
            const string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(sampleScenePath))
            {
                AssetDatabase.DeleteAsset(sampleScenePath);
                Debug.Log("[SceneBuilder] Removed legacy SampleScene.unity");
            }

            VolumeProfile volumeProfile = EnsureVolumeGameplayProfile();

            // Popup prefabs must exist before any scene instantiates them
            UIBuilder.Run();

            BuildBootScene("Assets/Scenes/00_Boot.unity");
            BuildMainMenuScene("Assets/Scenes/01_MainMenu.unity");

            // Build Sandbox Scene (99_Dev_BoardSandbox.unity)
            BuildBoardScene(SandboxScenePath, volumeProfile, isSandbox: true);

            // Build Gameplay Scene (02_Gameplay.unity)
            BuildBoardScene("Assets/Scenes/02_Gameplay.unity", volumeProfile, isSandbox: false);

            // Configure EditorBuildSettings: 00_Boot, 01_MainMenu, 02_Gameplay (excluding 99_Dev_BoardSandbox)
            var buildScenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/00_Boot.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/01_MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/02_Gameplay.unity", true)
            };

            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[SceneBuilder] EditorBuildSettings configured with 00_Boot, 01_MainMenu, 02_Gameplay (99_Dev_BoardSandbox excluded).");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] Scene builder completed successfully.");
        }

        private static VolumeProfile EnsureVolumeGameplayProfile()
        {
            const string profilePath = "Assets/Settings/Volume_Gameplay.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();

                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.value = 0.8f;
                bloom.intensity.overrideState = true;
                bloom.threshold.value = 0.9f;
                bloom.threshold.overrideState = true;
                bloom.scatter.value = 0.7f;
                bloom.scatter.overrideState = true;

                var tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.value = TonemappingMode.Neutral;
                tonemapping.mode.overrideState = true;

                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.value = 0.25f;
                vignette.intensity.overrideState = true;

                var colorAdjustments = profile.Add<ColorAdjustments>(true);
                colorAdjustments.postExposure.value = 0.2f;
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.contrast.value = 10f;
                colorAdjustments.contrast.overrideState = true;

                AssetDatabase.CreateAsset(profile, profilePath);
                Debug.Log($"[SceneBuilder] Created VolumeProfile at {profilePath}");
            }
            return profile;
        }

        private static Camera CreateMenuCamera(Transform parent)
        {
            var camGo = new GameObject("MainCamera");
            camGo.transform.SetParent(parent);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            return cam;
        }

        private static void CreateEventSystem(Transform parent)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.transform.SetParent(parent);
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Canvas_Popups (sort 200) → PopupHost with UIManager and the given popup prefabs.</summary>
        private static UIManager CreatePopupHost(Transform uiRoot, params string[] popupPrefabPaths)
        {
            Canvas popupCanvas = UIBuilder.CreateCanvas("Canvas_Popups", uiRoot, 200);

            RectTransform host = UIBuilder.CreateRect("PopupHost", popupCanvas.transform);
            UIBuilder.Stretch(host);
            var uiManager = host.gameObject.AddComponent<UIManager>();

            var popups = new List<UIPopup>();
            foreach (string path in popupPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[SceneBuilder] Popup prefab missing at {path}");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host);
                instance.SetActive(false);
                popups.Add(instance.GetComponent<UIPopup>());
            }

            uiManager.SetPopups(popups);
            return uiManager;
        }

        private static void BuildBootScene(string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootGo = new GameObject("[BOOT]");
            bootGo.AddComponent<GameBootstrap>();

            var camerasGo = new GameObject("[CAMERAS]");
            CreateMenuCamera(camerasGo.transform);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneBuilder] Successfully built Boot scene at {scenePath}");
        }

        private static void BuildMainMenuScene(string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camerasGo = new GameObject("[CAMERAS]");
            CreateMenuCamera(camerasGo.transform);

            var uiGo = new GameObject("[UI]");
            CreateEventSystem(uiGo.transform);

            Canvas menuCanvas = UIBuilder.CreateCanvas("Canvas_Menu", uiGo.transform, 100);
            var safeAreaGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaRect));
            safeAreaGo.transform.SetParent(menuCanvas.transform, false);
            UIBuilder.Stretch(safeAreaGo.GetComponent<RectTransform>());

            RectTransform menuRoot = UIBuilder.CreateRect("MenuRoot", safeAreaGo.transform);
            UIBuilder.Stretch(menuRoot);
            menuRoot.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI title = UIBuilder.CreateText("Txt_Title", menuRoot, "BLOCK\nBREAKER", 150f, UIBuilder.TextColor, FontStyles.Bold);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1000f, 400f));
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Decorative R / B / Y dots echoing the gameplay legend
            RectTransform dots = UIBuilder.CreateRect("TitleDots", menuRoot);
            UIBuilder.Place(dots, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(400f, 70f));
            dots.pivot = new Vector2(0.5f, 0.5f);
            var dotsLayout = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 40f;
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.childControlWidth = false;
            dotsLayout.childControlHeight = false;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;
            Sprite circle = UIBuilder.LoadSprite(UISpriteForge.CirclePath);
            foreach (GameColor color in new[] { GameColor.Red, GameColor.Blue, GameColor.Yellow })
            {
                Image dot = UIBuilder.CreateImage($"Dot_{color}", dots, circle, color.ToDisplayColor());
                dot.rectTransform.sizeDelta = new Vector2(64f, 64f);
                dot.raycastTarget = false;
            }

            TextMeshProUGUI levelText = UIBuilder.CreateText("Txt_Level", menuRoot, "LEVEL 1", 64f, UIBuilder.SubtleTextColor, FontStyles.Bold);
            UIBuilder.Place(levelText.rectTransform, new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(800f, 100f));
            levelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Button playButton = UIBuilder.CreateButton("Btn_Play", menuRoot, "PLAY", UIBuilder.PlayColor, 88f, out _);
            RectTransform playRt = playButton.GetComponent<RectTransform>();
            UIBuilder.Place(playRt, new Vector2(0.5f, 0.33f), Vector2.zero, new Vector2(620f, 190f));
            playRt.pivot = new Vector2(0.5f, 0.5f);

            Button settingsButton = UIBuilder.CreateIconButton("Btn_Settings", menuRoot, UIBuilder.LoadSprite(UISpriteForge.GearPath), UIBuilder.HudButtonColor, new Vector2(84f, 84f), out _);
            UIBuilder.Place(settingsButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(130f, 130f));

            UIManager uiManager = CreatePopupHost(uiGo.transform, UIBuilder.PopupSettingsPath);

            var levelPack = AssetDatabase.LoadAssetAtPath<LevelPack>(LevelAssetBuilder.LevelPackPath);
            var mainMenu = menuRoot.gameObject.AddComponent<UIMainMenu>();
            mainMenu.SetReferences(levelPack, uiManager);
            mainMenu.SetWidgets(levelText, playButton, settingsButton);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneBuilder] Successfully built MainMenu scene at {scenePath}");
        }

        /// <summary>
        /// HUD TopBar (scene_structure.md §2/§7): Btn_Settings · Txt_LevelTitle · Btn_Restart, legend below.
        /// Height matches the camera's top margin (CameraMath.DefaultTopMargin ≈ 180 canvas px at 1080 wide).
        /// </summary>
        private static UIHud BuildTopBar(Transform safeAreaRoot, LevelDefinition level)
        {
            RectTransform topBar = UIBuilder.CreateRect("TopBar", safeAreaRoot);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = Vector2.zero;
            topBar.sizeDelta = new Vector2(0f, 180f);

            Button settingsButton = UIBuilder.CreateIconButton("Btn_Settings", topBar, UIBuilder.LoadSprite(UISpriteForge.GearPath), UIBuilder.HudButtonColor, new Vector2(84f, 84f), out _);
            UIBuilder.Place(settingsButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(130f, 130f));

            string title = level != null ? level.DisplayName : "LEVEL 1";
            TextMeshProUGUI levelTitle = UIBuilder.CreateText("Txt_LevelTitle", topBar, title, 76f, UIBuilder.TextColor, FontStyles.Bold);
            UIBuilder.Place(levelTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(600f, 96f));

            Button restartButton = UIBuilder.CreateIconButton("Btn_Restart", topBar, UIBuilder.LoadSprite(UISpriteForge.RestartPath), UIBuilder.HudButtonColor, new Vector2(84f, 84f), out RectTransform restartIcon);
            UIBuilder.Place(restartButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(130f, 130f));

            RectTransform legend = UIBuilder.CreateRect("Legend", topBar);
            UIBuilder.Place(legend, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 60f));
            var legendLayout = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            legendLayout.spacing = 16f;
            legendLayout.childAlignment = TextAnchor.MiddleCenter;
            legendLayout.childControlWidth = true;
            legendLayout.childControlHeight = true;
            legendLayout.childForceExpandWidth = false;
            legendLayout.childForceExpandHeight = false;

            var hud = topBar.gameObject.AddComponent<UIHud>();
            hud.SetWidgets(settingsButton, restartButton, restartIcon, levelTitle, legend, UIBuilder.LoadSprite(UISpriteForge.CirclePath));

            IReadOnlyList<GameColor> colors = level != null && level.ColorsUsed.Count > 0
                ? level.ColorsUsed
                : new[] { GameColor.Red, GameColor.Blue, GameColor.Yellow };
            hud.BuildLegend(colors);
            return hud;
        }

        private static void BuildBoardScene(string scenePath, VolumeProfile volumeProfile, bool isSandbox)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. [SYSTEMS]
            var systemsGo = new GameObject("[SYSTEMS]");
            var gameManager = systemsGo.AddComponent<GameManager>();
            var levelController = systemsGo.AddComponent<LevelController>();
            var poolService = systemsGo.AddComponent<PoolService>();
            var vfxService = systemsGo.AddComponent<VFXService>();
            var audioService = systemsGo.AddComponent<AudioService>();
            var cameraShaker = systemsGo.AddComponent<CameraShaker>();
            systemsGo.AddComponent<CinemachineImpulseSource>();
            var objectiveTracker = systemsGo.AddComponent<ObjectiveTracker>();
            var ballStream = systemsGo.AddComponent<BallStream>();
            var adController = systemsGo.AddComponent<AdController>();
            var aimController = systemsGo.AddComponent<AimController>();
            var inputReader = systemsGo.AddComponent<InputReader>();
            var levelSession = systemsGo.AddComponent<LevelSession>();

            // 2. [CAMERAS]
            var camerasGo = new GameObject("[CAMERAS]");
            var camGo = new GameObject("MainCamera");
            camGo.transform.SetParent(camerasGo.transform);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 4.0f, -10f);

            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f); // Dark Slate / Navy
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineImpulseListener>();
            var cameraFitter = camGo.AddComponent<CameraFitter>();

            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
            }

            // 3. [RENDERING]
            var renderingGo = new GameObject("[RENDERING]");
            var volumeGo = new GameObject("Global Volume");
            volumeGo.transform.SetParent(renderingGo.transform);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = volumeProfile;

            var backdropGo = new GameObject("Backdrop");
            backdropGo.transform.SetParent(renderingGo.transform);

            // Populate Grid Slots in Backdrop
            var slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Arena/Sprite_GridSlot.png");
            Vector3 originPos = new Vector3(GameConstants.GridOriginX, GameConstants.GridOriginY, 0f);

            var gridSlotsParent = new GameObject("GridSlots");
            gridSlotsParent.transform.SetParent(backdropGo.transform);

            for (int col = 0; col < GameConstants.GridColumns; col++)
            {
                for (int row = 0; row < GameConstants.GridRows; row++)
                {
                    var slotGo = new GameObject($"Slot_{col}_{row}");
                    slotGo.transform.SetParent(gridSlotsParent.transform);
                    slotGo.transform.position = GridMath.CellToWorld(new Vector2Int(col, row), originPos);
                    var sr = slotGo.AddComponent<SpriteRenderer>();
                    sr.sprite = slotSprite;
                    sr.sortingLayerName = GameConstants.SortingGridSlots;
                }
            }

            // 4. [LEVEL]
            var levelGo = new GameObject("[LEVEL]");
            var arenaGo = new GameObject("Arena");
            arenaGo.transform.SetParent(levelGo.transform);

            var gridOriginGo = new GameObject("GridOrigin");
            gridOriginGo.transform.SetParent(arenaGo.transform);
            gridOriginGo.transform.position = originPos;

            var wallsGo = new GameObject("Walls");
            wallsGo.transform.SetParent(arenaGo.transform);

            // Wall_Left
            var wallLeftGo = new GameObject("Wall_Left");
            wallLeftGo.transform.SetParent(wallsGo.transform);
            wallLeftGo.layer = GameConstants.WallLayer;
            wallLeftGo.transform.position = new Vector3(-5.6f, 6.5f, 0f);
            var colLeft = wallLeftGo.AddComponent<BoxCollider2D>();
            colLeft.size = new Vector2(1.2f, 15f);

            // Wall_Right
            var wallRightGo = new GameObject("Wall_Right");
            wallRightGo.transform.SetParent(wallsGo.transform);
            wallRightGo.layer = GameConstants.WallLayer;
            wallRightGo.transform.position = new Vector3(5.6f, 6.5f, 0f);
            var colRight = wallRightGo.AddComponent<BoxCollider2D>();
            colRight.size = new Vector2(1.2f, 15f);

            // Wall_Top
            var wallTopGo = new GameObject("Wall_Top");
            wallTopGo.transform.SetParent(wallsGo.transform);
            wallTopGo.layer = GameConstants.WallLayer;
            wallTopGo.transform.position = new Vector3(0f, 13.6f, 0f);
            var colTop = wallTopGo.AddComponent<BoxCollider2D>();
            colTop.size = new Vector2(12.4f, 1.2f);

            // ReturnLine
            var returnLineGo = new GameObject("ReturnLine");
            returnLineGo.transform.SetParent(arenaGo.transform);
            returnLineGo.layer = GameConstants.SensorLayer;
            returnLineGo.transform.position = new Vector3(0f, GameConstants.ReturnLineY, 0f);

            var returnCol = returnLineGo.AddComponent<BoxCollider2D>();
            returnCol.isTrigger = true;
            returnCol.size = new Vector2(12f, 1.0f);

            var returnSr = returnLineGo.AddComponent<SpriteRenderer>();
            var returnLineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Arena/Sprite_ReturnLine.png");
            if (returnLineSprite != null)
            {
                returnSr.sprite = returnLineSprite;
            }
            returnSr.sortingLayerName = GameConstants.SortingReturnLine;
            returnSr.drawMode = SpriteDrawMode.Tiled;
            returnSr.size = new Vector2(11f, 0.25f);

            // Blocks and Obstacles parents
            var blocksParentGo = new GameObject("Blocks");
            blocksParentGo.transform.SetParent(arenaGo.transform);

            var obstaclesParentGo = new GameObject("Obstacles");
            obstaclesParentGo.transform.SetParent(arenaGo.transform);

            // Cannon & Muzzle
            var cannonGo = new GameObject("Cannon");
            cannonGo.transform.SetParent(levelGo.transform);

            var cannonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cannon/CannonRoot.prefab");
            GameObject cannonRootGo;
            if (cannonPrefab != null)
            {
                cannonRootGo = (GameObject)PrefabUtility.InstantiatePrefab(cannonPrefab, cannonGo.transform);
            }
            else
            {
                cannonRootGo = new GameObject("CannonRoot");
                cannonRootGo.transform.SetParent(cannonGo.transform);
                cannonRootGo.AddComponent<CannonController>();
            }
            cannonRootGo.transform.position = new Vector3(0f, -3.0f, 0f);
            var cannonController = cannonRootGo.GetComponent<CannonController>();
            var muzzleTransform = cannonController != null ? cannonController.MuzzlePoint : cannonRootGo.transform;

            // AimRig (SightLine + AimReticle)
            var aimRigGo = new GameObject("AimRig");
            aimRigGo.transform.SetParent(levelGo.transform);

            var sightLinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Aim/SightLine.prefab");
            GameObject sightLineGo;
            if (sightLinePrefab != null)
            {
                sightLineGo = (GameObject)PrefabUtility.InstantiatePrefab(sightLinePrefab, aimRigGo.transform);
            }
            else
            {
                sightLineGo = new GameObject("SightLine");
                sightLineGo.transform.SetParent(aimRigGo.transform);
                sightLineGo.AddComponent<SightLine>();
            }
            var sightLine = sightLineGo.GetComponent<SightLine>();

            var reticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Aim/AimReticle.prefab");
            GameObject reticleGo;
            if (reticlePrefab != null)
            {
                reticleGo = (GameObject)PrefabUtility.InstantiatePrefab(reticlePrefab, aimRigGo.transform);
            }
            else
            {
                reticleGo = new GameObject("AimReticle");
                reticleGo.transform.SetParent(aimRigGo.transform);
            }

            // Balls parent
            var ballsParentGo = new GameObject("Balls");
            ballsParentGo.transform.SetParent(levelGo.transform);

            // 5. [VFX_POOL]
            var vfxPoolGo = new GameObject("[VFX_POOL]");

            // Load assets
            var blockPrefab = AssetDatabase.LoadAssetAtPath<Block>("Assets/Prefabs/Blocks/Block_Base.prefab");
            var level01 = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GameData/Levels/LevelDefinition_01.asset");
            var ballPrefab = AssetDatabase.LoadAssetAtPath<Ball>("Assets/Prefabs/Balls/Ball_Base.prefab");
            var trailRunPrefab = AssetDatabase.LoadAssetAtPath<TrailRun>("Assets/Prefabs/FX/TrailRun.prefab");
            var gameplayConfig = AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/GameData/Configs/GameplayConfig.asset");

            var ballSkinWhite = AssetDatabase.LoadAssetAtPath<BallSkin>("Assets/GameData/Balls/BallSkin_White.asset");
            var ballSkinRed = AssetDatabase.LoadAssetAtPath<BallSkin>("Assets/GameData/Balls/BallSkin_Red.asset");
            var ballSkinBlue = AssetDatabase.LoadAssetAtPath<BallSkin>("Assets/GameData/Balls/BallSkin_Blue.asset");
            var ballSkinYellow = AssetDatabase.LoadAssetAtPath<BallSkin>("Assets/GameData/Balls/BallSkin_Yellow.asset");

            var skinsList = new List<BallSkin>();
            if (ballSkinWhite != null) skinsList.Add(ballSkinWhite);
            if (ballSkinRed != null) skinsList.Add(ballSkinRed);
            if (ballSkinBlue != null) skinsList.Add(ballSkinBlue);
            if (ballSkinYellow != null) skinsList.Add(ballSkinYellow);

            var breakRedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FX/FX_Break_Red.prefab");
            var breakBluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FX/FX_Break_Blue.prefab");
            var breakYellowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FX/FX_Break_Yellow.prefab");
            var breakNeutralPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FX/FX_Break_Neutral.prefab");
            var impactSparkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FX/FX_ImpactSpark.prefab");
            var muzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FX/FX_MuzzleFlash.prefab");
            var radialFlashSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/FX/Sprite_RadialFlash.png");

            // Wire LevelController
            levelController.SetReferences(gridOriginGo.transform, blocksParentGo.transform, obstaclesParentGo.transform, blockPrefab, gameplayConfig);
            var serializedController = new SerializedObject(levelController);
            serializedController.FindProperty("m_GridOrigin").objectReferenceValue = gridOriginGo.transform;
            serializedController.FindProperty("m_BlocksParent").objectReferenceValue = blocksParentGo.transform;
            serializedController.FindProperty("m_ObstaclesParent").objectReferenceValue = obstaclesParentGo.transform;
            if (blockPrefab != null) serializedController.FindProperty("m_BlockPrefab").objectReferenceValue = blockPrefab;
            if (level01 != null) serializedController.FindProperty("m_CurrentLevel").objectReferenceValue = level01;
            if (gameplayConfig != null) serializedController.FindProperty("m_Config").objectReferenceValue = gameplayConfig;
            serializedController.ApplyModifiedProperties();

            // Wire PoolService
            var serializedPool = new SerializedObject(poolService);
            serializedPool.FindProperty("m_DefaultPoolRoot").objectReferenceValue = vfxPoolGo.transform;
            var poolsProp = serializedPool.FindProperty("m_ConfiguredPools");
            poolsProp.ClearArray();

            void AddPoolEntry(PoolId id, GameObject prefab, int capacity)
            {
                if (prefab == null) return;
                int idx = poolsProp.arraySize;
                poolsProp.InsertArrayElementAtIndex(idx);
                var elem = poolsProp.GetArrayElementAtIndex(idx);
                elem.FindPropertyRelative("Id").enumValueIndex = (int)id;
                elem.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
                elem.FindPropertyRelative("InitialCapacity").intValue = capacity;
            }

            if (ballPrefab != null) AddPoolEntry(PoolId.Ball, ballPrefab.gameObject, 1);
            if (trailRunPrefab != null) AddPoolEntry(PoolId.TrailRun, trailRunPrefab.gameObject, 5);
            if (blockPrefab != null) AddPoolEntry(PoolId.Block, blockPrefab.gameObject, 64);
            if (breakRedPrefab != null) AddPoolEntry(PoolId.BreakFxRed, breakRedPrefab, 4);
            if (breakBluePrefab != null) AddPoolEntry(PoolId.BreakFxBlue, breakBluePrefab, 4);
            if (breakYellowPrefab != null) AddPoolEntry(PoolId.BreakFxYellow, breakYellowPrefab, 4);
            if (breakNeutralPrefab != null) AddPoolEntry(PoolId.BreakFxNeutral, breakNeutralPrefab, 8);
            if (impactSparkPrefab != null) AddPoolEntry(PoolId.ImpactSpark, impactSparkPrefab, 8);
            if (muzzleFlashPrefab != null) AddPoolEntry(PoolId.MuzzleFlash, muzzleFlashPrefab, 2);
            serializedPool.ApplyModifiedProperties();

            // Wire VFXService & CameraShaker & ObjectiveTracker & GameManager
            vfxService.SetReferences(poolService, radialFlashSprite);
            cameraShaker.SetReferences(gameplayConfig, cam);
            objectiveTracker.SetReferences(levelController);
            gameManager.SetReferences(levelController, ballStream, objectiveTracker);
            objectiveTracker.SetBallStream(ballStream);

            // Wire BallStream
            ballStream.SetReferences(gameplayConfig, ballPrefab, muzzleTransform, levelController, poolService);
            ballStream.SetSkins(skinsList);

            var serializedBallStream = new SerializedObject(ballStream);
            if (gameplayConfig != null) serializedBallStream.FindProperty("m_Config").objectReferenceValue = gameplayConfig;
            if (ballPrefab != null) serializedBallStream.FindProperty("m_BallPrefab").objectReferenceValue = ballPrefab;
            serializedBallStream.FindProperty("m_MuzzlePoint").objectReferenceValue = muzzleTransform;
            serializedBallStream.FindProperty("m_LevelController").objectReferenceValue = levelController;
            serializedBallStream.FindProperty("m_PoolService").objectReferenceValue = poolService;

            var skinsProp = serializedBallStream.FindProperty("m_BallSkins");
            skinsProp.ClearArray();
            for (int i = 0; i < skinsList.Count; i++)
            {
                skinsProp.InsertArrayElementAtIndex(i);
                skinsProp.GetArrayElementAtIndex(i).objectReferenceValue = skinsList[i];
            }
            serializedBallStream.ApplyModifiedProperties();

            // Wire AimController
            aimController.SetReferences(gameplayConfig, cannonController, sightLine, reticleGo.transform);

            // Wire InputReader
            inputReader.SetReferences(cam, aimController, cannonController, ballStream);

            // Wire CameraFitter
            cameraFitter.SetReferences(cam, cannonRootGo.transform, gameplayConfig, adController);

            // 6. [UI]
            var uiGo = new GameObject("[UI]");

            CreateEventSystem(uiGo.transform);

            var hudGo = UIBuilder.CreateCanvas("Canvas_HUD", uiGo.transform, 100).gameObject;

            // SafeAreaRoot
            var safeAreaGo = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaRect));
            safeAreaGo.transform.SetParent(hudGo.transform, false);
            UIBuilder.Stretch(safeAreaGo.GetComponent<RectTransform>());

            // TopBar: settings, level title, restart, static legend
            UIHud hud = BuildTopBar(safeAreaGo.transform, level01);

            // BottomBar: a strip under the cannon, lifted above the banner reserve by BannerInset.
            // Its height is what GameplayConfig.CannonBottomPad reserves below the cannon.
            var bottomBarGo = new GameObject("BottomBar", typeof(RectTransform), typeof(BannerInset));
            bottomBarGo.transform.SetParent(safeAreaGo.transform, false);
            var bottomRt = bottomBarGo.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0f, 0f);
            bottomRt.anchorMax = new Vector2(1f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.anchoredPosition = Vector2.zero;
            bottomRt.sizeDelta = new Vector2(0f, BottomBarHeight);
            bottomBarGo.GetComponent<BannerInset>().SetReferences(adController, cam);

            // BallPicker: round swatches, bottom-left of the tray
            var ballPickerGo = new GameObject("BallPicker", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(BallPicker));
            ballPickerGo.transform.SetParent(bottomBarGo.transform, false);
            var pickerRt = ballPickerGo.GetComponent<RectTransform>();
            pickerRt.anchorMin = new Vector2(0f, 0f);
            pickerRt.anchorMax = new Vector2(0f, 1f);
            pickerRt.pivot = new Vector2(0f, 0.5f);
            pickerRt.anchoredPosition = new Vector2(48f, 0f);
            pickerRt.sizeDelta = new Vector2(520f, 0f);
            var layout = ballPickerGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 36f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var ballPicker = ballPickerGo.GetComponent<BallPicker>();
            ballPicker.SetReferences(ballStream, cannonController);
            ballPicker.SetItemSprite(UIBuilder.LoadSprite(UISpriteForge.CirclePath));
            ballPicker.Build(new[] { GameColor.Red, GameColor.Blue, GameColor.Yellow });

            // Btn_Fire: prominent, bottom-right of the tray (technical_design.md §6.3)
            Button fireButton = UIBuilder.CreateButton("Btn_Fire", bottomBarGo.transform, "FIRE", UIBuilder.AccentColor, 60f, out _);
            fireButton.gameObject.AddComponent<FireButton>();
            UIBuilder.Place(fireButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(300f, 104f));

            // AdBannerAnchor: hangs directly below the tray, over the banner reserve
            var bannerAnchorGo = new GameObject("AdBannerAnchor", typeof(RectTransform));
            bannerAnchorGo.transform.SetParent(bottomBarGo.transform, false);
            var bannerRt = bannerAnchorGo.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0f);
            bannerRt.anchorMax = new Vector2(0.5f, 0f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.sizeDelta = new Vector2(320f, 50f);
            bannerRt.anchoredPosition = Vector2.zero;

            // ScreenFeedback (fullscreen flash and vignette overlay, RaycastTarget = false)
            var screenFeedbackGo = new GameObject("ScreenFeedback", typeof(RectTransform), typeof(Image), typeof(ScreenFeedback));
            screenFeedbackGo.transform.SetParent(hudGo.transform, false);
            var sfRt = screenFeedbackGo.GetComponent<RectTransform>();
            sfRt.anchorMin = Vector2.zero;
            sfRt.anchorMax = Vector2.one;
            sfRt.offsetMin = Vector2.zero;
            sfRt.offsetMax = Vector2.zero;
            var sfImg = screenFeedbackGo.GetComponent<Image>();
            sfImg.color = Color.clear;
            sfImg.raycastTarget = false;

            // Canvas_Popups → PopupHost: Settings (pause menu) + LevelComplete. No fail / confirm popups.
            UIManager uiManager = CreatePopupHost(uiGo.transform, UIBuilder.PopupSettingsPath, UIBuilder.PopupLevelCompletePath);
            hud.SetReferences(levelController, uiManager);

            var levelPack = AssetDatabase.LoadAssetAtPath<LevelPack>(LevelAssetBuilder.LevelPackPath);
            levelSession.SetReferences(levelPack, levelController, objectiveTracker, ballPicker, uiManager);

            // In sandbox scene, build the board immediately so it's fully populated
            if (isSandbox && level01 != null)
            {
                levelController.Build(level01);
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneBuilder] Successfully built {(isSandbox ? "Sandbox" : "Gameplay")} scene at {scenePath}");
        }
    }
}
