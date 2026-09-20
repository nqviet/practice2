using Game.Core;
using UnityEngine;

namespace Game.Runtime.Bootstrap
{
    /// <summary>
    /// 00_Boot entry (scene_structure.md §1): make sure app services exist, apply device
    /// settings, then go straight to the main menu. SDK init (ads / IAP / analytics) will run in
    /// parallel behind a timeout and must never hold this first frame (technical_design.md §5.3).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string m_FirstScene = GameConstants.SceneMainMenu;
        [SerializeField] private int m_TargetFrameRate = 60;

        private void Start()
        {
            Run();
        }

        public void Run()
        {
            AppServices services = AppServices.EnsureCreated();

            Application.targetFrameRate = m_TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            services.SceneLoader.Load(m_FirstScene);
        }
    }
}
