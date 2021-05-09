using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Derbys
{
	class DerbyEvent : Event
    {
        private Derby loadedRace;

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
                loadedRace = e.derby;
                this.Name = e.derby.Name;
                this.Status = EventStatus.Loaded;
                this.Type = EventType.Race;
                this.Source = loadedRace;
                OnLoaded(new EventLoadedEventArgs { EventLoaded = this, ErrorMessage = null });
            }
            else OnLoaded(new EventLoadedEventArgs { ErrorMessage = "This race is not playable !" });
        }

        public override void Start()
        {
            if (loadedRace != null && this.players.Count > Derby.MIN_PLAYERS_IN_DERBY)
            {
                loadedRace.Prepare(this.players, 1);
                Player.SendClientMessageToAll("[Event] The " + this.Type.ToString() + " is starting, you cannot longer join it !");
                this.Status = EventStatus.Running;
                loadedRace.Finished += (sender, eventArgs) => { this.End(); };
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
