using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Events.Races
{
    public class RaceLauncher
    {
        private static RaceLauncher _instance = null;

        public const int MAX_RACES = 20;

        public const int TIMER_LAUNCH_RACE = 5000;

        private Dictionary<int, Player> raceLoader = new Dictionary<int, Player>();
        private Queue<Race> loadedRaces;
        private List<Race> runningRaces;

        private List<Player> playersWaiting;
        private List<Player>[] playersInRace;

        private bool isWaitlistOpened;

        public bool IsWaitlistOpened { get => isWaitlistOpened; private set => isWaitlistOpened = value; }

        private Timer launchingRaceTimer;

        public RaceLauncher()
        {
            playersWaiting = new List<Player>(Race.MAX_PLAYERS_IN_RACE);
            loadedRaces = new Queue<Race>();
            runningRaces = new List<Race>();
        }

        public static RaceLauncher Instance()
        {
            if (_instance == null)
                _instance = new RaceLauncher();
            return _instance;
        }

        public void Load(Player player, int id)
        {
            if (id > 0)
            {
                if (loadedRaces.Count < MAX_RACES)
                {
                    Race loadingRace = new Race();
                    loadingRace.Loaded += LoadingRace_Loaded;
                    loadingRace.Load(id);
                    raceLoader[id] = player;
                }
                else player.SendClientMessage(Color.Red, "There is too much loaded races");
            }
            else player.SendClientMessage(Color.Red, "Error loading race #" + id);
        }

        private void LoadingRace_Loaded(object sender, RaceLoadedEventArgs e)
        {
            if(e.race.IsPlayable())
            {
                loadedRaces.Enqueue(e.race);
                raceLoader[e.race.Id].SendClientMessage(Color.Green, "Race #" + e.race.Id + " loaded successfully");
            }
            else raceLoader[e.race.Id].SendClientMessage(Color.Red, "This race is not playable !");
        }

        public Boolean LaunchNext()
        {
            if (playersWaiting.Count >= Race.MIN_PLAYERS_IN_RACE)
            {
                launchingRaceTimer = new Timer(TIMER_LAUNCH_RACE, false);
                launchingRaceTimer.Tick += LaunchingRaceTimer_Elapsed;
                foreach (Player player in playersWaiting)
                {
                    player.SendClientMessage("The next race starts in " + (TIMER_LAUNCH_RACE / 1000).ToString() + " seconds, you can join with " + Color.AliceBlue + "/race join");
                }
                return true;
            }
            else return false;
        }

        private void LaunchingRaceTimer_Elapsed(object sender, EventArgs e)
        {
            Race race = loadedRaces.Dequeue();
            runningRaces.Add(race);
            race.Finished += Race_Finished;
            race.IsStarted = true;
            this.IsWaitlistOpened = false;
            race.Prepare(playersWaiting, 1);
            playersInRace[race.Id] = race.players;
        }

        private void Race_Finished(object sender, RaceEventArgs e)
        {
            Player.SendClientMessageToAll("Race \"" + e.race.Name + "\" is finished, the winner is " + e.race.winner.Name + " !");
            playersInRace[e.race.Id].Clear();
        }

        public void AbortNext()
        {
            loadedRaces.Dequeue();
        }

        public Boolean Join(Player player)
        {
            if(loadedRaces.Count > 0)
            {
                if (playersWaiting.Count < Race.MAX_PLAYERS_IN_RACE)
                {
                    playersWaiting.Add(player);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
    }
}
