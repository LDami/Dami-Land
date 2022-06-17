using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1
{
	public class MapCreator
	{
		const int MAX_OBJECTS_PER_MAP = 1000;

		public Map editingMap = null;

		private Player player;
		private HUD hud;
		private PlayerObject[] markers;
		private Dictionary<int, PlayerTextLabel> textLabels;
		private MySQLConnector mySQLConnector = MySQLConnector.Instance();

		public MapCreator(Player p)
		{
			if (p != null && p.IsConnected)
			{
				player = p;
				editingMap = null;
				markers = new PlayerObject[2];
				textLabels = new Dictionary<int, PlayerTextLabel>();
				p.SendClientMessage("Map creator initialized");
				p.SendClientMessage("/mapping help for command list");
			}
			else
				Logger.WriteLineAndClose("MapCreator.cs - MapCreator._:E: MapCreator was initiliazed with an invalid player");
		}

		public void CreateMap()
		{
			editingMap = new Map();
			editingMap.Name = "[Untitled]";
			this.SetPlayerInEditor();
		}

		public void Load(int id)
		{
			if (id > 0)
			{
				Map loadingMap = new Map();
				loadingMap.Loaded += LoadingMap_Loaded;
				loadingMap.Load(id, (int)VirtualWord.EventCreators + player.Id);
			}
			else player.SendClientMessage(Color.Red, "Error loading Map #" + id + " (invalid ID)");
		}

		private void LoadingMap_Loaded(object sender, MapLoadedEventArgs e)
		{
			if (e.success)
			{
				if (e.map.Creator == player.DbId)
				{
					editingMap = e.map;
					player.SendClientMessage(Color.Green, "Map #" + e.map.Id + " loaded successfully in creation mode");
					this.SetPlayerInEditor();
					textLabels.Clear();
					foreach(MapObject obj in e.map.Objects)
                    {
						textLabels.Add(obj.Id, new PlayerTextLabel(player, $"Object #{obj.Id}", Color.White, obj.Position, 50.0f));
                    }
				}
				else
					player.SendClientMessage(Color.Red, "You cannot edit this Map because you are not it's creator");
			}
			else
				player.SendClientMessage(Color.Red, "Error loading Map (missing mandatory datas)");
		}
		private void SetPlayerInEditor()
		{
			player.VirtualWorld = (int)VirtualWord.EventCreators + player.Id;
			player.EnablePlayerCameraTarget(true);
			player.KeyStateChanged += Player_KeyStateChanged;
			if(editingMap.Spawn != Vector3.Zero)
				player.Position = editingMap.Spawn;

			hud = new HUD(player, "mapcreator.json");
			hud.SetText("mapname", editingMap.Name);
			hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
			player.SendClientMessage("Map loaded, here are the controls:");
			player.SendClientMessage("    submission key (2/é):                    Open menu");
			player.SendClientMessage("    Y/N:                    Unfreeze/Freeze");
		}

		private void Player_KeyStateChanged(object sender, KeyStateChangedEventArgs e)
		{
			switch(e.NewKeys)
			{
				case SampSharp.GameMode.Definitions.Keys.No:
					player.ToggleControllable(false);
					player.Notificate("Freezed, Y to unfreeze");
					break;
				case SampSharp.GameMode.Definitions.Keys.Yes:
					player.ToggleControllable(true);
					player.Notificate("Unfreezed");
					break;
			}
		}

		public Boolean Save()
		{
			Dictionary<string, object> param = new Dictionary<string, object>();

			string queryUpdate = "UPDATE mapobjects SET obj_model=@model, obj_pos_x=@posx, obj_pos_y=@posy, obj_pos_z=@posz, obj_rot_x=@rotx, obj_rot_y=@roty, obj_rot_z=@rotz WHERE obj_id=@id;";
			string queryInsert = "INSERT INTO mapobjects (map_id, obj_model, obj_pos_x, obj_pos_y, obj_pos_z, obj_rot_x, obj_rot_y, obj_rot_z) VALUES (@mapid, @model, @posx, @posy, @posz, @rotx, @roty, @rotz);";
			foreach (MapObject obj in MapObject.All)
			{
				if(!obj.IsDisposed)
				{
					param.Clear();
					param.Add("posx", obj.Position.X);
					param.Add("model", obj.ModelId);
					param.Add("posy", obj.Position.Y);
					param.Add("posz", obj.Position.Z);
					param.Add("rotx", obj.Rotation.X);
					param.Add("roty", obj.Rotation.Y);
					param.Add("rotz", obj.Rotation.Z);
					if (obj.DbId == -1) // Does not exists in database
					{
						param.Add("@mapid", editingMap.Id);
						obj.DbId = (int)mySQLConnector.Execute(queryInsert, param);
					}
					else
					{
						if (obj.Modified)
						{
							param.Add("@id", obj.DbId);
							mySQLConnector.Execute(queryUpdate, param);
						}
					}
				}
			}

			param.Clear();
			param.Add("@name", editingMap.Name);
			param.Add("@lastedit", DateTime.Now);
			mySQLConnector.Execute("UPDATE maps SET map_name=@name, map_lasteditdate=@lastedit", param);
			return mySQLConnector.RowsAffected > 0;
		}

		public Boolean Save(string name)
		{
			if(name.Length > 0)
			{
				Dictionary<string, object> param = new Dictionary<string, object>();
				param.Add("@name", name);
				param.Add("@creator", player.DbId);
				param.Add("@creationdate", DateTime.Now);

				editingMap.Id = (int)mySQLConnector.Execute("INSERT INTO maps (map_name, map_creator, map_creationdate, map_lasteditdate) VALUES (@name, @creator, @creationdate, @creationdate)", param);
				if (mySQLConnector.RowsAffected > 0)
				{
					editingMap.Name = name;
					hud.SetText("mapname", name);
					return Save();
				}
				return false;
			}
			return false;
		}
		public void Unload()
		{
			foreach(MapObject obj in editingMap.Objects)
			{
				obj.Dispose();
			}
			editingMap = null;
			if (hud != null)
				hud.Hide();
			hud = null;
			foreach(PlayerTextLabel label in textLabels.Values)
            {
				label?.Dispose();
            }
			textLabels.Clear();
			if (player != null)
			{
				player.VirtualWorld = 0;
				player.CancelEdit();
				player.KeyStateChanged -= Player_KeyStateChanged;
			}
		}

		public void AddObject(int modelid, Vector3? position = null, Vector3? rotation = null)
		{
			MapObject mapObject = new MapObject(
				-1,
				modelid,
				position ?? new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z), 
				rotation ?? Vector3.Zero,
				player.VirtualWorld
			);
			mapObject.Edit(player);
			mapObject.Edited += (object sender, SampSharp.Streamer.Events.PlayerEditEventArgs e) =>
			{
				mapObject.Position = e.Position;
				mapObject.Rotation = e.Rotation;
				textLabels[mapObject.Id].Position = e.Position;
			};
			editingMap.Objects.Add(mapObject);
			textLabels.Add(mapObject.Id, new PlayerTextLabel(player, $"Object #{mapObject.Id}", Color.White, mapObject.Position, 50.0f));
			player.SendClientMessage($"Object #{mapObject.Id} created with model {modelid}");
			hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
		}

		public void DelObject(int objectid)
		{
			if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
			{
				obj?.Dispose();
				editingMap.Objects.Remove(obj);
				textLabels[obj.Id].Dispose();
				textLabels.Remove(objectid);
				player.Notificate($"Object #{objectid} deleted");
				hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
			}
			else
				player.SendClientMessage("Unknown object id");
		}
		public void ReplaceObject(int objectid, int modelid)
		{
			if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
			{
				Vector3 pos = obj.Position;
				Vector3 rot = obj.Rotation;
				DelObject(objectid);
				AddObject(modelid, pos, rot);
				player.Notificate($"Object #{objectid} replaced");
				hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
			}
			else
				player.SendClientMessage("Unknown object id");
		}
		public void EditObject(int objectid)
		{
			if(editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
			{
				obj.Edit(player);
				obj.Edited += (object sender, SampSharp.Streamer.Events.PlayerEditEventArgs e) =>
				{
					obj.Position = e.Position;
					obj.Rotation = e.Rotation;
					textLabels[obj.Id].Position = e.Position;
				};
			}
		}

		public void EditMarker(int marker)
		{
			if(marker > 0 && marker < markers.Length + 1)
			{
				marker --;
				markers[marker] ??= new PlayerObject(player, 19133, player.Position + new Vector3(5.0, 0.0, 0.0), Vector3.Zero);
				markers[marker].Edit();
				markers[marker].Edited += (object sender, EditPlayerObjectEventArgs e) =>
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
				if(keywords[i].Length > 0)
				{
					whereStatement += $" AND maps.map_name LIKE @w{i} OR users.name LIKE @w{i}";
					param.Add($"@w{i}", keywords[i]);
				}
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
