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

        public RaceEvent(int eventId)
        {
            if (eventId > 0)
            {
                this.Id = eventId;
                this.VirtualWorld = (int)VirtualWord.Events + eventId;
            }
        }

        public override void Load(int _id)
        {
            Race loadingRace = new Race();
            loadingRace.Loaded += LoadingRace_Loaded;
            loadingRace.Load(_id, this.VirtualWorld);
        }

        private void LoadingRace_Loaded(object sender, RaceLoadedEventArgs e)
        {
            this.Type = EventType.Race;
            if (e.race.IsPlayable())
            {
                loadedRace = e.race;
                this.Name = e.race.Name;
                this.Status = EventStatus.Loaded;
                this.Source = loadedRace;
                this.AvailableSlots = e.availableSlots;
                OnLoaded(new EventLoadedEventArgs { EventLoaded = this, ErrorMessage = null });
            }
            else OnLoaded(new EventLoadedEventArgs { ErrorMessage = "This " + this.Type.ToString() + " is not playable !" });
        }

        public override bool Start(List<EventSlot> slots)
        {
            if (loadedRace != null && slots.Count > Race.MIN_PLAYERS_IN_RACE)
            {
                base.Start(slots);
                loadedRace.Prepare(slots);
                Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.White + " The " + this.Type.ToString() + " is starting, you cannot longer join it !");
                this.Status = EventStatus.Running;
                loadedRace.Finished += (sender, eventArgs) => { this.End(EventFinishedReason.Terminated); };
                this.OnStarted(new EventArgs { });
                return true;
            }
            else
            {
                Logger.WriteLineAndClose($"RaceEvent.cs - RaceEvent.Start:E: The race {this.loadedRace?.Name ?? "N/A"} cannot be started");
                return false;
            }
        }
        public override void End(EventFinishedReason reason)
        {
            base.End(reason);
            if (this.Status >= EventStatus.Waiting && this.Status != EventStatus.Running)
                Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.Red + " The " + this.Type.ToString() + " has been aborted !");
            this.loadedRace.Unload();
            this.Status = EventStatus.Finished;
            this.OnEnded(new EventFinishedEventArgs { Reason = reason });
        }
        /*
        public override void Restart()
        {
            // TODO: Implement
            this.Status = EventStatus.Running;
        }
        */
    }
}
