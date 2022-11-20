using SampSharp.GameMode;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions
{
    public class GoalEventArgs : EventArgs
    {
        public Player Player;
    }
    internal enum GoalType
    {
        GoTo, // Go to a position
        GoToSneaky, // Go to a position without being detected by target
        KillTarget,
    }
    internal class Goal
    {
        GoalType Type { get; set; }
        BasePlayer Target { get; set; }
        Vector3 TargetPosition { get; set; }

        public event EventHandler<GoalEventArgs> Complete;
        protected virtual void OnComplete(GoalEventArgs e)
        {
            e.Player.Update -= OnPlayerUpdate;
            Complete?.Invoke(this, e);
        }
        internal Goal(List<Player> players, GoalType type, BasePlayer target, Vector3 targetPosition)
        {
            Type = type;
            Target = target;
            TargetPosition = targetPosition;

            foreach(Player p in players)
            {
                p.Update += OnPlayerUpdate;
            }
        }
        internal void Abort(Player player)
        {
            player.Update -= OnPlayerUpdate;
        }

        private void OnPlayerUpdate(object sender, EventArgs args)
        {
            if (Type == GoalType.GoTo || Type == GoalType.GoToSneaky)
            {
                if ((sender as BasePlayer).IsInRangeOfPoint(10f, TargetPosition))
                {
                    OnComplete(new GoalEventArgs { Player = (sender as Player) });
                }
            }
        }

    }
}
