using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions
{
    class MissionEvent : Event
    {
        private Mission loadedMission;

        public MissionEvent(int eventId)
        {
            if (eventId > 0)
            {
                this.Id = eventId;
                this.VirtualWorld = (int)VirtualWord.Events + eventId;
            }
        }

        public override void Load(int _id)
        {
            Mission loadingMission = new Mission();
            loadingMission.Loaded += LoadingMission_Loaded;
            loadingMission.Load(_id, this.VirtualWorld);
        }

        private void LoadingMission_Loaded(object sender, MissionLoadedEventArgs e)
        {
            if (e.Mission.IsPlayable())
            {
                loadedMission = e.Mission;
                this.Name = e.Mission.Name;
                this.Status = EventStatus.Loaded;
                this.Type = EventType.Mission;
                this.Source = loadedMission;
                this.AvailableSlots = e.availableSlots;
                OnLoaded(new EventLoadedEventArgs { EventLoaded = this, ErrorMessage = null });
            }
            else OnLoaded(new EventLoadedEventArgs { ErrorMessage = "This " + this.Type.ToString() + " is not playable !" });
        }

        public override bool Start(List<EventSlot> slots)
        {
            if (loadedMission != null && slots.Count > 10)
            {
                loadedMission.Prepare(slots);
                Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.White + " The " + this.Type.ToString() + " is starting, you cannot longer join it !");
                this.Status = EventStatus.Running;
                loadedMission.Finished += (sender, eventArgs) => { this.End(EventFinishedReason.Terminated); };
                this.OnStarted(new EventArgs { });
                return true;
            }
            else
            {
                Logger.WriteLineAndClose($"MissionEvent.cs - MissionEvent.Start:E: The Mission {this.loadedMission?.Name ?? "N/A"} cannot be started");
                return false;
            }
        }
        public override void End(EventFinishedReason reason)
        {
            if (reason == EventFinishedReason.Aborted)
                Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.Red + " The " + this.Type.ToString() + " has been aborted !");
            this.loadedMission.Unload();
            this.Status = EventStatus.Finished;
            this.OnEnded(new EventFinishedEventArgs { Reason = reason });
        }
    }
}
