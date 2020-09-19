using SampSharp.GameMode.SAMP;
using SampSharpGameMode1.Events.Races;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampSharpGameMode1.Events
{
    public enum EventStatus
    {
        NotLoaded,
        Loaded,
        Waiting,
        Running,
        Finished
    }
    public enum EventType
    {
        Unknown,
        Race,
        Derby
    }
    public class EventLoadedEventArgs : EventArgs
    {
        public Event EventLoaded { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class EventStartedOrEndedEventArgs : EventArgs
    {
    }

    public abstract class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EventStatus Status { get; set; }
        public EventType Type { get; set; }

        public List<Player> players = new List<Player>();

        public event EventHandler<EventLoadedEventArgs> Loaded;
        protected virtual void OnLoaded(EventLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public event EventHandler<EventStartedOrEndedEventArgs> Started;
        protected virtual void OnStarted(EventStartedOrEndedEventArgs e)
        {
            Started?.Invoke(this, e);
        }

        public event EventHandler<EventStartedOrEndedEventArgs> Ended;
        protected virtual void OnEnded(EventStartedOrEndedEventArgs e)
        {
            Ended?.Invoke(this, e);
        }

        public abstract void Load();

        public void Open()
        {
            players.Clear();
            Player.SendClientMessageToAll("[Event] The " + this.Type.ToString() + " " + this.Name + " will start soon, join it with " + Color.AliceBlue + "/event join");
            this.Status = EventStatus.Waiting;
        }
        public abstract void Start();

        public void Join(Player player)
        {
            if (player.IsConnected && !players.Contains(player))
            {
                players.Add(player);
                player.SendClientMessage("[Event] You joined the " + this.Type.ToString() + ", good luck !");
                if(players.Count == Player.All.Count())
                {
                    this.Start();
                }
            }
        }

        public abstract void End();

    }
}
