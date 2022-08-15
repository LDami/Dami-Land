using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events
{
    public interface EventSource
    {
        #region Callbacks
        /// <summary>
        ///     Called when the player disconnects from the game
        /// </summary>
        /// <param name="sender">Player</param>
        /// <param name="e">Event argument</param>
        public void OnPlayerDisconnect(object sender, SampSharp.GameMode.Events.DisconnectEventArgs e);
        /// <summary>
        ///     Called when the player's vehicle is destroyed during the event
        /// </summary>
        /// <param name="sender">Vehicle that exploded</param>
        /// <param name="e">Event argument</param>
        public void OnPlayerVehicleDied(object sender, SampSharp.GameMode.Events.PlayerEventArgs e);
        #endregion
        #region Methods
        /// <summary>
        ///     Loads the event's data on memory
        /// </summary>
        /// <param name="id">ID of the event</param>
        /// <param name="virtualworld">VirtualWorld where to load event data, ignored if the EventSource does not need specific VirtualWorld</param>
        public void Load(int id, int virtualworld = -1);
        /// <summary>
        ///     Determines if the event is playable
        /// </summary>
        /// <returns>Return true if the event is playable, else false</returns>
        public Boolean IsPlayable();
        /// <summary>
        ///     Teleports all the players to the event and prepare them for the start
        /// </summary>
        /// <param name="players">List of Players who joined</param>
        /// <param name="virtualWorld">VirtualWorld of the event</param>
        public void Prepare(List<EventSlot> slots);
        /// <summary>
        ///     Start the event
        /// </summary>
        public void Start();
        /// <summary>
        ///     Executes all scripts to proceed to the end of the event for a player (rank registration, putting player in spectator mode, ...)
        /// </summary>
        /// <param name="player">Player to eject</param>
        public void OnPlayerFinished(Player player, string reason);
        /// <summary>
        ///     Teleports the player out of the event (event finished, kicked out before launched, ...)
        ///     Must not be called when the event is running
        /// </summary>
        /// <param name="player">Player to eject</param>
        public void Eject(Player player);
        /// <summary>
        ///     Unload all event elements (maps, players, vehicles)
        /// </summary>
        public void Unload();
        /// <summary>
        ///     Returns true if the player is spectating the event
        /// </summary>
        /// <param name="player">Player</param>
        /// <returns>True if player is spectating the event, False otherwise</returns>
        public bool IsPlayerSpectating(Player player);
        /// <summary>
        ///     Returns all the players playing in the event
        /// </summary>
        /// <returns>List of players</returns>
        public List<Player> GetPlayers();
        #endregion
    }
}
