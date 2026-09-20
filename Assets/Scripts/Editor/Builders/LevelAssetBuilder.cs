using System.Collections.Generic;
using System.IO;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    public static class LevelAssetBuilder
    {
        public const string LevelPackPath = "Assets/GameData/Levels/LevelPack_01.asset";

        [MenuItem("Tools/Block Breaker/Run Level Asset Builder", false, 103)]
        public static void Run()
        {
            Debug.Log("[LevelAssetBuilder] Creating block definitions, palette, gameplay config, ball skins, and Level_01...");

            EnsureDirectories();

            var brickDef = CreateOrUpdateBlockDef("Assets/GameData/Blocks/BlockDefinition_Brick.asset", BlockKind.Brick, GameColor.None, 1, false, "Assets/Sprites/Blocks/Sprite_Brick.png");
            var steelDef = CreateOrUpdateBlockDef("Assets/GameData/Blocks/BlockDefinition_Steel.asset", BlockKind.Steel, GameColor.None, 1, true, "Assets/Sprites/Blocks/Sprite_Steel.png");
            var redDef = CreateOrUpdateBlockDef("Assets/GameData/Blocks/BlockDefinition_Red.asset", BlockKind.Colored, GameColor.Red, 1, false, "Assets/Sprites/Blocks/Sprite_Block_Red.png");
            var blueDef = CreateOrUpdateBlockDef("Assets/GameData/Blocks/BlockDefinition_Blue.asset", BlockKind.Colored, GameColor.Blue, 1, false, "Assets/Sprites/Blocks/Sprite_Block_Blue.png");
            var yellowDef = CreateOrUpdateBlockDef("Assets/GameData/Blocks/BlockDefinition_Yellow.asset", BlockKind.Colored, GameColor.Yellow, 1, false, "Assets/Sprites/Blocks/Sprite_Block_Yellow.png");

            var palette = CreateOrUpdatePalette("Assets/GameData/Configs/BlockPalette.asset", brickDef, steelDef, redDef, blueDef, yellowDef);

            var level01 = CreateOrUpdateLevel01("Assets/GameData/Levels/LevelDefinition_01.asset", palette);

            CreateOrUpdateGameplayConfig("Assets/GameData/Configs/GameplayConfig.asset");

            CreateOrUpdateBallSkin("Assets/GameData/Balls/BallSkin_White.asset", GameColor.White, "Assets/Sprites/Balls/Sprite_Ball_White.png", Color.white);
            CreateOrUpdateBallSkin("Assets/GameData/Balls/BallSkin_Red.asset", GameColor.Red, "Assets/Sprites/Balls/Sprite_Ball_Red.png", new Color(1f, 0.25f, 0.27f));
            CreateOrUpdateBallSkin("Assets/GameData/Balls/BallSkin_Blue.asset", GameColor.Blue, "Assets/Sprites/Balls/Sprite_Ball_Blue.png", new Color(0.2f, 0.55f, 1f));
            CreateOrUpdateBallSkin("Assets/GameData/Balls/BallSkin_Yellow.asset", GameColor.Yellow, "Assets/Sprites/Balls/Sprite_Ball_Yellow.png", new Color(1f, 0.85f, 0.15f));

            CreateOrUpdateLevelPack(LevelPackPath, level01);

            var report = LevelValidator.Validate(level01);
            Debug.Log(report.GenerateSummary());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelAssetBuilder] Level assets built successfully.");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/GameData/Blocks")) Directory.CreateDirectory("Assets/GameData/Blocks");
            if (!Directory.Exists("Assets/GameData/Configs")) Directory.CreateDirectory("Assets/GameData/Configs");
            if (!Directory.Exists("Assets/GameData/Levels")) Directory.CreateDirectory("Assets/GameData/Levels");
            if (!Directory.Exists("Assets/GameData/Balls")) Directory.CreateDirectory("Assets/GameData/Balls");
        }

        private static BlockDefinition CreateOrUpdateBlockDef(string path, BlockKind kind, GameColor color, int hp, bool indestructible, string spritePath)
        {
            var def = AssetDatabase.LoadAssetAtPath<BlockDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<BlockDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.Kind = kind;
            def.Color = color;
            def.Hp = hp;
            def.IsIndestructible = indestructible;
            def.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            EditorUtility.SetDirty(def);
            return def;
        }

        private static BlockPalette CreateOrUpdatePalette(string path, BlockDefinition brick, BlockDefinition steel, BlockDefinition red, BlockDefinition blue, BlockDefinition yellow)
        {
            var palette = AssetDatabase.LoadAssetAtPath<BlockPalette>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<BlockPalette>();
                AssetDatabase.CreateAsset(palette, path);
            }

            palette.AddOrUpdateEntry(new PaletteEntry('N', brick));
            palette.AddOrUpdateEntry(new PaletteEntry('S', steel));
            palette.AddOrUpdateEntry(new PaletteEntry('R', red));
            palette.AddOrUpdateEntry(new PaletteEntry('B', blue));
            palette.AddOrUpdateEntry(new PaletteEntry('Y', yellow));
            palette.AddOrUpdateEntry(new PaletteEntry('M', null, true, "Character 'M' (Armored Brick) is reserved, not implemented."));
            palette.AddOrUpdateEntry(new PaletteEntry('X', null, true, "Character 'X' (Mystery Block) is reserved, not implemented."));

            EditorUtility.SetDirty(palette);
            return palette;
        }

        public static LevelDefinition CreateOrUpdateLevel01(string path, BlockPalette palette)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(level, path);
            }

            level.LevelId = "Level_01";
            level.DisplayName = "LEVEL 1";
            level.ParShots = 10;
            level.Palette = palette;

            // 10 columns x 13 rows, top row first (row 12 down to row 0)
            level.Rows = new string[]
            {
                "SNY..BN.YS", // Row 12 (top)
                "SNN.NNN.NS", // Row 11
                "SNN.SSS.NS", // Row 10
                "SRR.....BS", // Row 9
                "SNN.SSS.NS", // Row 8
                "SNN.NNN.NS", // Row 7
                "SNY.BBN.YS", // Row 6
                "SNN.NNN.NS", // Row 5
                "SNN.NNN.NS", // Row 4
                "SNB..RN.YS", // Row 3
                "SNN.NNN.NS", // Row 2
                "..........", // Row 1 (Landing Lane)
                ".........."  // Row 0 (Landing Lane)
            };

            EditorUtility.SetDirty(level);
            return level;
        }

        /// <summary>Ordered levels for the meta loop; append new LevelDefinitions here as they are authored.</summary>
        public static LevelPack CreateOrUpdateLevelPack(string path, params LevelDefinition[] levels)
        {
            var pack = AssetDatabase.LoadAssetAtPath<LevelPack>(path);
            if (pack == null)
            {
                pack = ScriptableObject.CreateInstance<LevelPack>();
                AssetDatabase.CreateAsset(pack, path);
            }

            pack.PackId = Path.GetFileNameWithoutExtension(path);
            pack.SetLevels(levels);

            EditorUtility.SetDirty(pack);
            return pack;
        }

        public static GameplayConfig CreateOrUpdateGameplayConfig(string path)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameplayConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            config.BallRadius = 0.28f;
            config.BallSpeed = 14.0f;
            config.FastForwardMultiplier = 2.0f;
            config.AimClampFromUpDeg = 75.0f;
            config.MaxTrailRuns = 5;
            config.TrailRunFadeSec = 0.25f;
            config.TrailDissipateSec = 1.0f;
            config.ChainStaggerSec = 0.05f;
            config.DetonationFreezeSec = 0.04f;
            config.ReturnMargin = 0.35f;
            config.ShotTimeoutSec = 12.0f;
            config.CannonRailHalfWidth = 4.4f;
            config.CannonBottomPad = 1.8f;
            config.ShakeScale = 1.0f;
            config.Warmups = new[]
            {
                new PoolWarmup(PoolId.Ball, 1),
                new PoolWarmup(PoolId.TrailRun, 5),
                new PoolWarmup(PoolId.Block, 64)
            };

            EditorUtility.SetDirty(config);
            return config;
        }

        public static BallSkin CreateOrUpdateBallSkin(string path, GameColor color, string spritePath, Color glowColor)
        {
            var skin = AssetDatabase.LoadAssetAtPath<BallSkin>(path);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<BallSkin>();
                AssetDatabase.CreateAsset(skin, path);
            }

            skin.Color = color;
            skin.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            skin.GlowColor = glowColor;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(glowColor, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.05f, 0f),
                    new GradientAlphaKey(0.85f, 1f)
                }
            );
            skin.TrailGradient = grad;

            EditorUtility.SetDirty(skin);
            return skin;
        }
    }
}
