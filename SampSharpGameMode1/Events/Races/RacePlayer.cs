using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Races
{
    public enum RacePlayerStatus
    {
        Running,
        Spectating
    }
    public class RacePlayer
    {
        public RacePlayerStatus status;
        public int spectatePlayerIndex;
        public Checkpoint nextCheckpoint;
        public TimeSpan record;
        public Vector3R startPosition;
    }
}