using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;

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
        private int idx;
        private Vector3 position;
        private CheckpointType type;
        private float size;
        private VehicleModelType? nextVehicle;
        private NitroEvent nextNitro;
        public int Idx { get => idx; set => idx = value; }

        public Vector3 Position { get => position; set => position = value; }
        public CheckpointType Type { get => type; set => type = value; }
        public float Size { get => size; set => size = value; }
        public VehicleModelType? NextVehicle { get => nextVehicle; set => nextVehicle = value; }
        public NitroEvent NextNitro { get => nextNitro; set => nextNitro = value; }

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
    }
}
