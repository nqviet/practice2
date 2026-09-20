using Game.Core;
using UnityEngine;

namespace Game.Runtime.Ball
{
    [CreateAssetMenu(fileName = "BallSkin_New", menuName = "Block Breaker/Ball Skin")]
    public class BallSkin : ScriptableObject
    {
        [SerializeField] private GameColor m_Color = GameColor.White;
        [SerializeField] private Sprite m_Sprite;
        [SerializeField] private UnityEngine.Color m_GlowColor = UnityEngine.Color.white;
        [SerializeField] private Gradient m_TrailGradient;

        public GameColor Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        public Sprite Sprite
        {
            get => m_Sprite;
            set => m_Sprite = value;
        }

        public UnityEngine.Color GlowColor
        {
            get => m_GlowColor;
            set => m_GlowColor = value;
        }

        public Gradient TrailGradient
        {
            get => m_TrailGradient;
            set => m_TrailGradient = value;
        }
    }
}
