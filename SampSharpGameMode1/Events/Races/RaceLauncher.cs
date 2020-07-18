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

        private Queue<Race> loadedRaces;
        private List<Race> runningRaces;

        private List<Player> playersWaiting;
        private List<Player> playersInRace;

        private bool isWaitlistOpened;

        public bool IsWaitlistOpened { get => isWaitlistOpened; private set => isWaitlistOpened = value; }

        private Timer launchingRaceTimer;
        private Race startingRace = null;

        public event EventHandler<RaceLoadedEventArgs> RaceLoaded;
        public class RaceLoadedEventArgs : EventArgs
        {
            public int RaceID { get; set; }
            public int CheckpointsCount { get; set; }
        }
        public event EventHandler<EventArgs> RaceStarted;
        public class RaceStartedEventArgs : EventArgs
        {
            public int RaceID { get; set; }
        }
        public event EventHandler<EventArgs> RaceFinished;
        public class RaceFinishedEventArgs : EventArgs
        {
            public int RaceID { get; set; }
        }
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

        public Boolean Load(int id)
        {
            if (id > 0)
            {
                if (loadedRaces.Count < MAX_RACES)
                {
                    Race loadedRace = new Race();
                    loadedRace.Load(id);
                    if (loadedRace.IsPlayable())
                    {
                        loadedRaces.Enqueue(loadedRace);
                        RaceLoadedEventArgs args = new RaceLoadedEventArgs();
                        args.RaceID = id;
                        args.CheckpointsCount = loadedRace.checkpoints.Count;
                        //RaceLoaded(this, args);
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            return false;
        }
        /*
        public override void Load()
        {
            if (id > 0)
            {
                if (loadedRaces.Count < MAX_RACES)
                {
                    Race loadedRace = new Race();
                    loadedRace.Load(id);
                    if (loadedRace.IsPlayable())
                    {
                        loadedRaces.Enqueue(loadedRace);
                        RaceLoadedEventArgs args = new RaceLoadedEventArgs();
                        args.RaceID = id;
                        args.CheckpointsCount = loadedRace.checkpoints.Count;
                        RaceLoaded(this, args);
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            return false;
        }
        */
        public Boolean LaunchNext()
        {
            if (playersWaiting.Count >= Race.MIN_PLAYERS_IN_RACE)
            {
                launchingRaceTimer = new Timer(TIMER_LAUNCH_RACE, false);
                launchingRaceTimer.Tick += LaunchingRaceTimer_Elapsed;
                foreach (Player player in playersWaiting)
                {
                    player.SendClientMessage("The next race starts in " + (TIMER_LAUNCH_RACE / 1000).ToString() + " seconds, you can still join with " + Color.AliceBlue + "/race join");
                }
                return true;
            }
            else return false;
        }

        private void LaunchingRaceTimer_Elapsed(object sender, EventArgs e)
        {
            Race race = loadedRaces.Dequeue();
            runningRaces.Add(race);
            race.IsStarted = true;
            this.IsWaitlistOpened = false;
            race.Prepare(playersWaiting, 1);
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
