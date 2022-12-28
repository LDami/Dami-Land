using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Works
{
    struct DepositPoint
    {
        public Vector3 Position;
        public string Name;
    }
    public class TruckWork : WorkBase
    {
        private const int MAX_TRAILER_PACKAGES = 100;
        public static Vector3 StartPosition { get; private set; }
        private static List<BaseVehicle> vehicles;
        private static List<BaseVehicle> trailers;
        private static List<DepositPoint> depositPoints;
        private static DepositPoint truckDepot = new DepositPoint { Name = "Truck Depot", Position = new Vector3(2824.53, 915.35, 11.33) };
        private static DynamicCheckpoint startWorkCheckpoint;
        public static void Init()
        {
            StartPosition = new Vector3(2814.61, 969.70, 10.75);
            startWorkCheckpoint = new DynamicCheckpoint(StartPosition, 5f, 0, streamdistance: 200f);
            startWorkCheckpoint.ShowInWorld(0);
            startWorkCheckpoint.Enter += (object sender, SampSharp.GameMode.Events.PlayerEventArgs e) =>
            {
                TruckWork work = new TruckWork();
                work.StartWork(e.Player as Player);
            };

            vehicles = new List<BaseVehicle>();
            vehicles.Add(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Tanker, new Vector3(2838.61, 980.88, 11.34), 180, 0, 0));
            vehicles.Add(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Tanker, new Vector3(2859.47, 937.65, 11.34), 270, 0, 0));
            vehicles.Add(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Tanker, new Vector3(2859.47, 931.92, 11.34), 270, 0, 0));

            trailers = new List<BaseVehicle>();
            trailers.Add(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.ArticleTrailer, new Vector3(2855.2, 895.5, 10.67), 0, 0, 0));
            trailers.Add(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.ArticleTrailer, new Vector3(2827.44, 895.5, 10.68), 0, 0, 0));
            trailers.Add(BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.ArticleTrailer, new Vector3(2817.93, 895.5, 10.67), 0, 0, 0));

            depositPoints = new List<DepositPoint>
            {
                new DepositPoint { Position = new Vector3(2186.92, 1476.19, 11.38), Name = "Las Venturas Royal Casino" },
                new DepositPoint { Position = new Vector3(1692.49, 1760.29, 11.39), Name = "Las Venturas Binco" },
                new DepositPoint { Position = new Vector3(1070.48, 1457.05, 6.39), Name = "Dirtring backstage" },
                new DepositPoint { Position = new Vector3(1138.64, 1917.85, 11.39), Name = "External storage" },
                new DepositPoint { Position = new Vector3(1492.214, 2132.76, 11.34), Name = "Bandito Baseball club" },
                new DepositPoint { Position = new Vector3(2783.2, 2575.85, 11.4), Name = "Las Venturas commercial center" },
                new DepositPoint { Position = new Vector3(2228.28, 72.93, 27), Name = "Palomino Creek" },
                new DepositPoint { Position = new Vector3(1345.45, 280.97, 20.14), Name = "Montgomery Sprunk factory" },
            };
        }


        private DepositPoint currentDepositPoint;
        private DynamicCheckpoint currentCP;
        private int currentRound;
        private bool isTrailerAttached;
        private int trailerPackages;
        public void StartWork(Player player)
        {
            if (!player.IsInWork)
            {
                startWorkCheckpoint.HideForPlayer(player);
                player.pWork = this;
                currentRound = 0;
                Logger.WriteLineAndClose("TruckWork.cs - TruckWork.StartWork:I: " + player.Name + " has started Truck work");
                player.SendClientMessage("Truck work has started, please take one of the trucks");
                player.EnterVehicle += (sender, e) =>
                {
                    if (vehicles.Contains(e.Vehicle) && !e.IsPassenger)
                    {
                        if (e.Vehicle.Trailer == null)
                        {
                            isTrailerAttached = false;
                            player.SendClientMessage("Great, now attach a trailer");
                        }
                        else
                        {
                            isTrailerAttached = true;
                            GoToDepot(player);
                        }
                    }
                };
                player.Update += Player_Update;
            }
            else
                player.SendClientMessage("You must quit your current job before");
        }

        private void Player_Update(object sender, SampSharp.GameMode.Events.PlayerUpdateEventArgs e)
        {
            Player p = sender as Player;
            if(p.InAnyVehicle)
            {
                if(p.Vehicle.Trailer == null)
                {
                    if(isTrailerAttached)
                    {
                        Logger.WriteLineAndClose("TruckWork.cs - TruckWork.Player_Update:I: " + p.Name + " has detached his trailer");
                        currentCP.HideForPlayer(p);
                    }
                    isTrailerAttached = false;
                }
                else
                {
                    if (!isTrailerAttached)
                    {
                        Logger.WriteLineAndClose("TruckWork.cs - TruckWork.Player_Update:I: " + p.Name + " has atached a trailer");
                        if (currentCP == null)
                            SetNewDeposit(p);
                        else
                            currentCP.ShowForPlayer(p);
                    }
                    isTrailerAttached = true;
                }
            }
        }

        public void SetNewDeposit(Player player, bool decrementTrailerPackages = true)
        {
            Random rand = new Random();
            if(decrementTrailerPackages)
                trailerPackages -= rand.Next(10, 30);
            if (trailerPackages <= 0)
            {
                GoToDepot(player);
            }
            else
            {
                currentDepositPoint = depositPoints[rand.Next(depositPoints.Count - 1)];

                if (currentCP != null)
                    currentCP.Dispose();
                currentCP = new DynamicCheckpoint(currentDepositPoint.Position, 5f);
                currentCP.StreamDistance = 4000f;
                // Maybe currentCP.HideForAll before ?
                currentCP.ShowForPlayer(player);
                currentCP.Enter += (sender, e) =>
                {
                    SetNewDeposit(e.Player as Player);
                };

                player.SendClientMessage($"Next destination: {ColorPalette.Primary.Main}{currentDepositPoint.Name}{Color.White} ({trailerPackages}/{MAX_TRAILER_PACKAGES} packages left)");
            }
        }

        public void GoToDepot(Player player)
        {
            trailerPackages = 0;
            currentDepositPoint = truckDepot;

            if (currentCP != null)
                currentCP.Dispose();
            currentCP = new DynamicCheckpoint(currentDepositPoint.Position, 5f);
            currentCP.StreamDistance = 4000f;
            // Maybe currentCP.HideForAll before ?
            currentCP.ShowForPlayer(player);
            currentCP.Enter += (sender, e) =>
            {
                trailerPackages = 100;
                SetNewDeposit(e.Player as Player, false);
            };
            player.SendClientMessage($"Next destination: {ColorPalette.Primary.Main}{currentDepositPoint.Name}{Color.White}");
            if (currentRound > 0)
            {
                int money = currentRound * 1500;
                player.GiveMoney(money);
                player.Notificate($"~g~+ {money}$");
            }
        }
    }
}
