using System.IO;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Cannon;
using Game.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    public static class PrefabBuilder
    {
        private const string UnlitSpriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string LineMaterialPath = "Assets/Materials/FX/Mat_Line.mat";

        [MenuItem("Tools/Block Breaker/Run Prefab Builder", false, 102)]
        public static void Run()
        {
            Debug.Log("[PrefabBuilder] Building block, arena, ball, trail, cannon, and aim prefabs...");

            if (!Directory.Exists("Assets/Prefabs/Blocks")) Directory.CreateDirectory("Assets/Prefabs/Blocks");
            if (!Directory.Exists("Assets/Prefabs/Arena")) Directory.CreateDirectory("Assets/Prefabs/Arena");
            if (!Directory.Exists("Assets/Prefabs/Balls")) Directory.CreateDirectory("Assets/Prefabs/Balls");
            if (!Directory.Exists("Assets/Prefabs/FX")) Directory.CreateDirectory("Assets/Prefabs/FX");
            if (!Directory.Exists("Assets/Prefabs/Cannon")) Directory.CreateDirectory("Assets/Prefabs/Cannon");
            if (!Directory.Exists("Assets/Prefabs/Aim")) Directory.CreateDirectory("Assets/Prefabs/Aim");

            BuildBlockBasePrefab();
            BuildGridSlotPrefab();
            BuildTrailRunPrefab();
            BuildBallBasePrefab();
            BuildCannonPrefab();
            BuildAimReticlePrefab();
            BuildSightLinePrefab();

            BuildBreakBurstPrefab("Assets/Prefabs/FX/FX_Break_Red.prefab", PoolId.BreakFxRed, new Color(1f, 0.28f, 0.28f));
            BuildBreakBurstPrefab("Assets/Prefabs/FX/FX_Break_Blue.prefab", PoolId.BreakFxBlue, new Color(0.25f, 0.58f, 1f));
            BuildBreakBurstPrefab("Assets/Prefabs/FX/FX_Break_Yellow.prefab", PoolId.BreakFxYellow, new Color(1f, 0.88f, 0.2f));
            BuildBreakBurstPrefab("Assets/Prefabs/FX/FX_Break_Neutral.prefab", PoolId.BreakFxNeutral, new Color(0.9f, 0.9f, 0.92f));
            BuildImpactSparkPrefab("Assets/Prefabs/FX/FX_ImpactSpark.prefab");
            BuildMuzzleFlashPrefab("Assets/Prefabs/FX/FX_MuzzleFlash.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabBuilder] Prefabs built successfully.");
        }

        private static void BuildBlockBasePrefab()
        {
            const string prefabPath = "Assets/Prefabs/Blocks/Block_Base.prefab";

            var go = new GameObject("Block_Base");
            go.layer = GameConstants.BlockLayer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConstants.SortingBlocks;

            var defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Blocks/Sprite_Brick.png");
            if (defaultSprite != null)
            {
                sr.sprite = defaultSprite;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);

            go.AddComponent<Block>();
            var pooled = go.AddComponent<PooledInstance>();
            pooled.PoolId = PoolId.Block;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildGridSlotPrefab()
        {
            const string prefabPath = "Assets/Prefabs/Arena/GridSlot.prefab";

            var go = new GameObject("GridSlot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConstants.SortingGridSlots;

            var slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Arena/Sprite_GridSlot.png");
            if (slotSprite != null)
            {
                sr.sprite = slotSprite;
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildTrailRunPrefab()
        {
            const string prefabPath = "Assets/Prefabs/FX/TrailRun.prefab";

            var go = new GameObject("TrailRun");
            go.layer = GameConstants.FXLayer;

            var lr = go.AddComponent<LineRenderer>();
            lr.sortingLayerName = GameConstants.SortingBallTrail;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.24f;

            lr.sharedMaterial = GetOrCreateMaterial(LineMaterialPath, null);

            go.AddComponent<TrailRun>();
            var pooled = go.AddComponent<PooledInstance>();
            pooled.PoolId = PoolId.TrailRun;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildBallBasePrefab()
        {
            const string prefabPath = "Assets/Prefabs/Balls/Ball_Base.prefab";

            var go = new GameObject("Ball_Base");
            go.layer = GameConstants.BallLayer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConstants.SortingBalls;

            var ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Balls/Sprite_Ball_White.png");
            if (ballSprite != null)
            {
                sr.sprite = ballSprite;
            }

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.28f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0f;

            go.AddComponent<Ball>();
            var ballTrail = go.AddComponent<BallTrail>();

            var trailRunPrefab = AssetDatabase.LoadAssetAtPath<TrailRun>("Assets/Prefabs/FX/TrailRun.prefab");
            if (trailRunPrefab != null)
            {
                var serializedTrail = new SerializedObject(ballTrail);
                serializedTrail.FindProperty("m_TrailRunPrefab").objectReferenceValue = trailRunPrefab;
                serializedTrail.ApplyModifiedProperties();
            }

            var pooled = go.AddComponent<PooledInstance>();
            pooled.PoolId = PoolId.Ball;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildCannonPrefab()
        {
            const string prefabPath = "Assets/Prefabs/Cannon/CannonRoot.prefab";

            var rootGo = new GameObject("CannonRoot");
            var controller = rootGo.AddComponent<CannonController>();

            // Cannon Base
            var baseGo = new GameObject("Cannon_Base");
            baseGo.transform.SetParent(rootGo.transform, false);
            var baseSr = baseGo.AddComponent<SpriteRenderer>();
            baseSr.sortingLayerName = GameConstants.SortingForeground;
            var baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cannon/Sprite_Cannon_Base.png");
            if (baseSprite != null) baseSr.sprite = baseSprite;

            // Cannon Head (rotates toward aim)
            var headGo = new GameObject("Cannon_Head");
            headGo.transform.SetParent(rootGo.transform, false);
            headGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var headSr = headGo.AddComponent<SpriteRenderer>();
            headSr.sortingLayerName = GameConstants.SortingForeground;
            var headSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cannon/Sprite_Cannon_Head.png");
            if (headSprite != null) headSr.sprite = headSprite;

            // MuzzlePoint (child of head, rotates with barrel)
            var muzzleGo = new GameObject("MuzzlePoint");
            muzzleGo.transform.SetParent(headGo.transform, false);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            // Loaded Ball Visual at Muzzle
            var loadedBallGo = new GameObject("LoadedBallVisual");
            loadedBallGo.transform.SetParent(muzzleGo.transform, false);
            var loadedBallSr = loadedBallGo.AddComponent<SpriteRenderer>();
            loadedBallSr.sortingLayerName = GameConstants.SortingBalls;
            loadedBallSr.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            // Neutral white: CannonController tints it when no coloured skin sprite is supplied
            var ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Balls/Sprite_Ball_White.png");
            if (ballSprite != null) loadedBallSr.sprite = ballSprite;

            // Side arrow hints (mockup parity)
            var arrowLeftGo = new GameObject("Arrow_Left");
            arrowLeftGo.transform.SetParent(rootGo.transform, false);
            arrowLeftGo.transform.localPosition = new Vector3(-1.1f, 0f, 0f);
            var arrowLeftSr = arrowLeftGo.AddComponent<SpriteRenderer>();
            arrowLeftSr.sortingLayerName = GameConstants.SortingForeground;
            var arrowLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cannon/Sprite_Arrow_Left.png");
            if (arrowLeftSprite != null) arrowLeftSr.sprite = arrowLeftSprite;

            var arrowRightGo = new GameObject("Arrow_Right");
            arrowRightGo.transform.SetParent(rootGo.transform, false);
            arrowRightGo.transform.localPosition = new Vector3(1.1f, 0f, 0f);
            var arrowRightSr = arrowRightGo.AddComponent<SpriteRenderer>();
            arrowRightSr.sortingLayerName = GameConstants.SortingForeground;
            var arrowRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cannon/Sprite_Arrow_Right.png");
            if (arrowRightSprite != null) arrowRightSr.sprite = arrowRightSprite;

            var config = AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/GameData/Configs/GameplayConfig.asset");

            var serialized = new SerializedObject(controller);
            if (config != null) serialized.FindProperty("m_Config").objectReferenceValue = config;
            serialized.FindProperty("m_CannonRoot").objectReferenceValue = rootGo.transform;
            serialized.FindProperty("m_CannonBase").objectReferenceValue = baseGo.transform;
            serialized.FindProperty("m_CannonHead").objectReferenceValue = headGo.transform;
            serialized.FindProperty("m_MuzzlePoint").objectReferenceValue = muzzleGo.transform;
            serialized.FindProperty("m_LoadedBallRenderer").objectReferenceValue = loadedBallSr;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            Object.DestroyImmediate(rootGo);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildAimReticlePrefab()
        {
            const string prefabPath = "Assets/Prefabs/Aim/AimReticle.prefab";

            var go = new GameObject("AimReticle");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConstants.SortingAimGuide;

            var reticleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Aim/Sprite_AimReticle.png");
            if (reticleSprite != null)
            {
                sr.sprite = reticleSprite;
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildSightLinePrefab()
        {
            const string prefabPath = "Assets/Prefabs/Aim/SightLine.prefab";

            var go = new GameObject("SightLine");
            var lr = go.AddComponent<LineRenderer>();
            lr.sortingLayerName = GameConstants.SortingAimGuide;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.08f;

            lr.sharedMaterial = GetOrCreateMaterial(LineMaterialPath, null);

            go.AddComponent<SightLine>();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildBreakBurstPrefab(string prefabPath, PoolId id, Color color)
        {
            var go = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            go.layer = GameConstants.FXLayer;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 8.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = color;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;


            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sortingLayerName = GameConstants.SortingFX;
            var shardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/FX/Sprite_Shard.png");
            psr.sharedMaterial = GetOrCreateMaterial("Assets/Materials/FX/Mat_FX_Shard.mat", shardSprite != null ? shardSprite.texture : null);

            var pooled = go.AddComponent<PooledInstance>();
            pooled.PoolId = id;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildImpactSparkPrefab(string prefabPath)
        {
            var go = new GameObject("FX_ImpactSpark");
            go.layer = GameConstants.FXLayer;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
            main.startColor = new Color(1f, 0.95f, 0.8f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 4, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sortingLayerName = GameConstants.SortingFX;
            var sparkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/FX/Sprite_ImpactSpark.png");
            psr.sharedMaterial = GetOrCreateMaterial("Assets/Materials/FX/Mat_FX_ImpactSpark.mat", sparkSprite != null ? sparkSprite.texture : null);

            var pooled = go.AddComponent<PooledInstance>();
            pooled.PoolId = PoolId.ImpactSpark;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        private static void BuildMuzzleFlashPrefab(string prefabPath)
        {
            var go = new GameObject("FX_MuzzleFlash");
            go.layer = GameConstants.FXLayer;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.20f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startColor = new Color(1f, 0.9f, 0.6f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 6) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sortingLayerName = GameConstants.SortingFX;
            var muzzleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/FX/Sprite_MuzzleFlash.png");
            psr.sharedMaterial = GetOrCreateMaterial("Assets/Materials/FX/Mat_FX_MuzzleFlash.mat", muzzleSprite != null ? muzzleSprite.texture : null);

            var pooled = go.AddComponent<PooledInstance>();
            pooled.PoolId = PoolId.MuzzleFlash;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[PrefabBuilder] Created {prefabPath}");
        }

        /// <summary>
        /// Prefabs can only reference persisted materials — an in-memory `new Material()` is dropped
        /// on save and the renderer draws magenta. Reuses the asset (and its GUID) on rebuilds.
        /// </summary>
        public static Material GetOrCreateMaterial(string path, Texture texture)
        {
            Shader shader = Shader.Find(UnlitSpriteShader) ?? Shader.Find("Sprites/Default");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                string folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.mainTexture = texture;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
