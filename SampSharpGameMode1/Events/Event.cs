using Microsoft.VisualBasic;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampSharpGameMode1.Events
{
    public class AnnounceHUD : HUD
    {
        public AnnounceHUD(Player player) : base(player, "event-announce.json") // TODO: Improve to use Global Textdraws instead of Player Textdraws
        {
            this.Hide();
        }
        public void Open(Event evt)
        {
            layer.SetTextdrawText("eventtype", $"A {evt.Type} is starting soon");
            layer.SetTextdrawText("eventname", evt.Name);
            layer.SetTextdrawText("joincommand", $"/join to join the {evt.Type}");
            this.Show();
        }
    }

    class SpectatingHUD : HUD
    {
        List<Player> players;

        public SpectatingHUD(Player player, List<Player> players) : base(player, "eventspectating.json")
        {
            player.KeyStateChanged += Player_KeyStateChanged;
            this.DynamicDuplicateLayer("playername#", players.Count, "playerlistbg");

            SetPlayersList(players);

            layer.SetClickable("leavebutton");
            int idx = 0;
            foreach (Player p in players)
            {
                layer.SetClickable("playername" + idx++);
            }
            layer.TextdrawClicked += Layer_TextdrawClicked;
        }

        private void Layer_TextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            if(e.TextdrawName == "leavebutton")
            {
                (sender as Player).pEvent.Leave(sender as Player);
            }
            else if(e.TextdrawName.StartsWith("playername"))
            {
                int idx = Convert.ToInt16(e.TextdrawName.Substring("playername".Length, e.TextdrawName.Length - "playername".Length));
                player.SpectateVehicle((sender as Player).pEvent.Source.GetPlayers()[idx].Vehicle);
            }
            player.CancelSelectTextDraw();
        }

        public void Dispose()
        {
            player.KeyStateChanged -= Player_KeyStateChanged;
            this.Unload();
        }

        private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            if(e.NewKeys == SampSharp.GameMode.Definitions.Keys.Crouch)
            {
                player.SelectTextDraw(ColorPalette.Primary.Main.GetColor());
            }
        }

        public void SetPlayersList(List<Player> players)
        {
            this.players = players;
            int idx = 0;
            foreach(Player p in players)
            {
                layer.SetTextdrawText("playername" + idx++, p.Name);
            }
        }
    }

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

        private Dictionary<Player, SpectatingHUD> spectatingPlayersHUD = new();

        public event EventHandler<EventLoadedEventArgs> Loaded;
        protected virtual void OnLoaded(EventLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
        }

        public event EventHandler<EventArgs> Started;
        protected virtual void OnStarted(EventArgs e)
        {
            Started?.Invoke(this, e);
            foreach (Player player in Player.All.Cast<Player>())
            {
                player.AnnounceHUD.Hide();
            }
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
            foreach(Player player in Player.All.Cast<Player>())
            {
                player.AnnounceHUD.Open(this);
            }
        }
        public virtual bool Start(List<EventSlot> slots)
        {
            foreach (Player player in Player.All.Cast<Player>())
            {
                player.AnnounceHUD.Hide();
            }
            return true;
        }

        public void Join(Player player)
        {
            if (player.IsConnected && Slots.Find(x => x.Player.Equals(player)) == null && this.HasAvailableSlots())
            {
                Slots.Add(new EventSlot(player, Vector3R.Zero));
                player.pEvent = this;
                player.SendClientMessage(Color.Wheat, "[Event]" + Color.White + " You joined the " + this.Type.ToString() + ", good luck !");
                player.AnnounceHUD.Hide("joinCommand");
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

        public virtual void End(EventFinishedReason reason)
        {
            if(reason == EventFinishedReason.Aborted)
            {
                foreach (Player player in Player.All.Cast<Player>())
                {
                    player.AnnounceHUD.Hide();
                }
            }
        }


        public static void SendEventMessageToPlayer(BasePlayer player, string msg)
        {
            player.SendClientMessage(Color.Wheat, "[Event] " + Color.White + msg);
        }
        public static void SendEventMessageToAll(Event evt, string msg)
        {
            foreach (Player p in evt.Source.GetPlayers())
                p.SendClientMessage(Color.Wheat, "[Event] " + Color.White + msg);
        }

        public void SetPlayerInSpectator(Player player)
        {
            if (!this.Source.IsPlayerSpectating(player) && this.Status == EventStatus.Running)
            {
                spectatingPlayersHUD[player] = new SpectatingHUD(player, this.Source.GetPlayers());
                player.ToggleSpectating(true);
                player.SpectateVehicle(this.Source.GetPlayers()[0].Vehicle);
            }
        }

        public void RemoveFromSpectating(Player player)
        {
            if (this.Source.IsPlayerSpectating(player))
            {
                spectatingPlayersHUD[player].Dispose();
                player.ToggleSpectating(false);
            }
        }
    }
}
