using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Derbys
{
    public enum DerbyPlayerStatus
    {
        Running,
        Spectating
    }
    public class DerbyPlayer
    {
        public DerbyPlayerStatus status;
        public int spectatePlayerIndex;
        public Vector3R startPosition;
    }
}
