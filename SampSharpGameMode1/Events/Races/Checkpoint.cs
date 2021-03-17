using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.World;

namespace SampSharpGameMode1.Events.Races
{
    public class Checkpoint
    {
        public enum NitroEvent
        {
            None,
            Give,
            Remove
        }
        public const float DefaultSize = 3.0f;
        public int Idx { get; set; }

        public Vector3 Position { get; set; }
        public CheckpointType Type { get; set; }
        public float Size { get; set; }
        public VehicleModelType? NextVehicle { get; set; }
        public NitroEvent NextNitro { get; set; }

        public Checkpoint(Vector3 _pos, CheckpointType _type, float _size = DefaultSize)
        {
            this.Position = _pos;
            this.Type = _type;
            this.Size = _size;
            this.NextVehicle = null;
            this.NextNitro = NitroEvent.None;
        }
        public Checkpoint(int _index, Vector3 _pos, CheckpointType _type, float _size = DefaultSize)
        {
            this.Idx = _index;
            this.Position = _pos;
            this.Type = _type;
            this.Size = _size;
            this.NextVehicle = null;
            this.NextNitro = NitroEvent.None;
        }

        public void ExecuteEvents(Player player)
        {
            if (this.NextVehicle != null)
            {
                float vRot = player.Rotation.X;
                Vector3 vVel = Vector3.Zero;
                if (player.InAnyVehicle)
                {
                    vRot = player.Vehicle.Rotation.Z;
                    vVel = player.Vehicle.Velocity;
                    BaseVehicle vehicle = player.Vehicle;
                    player.RemoveFromVehicle();
                    vehicle.Dispose();
                }

                BaseVehicle veh = BaseVehicle.Create(this.NextVehicle.GetValueOrDefault(VehicleModelType.Ambulance), player.Position, vRot, 1, 1);
                veh.VirtualWorld = player.VirtualWorld;
                veh.Engine = true;
                veh.Doors = true;
                veh.Died += player.pEvent.Source.OnPlayerVehicleDied;
                player.PutInVehicle(veh);
                veh.Velocity = vVel;
            }
            if (this.NextNitro == NitroEvent.Give)
            {
                if (player.InAnyVehicle)
                {
                    BaseVehicle veh = player.Vehicle;
                    if (VehicleComponents.Get(1010).IsCompatibleWithVehicle(veh))
                    {
                        veh.AddComponent(1010);
                        player.Notificate("Nitro added !");
                        player.PlaySound(36842);
                    }
                    else
                        Logger.WriteLineAndClose("Checkpoint.cs - Checkpoint.ExecuteEvents:E: Component ID 1010 is not compatible with vehicle id " + veh.Id);
                }
            }
            if (this.NextNitro == NitroEvent.Remove)
            {
                if (player.InAnyVehicle)
                {
                    BaseVehicle veh = player.Vehicle;
                    veh.RemoveComponent(1010);
                    player.Notificate("Nitro removed !");
                }
            }
        }
    }
}
