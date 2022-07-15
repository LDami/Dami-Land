using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events
{
    public interface EventCreator
    {
        /// <summary>
        ///     Creates a new event (race, derby, ...)
        /// </summary>
        public void Create();
        /// <summary>
        ///     Loads an existing event
        /// </summary>
        /// <param name="id">ID of the event</param>
        public void Load(int id);
        /// <summary>
        ///     Unloads the event
        /// </summary>
        public void Unload();
        /// <summary>
        ///     Save the event (overwrite)
        /// </summary>
        /// <returns>Returns true if the event has been saved</returns>
        public Boolean Save();
        /// <summary>
        ///     Save the event (create)
        /// </summary>
        /// <param name="name">Name of the event</param>
        /// <returns>Returns true if the event has been saved</returns>
        public Boolean Save(string name);

    }
}
