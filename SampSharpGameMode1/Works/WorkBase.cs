using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Works
{
    public interface WorkBase
    {
        /*
         * C# 11 feature only
         * 
        public static abstract void Init();
        public static abstract void Dispose();
        public static Vector3 StartPosition { get; }
        public static void StartWork(Player player);
        */
        public void StartWork(Player player);
        public void StopWork(Player player);
    }
}
