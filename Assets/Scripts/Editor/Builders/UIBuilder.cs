using System.IO;
using Game.Core;
using Game.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// uGUI construction helpers shared by SceneBuilder, plus the popup prefabs
    /// (Prefabs/Popups/Popup_Settings, Popup_LevelComplete). Idempotent: prefabs are rebuilt
    /// and overwritten in place on every run, keeping their GUIDs.
    /// </summary>
    public static class UIBuilder
    {
        public const string PopupSettingsPath = "Assets/Prefabs/Popups/Popup_Settings.prefab";
        public const string PopupLevelCompletePath = "Assets/Prefabs/Popups/Popup_LevelComplete.prefab";

        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        public static readonly Color ScrimColor = new Color(0.02f, 0.03f, 0.05f, 0.72f);
        public static readonly Color PanelColor = new Color(0.11f, 0.14f, 0.19f, 1f);
        public static readonly Color ButtonColor = new Color(0.21f, 0.26f, 0.34f, 1f);
        public static readonly Color HudButtonColor = new Color(0.14f, 0.17f, 0.23f, 0.95f);
        public static readonly Color AccentColor = new Color(0.98f, 0.55f, 0.16f, 1f);
        public static readonly Color PlayColor = new Color(0.22f, 0.74f, 0.42f, 1f);
        public static readonly Color TextColor = new Color(0.96f, 0.97f, 1f, 1f);
        public static readonly Color SubtleTextColor = new Color(0.70f, 0.75f, 0.84f, 1f);

        [MenuItem("Tools/Block Breaker/Run UI Builder", false, 105)]
        public static void Run()
        {
            if (!Directory.Exists("Assets/Prefabs/Popups")) Directory.CreateDirectory("Assets/Prefabs/Popups");

            BuildSettingsPopup();
            BuildLevelCompletePopup();

            AssetDatabase.SaveAssets();
            Debug.Log("[UIBuilder] Popup prefabs built.");
        }

        // ---------------------------------------------------------------- Popups

        private static void BuildSettingsPopup()
        {
            RectTransform panel;
            GameObject root = CreatePopupRoot("Popup_Settings", out panel);

            CreateText("Txt_Title", panel, "SETTINGS", 80f, TextColor, FontStyles.Bold, 120f);

            Button music = CreateLayoutButton("Btn_Music", panel, "MUSIC: ON", ButtonColor, out TextMeshProUGUI musicLabel);
            Button sound = CreateLayoutButton("Btn_Sound", panel, "SOUND: ON", ButtonColor, out TextMeshProUGUI soundLabel);
            Button restart = CreateLayoutButton("Btn_Restart", panel, "RESTART", AccentColor, out _);
            Button home = CreateLayoutButton("Btn_Home", panel, "MAIN MENU", ButtonColor, out _);
            Button close = CreateCloseButton(panel);

            var settings = root.AddComponent<UISettings>();
            settings.Configure(PopupId.Settings, true, true, close);
            settings.SetAnimatedPanel(panel);
            settings.SetWidgets(music, musicLabel, sound, soundLabel, restart, home);

            SavePrefab(root, PopupSettingsPath);
        }

        private static void BuildLevelCompletePopup()
        {
            RectTransform panel;
            GameObject root = CreatePopupRoot("Popup_LevelComplete", out panel);

            TextMeshProUGUI title = CreateText("Txt_Title", panel, "LEVEL 1\nCOMPLETE", 84f, TextColor, FontStyles.Bold, 220f);
            TextMeshProUGUI shots = CreateText("Txt_Shots", panel, "Cleared in 10 shots", 56f, TextColor, FontStyles.Normal, 80f);
            TextMeshProUGUI best = CreateText("Txt_Best", panel, "Best: 10", 44f, SubtleTextColor, FontStyles.Normal, 64f);

            Button next = CreateLayoutButton("Btn_Next", panel, "NEXT", PlayColor, out _);
            Button replay = CreateLayoutButton("Btn_Replay", panel, "REPLAY", AccentColor, out _);
            Button menu = CreateLayoutButton("Btn_Menu", panel, "MENU", ButtonColor, out _);

            var win = root.AddComponent<UIWin>();
            // Not closable by back: the only ways on are Replay / Next / Menu
            win.Configure(PopupId.LevelComplete, false, false, null);
            win.SetAnimatedPanel(panel);
            win.SetWidgets(title, shots, best, replay, next, menu);

            SavePrefab(root, PopupLevelCompletePath);
        }

        private static GameObject CreatePopupRoot(string name, out RectTransform panel)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            Stretch(rootRt);

            // Full-screen dimmer: blocks every tap to the HUD / field below
            var scrim = root.GetComponent<Image>();
            scrim.color = ScrimColor;
            scrim.raycastTarget = true;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel = panelGo.GetComponent<RectTransform>();
            panel.SetParent(rootRt, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(820f, 0f);

            var panelImage = panelGo.GetComponent<Image>();
            panelImage.sprite = LoadSprite(UISpriteForge.PanelPath);
            panelImage.type = Image.Type.Sliced;
            panelImage.color = PanelColor;

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(70, 70, 70, 80);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return root;
        }

        private static Button CreateLayoutButton(string name, Transform parent, string label, Color color, out TextMeshProUGUI labelText)
        {
            Button button = CreateButton(name, parent, label, color, 56f, out labelText);
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 140f;
            element.minHeight = 140f;
            return button;
        }

        private static Button CreateCloseButton(RectTransform panel)
        {
            Button close = CreateButton("Btn_Close", panel, "X", ButtonColor, 56f, out _);
            close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var rt = close.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(110f, 110f);
            return close;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- Helpers

        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            // "Match width" per technical_design.md §6.1 — in Unity that is 0 (1 = height). The HUD then
            // scales exactly like the width-driven camera, so the TopBar always fits the top margin.
            scaler.matchWidthOrHeight = 0f;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool sliced = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced && sprite != null;
            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, FontStyles style, float preferredHeight = -1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            if (preferredHeight > 0f)
            {
                var element = go.AddComponent<LayoutElement>();
                element.preferredHeight = preferredHeight;
            }
            return tmp;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color color, float fontSize, out TextMeshProUGUI labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite(UISpriteForge.PanelPath);
            image.type = Image.Type.Sliced;
            image.color = color;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            labelText = CreateText("Label", go.transform, label, fontSize, TextColor, FontStyles.Bold);
            Stretch(labelText.rectTransform);
            return button;
        }

        public static Button CreateIconButton(string name, Transform parent, Sprite icon, Color color, Vector2 iconSize, out RectTransform iconRect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite(UISpriteForge.PanelPath);
            image.type = Image.Type.Sliced;
            image.color = color;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            Image iconImage = CreateImage("Icon", go.transform, icon, TextColor);
            iconImage.raycastTarget = false;
            iconRect = iconImage.rectTransform;
            Place(iconRect, new Vector2(0.5f, 0.5f), Vector2.zero, iconSize);
            iconRect.pivot = new Vector2(0.5f, 0.5f); // spin around the centre
            return button;
        }

        public static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
