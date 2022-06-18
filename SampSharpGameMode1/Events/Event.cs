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
        public EventSource Source { get; set; }
        public int VirtualWorld { get; set; }
        public List<EventSlot> Slots { get; set; }
        public int AvailableSlots { get; set; }

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
            Slots = new List<EventSlot>();
            Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.White + " The " + this.Type.ToString() + " " + Color.PowderBlue + this.Name + Color.White + " will start soon, join it with " + Color.PowderBlue + "/event join");
            this.Status = EventStatus.Waiting;
        }
        public abstract bool Start(List<EventSlot> slots);

        public void Join(Player player)
        {
            if (player.IsConnected && Slots.Find(x => x.Player.Equals(player)) == null && this.HasAvailableSlots())
            {
                Slots.Add(new EventSlot(player, Vector3R.Zero));
                player.pEvent = this;
                player.SendClientMessage(Color.Wheat, "[Event]" + Color.White + " You joined the " + this.Type.ToString() + ", good luck !");
                if (Slots.Count == Player.All.Count() || !this.HasAvailableSlots())
                {
                    this.Start(Slots);
                }
            }
        }

        public void Leave(Player player)
		{
            if (player.IsConnected && Slots.Find(x => x.Player.Equals(player)) != null)
			{
                this.Source.OnPlayerFinished(player, "Leave");
            }

        }

        public bool HasAvailableSlots()
		{
            return (Slots.Count < AvailableSlots);
		}

        public abstract void End();

    }
}
