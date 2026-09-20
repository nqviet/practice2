using System.Collections.Generic;
using System.IO;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    public static class ProjectSetup
    {
        private static readonly string[] s_RequiredFolders = new[]
        {
            "Assets/Animations/Board",
            "Assets/Animations/UI",
            "Assets/Audios/Music",
            "Assets/Audios/SFX/Ball",
            "Assets/Audios/SFX/Block",
            "Assets/Audios/SFX/Cannon",
            "Assets/Audios/SFX/UI",
            "Assets/Audios/Mixers",
            "Assets/GameData/Configs",
            "Assets/GameData/Blocks",
            "Assets/GameData/Balls",
            "Assets/GameData/Levels",
            "Assets/GameData/VFX",
            "Assets/Materials/Blocks",
            "Assets/Materials/Balls",
            "Assets/Materials/FX",
            "Assets/Materials/UI",
            "Assets/Particles/Textures",
            "Assets/Particles/Hyper Casual FX",
            "Assets/Plugins/Demigiant/DOTween",
            "Assets/Prefabs/Arena",
            "Assets/Prefabs/Blocks",
            "Assets/Prefabs/Balls",
            "Assets/Prefabs/Cannon",
            "Assets/Prefabs/Aim",
            "Assets/Prefabs/FX",
            "Assets/Prefabs/UI",
            "Assets/Prefabs/Popups",
            "Assets/Resources",
            "Assets/Scenes",
            "Assets/Scripts/Core",
            "Assets/Scripts/Runtime/Bootstrap",
            "Assets/Scripts/Runtime/GameFlow/States",
            "Assets/Scripts/Runtime/Board",
            "Assets/Scripts/Runtime/Cannon",
            "Assets/Scripts/Runtime/Ball",
            "Assets/Scripts/Runtime/Objectives",
            "Assets/Scripts/Runtime/Services",
            "Assets/Scripts/Runtime/UI",
            "Assets/Scripts/Editor/Builders",
            "Assets/Scripts/Utils",
            "Assets/Settings/Scenes",
            "Assets/Shaders/Graph",
            "Assets/Shaders/Include",
            "Assets/Sprites/Blocks",
            "Assets/Sprites/Balls",
            "Assets/Sprites/Cannon",
            "Assets/Sprites/Arena",
            "Assets/Sprites/FX",
            "Assets/Sprites/UI",
            "Assets/Sprites/GUI Pro-SuperCasual",
            "Assets/SpriteAtlases",
            "Assets/Tests/EditMode",
            "Assets/Tests/PlayMode",
            "Assets/TextMesh Pro",
            "Assets/Texture2D"
        };

        private static readonly string[] s_SortingLayers = new[]
        {
            GameConstants.SortingBackground,
            GameConstants.SortingGridSlots,
            GameConstants.SortingReturnLine,
            GameConstants.SortingBlocks,
            GameConstants.SortingBallTrail,
            GameConstants.SortingBalls,
            GameConstants.SortingFX,
            GameConstants.SortingAimGuide,
            GameConstants.SortingForeground
        };

        [MenuItem("Tools/Block Breaker/Run All Builders", false, 50)]
        public static void BuildAll()
        {
            Debug.Log("[ProjectSetup] Running all builders in sequence...");
            Run();
            SpriteForge.Run();
            PrefabBuilder.Run();
            LevelAssetBuilder.Run();
            SceneBuilder.Run();
            Debug.Log("[ProjectSetup] All builders finished successfully.");
        }

        [MenuItem("Tools/Block Breaker/Run Project Setup", false, 100)]
        public static void Run()
        {
            Debug.Log("[ProjectSetup] Starting idempotent project setup...");

            EnsureFolderSkeleton();
            ConfigureOrientationAndResolution();
            ConfigureFixedDeltaTime();
            ConfigureLayers();
            ConfigureSortingLayers();
            ConfigurePhysicsCollisionMatrix();
            RelocateGlobalAssets();
            ImportTmpEssentials();
            EnsureDotweenSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ProjectSetup] Project setup completed successfully.");
        }

        private static void EnsureFolderSkeleton()
        {
            foreach (string folder in s_RequiredFolders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static void ConfigureOrientationAndResolution()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;

            Debug.Log("[ProjectSetup] Orientation configured to Portrait (1080x1920 reference).");
        }

        private static void ConfigureFixedDeltaTime()
        {
            const float targetTimestep = 0.0166667f; // 1/60s
            Time.fixedDeltaTime = targetTimestep;

            UnityEngine.Object[] timeManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            if (timeManagerAssets != null && timeManagerAssets.Length > 0)
            {
                SerializedObject timeManager = new SerializedObject(timeManagerAssets[0]);
                SerializedProperty fixedTimestepProp = timeManager.FindProperty("Fixed Timestep");
                if (fixedTimestepProp != null)
                {
                    fixedTimestepProp.floatValue = targetTimestep;
                    timeManager.ApplyModifiedProperties();
                }
            }

            Debug.Log($"[ProjectSetup] Fixed delta time configured to {targetTimestep:F7} (60 Hz).");
        }

        private static void ConfigureLayers()
        {
            UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogWarning("[ProjectSetup] TagManager.asset not found.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (layersProp == null)
            {
                Debug.LogWarning("[ProjectSetup] layers property not found in TagManager.");
                return;
            }

            SetLayer(layersProp, GameConstants.BallLayer, GameConstants.BallLayerName);
            SetLayer(layersProp, GameConstants.BlockLayer, GameConstants.BlockLayerName);
            SetLayer(layersProp, GameConstants.WallLayer, GameConstants.WallLayerName);
            SetLayer(layersProp, GameConstants.SensorLayer, GameConstants.SensorLayerName);
            SetLayer(layersProp, GameConstants.FXLayer, GameConstants.FXLayerName);
            SetLayer(layersProp, GameConstants.UIWorldLayer, GameConstants.UIWorldLayerName);

            tagManager.ApplyModifiedProperties();
            Debug.Log("[ProjectSetup] Layers 8..13 configured (Ball, Block, Wall, Sensor, FX, UIWorld).");
        }

        private static void SetLayer(SerializedProperty layersProp, int index, string name)
        {
            if (index >= 0 && index < layersProp.arraySize)
            {
                SerializedProperty element = layersProp.GetArrayElementAtIndex(index);
                element.stringValue = name;
            }
        }

        private static void ConfigureSortingLayers()
        {
            UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayersProp == null) return;

            // Preserve Default layer and map existing layers
            var existingNames = new HashSet<string>();
            int maxId = 0;
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                SerializedProperty entry = sortingLayersProp.GetArrayElementAtIndex(i);
                string entryName = entry.FindPropertyRelative("name")?.stringValue;
                int entryId = entry.FindPropertyRelative("uniqueID")?.intValue ?? 0;
                if (!string.IsNullOrEmpty(entryName)) existingNames.Add(entryName);
                if (entryId > maxId) maxId = entryId;
            }

            foreach (string layerName in s_SortingLayers)
            {
                if (!existingNames.Contains(layerName))
                {
                    int newIndex = sortingLayersProp.arraySize;
                    sortingLayersProp.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty newEntry = sortingLayersProp.GetArrayElementAtIndex(newIndex);
                    maxId++;
                    newEntry.FindPropertyRelative("name").stringValue = layerName;
                    newEntry.FindPropertyRelative("uniqueID").intValue = maxId;
                    newEntry.FindPropertyRelative("locked").boolValue = false;
                }
            }

            tagManager.ApplyModifiedProperties();
            Debug.Log("[ProjectSetup] Sorting layers configured.");
        }

        private static void ConfigurePhysicsCollisionMatrix()
        {
            // Ball ↔ Block: yes
            Physics2D.IgnoreLayerCollision(GameConstants.BallLayer, GameConstants.BlockLayer, false);

            // Ball ↔ Wall: yes
            Physics2D.IgnoreLayerCollision(GameConstants.BallLayer, GameConstants.WallLayer, false);

            // Ball ↔ Ball: no (balls must not clump)
            Physics2D.IgnoreLayerCollision(GameConstants.BallLayer, GameConstants.BallLayer, true);

            // Ball ↔ Sensor: yes (trigger)
            Physics2D.IgnoreLayerCollision(GameConstants.BallLayer, GameConstants.SensorLayer, false);

            // Block ↔ Block: no (static board)
            Physics2D.IgnoreLayerCollision(GameConstants.BlockLayer, GameConstants.BlockLayer, true);

            // Block ↔ Wall: no (static board)
            Physics2D.IgnoreLayerCollision(GameConstants.BlockLayer, GameConstants.WallLayer, true);

            Debug.Log("[ProjectSetup] Physics2D layer collision matrix configured.");
        }

        private static void RelocateGlobalAssets()
        {
            MoveAssetIfExists("Assets/UniversalRenderPipelineGlobalSettings.asset", "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset");
            MoveAssetIfExists("Assets/DefaultVolumeProfile.asset", "Assets/Settings/DefaultVolumeProfile.asset");
        }

        private static void MoveAssetIfExists(string sourcePath, string destPath)
        {
            if (File.Exists(sourcePath))
            {
                if (File.Exists(destPath))
                {
                    AssetDatabase.DeleteAsset(destPath);
                }
                string error = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log($"[ProjectSetup] Moved {sourcePath} -> {destPath}");
                }
                else
                {
                    Debug.LogWarning($"[ProjectSetup] Could not move {sourcePath} to {destPath}: {error}");
                }
            }
        }

        private static void ImportTmpEssentials()
        {
            string tmpSettingsGuid = AssetDatabase.FindAssets("t:TMP_Settings").Length > 0 ? "found" : null;
            if (tmpSettingsGuid == null)
            {
                string packagePath = TMPro.EditorUtilities.TMP_EditorUtility.packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";
                if (File.Exists(packagePath))
                {
                    Debug.Log("[ProjectSetup] Importing TMP Essential Resources...");
                    AssetDatabase.ImportPackage(packagePath, false);
                }
                else
                {
                    Debug.LogWarning($"[ProjectSetup] TMP Essential package not found at: {packagePath}");
                }
            }
            else
            {
                Debug.Log("[ProjectSetup] TMP Essentials already present.");
            }
        }

        private static void EnsureDotweenSettings()
        {
            const string settingsPath = "Assets/Resources/DOTweenSettings.asset";
            if (!File.Exists(settingsPath))
            {
                if (!Directory.Exists("Assets/Resources"))
                {
                    Directory.CreateDirectory("Assets/Resources");
                }

                var settings = ScriptableObject.CreateInstance<DG.Tweening.Core.DOTweenSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                Debug.Log("[ProjectSetup] Created default DOTweenSettings at Assets/Resources/DOTweenSettings.asset");
            }
            else
            {
                Debug.Log("[ProjectSetup] DOTweenSettings already present.");
            }
        }
    }
}
