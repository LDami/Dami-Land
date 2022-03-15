using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
	class StoredPlayerObject : PlayerObject
	{
		private int? dbid;
		public int? DbId { get { return dbid; } set { dbid = value; } }
	}
	public class MapCreator
	{
		const int MAX_OBJECTS_PER_MAP = 1000;

		private Player player;
		private List<int> playerObjectIds;
		private PlayerObject[] markers;
		private int editingId;
		private MySQLConnector mySQLConnector = MySQLConnector.Instance();

		public MapCreator(Player p)
		{
			if (p != null && p.IsConnected)
			{
				player = p;
				playerObjectIds = new List<int>();
				markers = new PlayerObject[2];
				p.SendClientMessage("Map creator initialized");
				p.SendClientMessage("/mapping [addo|delo|replace|edit|save]");
			}
			else
				Logger.WriteLineAndClose("MapCreator.cs - MapCreator._:E: MapCreator was initiliazed with an invalid player");
		}

		public void CreateMap()
		{
			editingId = -1;
		}

		public void Save()
		{
			Dictionary<string, object> param = new Dictionary<string, object>();
			if (editingId == -1) // New map
			{
				string name = "";
				InputDialog nameDialog = new InputDialog("Name of the map", "Please enter the name of the map", false, "Save", "Cancel");
				nameDialog.Response += (object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e) => {
					if(e.DialogButton == SampSharp.GameMode.Definitions.DialogButton.Left)
					{
						if (e.InputText.Length < 100 && e.InputText.Length > 3)
							name = e.InputText;
						else
						{
							nameDialog.Message = "Map name must have at least 3 characters and cannot exceed 100 characters";
							nameDialog.Show(player);
						}
					}
				};
				nameDialog.Show(player);
				param.Add("@name", name);
				param.Add("@creator", player.DbId);
				param.Add("@creationdate", DateTime.Now);

				editingId = (int)mySQLConnector.Execute("INSERT INTO maps (map_name, map_creator, map_creationdate) VALUES (@name, @creator, @creationdate)", param);
			}

			string queryUpdate = "UPDATE mapobjects SET obj_pos_x=@posx, obj_pos_y=@posy, obj_pos_z=@posz, obj_rot_x=@rotx, obj_rot_y=@roty, obj_rot_z=@rotz WHERE obj_id=@id;";
			string queryInsert = "INSERT INTO mapobjects (map_id, obj_pos_x, obj_pos_y, obj_pos_z, obj_rot_x, obj_rot_y, obj_rot_z) VALUES (@mapid, @posx, @posy, @posz, @rotx, @roty, @rotz);";
			foreach (StoredPlayerObject obj in StoredPlayerObject.All)
			{
				param.Clear();
				param.Add("posx", obj.Position.X);
				param.Add("posy", obj.Position.Y);
				param.Add("posz", obj.Position.Z);
				param.Add("rotx", obj.Rotation.X);
				param.Add("roty", obj.Rotation.Y);
				param.Add("rotz", obj.Rotation.Z);

				if (obj.DbId != null) // Does not exists in database
				{
					param.Add("@mapid", editingId);
					obj.DbId = (int)mySQLConnector.Execute(queryInsert, param);
				}
				else
				{
					param.Add("@id", obj.DbId);
					mySQLConnector.Execute(queryUpdate, param);
				}

			}
		}

		public void AddObject(int modelid, Vector3? position = null, Vector3? rotation = null)
		{
			PlayerObject tmp = new PlayerObject(
				player,
				modelid,
				position ?? new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z), /* TODO: récupérer la position devant le joueur en fonction de la caméra */
				rotation ?? new Vector3(0.0, 0.0, 0.0));

			tmp.Edit();
			playerObjectIds.Add(tmp.Id);
			player.SendClientMessage($"Object #{tmp.Id} created with model {modelid}");
		}
		public void DelObject(int objectid)
		{
			if(playerObjectIds.Contains(objectid))
			{
				PlayerObject.Find(player, objectid)?.Dispose();
				playerObjectIds.Remove(objectid);
				player.Notificate("Object deleted");
			}
		}
		public void ReplaceObject(int objectid, int modelid)
		{
			if (playerObjectIds.Contains(objectid))
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
		}
		public void EditObject(int objectid)
		{
			PlayerObject.Find(player, objectid)?.Edit();
		}

		public void EditMarker(int marker)
		{
			if(marker > 0 && marker < markers.Length + 1)
			{
				marker --;
				markers[marker] ??= new PlayerObject(player, 19133, player.Position + new Vector3(5.0, 0.0, 0.0), Vector3.Zero);
				markers[marker].Edit();
				markers[marker].Edited += (object sender, SampSharp.GameMode.Events.EditPlayerObjectEventArgs e) =>
				{
					markers[marker].Position = e.Position;
				};
			}
		}

		public void GetMarkersDistance()
		{
			if(markers[0] is null || markers[1] is null)
				player.SendClientMessage("You must set 2 markers to use this command");
			else
			{
				player.SendClientMessage("Distance: " + (markers[0].Position - markers[1].Position).ToString());
			}
		}

		public static void ShowMapList(Player p, string[] keywords)
		{
			MySQLConnector mySQLConnector = MySQLConnector.Instance();
			string whereStatement = "1=1";
			Dictionary<string, object> param = new Dictionary<string, object>();
			for (int i = 0; i < keywords.Length; i++)
			{
				whereStatement += $" AND maps.map_name LIKE '@w{i}' OR users.name LIKE '@w{i}'";
				param.Add($"@w{i}", keywords[i]);
			}
			mySQLConnector.OpenReader($"SELECT maps.map_id, maps.map_name as name, users.name as creator FROM maps LEFT JOIN users ON maps.map_creator = users.id WHERE {whereStatement}", param);

			ListDialog dialog = new ListDialog("Maps", "Select", "Cancel");
			dialog.Style = SampSharp.GameMode.Definitions.DialogStyle.TablistHeaders;
			dialog.AddItem("Map name\tCreator");

			Dictionary<string, string> row = mySQLConnector.GetNextRow();
			while (row.Count > 0)
			{
				dialog.AddItem(row["name"] + "\t" + row["creator"]);
				row = mySQLConnector.GetNextRow();
			}
			mySQLConnector.CloseReader();
			dialog.Show(p);
		}
	}
}
