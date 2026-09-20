using System.Collections;
using System.Collections.Generic;
using Game.Board;
using Game.Core;
using Game.Runtime.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    /// <summary>
    /// Gameplay TopBar (scene_structure.md §7, gameplay.md §8): settings gear, "LEVEL N" title,
    /// instant restart that spins on tap, and the STATIC legend of the board's ball kinds —
    /// it never depletes, never reorders and never counts down.
    /// </summary>
    public class UIHud : MonoBehaviour
    {
        private const float RestartSpinDuration = 0.4f;

        [Header("References")]
        [SerializeField] private LevelController m_LevelController;
        [SerializeField] private UIManager m_UIManager;

        [Header("TopBar")]
        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private Button m_RestartButton;
        [SerializeField] private RectTransform m_RestartIcon;
        [SerializeField] private TMP_Text m_LevelTitle;

        [Header("Legend")]
        [SerializeField] private RectTransform m_Legend;
        [SerializeField] private Sprite m_LegendDotSprite;
        [SerializeField] private Vector2 m_LegendDotSize = new Vector2(64f, 64f);
        [SerializeField] private Vector2 m_LegendDashSize = new Vector2(28f, 8f);
        [SerializeField] private Color m_LegendDashColor = new Color(1f, 1f, 1f, 0.45f);

        private readonly List<GameColor> m_LegendColors = new List<GameColor>();
        private Coroutine m_SpinRoutine;

        public void SetReferences(LevelController levelController, UIManager uiManager)
        {
            m_LevelController = levelController;
            m_UIManager = uiManager;
        }

        public void SetWidgets(Button settingsButton, Button restartButton, RectTransform restartIcon, TMP_Text levelTitle, RectTransform legend, Sprite legendDotSprite)
        {
            m_SettingsButton = settingsButton;
            m_RestartButton = restartButton;
            m_RestartIcon = restartIcon;
            m_LevelTitle = levelTitle;
            m_Legend = legend;
            m_LegendDotSprite = legendDotSprite;
        }

        private void Awake()
        {
            if (m_LevelController == null) m_LevelController = FindFirstObjectByType<LevelController>();
            if (m_UIManager == null) m_UIManager = FindFirstObjectByType<UIManager>();

            if (m_SettingsButton != null) m_SettingsButton.onClick.AddListener(OnSettingsClicked);
            if (m_RestartButton != null) m_RestartButton.onClick.AddListener(OnRestartClicked);
        }

        private void Start()
        {
            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt += HandleLevelBuilt;

                if (m_LevelController.State != null && m_LevelController.CurrentLevel != null)
                {
                    HandleLevelBuilt(m_LevelController.CurrentLevel);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt -= HandleLevelBuilt;
            }
        }

        private void HandleLevelBuilt(LevelDefinition levelDef)
        {
            Refresh(levelDef);
        }

        public void Refresh(LevelDefinition levelDef)
        {
            if (levelDef == null) return;

            if (m_LevelTitle != null)
            {
                m_LevelTitle.text = levelDef.DisplayName;
            }

            BuildLegend(levelDef.ColorsUsed);
        }

        /// <summary>Rebuilds the legend only when the board's set of ball kinds changes.</summary>
        public void BuildLegend(IReadOnlyList<GameColor> colors)
        {
            if (m_Legend == null || colors == null) return;
            if (m_Legend.childCount > 0 && SameColors(colors)) return;

            m_LegendColors.Clear();
            for (int i = m_Legend.childCount - 1; i >= 0; i--)
            {
                GameObject child = m_Legend.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            for (int i = 0; i < colors.Count; i++)
            {
                GameColor color = colors[i];
                if (color == GameColor.None) continue;

                if (m_LegendColors.Count > 0)
                {
                    CreateLegendImage($"LegendDash_{m_LegendColors.Count}", null, m_LegendDashSize, m_LegendDashColor);
                }

                CreateLegendImage($"LegendItem_{color}", m_LegendDotSprite, m_LegendDotSize, color.ToDisplayColor());
                m_LegendColors.Add(color);
            }
        }

        private void CreateLegendImage(string name, Sprite sprite, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(m_Legend, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false; // legend is not interactive
        }

        private bool SameColors(IReadOnlyList<GameColor> colors)
        {
            int count = 0;
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] == GameColor.None) continue;
                if (count >= m_LegendColors.Count || m_LegendColors[count] != colors[i]) return false;
                count++;
            }
            return count == m_LegendColors.Count;
        }

        private void OnSettingsClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
            if (m_UIManager != null)
            {
                m_UIManager.Show(PopupId.Settings);
            }
        }

        private void OnRestartClicked()
        {
            SpinRestartIcon();
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.Restart);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        public void SpinRestartIcon()
        {
            if (m_RestartIcon == null) return;

            if (m_SpinRoutine != null)
            {
                StopCoroutine(m_SpinRoutine);
            }
            m_SpinRoutine = StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            float elapsed = 0f;
            while (elapsed < RestartSpinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / RestartSpinDuration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t); // OutCubic
                m_RestartIcon.localRotation = Quaternion.Euler(0f, 0f, -360f * eased);
                yield return null;
            }

            m_RestartIcon.localRotation = Quaternion.identity;
            m_SpinRoutine = null;
        }
    }
}
