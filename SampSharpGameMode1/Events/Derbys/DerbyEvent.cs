using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Derbys
{
	class DerbyEvent : Event
    {
        private Derby loadedDerby;

        public DerbyEvent(int eventId)
        {
            if (eventId > 0)
            {
                this.Id = eventId;
                this.VirtualWorld = (int)VirtualWord.Events + eventId;
            }
        }

        public override void Load(int _id)
        {
            Derby loadingDerby = new Derby();
            loadingDerby.Loaded += LoadingDerby_Loaded;
            loadingDerby.Load(_id, this.VirtualWorld);
        }

        private void LoadingDerby_Loaded(object sender, DerbyLoadedEventArgs e)
        {
            this.Type = EventType.Derby;
            if (e.derby.IsPlayable())
            {
                loadedDerby = e.derby;
                this.Name = e.derby.Name;
                this.Status = EventStatus.Loaded;
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
                loadedDerby.Prepare(slots);
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
