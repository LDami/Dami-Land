using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events
{
    public interface EventSource
    {
        #region Callbacks
        /// <summary>
        ///     Called when the player's vehicle is destroyed during the event
        /// </summary>
        /// <param name="sender">Vehicle that exploded</param>
        /// <param name="e">Event argument</param>
        public void OnPlayerVehicleDied(object sender, SampSharp.GameMode.Events.PlayerEventArgs e);
        #endregion
        #region Methods
        /// <summary>
        ///     Load the event's data on memory
        /// </summary>
        /// <param name="id">ID of the event</param>
        public void Load(int id);
        /// <summary>
        ///     Determine if the event is playable
        /// </summary>
        /// <returns>Return true if the event is playable, else false</returns>
        public Boolean IsPlayable();
        /// <summary>
        ///     Teleport all the players to the event and prepare them for the start
        /// </summary>
        /// <param name="players">List of Players who joined</param>
        /// <param name="virtualWorld">VirtualWorld of the event</param>
        public void Prepare(List<Player> players, int virtualWorld);
        /// <summary>
        ///     Start the event
        /// </summary>
        public void Start();
        /// <summary>
        ///     Teleport the player out of the event (event finished, kicked out, ...)
        /// </summary>
        /// <param name="player">Player to eject</param>
        public void Eject(Player player);
        #endregion
    }
}
