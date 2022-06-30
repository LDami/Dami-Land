﻿using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Derbys
{
	class DerbyEvent : Event
    {
        private Derby loadedDerby;

        public DerbyEvent(int _id)
        {
            if (_id > 0)
            {
                this.Id = _id;
            }
        }

        public override void Load()
        {
            Derby loadingRace = new Derby();
            loadingRace.Loaded += LoadingRace_Loaded;
            loadingRace.Load(this.Id);
        }

        private void LoadingRace_Loaded(object sender, DerbyLoadedEventArgs e)
        {
            if (e.derby.IsPlayable())
            {
                loadedDerby = e.derby;
                this.Name = e.derby.Name;
                this.Status = EventStatus.Loaded;
                this.Type = EventType.Derby;
                this.Source = loadedDerby;
                this.AvailableSlots = e.availableSlots;
                OnLoaded(new EventLoadedEventArgs { EventLoaded = this, ErrorMessage = null });
            }
            else OnLoaded(new EventLoadedEventArgs { ErrorMessage = "This " + this.Type.ToString() + " is not playable !" });
        }

        public override bool Start(List<EventSlot> slots)
        {
            if (loadedDerby != null && slots.Count > Derby.MIN_PLAYERS_IN_DERBY)
            {
                loadedDerby.Prepare(slots, 1);
                Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.White + " The " + this.Type.ToString() + " is starting, you cannot longer join it !");
                this.Status = EventStatus.Running;
                loadedDerby.Finished += (sender, eventArgs) => { this.End(EventFinishedReason.Terminated); };
                this.OnStarted(new EventArgs { });
                return true;
            }
            else
            {
                Logger.WriteLineAndClose($"DerbyEvent.cs - DerbyEvent.Start:E: The derby {this.loadedDerby?.Name ?? "N/A"} cannot be started");
                return false;
            }
        }
        public override void End(EventFinishedReason reason)
        {
            if (reason == EventFinishedReason.Aborted)
                Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.Red + " The " + this.Type.ToString() + " has been aborted !");
            this.loadedDerby.Unload();
            this.Status = EventStatus.Finished;
            this.OnEnded(new EventFinishedEventArgs { Reason = reason });
        }
    }
}
