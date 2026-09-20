using Game.Core;
using UnityEngine;

namespace Game.Board
{
    [CreateAssetMenu(fileName = "BlockDefinition_New", menuName = "Block Breaker/Block Definition")]
    public class BlockDefinition : ScriptableObject
    {
        [SerializeField] private BlockKind m_Kind = BlockKind.Brick;
        [SerializeField] private GameColor m_Color = GameColor.None;
        [SerializeField] private int m_Hp = 1;
        [SerializeField] private Sprite m_Sprite;
        [SerializeField] private bool m_IsIndestructible;
        [SerializeField] private GameObject m_BreakFx;
        [SerializeField] private SfxId m_BreakSfx = SfxId.Detonation;

        public BlockKind Kind
        {
            get => m_Kind;
            set => m_Kind = value;
        }

        public GameColor Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        public int Hp
        {
            get => m_Hp;
            set => m_Hp = value;
        }

        public Sprite Sprite
        {
            get => m_Sprite;
            set => m_Sprite = value;
        }

        public bool IsIndestructible
        {
            get => m_IsIndestructible;
            set => m_IsIndestructible = value;
        }

        public GameObject BreakFx
        {
            get => m_BreakFx;
            set => m_BreakFx = value;
        }

        public SfxId BreakSfx
        {
            get => m_BreakSfx;
            set => m_BreakSfx = value;
        }
    }
}
