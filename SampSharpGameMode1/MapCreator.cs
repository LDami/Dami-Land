using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.CustomDatas;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SampSharpGameMode1
{
	public class MapCreator
	{
		const int MAX_OBJECTS_PER_MAP = 1000;
		enum Axis
		{
			X, Y, Z
		};

		public Map editingMap = null;

		private bool isNew;
		public bool IsNew { get { return isNew; } private set { isNew = value; } }

		private bool magnet; // If true, the edited object will try to stick to other objects
		public bool Magnet {
			get { return magnet; }
			set {
				magnet = value;
				hud.SetText("magnet", "Magnet: " + (value ? "On" : "Off"));
				hud.SetColor("magnet", value ? Color.Green : Color.White);
			}
		}


		private Player player;
		private HUD hud;
		private MapObjectSelector mapObjectSelector;
        private PlayerObject[] markers;
		private Dictionary<int, DynamicTextLabel> textLabels;
		private MySQLConnector mySQLConnector = MySQLConnector.Instance();
		private List<int> deletedObjects; // Contains list of MapObject's DbId that must be deleted on save

		public MapCreator(Player p)
		{
			if (p != null && p.IsConnected)
			{
				player = p;
				editingMap = null;
				markers = new PlayerObject[2];
				textLabels = new Dictionary<int, DynamicTextLabel>();
				deletedObjects = new List<int>();
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
			isNew = true;
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
					isNew = false;
					player.SendClientMessage(Color.Green, "Map #" + e.map.Id + " loaded successfully in creation mode");
					this.SetPlayerInEditor();
					textLabels.Clear();
					foreach(MapObject obj in e.map.Objects)
                    {
						textLabels.Add(obj.Id, new DynamicTextLabel($"Object #{obj.Id}", obj.Group?.ForeColor ?? Color.White, obj.Position, 100.0f, null, null, false, player.VirtualWorld));
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
            player.Disconnected += Player_Disconnected;
			player.KeyStateChanged += Player_KeyStateChanged;
			if(editingMap.Spawn != Vector3.Zero)
				player.Position = editingMap.Spawn;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			hud = new HUD(player, "mapcreator.json");
			sw.Stop();
			Console.WriteLine("Time for load HUD: " + sw.ElapsedMilliseconds + "ms");
			sw.Restart();
			hud.SetText("mapname", editingMap.Name);
			hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
			
			hud.Hide(@"^group.*$");
			
			sw.Stop();
			Console.WriteLine("Time for edit HUD: " + sw.ElapsedMilliseconds + "ms");
			Magnet = true;
			deletedObjects = new List<int>();
			player.SendClientMessage("Here are the controls:");
			player.SendClientMessage("    Y/N:                    Unfreeze/Freeze (useful in Jetpack !)");
			player.SendClientMessage("    Z/LShift:              Move camera during object edition");
		}

        private void Player_Disconnected(object sender, DisconnectEventArgs e)
        {
			if (editingMap.Objects.Count > 0)
			{
				if (isNew)
					this.Save("Unsaved_" + DateTime.Now.ToString("G"));
				else
					this.Save();
			}

			this.Unload();
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

            string queryUpdate = "UPDATE mapobjects SET obj_model=@model, obj_pos_x=@posx, obj_pos_y=@posy, obj_pos_z=@posz, obj_rot_x=@rotx, obj_rot_y=@roty, obj_rot_z=@rotz, group_id=@grp_id WHERE obj_id=@id;";
            string queryInsert = "INSERT INTO mapobjects (map_id, obj_model, obj_pos_x, obj_pos_y, obj_pos_z, obj_rot_x, obj_rot_y, obj_rot_z, group_id) VALUES (@mapid, @model, @posx, @posy, @posz, @rotx, @roty, @rotz, @grp_id);";
			foreach (MapObject obj in editingMap.Objects)
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
					param.Add("grp_id", obj.Group.Id);
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
			string queryDelete = "DELETE FROM mapobjects WHERE obj_id = @id";
			foreach(int id in deletedObjects)
            {
				param.Clear();
				param.Add("@id", id);
				mySQLConnector.Execute(queryDelete, param);
            }
			deletedObjects.Clear();

			param.Clear();
			param.Add("@name", editingMap.Name);
			param.Add("@lastedit", DateTime.Now);
			param.Add("@id", editingMap.Id);
			param.Add("@spawnx", editingMap.Spawn.X);
			param.Add("@spawny", editingMap.Spawn.Y);
			param.Add("@spawnz", editingMap.Spawn.Z);
            mySQLConnector.Execute("UPDATE maps SET map_name=@name, map_lasteditdate=@lastedit, @spawn_pos_x=@spawnx, @spawn_pos_y=@spawny, @spawn_pos_z=@spawnz WHERE map_id=@id", param);
			isNew = false;
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
            hud?.Unload();
			hud = null;
            mapObjectSelector?.Unload();
            mapObjectSelector = null;
			foreach(DynamicTextLabel label in textLabels.Values)
            {
				label?.Dispose();
            }
			textLabels.Clear();
			if (player != null)
			{
				player.VirtualWorld = 0;
				player.CancelEdit();
				player.Disconnected -= Player_Disconnected;
				player.KeyStateChanged -= Player_KeyStateChanged;
			}
		}

		public void ShowObjectList()
		{
            mapObjectSelector?.Unload();

            ListDialog<MapObjectGroupData> listDialog = new("Categories", "Select", "Cancel");
			listDialog.AddItems(MapObjectData.MapObjectCategories);
			listDialog.Response += (_, e) =>
			{
				mapObjectSelector = new(player, e.ItemValue);
				mapObjectSelector.Selected += (_, f) =>
				{
					player.CancelSelectTextDraw();
					AddObject(f.Id);
				};
				player.SelectTextDraw(ColorPalette.Primary.Main.GetColor());
				player.CancelClickTextDraw += (_, _) =>
                {
                    mapObjectSelector?.Unload();
                };
            };
			listDialog.Show(player);
		}

        /// <summary>
        /// Creates a new object and returns its ID
        /// </summary>
        /// <param name="modelid">Model ID of the object to create</param>
        /// <param name="position">Position of the new object</param>
        /// <param name="rotation">Rotation of the new object</param>
        /// <returns>Returns the object ID, -1 if the object is not created</returns>
        public int AddObject(int modelid, Vector3? position = null, Vector3? rotation = null)
        {
			if(editingMap.Objects.Count >= MAX_OBJECTS_PER_MAP)
            {
				player.SendClientMessage(Color.Red, "You reached the max number of objects per map");
				return -1;
            }
			MapObject mapObject = new MapObject(
				-1,
				modelid,
				position ?? new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z), 
				rotation ?? Vector3.Zero,
				null,
				player.VirtualWorld
			);
			mapObject.Edit(player);
			Axis editedAxis = Axis.X;
			Vector3 lastObjectPos = mapObject.Position; // Used to detect which axis is being modified
			Vector3 lastObjectRot = mapObject.Rotation; // Used to detect which axis is being modified
			mapObject.Edited += (object sender, SampSharp.Streamer.Events.PlayerEditEventArgs e) =>
			{
				Vector3 newPosition = e.Position;
				Vector3 newRotation = e.Rotation;
				if(Magnet)
				{
					if (newPosition.X != lastObjectPos.X)
						editedAxis = Axis.X;
					if (newPosition.Y != lastObjectPos.Y)
						editedAxis = Axis.Y;
					if (newPosition.Z != lastObjectPos.Z)
						editedAxis = Axis.Z;

					if (newRotation.X != lastObjectRot.X)
						editedAxis = Axis.X;
					if (newRotation.Y != lastObjectRot.Y)
						editedAxis = Axis.Y;
					if (newRotation.Z != lastObjectRot.Z)
						editedAxis = Axis.Z;

					MapObject nearestObject = null;
					float nearestObjectDistance = 99999.0f;
					foreach (MapObject obj in editingMap.Objects)
					{
						if (Vector3.Distance(obj.Position, e.Position) < 50.0 && obj.Id != mapObject.Id)
						{
							if (nearestObject == null || Vector3.Distance(obj.Position, e.Position) < nearestObjectDistance)
							{
								nearestObject = obj;
								nearestObjectDistance = Vector3.Distance(obj.Position, e.Position);
							}
						}
					}
					if(nearestObjectDistance < 3.0f)
					{
						if (Math.Abs(nearestObject.Position.X - mapObject.Position.X) < 3.0f && editedAxis == Axis.X)
							newPosition = new Vector3(nearestObject.Position.X, newPosition.Y, newPosition.Z);
						if (Math.Abs(nearestObject.Position.Y - mapObject.Position.Y) < 3.0f && editedAxis == Axis.Y)
							newPosition = new Vector3(newPosition.X, nearestObject.Position.Y, newPosition.Z);
						if (Math.Abs(nearestObject.Position.Z - mapObject.Position.Z) < 3.0f && editedAxis == Axis.Z)
							newPosition = new Vector3(newPosition.X, newPosition.Y, nearestObject.Position.Z);


						if (Math.Abs(nearestObject.Rotation.X - mapObject.Rotation.X) < 3.0f && editedAxis == Axis.X)
							newRotation = new Vector3(nearestObject.Rotation.X, newRotation.Y, newRotation.Z);
						if (Math.Abs(nearestObject.Rotation.Y - mapObject.Rotation.Y) < 3.0f && editedAxis == Axis.Y)
							newRotation = new Vector3(newRotation.X, nearestObject.Rotation.Y, newRotation.Z);
						if (Math.Abs(nearestObject.Rotation.Z - mapObject.Rotation.Z) < 3.0f && editedAxis == Axis.Z)
							newRotation = new Vector3(newRotation.X, newRotation.Y, nearestObject.Rotation.Z);
					}
				}
				mapObject.Position = newPosition;
				mapObject.Rotation = newRotation;
				textLabels[mapObject.Id].Position = e.Position;
				if (e.Response == SampSharp.GameMode.Definitions.EditObjectResponse.Final || e.Response == SampSharp.GameMode.Definitions.EditObjectResponse.Cancel)
					textLabels[mapObject.Id].Text = $"Object #{mapObject.Id}\nGroup {mapObject.Group.Name}";
				else if(e.Response == SampSharp.GameMode.Definitions.EditObjectResponse.Update)
					textLabels[mapObject.Id].Text = $"Object #{mapObject.Id}\nPos: {mapObject.Position}\nRot: {mapObject.Rotation}";

				lastObjectPos = newPosition;
				lastObjectRot = newRotation;
			};
			editingMap.Objects.Add(mapObject);
			textLabels.Add(mapObject.Id, new DynamicTextLabel($"Object #{mapObject.Id}", Color.White, mapObject.Position, 100.0f, null, null, false));
			player.SendClientMessage($"Object #{mapObject.Id} created with model {modelid}");
			hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
			return mapObject.Id;
		}

		public void AddObject(int modelid, int groupid)
        {
			int objId = AddObject(modelid);
			MapGroup group = MapGroup.GetOrCreate(groupid);
			editingMap.Objects.Find(obj => obj.Id == objId).Group = group;
			textLabels[objId].Text = $"Object {objId}\nGroup {group.Name}";
			textLabels[objId].Color = group.ForeColor.GetValueOrDefault(Color.White);
		}

		/// <summary>
		/// Deletes an object
		/// </summary>
		/// <param name="objectid">Object ID of the object to delete</param>
		public void DelObject(int objectid)
		{
			if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
			{
				if(obj.DbId != -1)
					deletedObjects.Add(obj.DbId);
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

		/// <summary>
		/// Replaces the model ID of an object
		/// </summary>
		/// <param name="objectid">Object ID to replace</param>
		/// <param name="modelid">Model ID to replace with</param>
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

		/// <summary>
		/// Duplicates an object
		/// </summary>
		/// <param name="objectid">Object ID to duplicate</param>
		public void DuplicateObject(int objectid)
		{
			if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
			{
				AddObject(obj.ModelId, obj.Position, obj.Rotation);
				hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");
			}
			else
				player.SendClientMessage("Unknown object id");
		}

		/// <summary>
		/// Open the in-game position editor for an object
		/// </summary>
		/// <param name="objectid">Object ID to edit</param>
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
			dialog.AddItem("Id\tMap name\tCreator");

			Dictionary<string, string> row = mySQLConnector.GetNextRow();
			while (row.Count > 0)
			{
				dialog.AddItem(row["map_id"] + "\t" + row["name"] + "\t" + row["creator"]);
				row = mySQLConnector.GetNextRow();
			}
			mySQLConnector.CloseReader();
			dialog.Show(p);
		}
	}
}
