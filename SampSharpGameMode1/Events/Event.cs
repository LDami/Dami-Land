using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
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
        Derby,
        Mission
    }
    public enum EventFinishedReason
    {
        Aborted,
        Terminated
    }
    public class EventLoadedEventArgs : EventArgs
    {
        public Event EventLoaded { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class EventFinishedEventArgs : EventArgs
    {
        public Event EventFinished { get; set; }
        public EventFinishedReason Reason { get; set; }
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
        public BasePlayer Winner { get; set; }

        public event EventHandler<EventLoadedEventArgs> Loaded;
        protected virtual void OnLoaded(EventLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public event EventHandler<EventArgs> Started;
        protected virtual void OnStarted(EventArgs e)
        {
            Started?.Invoke(this, e);
        }

        public event EventHandler<EventFinishedEventArgs> Ended;
        protected virtual void OnEnded(EventFinishedEventArgs e)
        {
            Ended?.Invoke(this, e);
        }

        public abstract void Load(int _id);

        public void Open()
        {
            Slots = new List<EventSlot>();
            Player.SendClientMessageToAll(Color.Wheat, "[Event]" + Color.White + " The " + this.Type.ToString() + " " + ColorPalette.Secondary.Main + this.Name + Color.White + " will start soon, join it with " + ColorPalette.Primary.Main + "/event join");
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
                if (Slots.Count == Player.All.Count() || !this.HasAvailableSlots() || Player.All.OfType<Player>().Where(x => x.pEvent is null).Count() == 0)
                {
                    this.Start(Slots);
                }
            }
        }

        public void Leave(Player player)
		{
            if (this.Source.IsPlayerSpectating(player))
                this.Source.Eject(player);
            else
            {
                if (Slots.Find(x => x.Player.Equals(player)) != null)
                {
                    this.Source.OnPlayerFinished(player, "Leave");
                }
            }

        }

        public bool HasAvailableSlots()
		{
            return (Slots.Count < AvailableSlots);
		}

        public abstract void End(EventFinishedReason reason);


        public static void SendEventMessageToPlayer(BasePlayer player, string msg)
        {
            player.SendClientMessage(Color.Wheat, "[Event] " + Color.White + msg);
        }
        public static void SendEventMessageToAll(Event evt, string msg)
        {
            foreach (Player p in evt.Source.GetPlayers())
                p.SendClientMessage(Color.Wheat, "[Event] " + Color.White + msg);
        }
    }
}
