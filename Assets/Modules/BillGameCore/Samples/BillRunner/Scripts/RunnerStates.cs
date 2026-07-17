using UnityEngine;
using BillGameCore;

namespace BillSamples.Runner
{
    public class RunnerMenuState : GameState
    {
        public override string Name => "RunnerMenu";
        public override void Enter() { Time.timeScale = 1f; Bill.Audio.PlayMusic("bgm_menu", 0.5f); }
    }

    public class RunnerShopState : GameState
    {
        public override string Name => "RunnerShop";
    }

    public class RunnerLoadingState : GameState
    {
        public override string Name => "RunnerLoading";
    }

    public class RunnerPlayState : GameState
    {
        public override string Name => "RunnerPlay";
        public override void Enter() { Time.timeScale = 1f; Bill.Audio.PlayMusic("bgm_runner", 1.0f); }
    }

    public class RunnerPauseState : GameState
    {
        public override string Name => "RunnerPause";
        public override void Enter() => Time.timeScale = 0f;
        public override void Exit() => Time.timeScale = 1f;
    }

    public class RunnerGameOverState : GameState
    {
        public override string Name => "RunnerGameOver";
        public override void Enter() { Time.timeScale = 1f; Bill.Audio.StopMusic(0.5f); }
    }
}
