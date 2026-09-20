using Game.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Board
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Block : MonoBehaviour
    {
        [SerializeField] private Vector2Int m_Cell;
        [SerializeField] private BlockKind m_Kind;
        [SerializeField] private GameColor m_Color;
        [SerializeField] private BlockDefinition m_Definition;

        private SpriteRenderer m_SpriteRenderer;
        private BoxCollider2D m_BoxCollider;

        public Vector2Int Cell
        {
            get => m_Cell;
            set => m_Cell = value;
        }

        public BlockKind Kind => m_Kind;
        public GameColor Color => m_Color;
        public BlockDefinition Definition => m_Definition;

        public UnityAction<Block, bool> OnShattered;

        private void Awake()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            m_BoxCollider = GetComponent<BoxCollider2D>();
        }

        public void Init(Vector2Int cell, BlockDefinition definition)
        {
            m_Cell = cell;
            m_Definition = definition;

            if (definition != null)
            {
                m_Kind = definition.Kind;
                m_Color = definition.Color;

                if (m_SpriteRenderer == null)
                {
                    m_SpriteRenderer = GetComponent<SpriteRenderer>();
                }

                if (definition.Sprite != null)
                {
                    m_SpriteRenderer.sprite = definition.Sprite;
                }

                // Adjust sorting layer if needed
                m_SpriteRenderer.sortingLayerName = GameConstants.SortingBlocks;
            }
            else
            {
                m_Kind = BlockKind.Empty;
                m_Color = GameColor.None;
            }

            gameObject.SetActive(m_Kind != BlockKind.Empty);
        }

        public void SetSprite(Sprite sprite)
        {
            if (m_SpriteRenderer == null)
            {
                m_SpriteRenderer = GetComponent<SpriteRenderer>();
            }
            m_SpriteRenderer.sprite = sprite;
        }

        public void Shatter(bool chained)
        {
            OnShattered?.Invoke(this, chained);
            gameObject.SetActive(false);
        }
    }
}
