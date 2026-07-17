using UnityEngine;
using BillGameCore;

namespace BillSamples.Flappy
{
    // ───────────────────────────────────────────
    // Flappy-specific states
    // ───────────────────────────────────────────

    public class FlappyMenuState : GameState
    {
        public override string Name => "FlappyMenu";

        public override void Enter()
        {
            Time.timeScale = 1f;
            Bill.Audio.PlayMusic("bgm_menu", 0.5f);
        }
    }

    public class FlappyPlayState : GameState
    {
        public override string Name => "FlappyPlay";

        public override void Enter()
        {
            Time.timeScale = 1f;
        }
    }

    public class FlappyPauseState : GameState
    {
        public override string Name => "FlappyPause";

        public override void Enter() => Time.timeScale = 0f;
        public override void Exit() => Time.timeScale = 1f;
    }

    public class FlappyGameOverState : GameState
    {
        public override string Name => "FlappyGameOver";

        public override void Enter()
        {
            Time.timeScale = 1f;
        }
    }
}
