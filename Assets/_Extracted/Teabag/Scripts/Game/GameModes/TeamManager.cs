using Fusion;
using Teabag.Networking;
using Teabag.Player.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Teabag.Core;
using Teabag.Player;
using Squido.JungleXRKit.Core;

namespace Teabag.Game
{
    public class TeamManager : MonoBehaviour
    {
        public static TeamManager instance;
        public static Action<Gorilla> onTeamSwitched;
        static bool isBroadcasting; // reentrance guard for GameServices.OnTeamSwitched forwarding
        private Action<Gorilla> _forwardToGameServices;
        private Action<object> _forwardToLocal;
        public List<Team> teams = new List<Team>();
        public int maxPlayers = 2;

        private IGorillaService _gorillaService;

        public int teamCount
        {
            get { return teams.Count; }
        }

        public int activeTeamCount
        {
            get { return GetActiveTeamCount(); }
        }

        public bool showInBlimp = true;

        private void Awake()
        {
            instance = this;
            _gorillaService = ServiceLocator.Get<IGorillaService>();
            onTeamSwitched += OnTeamSwitched;

            // Wire GameServices bridges
            GameServices.TeamManagerExists = () => instance != null;
            GameServices.GetActiveTeamCount = () => instance != null ? instance.activeTeamCount : 0;
            GameServices.SharesTeam = (obj) => SharesTeam(obj as Gorilla);
            GameServices.GetTeamColour = (teamIndex) =>
            {
                Team t = GetTeam(teamIndex);
                return t != null ? t.colour : Color.white;
            };

            // Forward local onTeamSwitched to GameServices.OnTeamSwitched for Player-assembly subscribers.
            // Guard prevents double-invocation when GameServices.OnTeamSwitched is invoked directly.
            _forwardToGameServices = (gorilla) =>
            {
                if (!isBroadcasting)
                {
                    isBroadcasting = true;
                    try
                    {
                        GameServices.OnTeamSwitched?.Invoke(gorilla);
                    }
                    finally
                    {
                        isBroadcasting = false;
                    }
                }
            };
            onTeamSwitched += _forwardToGameServices;

            // Subscribe to GameServices.OnTeamSwitched so Player-assembly invocations
            // (e.g. GorillaTeam.OnTeamChanged) also reach local onTeamSwitched subscribers.
            _forwardToLocal = (obj) =>
            {
                if (!isBroadcasting)
                {
                    isBroadcasting = true;
                    try
                    {
                        onTeamSwitched?.Invoke(obj as Gorilla);
                    }
                    finally
                    {
                        isBroadcasting = false;
                    }
                }
            };
            GameServices.OnTeamSwitched += _forwardToLocal;

            var initGorillas = _gorillaService?.Gorillas;
            if (initGorillas != null)
            {
                foreach (var gorillaEntry in initGorillas)
                {
                    try
                    {
                        onTeamSwitched?.Invoke((Gorilla)gorillaEntry);
                    }
                    catch (Exception e)
                    {
                        GameLogger.Error("Failed to invoke team switched: " + e);
                    }
                }
            }
        }

        private void OnDisable()
        {
            onTeamSwitched -= OnTeamSwitched;
            if (_forwardToGameServices != null)
                onTeamSwitched -= _forwardToGameServices;
            if (_forwardToLocal != null)
                GameServices.OnTeamSwitched -= _forwardToLocal;

            // Clear GameServices bridges
            if (instance == this)
            {
                GameServices.TeamManagerExists = null;
                GameServices.GetActiveTeamCount = null;
                GameServices.SharesTeam = null;
                GameServices.GetTeamColour = null;
            }
        }

        public static void HandleOverflow()
        {
            var localGorilla = ServiceLocator.Get<IGorillaService>()?.LocalGorilla as Gorilla;
            if (localGorilla == null)
                return;

            if (localGorilla.team == null)
                return;

            int maxPlayers = 2;
            if (instance != null)
                maxPlayers = instance.maxPlayers;

            List<Gorilla> controllers = GetTeamPlayers(localGorilla.team.team);

            for (int i = 0; i < controllers.Count; i++)
            {
                Gorilla controller = controllers[i];
                GameLogger.Info($"Team ({i}): {controller}");
                // i starts at 0, so...
                if (i + 1 > maxPlayers && controller == localGorilla)
                {
                    localGorilla.team.team++;
                    GameLogger.Info("Team full, switching");
                }
            }
        }

        public void OnTeamSwitched(Gorilla c)
        {
            HandleOverflow();
            /*
            if (!controller.isMine)
                return;

            if (GetTeamPlayers(controller.team).Count > maxPlayers)
                controller.team++;
            */
        }

        public static bool SharesTeam(Gorilla other)
        {
            if (other.HasStateAuthority)
                return true;

            if (instance == null)
                return false;

            if (other == null)
                return false;

            if (other.Object == null)
                return false;

            var localGorilla = ServiceLocator.Get<IGorillaService>()?.LocalGorilla as Gorilla;
            if (localGorilla == null)
                return false;

            if (localGorilla.team == null)
                return false;

            if (other.team == null)
                return false;

            if (!other.Object.IsValid)
                return false;

            return localGorilla.team.team == other.team.team;
        }

        public static bool SharesTeam(Gorilla a, Gorilla b)
        {
            if (instance == null)
                return false;

            if (a == null || b == null)
                return false;

            if (a.Object == null || b.Object == null)
                return false;

            if (!a.Object.IsValid || !b.Object.IsValid)
                return false;

            if (a.team == null || b.team == null)
                return false;

            return a.team == b.team;
        }

        public void MaxPlayers(int max)
        {
            while (!CheckMax(max))
                teams.RemoveAt(instance.teams.Count - 1);
        }

        private bool CheckMax(int max)
        {
            int i = 0;
            foreach (Team team in teams)
            {
                if (i > max)
                    i += maxPlayers;
                else
                    return false;
            }

            return true;
        }

        public static Team GetTeam(int team)
        {
            if (instance != null)
            {
                if (instance.teams.Count > team)
                    return instance.teams[team];
            }

            return null;
        }

        public static bool TeamExists(int team)
        {
            if (instance == null)
                return false;

            return instance.teams.Count > team;
        }

        public static List<Gorilla> GetTeamPlayers(int team)
        {
            List<Gorilla> controllers = new List<Gorilla>();
            var svcGorillas = ServiceLocator.Get<IGorillaService>()?.Gorillas;
            if (svcGorillas != null)
            {
                foreach (var gorillaEntry in svcGorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    if (gorilla.team != null)
                    {
                        if (gorilla.team.team == team)
                            controllers.Add(gorilla);
                    }
                }
            }

            return controllers;
        }

        public static Dictionary<int, List<Gorilla>> GetAliveTeams()
        {
            if (instance == null)
                return new Dictionary<int, List<Gorilla>>();

            Dictionary<int, List<Gorilla>> teams = new Dictionary<int, List<Gorilla>>();
            for (int i = 0; i < instance.teams.Count; i++)
            {
                List<Gorilla> players = GetTeamPlayers(i);
                List<Gorilla> p = new List<Gorilla>();
                foreach (Gorilla player in players)
                {
                    if (player.health != null)
                    {
                        if (!player.health.isDead)
                            p.Add(player);
                    }
                }

                if (p.Count > 0)
                    teams.Add(i, p);
            }

            return teams;
        }

        public static int GetActiveTeamCount()
        {
            List<int> foundTeams = new List<int>();
            var allGorillas = ServiceLocator.Get<IGorillaService>()?.Gorillas;
            if (allGorillas != null)
            {
                foreach (var gorillaEntry in allGorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    if (gorilla.team != null)
                    {
                        if (!foundTeams.Contains(gorilla.team.team))
                            foundTeams.Add(gorilla.team.team);
                    }
                }
            }

            return foundTeams.Count;
        }

        public static int TeamRoom()
        {
            return 0;
        }

        public static bool CanJoinTeam(int team)
        {
            if (instance == null)
            {
                GameLogger.Error("No team manager instantiated");
                return false;
            }

            int players = GetTeamPlayers(team).Count;
            return players < instance.maxPlayers;
        }
    }

    [Serializable]
    public class Team
    {
        public string teamName;
        public Color colour;
    }
}
