﻿using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1.Events.Races
{
    class RaceEvent : Event
    {
        private Race loadedRace;

        public RaceEvent(int _id)
        {
            if (_id > 0)
            {
                this.Id = _id;
            }
        }

        public override void Load()
        {
            Race loadingRace = new Race();
            loadingRace.Loaded += LoadingRace_Loaded;
            loadingRace.Load(this.Id);
        }

        private void LoadingRace_Loaded(object sender, RaceLoadedEventArgs e)
        {
            if (e.race.IsPlayable())
            {
                loadedRace = e.race;
                OnLoaded(new EventLoadedEventArgs { EventLoaded = this, ErrorMessage = null });
            }
            else OnLoaded(new EventLoadedEventArgs { ErrorMessage = "This race is not playable !" });
        }

        public override void Start()
        {
            if(loadedRace != null && this.players.Count > Race.MIN_PLAYERS_IN_RACE)
            {
                loadedRace.Prepare(this.players, 1);
                Player.SendClientMessageToAll("[Event] The " + this.Type.ToString() + " is starting, you cannot longer join it !");
                this.Status = EventStatus.Running;
                loadedRace.Finished += (sender, eventArgs) => { this.End(); } ;
                this.OnStarted(new EventStartedOrEndedEventArgs { });
            }
        }
        public override void End()
        {
            this.Status = EventStatus.Finished;
            players.Clear();
            this.OnEnded(new EventStartedOrEndedEventArgs { });
        }
    }
}
