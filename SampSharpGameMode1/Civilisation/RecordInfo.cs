using SampSharp.GameMode;
using System.Collections;

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
            public Header(int _id, RECORD_TYPE _recordType)
            {
                id = 1000;
                recordType = _recordType;
            }
            public int id;
            public RECORD_TYPE recordType; // 1 for vehicle recordings, 2 for on-foot recordings
        }

        public class Block
        {
            public uint time;
            public short additionnalKeyCode;
            public float rotQuaternion1;
            public float rotQuaternion2;
            public float rotQuaternion3;
            public float rotQuaternion4;
            public Vector3 position;
        }

        public class PedBlock : Block
        {
            public short lrKeyCode; // Left Right key code
            public short udKeyCode; // Up Down key code
            public byte health;
            public byte armor;
            public byte weapon;
            public byte currentlyAppliedSpecialAction;
            public Vector3 currentVelocity;
            public Vector3 currentSurfing;
            public ushort currentlySurfingVehicleID;
            public ushort currentlyAppliedAnimationIndex;
            public short someAnimationParams; // Need more investigation
        }

        public class VehicleBlock : Block
        {
            public short vehicleID;
            public ushort lrKeyCode; // Left Right key code
            public ushort udKeyCode; // Up Down key code
            public Vector3 velocity;
            public float vehicleHealth;
            public byte driverHealth;
            public byte driverArmor;
            public byte driverWeaponID;
            public byte vehicleSirenState;
            public byte vehicleGearState;
            public ushort vehicleTrailerID;
            public uint hydraThrustAngle;
        }
    }
}
