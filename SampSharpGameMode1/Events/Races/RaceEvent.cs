using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1.Events.Races
{
    class RaceEvent : Event
    {
        private Race loadedRace;
        private int id;
        public string name { get; set; }
        public EventStatus status { get; set; }

        #region Event
        public event EventHandler<RaceLoadedEventArgs> Loaded;
        public class RaceLoadedEventArgs : EventArgs
        {
            public int RaceID { get; set; }
            public int CheckpointsCount { get; set; }
        }
        #endregion

        public RaceEvent(int _id)
        {
            if (_id > 0)
            {
                this.id = _id;
                loadedRace = new Race();
            }
        }

        public void Load()
        {
            Thread t = new Thread(() =>
            {
                loadedRace.Load(this.id);
                if (loadedRace.IsPlayable())
                {
                    RaceLoadedEventArgs args = new RaceLoadedEventArgs();
                    args.RaceID = this.id;
                    args.CheckpointsCount = loadedRace.checkpoints.Count;
                    this.Loaded(this, args);
                }
            });
            t.Start();
        }

        public void Start()
        {

        }

        public void End()
        {

        }

        public void Join(Player player)
        {

        }
    }
}
