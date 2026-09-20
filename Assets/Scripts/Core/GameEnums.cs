namespace Game.Core
{
    public enum GameColor
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Yellow = 3,
        White = 4 // Wildcard
    }

    public enum BlockKind
    {
        Empty = 0,
        Brick = 1,        // 'N'
        Colored = 2,      // 'R', 'B', 'Y'
        Steel = 3,        // 'S'
        ReservedArmor = 4,   // 'M'
        ReservedMystery = 5  // 'X'
    }

    public enum GameState
    {
        Loading,
        Ready,
        Aiming,
        Firing,
        Settling,
        Won
    }

    public enum SfxId
    {
        None = 0,
        Plink,
        Fire,
        Bounce,
        Detonation,
        ChainCombo,
        BallReturn,
        Restart,
        Win,
        UIClick
    }

    public enum PopupId
    {
        None = 0,
        Settings,
        LevelComplete,
        Shop
    }

    public enum PoolId
    {
        None = 0,
        Block,
        Ball,
        TrailRun,
        BreakFxRed,
        BreakFxBlue,
        BreakFxYellow,
        BreakFxNeutral,
        ImpactSpark,
        MuzzleFlash
    }
}
