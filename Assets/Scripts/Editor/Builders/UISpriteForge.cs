using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Procedural white UI sprites (tinted by Image.color): 9-slice rounded panel, disc,
    /// gear and restart icons. Idempotent — regenerates and re-imports on every run.
    /// </summary>
    public static class UISpriteForge
    {
        public const string PanelPath = "Assets/Sprites/UI/Sprite_UI_Panel.png";
        public const string CirclePath = "Assets/Sprites/UI/Sprite_UI_Circle.png";
        public const string GearPath = "Assets/Sprites/UI/Sprite_UI_Gear.png";
        public const string RestartPath = "Assets/Sprites/UI/Sprite_UI_Restart.png";

        private const int IconSize = 128;
        private const int PanelSize = 128;
        private const int PanelRadius = 36;
        private const float UIPPU = 100f;

        [MenuItem("Tools/Block Breaker/Run UI Sprite Forge", false, 102)]
        public static void Run()
        {
            if (!Directory.Exists("Assets/Sprites/UI")) Directory.CreateDirectory("Assets/Sprites/UI");

            GeneratePanel(PanelPath);
            GenerateCircle(CirclePath);
            GenerateGear(GearPath);
            GenerateRestart(RestartPath);

            AssetDatabase.Refresh();

            ConfigureUIImporter(PanelPath, new Vector4(PanelRadius + 4, PanelRadius + 4, PanelRadius + 4, PanelRadius + 4));
            ConfigureUIImporter(CirclePath, Vector4.zero);
            ConfigureUIImporter(GearPath, Vector4.zero);
            ConfigureUIImporter(RestartPath, Vector4.zero);

            Debug.Log("[UISpriteForge] UI sprites generated.");
        }

        private static void ConfigureUIImporter(string assetPath, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = UIPPU;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void GeneratePanel(string path)
        {
            WriteTexture(path, PanelSize, (x, y) =>
            {
                float d = RoundedRectDistance(x + 0.5f, y + 0.5f, 2f, 2f, PanelSize - 2f, PanelSize - 2f, PanelRadius);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
            });
        }

        private static void GenerateCircle(string path)
        {
            float c = IconSize * 0.5f;
            float r = IconSize * 0.5f - 2f;
            WriteTexture(path, IconSize, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) - r;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
            });
        }

        private static void GenerateGear(string path)
        {
            float c = IconSize * 0.5f;
            const int teeth = 8;
            WriteTexture(path, IconSize, (x, y) =>
            {
                Vector2 p = new Vector2(x + 0.5f - c, y + 0.5f - c);
                float dist = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);

                // Square-ish teeth: outer radius alternates between body and tooth tips
                float wave = Mathf.Cos(angle * teeth);
                float outer = wave > 0.25f ? 54f : 43f;
                float alphaOuter = Mathf.Clamp01(outer - dist + 0.5f);
                float alphaHole = Mathf.Clamp01(dist - 17f + 0.5f);
                return new Color(1f, 1f, 1f, Mathf.Min(alphaOuter, alphaHole));
            });
        }

        private static void GenerateRestart(string path)
        {
            float c = IconSize * 0.5f;
            const float ringRadius = 38f;
            const float ringHalfWidth = 8f;
            const float gapStartDeg = 50f;  // open gap at the top-right, where the arrow head sits
            const float gapEndDeg = 100f;

            Vector2 headTip = PolarPoint(c, ringRadius, gapStartDeg - 32f);
            Vector2 headA = PolarPoint(c, ringRadius + 20f, gapStartDeg + 4f);
            Vector2 headB = PolarPoint(c, ringRadius - 20f, gapStartDeg + 4f);

            WriteTexture(path, IconSize, (x, y) =>
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Vector2 rel = p - new Vector2(c, c);
                float angle = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                float ringAlpha = Mathf.Clamp01(ringHalfWidth - Mathf.Abs(rel.magnitude - ringRadius) + 0.5f);
                if (angle > gapStartDeg && angle < gapEndDeg) ringAlpha = 0f;

                float headAlpha = PointInTriangle(p, headTip, headA, headB) ? 1f : 0f;
                return new Color(1f, 1f, 1f, Mathf.Max(ringAlpha, headAlpha));
            });
        }

        private static Vector2 PolarPoint(float center, float radius, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(center + Mathf.Cos(rad) * radius, center + Mathf.Sin(rad) * radius);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static float RoundedRectDistance(float x, float y, float minX, float minY, float maxX, float maxY, float radius)
        {
            float cx = Mathf.Clamp(x, minX + radius, maxX - radius);
            float cy = Mathf.Clamp(y, minY + radius, maxY - radius);
            return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - radius;
        }

        private static void WriteTexture(string path, int size, System.Func<int, int, Color> shader)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, shader(x, y));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
