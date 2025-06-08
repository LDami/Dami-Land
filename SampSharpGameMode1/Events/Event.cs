using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SampSharpGameMode1.Events
{
    public class AnnounceHUD : HUD
    {
        public AnnounceHUD(Player player) : base(player, "eventannounce.json") // TODO: Improve to use Global Textdraws instead of Player Textdraws
        {
            this.Hide();
        }
        public void Open(Event evt)
        {
            layer.SetTextdrawText("eventtype", $"A {evt.Type} is starting soon");
            layer.SetTextdrawText("eventname", evt.Name);
            layer.SetTextdrawText("joincommand", "/join to join the event");
            this.Show();
        }
    }

    class SpectatingHUD : HUD
    {
        List<Player> players;
        int currentSpectatingIndex = 0;

        public SpectatingHUD(Player player, List<Player> _players) : base(player, "eventspectating.json")
        {
            player.KeyStateChanged += Player_KeyStateChanged;
            players = _players;
            this.DynamicDuplicateLayer("playernamelabel#", players.Count, "playerlistbg");
            this.DynamicDuplicateLayer("playername#", players.Count, "playerlistbg");

            SetPlayersList(players);

            layer.SetClickable("leavebutton");
            int idx = 0;
            foreach (Player p in players)
            {
                layer.SetClickable($"playername[{idx}]");
                layer.SetTextdrawText($"playernamelabel[{idx++}]", p.Name);
            }
            layer.TextdrawClicked += Layer_TextdrawClicked;
            SetSpectatingPlayer(0);
        }

        private void Layer_TextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            if(e.TextdrawName == "leavebutton")
            {
                (player as Player).pEvent.Leave(player as Player);
            }
            else if(e.TextdrawName.StartsWith("playername"))
            {
                Regex regex = new(@"playername\[(\d*)\]");
                try
                {
                    if (int.TryParse(regex.Matches(e.TextdrawName).First().Groups[1].Value, out int index))
                    {
                        SetSpectatingPlayer(index);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLineAndClose("MapObjectSelector.cs - MapObjectSelect.Layer_TextdrawClicked:E: " + ex.Message);
                }
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
                player.SelectTextDraw(ColorPalette.Secondary.Main.GetColor());
            }
            if (e.NewKeys == SampSharp.GameMode.Definitions.Keys.Fire)
            {
                SetSpectatingPlayer(currentSpectatingIndex + 1);
            }
            if (e.NewKeys == SampSharp.GameMode.Definitions.Keys.Aim)
            {
                SetSpectatingPlayer(currentSpectatingIndex - 1);
            }
        }

        public void SetPlayersList(List<Player> players)
        {
            this.players = players;
            int idx = 0;
            foreach(Player p in players)
            {
                layer.SetClickable($"playername[{idx}]");
                layer.SetTextdrawText($"playernamelabel[{idx++}]", p.Name);
            }
        }

        private void SetSpectatingPlayer(int newIndex)
        {
            if (newIndex >= players.Count)
                newIndex = 0;
            else if(newIndex < 0)
                newIndex = players.Count - 1;

            player.SpectateVehicle((player as Player).pEvent.Source.GetPlayers()[newIndex].Vehicle);
            layer.SetTextdrawColor($"playername[{currentSpectatingIndex}]", Color.White);
            currentSpectatingIndex = newIndex;
            layer.SetTextdrawColor($"playername[{newIndex}]", Color.Wheat);
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
            Player.SendClientMessageToAll(Color.Wheat,
                $"[Event] {Color.White} The {this.Type} {ColorPalette.Secondary.Main}{this.Name}{Color.White} is starting soon, join it with {ColorPalette.Primary.Main}/join"
                );
            Player.SendClientMessageToAll(Color.Wheat,
                $"[Event] {Color.White} You can also spectate it with {ColorPalette.Primary.Main}/specevent"
                );
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

        public void Join(Player player, bool spectateMode)
        {
            if (player.IsConnected && Slots.Find(x => x.Player.Equals(player)) == null && this.HasAvailableSlots())
            {
                Slots.Add(new EventSlot(player, Vector3R.Zero, spectateMode));
                player.pEvent = this;
                if(spectateMode)
                    player.SendClientMessage(Color.Wheat, "[Event]" + Color.White + " You will spectate the " + this.Type.ToString() + ", have fun !");
                else
                    player.SendClientMessage(Color.Wheat, "[Event]" + Color.White + " You joined the " + this.Type.ToString() + ", good luck !");
                player.AnnounceHUD.SetText("joincommand", "~g~~h~You're in !");
                if (Slots.Count == Player.All.Count() || !this.HasAvailableSlots() || !Player.All.OfType<Player>().Where(x => x.pEvent is null).Any())
                {
                    this.Start(Slots);
                }
            }
        }

        public void Leave(Player player)
		{
            if (this.Source.IsPlayerSpectating(player))
            {
                RemoveFromSpectating(player);
                this.Source.Eject(player);
            }
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
            if (!this.Source.IsPlayerSpectating(player) && (this.Status == EventStatus.Waiting || this.Status == EventStatus.Running))
            {
                if(player.pEvent != this) // is not in the event
                {
                    player.pEvent = this;
                    this.Source.AddSpectator(player);
                }
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

        public void UpdateSpectatingPlayersHUD(Player player)
        {
            if (!this.Source.IsPlayerSpectating(player) && (this.Status == EventStatus.Waiting || this.Status == EventStatus.Running))
            {
                if(spectatingPlayersHUD.ContainsKey(player))
                {
                    spectatingPlayersHUD[player].SetPlayersList(this.Source.GetPlayers());
                }
            }
        }
    }
}
