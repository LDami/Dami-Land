using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;

namespace SampSharpGameMode1.Race
{
    public enum CheckpointType //0-Normal, 1-Finish, 3-Air normal, 4-Air finish, 5-Air (rotates and stops), 6-Air (increases, decreases and disappears), 7-Air (swings down and up), 8-Air (swings up and down)
    {
        Normal=0,
        Finish=1,
        AirNormal=3,
        AirFinish=4
    }
    class Checkpoint
    {
        public const double DefaultSize = 3.0;
        private Vector3 position;
        private CheckpointType type;
        private double size;
        private VehicleModelType nextVehicle;

        public Vector3 Position { get => position; set => position = value; }
        public CheckpointType Type { get => type; set => type = value; }
        public double Size { get => size; set => size = value; }
        public VehicleModelType NextVehicle { get => nextVehicle; set => nextVehicle = value; }

        public Checkpoint(Vector3 _pos, CheckpointType _type, double _size = DefaultSize)
        {
            this.Position = _pos;
            this.Type = _type;
            this.Size = _size;
            this.NextVehicle = VehicleModelType.Ambulance;
        }
    }
}
