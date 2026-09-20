using UnityEngine;

namespace Game.Core
{
    public static class GameColorExtensions
    {
        /// <summary>UI tint for a ball kind — matches the block sprite palette (SpriteForge).</summary>
        public static Color ToDisplayColor(this GameColor color)
        {
            switch (color)
            {
                case GameColor.Red: return new Color(0.92f, 0.22f, 0.24f, 1f);
                case GameColor.Blue: return new Color(0.18f, 0.48f, 0.96f, 1f);
                case GameColor.Yellow: return new Color(0.98f, 0.76f, 0.12f, 1f);
                case GameColor.White: return Color.white;
                default: return Color.gray;
            }
        }
    }
}
