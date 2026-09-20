namespace Game.Core
{
    public static class GameConstants
    {
        // Physics / Unity Layers
        public const int BallLayer = 8;
        public const int BlockLayer = 9;
        public const int WallLayer = 10;
        public const int SensorLayer = 11;
        public const int FXLayer = 12;
        public const int UIWorldLayer = 13;

        public const string BallLayerName = "Ball";
        public const string BlockLayerName = "Block";
        public const string WallLayerName = "Wall";
        public const string SensorLayerName = "Sensor";
        public const string FXLayerName = "FX";
        public const string UIWorldLayerName = "UIWorld";

        // Sorting Layers
        public const string SortingBackground = "Background";
        public const string SortingGridSlots = "GridSlots";
        public const string SortingReturnLine = "ReturnLine";
        public const string SortingBlocks = "Blocks";
        public const string SortingBallTrail = "BallTrail";
        public const string SortingBalls = "Balls";
        public const string SortingFX = "FX";
        public const string SortingAimGuide = "AimGuide";
        public const string SortingForeground = "Foreground";

        // Grid Authoring Constants
        public const int GridColumns = 10;
        public const int GridRows = 13;
        public const float CellSize = 1.0f;
        public const float GridOriginX = -5.0f;
        public const float GridOriginY = 0.0f;
        public const float ReturnLineY = -0.30f;

        // Scenes (build order: 00_Boot → 01_MainMenu → 02_Gameplay)
        public const string SceneBoot = "00_Boot";
        public const string SceneMainMenu = "01_MainMenu";
        public const string SceneGameplay = "02_Gameplay";

        // Persistence
        public const string SaveFileName = "player.json";
        public const int SaveVersion = 1;
    }
}
