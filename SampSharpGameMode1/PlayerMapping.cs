using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    class PlayerMapping
    {
        const int MAX_OBJECTS = 1000;

        Player player;
        Boolean isInMappingMode;
        public Boolean IsInMappingMode { get => isInMappingMode; private set => isInMappingMode = value; }

        PlayerObject playerObject = null;
        PlayerTextLabel[] textLabel = new PlayerTextLabel[MAX_OBJECTS];
        
        public PlayerMapping(Player _player)
        {
            player = _player;
            player.KeyStateChanged += Player_KeyStateChanged;
            isInMappingMode = false;
        }

        private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            switch(e.NewKeys)
            {                
                case SampSharp.GameMode.Definitions.Keys.AnalogLeft:
                    playerObject.Rotation = Vector3.SmoothStep(playerObject.Rotation, new Vector3(playerObject.Rotation.X, playerObject.Rotation.Y, playerObject.Rotation.Z - 10.0), 10.0f);
                    break;

                case SampSharp.GameMode.Definitions.Keys.AnalogRight:
                    playerObject.Rotation = Vector3.SmoothStep(playerObject.Rotation, new Vector3(playerObject.Rotation.X, playerObject.Rotation.Y, playerObject.Rotation.Z + 10.0), 10.0f);
                    break;
                
            }
        }

        public void Enter()
        {
            isInMappingMode = true;

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

        public void AddObject(int modelid, Vector3? position = null, Vector3? rotation = null)
        {
            playerObject = new PlayerObject(
                player,
                modelid,
                position ?? new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z),
                rotation ?? new Vector3(0.0, 0.0, 0.0));

            playerObject.Edit();
            textLabel[playerObject.Id] = new PlayerTextLabel(player, playerObject.Id.ToString(), Color.White, playerObject.Position, 100.0f);
            player.SendClientMessage($"Object #{playerObject.Id} created with model {modelid}");
        }
        public void DelObject(int objectid)
		{
            PlayerObject.Find(player, objectid)?.Dispose();
            textLabel[objectid].Dispose();
            player.Notificate("Object deleted");
        }
        public void ReplaceObject(int objectid, int modelid)
		{
            PlayerObject obj = PlayerObject.Find(player, objectid);
            if (!(obj is null))
            {
                DelObject(objectid);
                AddObject(modelid, obj.Position, obj.Rotation);
                player.Notificate("Object replaced");
            }
            else
                player.SendClientMessage("Unknown object id");
        }
        public void EditObject(int objectid)
		{
            PlayerObject obj = PlayerObject.Find(player, objectid);
            if (!(obj is null))
            {
                playerObject = obj;
                playerObject.Edit();
            }
        }

    }
}
