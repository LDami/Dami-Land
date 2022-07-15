using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampSharpGameMode1.Events.Races
{
    internal class CheckpointLiveInfo
    {
        public struct Rank
        {
            public int Pos;
            public TimeSpan Time;
        }
        private Dictionary<Player, Rank> ranking = new Dictionary<Player, Rank>();
        public Dictionary<Player, Rank> Ranking { get { return ranking.OrderBy(x => x.Value.Pos).ToDictionary(x => x.Key, y => y.Value); } }

        public void Add(Player p, TimeSpan t)
        {
            ranking.Add(p, new Rank { Pos = ranking.Count + 1, Time = t });
        }

        public Rank GetRankForPlayer(Player p)
        {
            Ranking.TryGetValue(p, out Rank r);
            return r;
        }
    }
}
