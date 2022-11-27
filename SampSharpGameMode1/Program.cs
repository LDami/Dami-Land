using SampSharp.Core;
using SampSharp.Core.Logging;

namespace SampSharpGameMode1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new GameModeBuilder()
                .Use<GameMode>()
                .UseLogLevel(CoreLogLevel.Verbose)
                .Run();
        }
    }
}
