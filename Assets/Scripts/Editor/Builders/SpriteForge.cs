using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    public static class SpriteForge
    {
        private const int TexSize = 256;
        private const float PPU = 256f;

        [MenuItem("Tools/Block Breaker/Run Sprite Forge", false, 101)]
        public static void Run()
        {
            Debug.Log("[SpriteForge] Generating procedural block and arena sprites (256x256, PPU 256)...");

            EnsureDirectories();

            GenerateBrickSprite("Assets/Sprites/Blocks/Sprite_Brick.png");
            GenerateSteelSprite("Assets/Sprites/Blocks/Sprite_Steel.png");
            GenerateColoredBlockSprite("Assets/Sprites/Blocks/Sprite_Block_Red.png", new Color(0.92f, 0.22f, 0.24f));
            GenerateColoredBlockSprite("Assets/Sprites/Blocks/Sprite_Block_Blue.png", new Color(0.18f, 0.48f, 0.96f));
            GenerateColoredBlockSprite("Assets/Sprites/Blocks/Sprite_Block_Yellow.png", new Color(0.98f, 0.76f, 0.12f));
            GenerateGridSlotSprite("Assets/Sprites/Arena/Sprite_GridSlot.png");
            GenerateReturnLineSprite("Assets/Sprites/Arena/Sprite_ReturnLine.png");

            GenerateBallSprite("Assets/Sprites/Balls/Sprite_Ball_White.png", Color.white, new Color(0.9f, 0.95f, 1f));
            GenerateBallSprite("Assets/Sprites/Balls/Sprite_Ball_Red.png", new Color(1f, 0.25f, 0.27f), new Color(1f, 0.7f, 0.7f));
            GenerateBallSprite("Assets/Sprites/Balls/Sprite_Ball_Blue.png", new Color(0.2f, 0.55f, 1f), new Color(0.7f, 0.85f, 1f));
            GenerateBallSprite("Assets/Sprites/Balls/Sprite_Ball_Yellow.png", new Color(1f, 0.85f, 0.15f), new Color(1f, 0.95f, 0.7f));
            GenerateBallGlowSprite("Assets/Sprites/Balls/Sprite_Ball_Glow.png");

            GenerateCannonBaseSprite("Assets/Sprites/Cannon/Sprite_Cannon_Base.png");
            GenerateCannonHeadSprite("Assets/Sprites/Cannon/Sprite_Cannon_Head.png");
            GenerateAimReticleSprite("Assets/Sprites/Aim/Sprite_AimReticle.png");
            GenerateArrowSprite("Assets/Sprites/Cannon/Sprite_Arrow_Left.png", true);
            GenerateArrowSprite("Assets/Sprites/Cannon/Sprite_Arrow_Right.png", false);

            GenerateShardSprite("Assets/Sprites/FX/Sprite_Shard.png");
            GenerateRadialFlashSprite("Assets/Sprites/FX/Sprite_RadialFlash.png");
            GenerateImpactSparkSprite("Assets/Sprites/FX/Sprite_ImpactSpark.png");
            GenerateMuzzleFlashSprite("Assets/Sprites/FX/Sprite_MuzzleFlash.png");
            GenerateSmokeSprite("Assets/Sprites/FX/Sprite_Smoke.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ConfigureImporter("Assets/Sprites/Blocks/Sprite_Brick.png", true);
            ConfigureImporter("Assets/Sprites/Blocks/Sprite_Steel.png", true);
            ConfigureImporter("Assets/Sprites/Blocks/Sprite_Block_Red.png", true);
            ConfigureImporter("Assets/Sprites/Blocks/Sprite_Block_Blue.png", true);
            ConfigureImporter("Assets/Sprites/Blocks/Sprite_Block_Yellow.png", true);
            ConfigureImporter("Assets/Sprites/Arena/Sprite_GridSlot.png", false);
            ConfigureImporter("Assets/Sprites/Arena/Sprite_ReturnLine.png", false);
            ConfigureImporter("Assets/Sprites/Balls/Sprite_Ball_White.png", true);
            ConfigureImporter("Assets/Sprites/Balls/Sprite_Ball_Red.png", true);
            ConfigureImporter("Assets/Sprites/Balls/Sprite_Ball_Blue.png", true);
            ConfigureImporter("Assets/Sprites/Balls/Sprite_Ball_Yellow.png", true);
            ConfigureImporter("Assets/Sprites/Balls/Sprite_Ball_Glow.png", false);
            ConfigureImporter("Assets/Sprites/Cannon/Sprite_Cannon_Base.png", true);
            ConfigureImporter("Assets/Sprites/Cannon/Sprite_Cannon_Head.png", true);
            ConfigureImporter("Assets/Sprites/Aim/Sprite_AimReticle.png", false);
            ConfigureImporter("Assets/Sprites/Cannon/Sprite_Arrow_Left.png", false);
            ConfigureImporter("Assets/Sprites/Cannon/Sprite_Arrow_Right.png", false);
            ConfigureImporter("Assets/Sprites/FX/Sprite_Shard.png", false);
            ConfigureImporter("Assets/Sprites/FX/Sprite_RadialFlash.png", false);
            ConfigureImporter("Assets/Sprites/FX/Sprite_ImpactSpark.png", false);
            ConfigureImporter("Assets/Sprites/FX/Sprite_MuzzleFlash.png", false);
            ConfigureImporter("Assets/Sprites/FX/Sprite_Smoke.png", false);

            UISpriteForge.Run();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpriteForge] All sprites generated and configured successfully.");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Sprites/Blocks")) Directory.CreateDirectory("Assets/Sprites/Blocks");
            if (!Directory.Exists("Assets/Sprites/Arena")) Directory.CreateDirectory("Assets/Sprites/Arena");
            if (!Directory.Exists("Assets/Sprites/Balls")) Directory.CreateDirectory("Assets/Sprites/Balls");
            if (!Directory.Exists("Assets/Sprites/Cannon")) Directory.CreateDirectory("Assets/Sprites/Cannon");
            if (!Directory.Exists("Assets/Sprites/Aim")) Directory.CreateDirectory("Assets/Sprites/Aim");
            if (!Directory.Exists("Assets/Sprites/FX")) Directory.CreateDirectory("Assets/Sprites/FX");
        }

        private static void ConfigureImporter(string assetPath, bool enableMipmaps)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PPU;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.mipmapEnabled = enableMipmaps;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateBrickSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color baseColor = new Color(0.85f, 0.86f, 0.88f, 1f);
            Color highlightColor = new Color(0.96f, 0.97f, 0.98f, 1f);
            Color shadowColor = new Color(0.68f, 0.70f, 0.73f, 1f);
            Color borderColor = new Color(0.45f, 0.47f, 0.50f, 1f);

            int pad = 8;
            int r = 24; // corner radius

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    if (IsInsideRoundedRect(x, y, pad, TexSize - pad, pad, TexSize - pad, r))
                    {
                        // Beveled effect
                        if (x < pad + 6 || y > TexSize - pad - 6)
                        {
                            tex.SetPixel(x, y, highlightColor);
                        }
                        else if (x > TexSize - pad - 6 || y < pad + 6)
                        {
                            tex.SetPixel(x, y, shadowColor);
                        }
                        else
                        {
                            // Subtle gradient
                            float v = (float)(y - pad) / (TexSize - 2 * pad);
                            Color c = Color.Lerp(baseColor * 0.95f, baseColor * 1.05f, v);
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else if (IsInsideRoundedRect(x, y, pad - 2, TexSize - pad + 2, pad - 2, TexSize - pad + 2, r + 2))
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateSteelSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color steelBase = new Color(0.32f, 0.35f, 0.40f, 1f);
            Color steelHighlight = new Color(0.50f, 0.54f, 0.60f, 1f);
            Color steelShadow = new Color(0.20f, 0.22f, 0.26f, 1f);
            Color crossColor = new Color(0.45f, 0.48f, 0.54f, 1f);
            Color crossEdge = new Color(0.18f, 0.20f, 0.24f, 1f);

            int pad = 8;
            int r = 16;

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    if (IsInsideRoundedRect(x, y, pad, TexSize - pad, pad, TexSize - pad, r))
                    {
                        // Check if on diagonal cross lines (the "X")
                        int dx1 = Mathf.Abs(x - y);
                        int dx2 = Mathf.Abs(x - (TexSize - 1 - y));
                        int crossWidth = 14;

                        if (dx1 <= crossWidth || dx2 <= crossWidth)
                        {
                            if (dx1 <= crossWidth - 4 || dx2 <= crossWidth - 4)
                            {
                                tex.SetPixel(x, y, crossColor);
                            }
                            else
                            {
                                tex.SetPixel(x, y, crossEdge);
                            }
                        }
                        else if (x < pad + 6 || y > TexSize - pad - 6)
                        {
                            tex.SetPixel(x, y, steelHighlight);
                        }
                        else if (x > TexSize - pad - 6 || y < pad + 6)
                        {
                            tex.SetPixel(x, y, steelShadow);
                        }
                        else
                        {
                            tex.SetPixel(x, y, steelBase);
                        }
                    }
                    else if (IsInsideRoundedRect(x, y, pad - 2, TexSize - pad + 2, pad - 2, TexSize - pad + 2, r + 2))
                    {
                        tex.SetPixel(x, y, steelShadow);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateColoredBlockSprite(string path, Color themeColor)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color highlight = Color.Lerp(themeColor, Color.white, 0.45f);
            Color shadow = Color.Lerp(themeColor, Color.black, 0.45f);
            Color innerGlow = Color.Lerp(themeColor, Color.white, 0.25f);
            Color borderColor = Color.Lerp(themeColor, Color.black, 0.6f);

            int pad = 8;
            int r = 20;

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    if (IsInsideRoundedRect(x, y, pad, TexSize - pad, pad, TexSize - pad, r))
                    {
                        // Pyramid/gem facet look matching prototype mockup
                        float cx = TexSize * 0.5f;
                        float cy = TexSize * 0.5f;
                        float distFromCenter = Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy)) / (TexSize * 0.5f);

                        if (x < pad + 8 || y > TexSize - pad - 8)
                        {
                            tex.SetPixel(x, y, highlight);
                        }
                        else if (x > TexSize - pad - 8 || y < pad + 8)
                        {
                            tex.SetPixel(x, y, shadow);
                        }
                        else
                        {
                            Color c = Color.Lerp(innerGlow, themeColor, distFromCenter);
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else if (IsInsideRoundedRect(x, y, pad - 2, TexSize - pad + 2, pad - 2, TexSize - pad + 2, r + 2))
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateGridSlotSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color slotBorder = new Color(0.20f, 0.22f, 0.28f, 0.45f);
            Color slotFill = new Color(0.08f, 0.10f, 0.14f, 0.35f);

            int borderThickness = 4;
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    if (x < borderThickness || x >= TexSize - borderThickness ||
                        y < borderThickness || y >= TexSize - borderThickness)
                    {
                        tex.SetPixel(x, y, slotBorder);
                    }
                    else
                    {
                        tex.SetPixel(x, y, slotFill);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateReturnLineSprite(string path)
        {
            // 256x32 dashed line
            int width = 256;
            int height = 32;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color redDash = new Color(0.95f, 0.25f, 0.25f, 0.9f);

            int dashLength = 24;
            int gapLength = 16;
            int period = dashLength + gapLength;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pos = x % period;
                    if (pos < dashLength && y >= 10 && y <= 22)
                    {
                        tex.SetPixel(x, y, redDash);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static bool IsInsideRoundedRect(int x, int y, int minX, int maxX, int minY, int maxY, int radius)
        {
            if (x < minX || x >= maxX || y < minY || y >= maxY) return false;

            // Check four corners
            if (x < minX + radius && y < minY + radius)
            {
                return (x - (minX + radius)) * (x - (minX + radius)) + (y - (minY + radius)) * (y - (minY + radius)) <= radius * radius;
            }
            if (x >= maxX - radius && y < minY + radius)
            {
                return (x - (maxX - radius - 1)) * (x - (maxX - radius - 1)) + (y - (minY + radius)) * (y - (minY + radius)) <= radius * radius;
            }
            if (x < minX + radius && y >= maxY - radius)
            {
                return (x - (minX + radius)) * (x - (minX + radius)) + (y - (maxY - radius - 1)) * (y - (maxY - radius - 1)) <= radius * radius;
            }
            if (x >= maxX - radius && y >= maxY - radius)
            {
                return (x - (maxX - radius - 1)) * (x - (maxX - radius - 1)) + (y - (maxY - radius - 1)) * (y - (maxY - radius - 1)) <= radius * radius;
            }

            return true;
        }

        private static void GenerateBallSprite(string path, Color baseColor, Color highlightColor)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(TexSize * 0.5f, TexSize * 0.5f);
            float radius = 72f; // 0.28 u * 256 PPU = 71.68 px
            Vector2 lightOffset = new Vector2(center.x - 22f, center.y + 24f);

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    if (dist <= radius)
                    {
                        // Specular / highlight factor
                        float distToLight = Vector2.Distance(p, lightOffset);
                        float specular = Mathf.Clamp01(1.0f - (distToLight / (radius * 0.75f)));
                        specular = specular * specular;

                        // Radial falloff from center to edge (3D sphere shading)
                        float normDist = dist / radius;
                        Color coreColor = Color.Lerp(highlightColor, baseColor, normDist * 0.8f);
                        Color shaded = Color.Lerp(coreColor, baseColor * 0.6f, normDist * normDist);
                        Color finalColor = Color.Lerp(shaded, Color.white, specular * 0.7f);

                        // Anti-aliasing at the edge
                        float aa = Mathf.Clamp01((radius - dist) / 1.5f);
                        finalColor.a = aa;

                        tex.SetPixel(x, y, finalColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateBallGlowSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(TexSize * 0.5f, TexSize * 0.5f);
            float maxRadius = 110f;

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= maxRadius)
                    {
                        float t = dist / maxRadius;
                        // Smooth exponential falloff
                        float alpha = Mathf.Exp(-3.5f * t * t);
                        Color c = new Color(1f, 1f, 1f, alpha * 0.85f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateCannonBaseSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color darkMetal = new Color(0.24f, 0.27f, 0.32f, 1f);
            Color midMetal = new Color(0.38f, 0.42f, 0.48f, 1f);
            Color lightMetal = new Color(0.62f, 0.67f, 0.74f, 1f);
            Color outlineColor = new Color(0.12f, 0.14f, 0.18f, 1f);

            // Trapezoidal base: wider at bottom (x from 40 to 216 at y=20), narrower at top (x from 76 to 180 at y=180)
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    if (y >= 20 && y <= 190)
                    {
                        float t = (y - 20) / 170f;
                        float left = Mathf.Lerp(40f, 76f, t);
                        float right = Mathf.Lerp(216f, 180f, t);

                        if (x >= left && x <= right)
                        {
                            // Border
                            if (x <= left + 4 || x >= right - 4 || y <= 24 || y >= 186)
                            {
                                tex.SetPixel(x, y, outlineColor);
                            }
                            else
                            {
                                // Horizontal shading gradient for rounded 3D metal look
                                float u = (x - left) / (right - left);
                                Color col = Color.Lerp(darkMetal, lightMetal, 1.0f - Mathf.Abs(u - 0.45f) * 1.8f);
                                tex.SetPixel(x, y, col);
                            }
                            continue;
                        }
                    }
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateCannonHeadSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color darkMetal = new Color(0.20f, 0.23f, 0.28f, 1f);
            Color lightMetal = new Color(0.70f, 0.75f, 0.82f, 1f);
            Color outlineColor = new Color(0.10f, 0.12f, 0.15f, 1f);
            Color muzzleRim = new Color(0.85f, 0.88f, 0.92f, 1f);

            // Barrel pointing upwards, center x = 128, y from 40 to 220
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    if (y >= 40 && y <= 220)
                    {
                        float halfWidth = (y > 195) ? 42f : 36f; // Muzzle ring is slightly wider
                        float left = 128f - halfWidth;
                        float right = 128f + halfWidth;

                        if (x >= left && x <= right)
                        {
                            if (x <= left + 4 || x >= right - 4 || y >= 216)
                            {
                                tex.SetPixel(x, y, outlineColor);
                            }
                            else if (y >= 195)
                            {
                                tex.SetPixel(x, y, muzzleRim);
                            }
                            else
                            {
                                float u = (x - left) / (right - left);
                                Color col = Color.Lerp(darkMetal, lightMetal, 1.0f - Mathf.Abs(u - 0.4f) * 1.8f);
                                tex.SetPixel(x, y, col);
                            }
                            continue;
                        }
                    }
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateAimReticleSprite(string path)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(TexSize * 0.5f, TexSize * 0.5f);
            float outerRadius = 88f;
            float innerRadius = 74f;
            Color reticleColor = new Color(0.92f, 0.96f, 1f, 0.95f);
            Color glowColor = new Color(0.4f, 0.7f, 1f, 0.35f);

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    // Circle ring
                    bool inRing = dist >= innerRadius && dist <= outerRadius;

                    // 'X' cross mark (thickness 12, length 46 from center)
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    bool inCross = Mathf.Abs(dx - dy) <= 8f && dx <= 46f && dy <= 46f;

                    if (inRing || inCross)
                    {
                        tex.SetPixel(x, y, reticleColor);
                    }
                    else if (dist <= outerRadius + 8f && dist >= innerRadius - 8f)
                    {
                        // Outer subtle glow
                        tex.SetPixel(x, y, glowColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateArrowSprite(string path, bool flip)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            Color arrowColor = new Color(0.45f, 0.70f, 0.95f, 0.85f);
            Color arrowGlow = new Color(0.3f, 0.55f, 0.85f, 0.35f);

            Vector2 center = new Vector2(TexSize * 0.5f, TexSize * 0.5f);

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    int evalX = flip ? (TexSize - 1 - x) : x;

                    // Arrow pointing right: stem x in [60, 160], y in [116, 140]
                    // Head: tip at (200, 128), wings slope back to (150, 80) and (150, 176)
                    bool inStem = evalX >= 60 && evalX <= 160 && y >= 116 && y <= 140;

                    float headProgress = (evalX - 150) / 50f; // 0 at x=150, 1 at x=200
                    bool inHead = false;
                    if (headProgress >= 0f && headProgress <= 1.0f)
                    {
                        float halfH = (1.0f - headProgress) * 48f;
                        if (Mathf.Abs(y - center.y) <= halfH)
                        {
                            inHead = true;
                        }
                    }

                    if (inStem || inHead)
                    {
                        tex.SetPixel(x, y, arrowColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateShardSprite(string path)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color highlight = new Color(1f, 1f, 1f, 1f);
            Color body = new Color(0.85f, 0.85f, 0.85f, 1f);
            Color shadow = new Color(0.5f, 0.5f, 0.5f, 1f);
            Color border = new Color(0.2f, 0.2f, 0.2f, 1f);

            int pad = 4;
            int r = 8;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInsideRoundedRect(x, y, pad, size - pad, pad, size - pad, r))
                    {
                        if (x < pad + 4 || y > size - pad - 4)
                        {
                            tex.SetPixel(x, y, highlight);
                        }
                        else if (x > size - pad - 4 || y < pad + 4)
                        {
                            tex.SetPixel(x, y, shadow);
                        }
                        else
                        {
                            tex.SetPixel(x, y, body);
                        }
                    }
                    else if (IsInsideRoundedRect(x, y, pad - 1, size - pad + 1, pad - 1, size - pad + 1, r + 1))
                    {
                        tex.SetPixel(x, y, border);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateRadialFlashSprite(string path)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= maxR)
                    {
                        float t = d / maxR;
                        float alpha = Mathf.Exp(-4.0f * t * t);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateImpactSparkSprite(string path)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    float d = Vector2.Distance(new Vector2(x, y), center);

                    // 4-pointed cross star flare
                    float cross = Mathf.Max(
                        Mathf.Exp(-0.35f * dx) * Mathf.Exp(-0.06f * dy),
                        Mathf.Exp(-0.35f * dy) * Mathf.Exp(-0.06f * dx)
                    );
                    float core = Mathf.Exp(-0.15f * d * d);
                    float intensity = Mathf.Clamp01(cross + core);

                    if (intensity > 0.01f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, intensity));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateMuzzleFlashSprite(string path)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    float d = Vector2.Distance(new Vector2(x, y), center);

                    // Multi-pointed blast
                    float diag1 = Mathf.Abs(dx - dy);
                    float diag2 = Mathf.Abs(dx - (size - 1 - dy));
                    float star = Mathf.Max(
                        Mathf.Exp(-0.25f * dx) * Mathf.Exp(-0.08f * dy),
                        Mathf.Exp(-0.25f * dy) * Mathf.Exp(-0.08f * dx)
                    );
                    float diags = Mathf.Exp(-0.3f * Mathf.Min(diag1, diag2)) * Mathf.Exp(-0.08f * d);
                    float core = Mathf.Exp(-0.12f * d * d);

                    float intensity = Mathf.Clamp01(star * 0.8f + diags * 0.5f + core);
                    if (intensity > 0.01f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 0.95f, 0.8f, intensity));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateSmokeSprite(string path)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= maxR)
                    {
                        float t = d / maxR;
                        float alpha = Mathf.Clamp01((1.0f - t) * (1.0f - t));
                        tex.SetPixel(x, y, new Color(0.85f, 0.85f, 0.9f, alpha * 0.6f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
