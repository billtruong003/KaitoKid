using System;

namespace Teabag.GameMode
{
    [Serializable]
    public enum GameModeType
    {
        None = 0,

        MainLobby = 10,
        Shop = 11,

        BattleRoyale = 20,

        Bootcamp = 30,

        TestWeapons = 100,
	TestChest = 101,
    }
}
