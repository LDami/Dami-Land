using SampSharp.GameMode;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    class PlayerMapping
    {
        Player player;
        Boolean isInMappingMode;
        public Boolean IsInMappingMode { get => isInMappingMode; private set => isInMappingMode = value; }

        PlayerObject playerObject = null;
        
        public PlayerMapping(Player _player)
        {
            player = _player;
            player.KeyStateChanged += Player_KeyStateChanged;
            isInMappingMode = false;
        }

        private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            //if(e.NewKeys.ToString() != "0") player.GameText(e.NewKeys.ToString(), 100, 3);
            switch(e.NewKeys.ToString())
            {
                case "walk":
                    {
                        if (playerObject.Owner == player)
                            playerObject.Dispose();
                        break;
                    }
                /*
                case "analogleft":
                    {
                        playerObject.Rotation = Vector3.SmoothStep(playerObject.Rotation, new Vector3(playerObject.Rotation.X, playerObject.Rotation.Y - 1.0, playerObject.Rotation.Y), 1.0f);
                        break;
                    }
                case "analogright":
                    {
                        playerObject.Rotation = Vector3.SmoothStep(playerObject.Rotation, new Vector3(playerObject.Rotation.X, playerObject.Rotation.Y + 1.0, playerObject.Rotation.Y), 1.0f);
                        break;
                    }
                */
            }
        }

        public void Enter()
        {
            isInMappingMode = true;

            playerObject = new PlayerObject(
                player,
                3459,
                new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z),
                new Vector3(0.0, 0.0, 0.0));

            playerObject.Edit();

            player.GameText("Map mode loaded", 1000, 3);
        }
        public void Exit()
        {
            isInMappingMode = false;

            playerObject = null;

            player.GameText("Map mode unloaded", 1000, 3);
        }
        public void Update()
        {
            if (playerObject != null)
            {
                string str = string.Format("Position de l'objet: {0} ; {1} ; {2}", playerObject.Position.X, playerObject.Position.Y, playerObject.Position.Z);
                player.SendClientMessage(str);
            }
        }
    }
}
