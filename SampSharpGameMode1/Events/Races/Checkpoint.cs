using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Reflection;

namespace SampSharpGameMode1.Events.Races
{
    public class Checkpoint
    {
        public event EventHandler<PlayerEventArgs> PlayerVehicleChanged;
        protected virtual void OnPlayerVehicleChanged(PlayerEventArgs e)
		{
            PlayerVehicleChanged?.Invoke(this, e);
		}

        public enum EnableDisableEvent
        {
            None,
            Enable,
            Disable
        }
        public static string GetEventStringStatus(EnableDisableEvent evt)
        {
            switch (evt)
            {
                case Checkpoint.EnableDisableEvent.None:
                    return "[Unchanged]";
                case Checkpoint.EnableDisableEvent.Enable:
                    return "[" + Color.Green + "Enable" + Color.White + "]";
                case Checkpoint.EnableDisableEvent.Disable:
                    return "[" + Color.Green + "Disable" + Color.White + "]";
                default:
                    return "[Unknown state]";
            }
        }
        public const float DefaultSize = 5.0f;
        public int Idx { get; set; }

        public Vector3 Position { get; set; }
        public CheckpointType Type { get; set; }
        public float Size { get; set; }
        public VehicleModelType? NextVehicle { get; set; }
        public EnableDisableEvent NextNitro { get; set; }
        public EnableDisableEvent NextCollision { get; set; }
        public bool IsNitroCurrentlyActive { get; set; }

        public Checkpoint(int _index, Vector3 _pos, CheckpointType _type, float _size = DefaultSize)
        {
            this.Idx = _index;
            this.Position = _pos;
            this.Type = _type;
            this.Size = _size;
            this.NextVehicle = null;
            this.NextNitro = EnableDisableEvent.None;
            this.NextCollision = EnableDisableEvent.None;
        }
        public Checkpoint(Vector3 _pos, CheckpointType _type, float _size = DefaultSize) : this(-1, _pos, _type, _size)
        { }

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
                if (IsNitroCurrentlyActive)
                    veh.AddComponent(1010);
                player.PutInVehicle(veh);

                // Using a timer of 100ms because of open.mp issue
                SampSharp.GameMode.SAMP.Timer timer = new SampSharp.GameMode.SAMP.Timer(100, false);
                timer.Tick += (sender, e) => veh.Velocity = vVel;

                this.OnPlayerVehicleChanged(new PlayerEventArgs(player));
            }
            if (this.NextNitro == EnableDisableEvent.Enable)
            {
                if (player.InAnyVehicle)
                {
                    BaseVehicle veh = player.Vehicle;
                    if (VehicleComponents.Get(1010).IsCompatibleWithVehicle(veh))
                    {
                        veh.AddComponent(1010);
                        player.Notificate("Nitro enabled !");
                        player.PlaySound(36842);
                    }
                    else
                        Logger.WriteLineAndClose("Checkpoint.cs - Checkpoint.ExecuteEvents:E: Component ID 1010 is not compatible with vehicle id " + veh.Id);
                }
            }
            if (this.NextNitro == EnableDisableEvent.Disable)
            {
                if (player.InAnyVehicle)
                {
                    BaseVehicle veh = player.Vehicle;
                    veh.RemoveComponent(1010);
                    player.Notificate("Nitro disabled !");
                }
            }
            if (this.NextCollision == EnableDisableEvent.Enable)
            {
                player.DisableRemoteVehicleCollisions(false);
                player.Notificate("Collision enabled !");
            }
            if (this.NextCollision == EnableDisableEvent.Disable)
            {
                player.DisableRemoteVehicleCollisions(true);
                player.Notificate("Collision disabled !");
            }
        }
    }
}
