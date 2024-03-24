using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Civilisation
{
    internal class Record
    {
        public RecordInfo.Header Header { get; set; }
        public List<RecordInfo.Block> Blocks { get; set; } = new List<RecordInfo.Block>();
    }
}
