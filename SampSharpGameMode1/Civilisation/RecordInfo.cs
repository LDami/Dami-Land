using SampSharp.GameMode;

namespace SampSharpGameMode1.Civilisation
{
    public class RecordInfo
    {
        public enum RECORD_TYPE
        {
            VEHICLE = 1,
            ONFOOT = 2
        }
        public struct Header
        {
            public int id;
            public RECORD_TYPE recordType; // 1 for vehicle recordings, 2 for on-foot recordings
        }

        public struct VehicleBlock
        {
            public uint time;
            public short vehicleID;
            public ushort lrKeyCode; // Left Right key code
            public ushort udKeyCode; // Up Down key code
            public short additionnalKeyCode;
            public float rotQuaternion1;
            public float rotQuaternion2;
            public float rotQuaternion3;
            public float rotQuaternion4;
            public Vector3 position;
            public Vector3 velocity;
            public float vehicleHealth;
            public byte driverHealth;
            public byte driverArmor;
            public byte driverWeaponID;
            public byte vehicleSirenState;
            public byte vehicleGearState;
            public ushort vehicleTrailerID;
        }
    }
}
