using UnityEngine;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    public class TDMenuState : GameState { public override string Name => "TDMenu"; }
    public class TDMapSelectState : GameState { public override string Name => "TDMapSelect"; }
    public class TDLoadingState : GameState { public override string Name => "TDLoading"; }

    public class TDBuildPhaseState : GameState
    {
        public override string Name => "TDBuild";
        public override void Enter()
        {
            Time.timeScale = 1f;
            Bill.Audio.PlayMusic("bgm_td_build", 1f);
        }
    }

    public class TDWaveActiveState : GameState
    {
        public override string Name => "TDWave";
        public override void Enter()
        {
            Bill.Audio.PlayMusic("bgm_td_battle", 1f);
        }
    }

    public class TDWaveCompleteState : GameState { public override string Name => "TDWaveComplete"; }
    public class TDPauseState : GameState
    {
        public override string Name => "TDPause";
        public override void Enter() => Time.timeScale = 0f;
        public override void Exit() => Time.timeScale = 1f;
    }
    public class TDGameOverState : GameState { public override string Name => "TDGameOver"; }
    public class TDVictoryState : GameState
    {
        public override string Name => "TDVictory";
        public override void Enter() { Bill.Audio.PlayMusic("bgm_td_victory", 0f); }
    }
}
