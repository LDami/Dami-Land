using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampSharpGameMode1.Works
{
    class Tram
    {
        BaseVehicle vehicle;
        BasePlayer player;
        public BaseVehicle Vehicle { get { return vehicle; } }
        public BasePlayer Player { get { return player; } }
        public bool IsAvailable {  get { return player == null; } }
        public Tram(BaseVehicle vehicle)
        {
            this.vehicle = vehicle;
        }
        public void PutPlayer(BasePlayer player)
        {
            this.player = player;
            this.player.PutInVehicle(this.vehicle);
        }
        public void RemovePlayer(BasePlayer player)
        {
            this.player.RemoveFromVehicle();
            this.player = null;
        }
    }
    public class TramWork : WorkBase
    {
        private static List<Tram> trams = new List<Tram>();
        private static DynamicCheckpoint startWorkCheckpoint;
        private static List<Vector3> tramStops;
        public static void Init()
        {
            startWorkCheckpoint = new DynamicCheckpoint(new Vector3(-2271.82f, 533.43f, 35.01f), 5f, 0, streamdistance: 200f);
            startWorkCheckpoint.ShowInWorld(0);
            startWorkCheckpoint.Enter += (object sender, SampSharp.GameMode.Events.PlayerEventArgs e) =>
            {
                if(trams.Any(t => t.IsAvailable))
                {
                    TramWork work = new TramWork();
                    work.StartWork(e.Player as Player);
                }
                else
                {
                    ((Player)e.Player).Notificate("No available tramway");
                }
            };
            trams.Add(new Tram(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Tram, new Vector3(-2264.78f, 540.54f, 35.54f), 180, 1, 1)));
            trams.Add(new Tram(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Tram, new Vector3(-2264.78f, 525.55f, 35.59f), 180, 1, 1)));
            tramStops = new List<Vector3>
            {
                new Vector3(-2264.99, 794.98, 49.85),
                new Vector3(-2264.99, 933.00, 66.96),
                new Vector3(-2264.99, 1067.5, 82.00),
                new Vector3(-1987.00, 1307.87, 7.49),
                new Vector3(-1639.30, 1252.25, 7.49),
                new Vector3(-1537.45, 964.97, 7.49),
                new Vector3(-1728.50, 921.125, 25.12),
                new Vector3(-2001.625, 888.50, 45.74),
                new Vector3(-1886.20, 848.87, 35.49),
                new Vector3(-1601.76, 848.92, 7.99),
                new Vector3(-1623.00, 728.75, 14.76),
                new Vector3(-1867.70, 603.25, 35.49),
                new Vector3(-2006.50, 142.00, 27.99)
            };
        }
        public static void Dispose()
        {
            foreach (Player p in Player.All)
            {
                if (p.pWork is TramWork)
                {
                    p.pWork.StopWork(p);
                }
            }
            startWorkCheckpoint.Dispose();
            startWorkCheckpoint = null;
            trams.ForEach(x => x.Vehicle.Dispose());
            Logger.WriteLineAndClose("TramWork.cs - TramWork.Dispose:I: Tram work disposed");
        }

        private int stopIndex; // Currently displayed checkpoint index
        private DynamicCheckpoint currentCP;
        public void StartWork(Player player)
        {
            if (!player.IsInWork)
            {
                startWorkCheckpoint.HideForPlayer(player);
                trams.Find(t => t.IsAvailable).PutPlayer(player);
                player.pWork = this;
                stopIndex = 0;
                Logger.WriteLineAndClose("TramWork.cs - TramWork.StartWork:I: " + player.Name + " has started Tram work");
                player.SendClientMessage($"Tram work has started. You can leave the work with {ColorPalette.Primary.Main}/leavework");
                player.ExitVehicle += Player_ExitVehicle;
                player.Update += Player_Update;
                SetCheckpoint(player, tramStops[0]);
            }
            else
            {
                if (player.pWork == this)
                    player.SendClientMessage("You are already working in this tram depot");
                else
                    player.SendClientMessage("You must quit your current job before starting a new one");
            }
        }

        private void Player_Update(object sender, SampSharp.GameMode.Events.PlayerUpdateEventArgs e)
        {
            var player = sender as Player;
            if(player.Vehicle == null)
            {
                StopWork(player);
            }
            else
            {
                if (Vector3.Distance(player.Vehicle.Position, tramStops[stopIndex]) < 10)
                {
                    if (Utils.GetKmhSpeedFromVelocity(player.Vehicle.Velocity) < 30)
                    {
                        stopIndex++;
                        if (stopIndex >= tramStops.Count)
                            stopIndex = 0;
                        SetCheckpoint(player, tramStops[stopIndex]);
                    }
                }
            }
        }

        private void Player_ExitVehicle(object sender, SampSharp.GameMode.Events.PlayerVehicleEventArgs e)
        {
            this.StopWork((Player)sender);
        }

        public void StopWork(Player player)
        {
            if (player.IsInWork && player.pWork == this)
            {
                player.ExitVehicle -= Player_ExitVehicle;
                player.Update -= Player_Update;
                player.DisableCheckpoint();
                trams.Find(t => t.Player == player).RemovePlayer(player);
                player.pWork = null;
                startWorkCheckpoint.ShowForPlayer(player);
                player.SendClientMessage("You quit your job");
            }
        }

        public void SetCheckpoint(Player player, Vector3 pos)
        {
            currentCP?.Dispose();
            currentCP = new(tramStops[stopIndex], 5f);
            currentCP.StreamDistance = 4000f;
            currentCP.ShowForPlayer(player);
        }
    }
}
