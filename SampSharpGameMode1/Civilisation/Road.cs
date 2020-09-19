using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Civilisation
{
    class Road
    {
        public Vector3 Start { get; private set; }
        public Vector3 End { get; private set; }
        public List<Vector3> Steps { get; private set; } // From Start to End

        
    }
}
