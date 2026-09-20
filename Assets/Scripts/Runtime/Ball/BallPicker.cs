using System.Collections.Generic;
using Game.Core;
using Game.Runtime.Cannon;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Runtime.Ball
{
    public class BallPicker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallStream m_BallStream;
        [SerializeField] private CannonController m_CannonController;
        [SerializeField] private Transform m_ItemsContainer;
        [SerializeField] private GameObject m_PickerItemPrefab;

        [Header("Item Look")]
        [SerializeField] private Sprite m_ItemSprite;
        [SerializeField] private float m_ItemSize = 96f;
        [SerializeField] private float m_SelectedScale = 1.15f;
        [SerializeField] private float m_UnselectedAlpha = 0.45f;

        private readonly List<GameColor> m_AvailableColors = new List<GameColor>();
        private readonly Dictionary<GameColor, Button> m_ColorButtons = new Dictionary<GameColor, Button>();
        private GameColor m_SelectedColor = GameColor.Red;

        public GameColor Selected => m_SelectedColor;
        public IReadOnlyList<GameColor> AvailableColors => m_AvailableColors;
        public UnityAction<GameColor> OnColorSelected;

        public void SetReferences(BallStream ballStream, CannonController cannonController)
        {
            m_BallStream = ballStream;
            m_CannonController = cannonController;
        }

        public void SetItemSprite(Sprite sprite)
        {
            m_ItemSprite = sprite;
        }

        private void Awake()
        {
            if (m_BallStream == null) m_BallStream = FindFirstObjectByType<BallStream>();
            if (m_CannonController == null) m_CannonController = FindFirstObjectByType<CannonController>();
            if (m_ItemsContainer == null) m_ItemsContainer = transform;
        }

        public void Build(IReadOnlyList<GameColor> colors)
        {
            if (m_ItemsContainer == null) m_ItemsContainer = transform;

            m_AvailableColors.Clear();
            m_ColorButtons.Clear();

            // Clear existing child buttons
            for (int i = m_ItemsContainer.childCount - 1; i >= 0; i--)
            {
                var child = m_ItemsContainer.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            if (colors == null || colors.Count == 0)
            {
                // Default colors for Block Breaker: Red, Blue, Yellow
                colors = new[] { GameColor.Red, GameColor.Blue, GameColor.Yellow };
            }

            foreach (var color in colors)
            {
                if (color == GameColor.None) continue;
                m_AvailableColors.Add(color);
                CreateButtonForColor(color);
            }

            // Default to first color or Red
            if (m_AvailableColors.Count > 0)
            {
                SelectColor(m_AvailableColors[0]);
            }
        }

        private void CreateButtonForColor(GameColor color)
        {
            GameObject btnGo;
            if (m_PickerItemPrefab != null)
            {
                btnGo = Instantiate(m_PickerItemPrefab, m_ItemsContainer);
            }
            else
            {
                btnGo = new GameObject($"PickerItem_{color}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(m_ItemsContainer, false);

                var rt = btnGo.GetComponent<RectTransform>();
                // Minimum size: 88x88 dp (§6.3)
                rt.sizeDelta = new Vector2(m_ItemSize, m_ItemSize);

                var newImg = btnGo.GetComponent<Image>();
                newImg.sprite = m_ItemSprite;
                newImg.preserveAspect = true;
            }

            var img = btnGo.GetComponent<Image>();
            if (img != null)
            {
                img.color = color.ToDisplayColor();
            }

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => OnButtonClicked(color));

            m_ColorButtons[color] = btn;
        }

        private void OnButtonClicked(GameColor color)
        {
            // Input gating: picker is only usable when aim/firing input is allowed (§5.2)
            if (m_BallStream != null && m_BallStream.IsBallInFlight)
            {
                return;
            }

            SelectColor(color);
        }

        public void SelectColor(GameColor color)
        {
            m_SelectedColor = color;

            if (m_BallStream != null)
            {
                m_BallStream.LoadedKind = color;
            }

            if (m_CannonController != null)
            {
                BallSkin skin = m_BallStream != null ? m_BallStream.GetSkinForColor(color) : null;
                m_CannonController.SetLoadedKind(color, skin != null ? skin.Sprite : null);
            }

            UpdateVisualSelection();
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.Plink);
            OnColorSelected?.Invoke(color);
        }

        private void UpdateVisualSelection()
        {
            foreach (var kvp in m_ColorButtons)
            {
                bool isSelected = kvp.Key == m_SelectedColor;
                kvp.Value.transform.localScale = isSelected ? new Vector3(m_SelectedScale, m_SelectedScale, 1f) : Vector3.one;

                if (kvp.Value.targetGraphic != null)
                {
                    Color tint = kvp.Key.ToDisplayColor();
                    tint.a = isSelected ? 1f : m_UnselectedAlpha;
                    kvp.Value.targetGraphic.color = tint;
                }
            }
        }
    }
}
