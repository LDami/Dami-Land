using SampSharp.Core;
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
using System.Linq;

namespace SampSharpGameMode1.Map
{
    public class MapCreator
    {
        const int MAX_OBJECTS_PER_MAP = 1000;
        const int MAX_GROUPS_PER_MAP = 5;
        const int MAX_ITEMS_PER_GROUP = 6;
        enum Axis
        {
            X, Y, Z
        };

        public Map editingMap = null;

        private bool isNew;
        public bool IsNew { get { return isNew; } private set { isNew = value; } }

        private bool magnet; // If true, the edited object will try to stick to other objects
        public bool Magnet
        {
            get { return magnet; }
            set
            {
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
            SetPlayerInEditor();
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

        private async void LoadingMap_Loaded(object sender, MapLoadedEventArgs e)
        {
            await TaskHelper.SwitchToMainThread();
            if (e.success)
            {
                if (e.map.Creator == player.DbId)
                {
                    editingMap = e.map;
                    isNew = false;
                    player.SendClientMessage(Color.Green, "Map #" + e.map.Id + " loaded successfully in creation mode");
                    SetPlayerInEditor();
                    textLabels.Clear();
                    foreach (MapObject obj in e.map.Objects)
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
            if (editingMap.Spawn != Vector3.Zero)
                player.Position = editingMap.Spawn;

            player.SetTime(editingMap.Time.Hour, editingMap.Time.Minute);

            hud = new HUD(player, "mapcreator.json");
            hud.SetText("mapname", editingMap.Name);
            hud.SetText("totalobj", "Total: " + editingMap.Objects.Count.ToString() + " objects");

            hud.Hide("^group.*$");
            UpdateGroupsHUD();

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
                    Save("Unsaved_" + DateTime.Now.ToString("G"));
                else
                    Save();
            }

            Unload();
        }

        private void Player_KeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            switch (e.NewKeys)
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

        public bool Save()
        {
            Dictionary<string, object> param = new Dictionary<string, object>();

            string queryUpdateObj = "UPDATE mapobjects SET obj_model=@model, obj_pos_x=@posx, obj_pos_y=@posy, obj_pos_z=@posz, obj_rot_x=@rotx, obj_rot_y=@roty, obj_rot_z=@rotz, group_id=@grp_id WHERE obj_id=@id;";
            string queryInsertObj = "INSERT INTO mapobjects (map_id, obj_model, obj_pos_x, obj_pos_y, obj_pos_z, obj_rot_x, obj_rot_y, obj_rot_z, group_id) VALUES (@mapid, @model, @posx, @posy, @posz, @rotx, @roty, @rotz, @grp_id);";
            string queryUpdateGrp = "UPDATE mapobjects_groups SET group_color=@color, group_name=@name WHERE group_id=@id;";
            string queryInsertGrp = "INSERT INTO mapobjects_groups (group_id, group_color, group_name) VALUES (@id, @color, @name);";
            List<int> groupsSaved = new();
            foreach (MapObject obj in editingMap.Objects)
            {
                if (!obj.IsDisposed)
                {
                    param.Clear();
                    param.Add("posx", obj.Position.X);
                    param.Add("model", obj.ModelId);
                    param.Add("posy", obj.Position.Y);
                    param.Add("posz", obj.Position.Z);
                    param.Add("rotx", obj.Rotation.X);
                    param.Add("roty", obj.Rotation.Y);
                    param.Add("rotz", obj.Rotation.Z);
                    param.Add("grp_id", obj.Group?.DbId);
                    if (obj.DbId == -1) // Does not exists in database
                    {
                        param.Add("@mapid", editingMap.Id);
                        obj.DbId = (int)mySQLConnector.Execute(queryInsertObj, param);
                    }
                    else
                    {
                        if (obj.Modified)
                        {
                            param.Add("@id", obj.DbId);
                            mySQLConnector.Execute(queryUpdateObj, param);
                        }
                    }
                    if(obj.Group is not null && !groupsSaved.Contains(obj.Group.DbId))
                    {
                        param.Clear();
                        param.Add("id", obj.Group.DbId);
                        param.Add("color", obj.Group.ForeColor.ToString());
                        param.Add("name", obj.Group.Name);
                        if (obj.Group.DbId == -1)
                            obj.Group.DbId = (int)mySQLConnector.Execute(queryInsertGrp, param);
                        else
                            mySQLConnector.Execute(queryUpdateGrp, param);
                        groupsSaved.Add(obj.Group.DbId);
                    }
                }
            }
            string queryDelete = "DELETE FROM mapobjects WHERE obj_id = @id";
            foreach (int id in deletedObjects)
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
            param.Add("@time", editingMap.Time.ToShortTimeString());
            mySQLConnector.Execute("UPDATE maps SET map_name=@name, map_lasteditdate=@lastedit, spawn_pos_x=@spawnx, spawn_pos_y=@spawny, spawn_pos_z=@spawnz, map_time=@time WHERE map_id=@id", param);
            isNew = false;
            return mySQLConnector.RowsAffected > 0;
        }

        public bool Save(string name)
        {
            if (name.Length > 0)
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
            foreach (MapObject obj in editingMap.Objects)
            {
                obj.Dispose();
            }
            editingMap = null;
            hud?.Unload();
            hud = null;
            mapObjectSelector?.Unload();
            mapObjectSelector = null;
            foreach (DynamicTextLabel label in textLabels.Values)
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
                player.SetTime(12, 0);
            }
        }

        public void ShowObjectList()
        {
            mapObjectSelector?.Unload();

            ListDialog<MapObjectGroupData> listDialog = new("Categories", "Select", "Cancel");
            listDialog.AddItems(MapObjectData.MapObjectCategories);
            listDialog.Response += (_, e) =>
            {
                if (e.DialogButton == SampSharp.GameMode.Definitions.DialogButton.Left)
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
                        hud.Show();
                    };
                }
                else
                    hud.Show();
            };
            hud.Hide();
            listDialog.Show(player);
        }

        public void AddGroup(string name)
        {
            int highestIndex = this.editingMap.Groups.Select(g => g.Index).OrderBy(idx => idx).LastOrDefault(0);
            this.editingMap.Groups.Add(new MapGroup(-1, highestIndex + 1, Color.White, name));
            UpdateGroupsHUD();
        }

        public void DeleteGroup(int index)
        {
            MapGroup groupToDelete = this.editingMap.Groups.Where(g => g.Index == index).First();
            if (groupToDelete is null)
                player.SendClientMessage("There is not group with index " + index);
            else
            {
                MessageDialog dialog = new("Delete group", $"You are about to delete the group {groupToDelete.Name}, do you want to dissolve it or delete all the objects ?", "Dissolve", "Delete all objects");
                MessageDialog confirmDialog = new("Do you confirm ?", "", "Yes", "Cancel");
                int action = 0; // 0 = cancel, 1 = dissolve, 2 = delete all objects
                dialog.Response += (sender, evt) =>
                {
                    // TODO: check for ESCAPE key
                    if(evt.DialogButton == SampSharp.GameMode.Definitions.DialogButton.Left)
                    {
                        // Dissolve
                        action = 1;
                        confirmDialog.Show(player);
                    }
                    else
                    {
                        // Delete contained objects
                        action = 2;
                        confirmDialog.Show(player);
                    }
                };
                confirmDialog.Response += (sender, evt) =>
                {
                    if (evt.DialogButton == SampSharp.GameMode.Definitions.DialogButton.Left)
                    {
                        if (action == 1)
                        {
                            // Dissolve
                        }
                        else if (action == 2)
                        {
                            // Delete contained objects
                        }
                    }
                };
                dialog.Show(player);
                this.editingMap.Groups.RemoveAll(g => g.Index == index);
                UpdateGroupsHUD();
            }
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
            if (editingMap.Objects.Count >= MAX_OBJECTS_PER_MAP)
            {
                player.SendClientMessage(Color.Red, "You reached the max number of objects per map");
                return -1;
            }
            MapObject mapObject = new(
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
            mapObject.Edited += (sender, e) =>
            {
                Vector3 newPosition = e.Position;
                Vector3 newRotation = e.Rotation;
                if (Magnet)
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
                    if (nearestObjectDistance < 3.0f)
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
                    textLabels[mapObject.Id].Text = $"Object #{mapObject.Id}\nGroup {mapObject.Group?.Name}";
                else if (e.Response == SampSharp.GameMode.Definitions.EditObjectResponse.Update)
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

        public void AddObject(int modelid, int groupIndex)
        {
            int objId = AddObject(modelid);

            SetObjectGroupId(objId, groupIndex);
        }

        /// <summary>
        /// Deletes an object
        /// </summary>
        /// <param name="objectid">Object ID of the object to delete</param>
        public void DelObject(int objectid)
        {
            if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
            {
                if (obj.DbId != -1)
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
        /// Change the object's group
        /// </summary>
        /// <param name="objectid">Object ID to change</param>
        /// <param name="groupIndex">Group ID to affect</param>
        public void SetObjectGroupId(int objectid, int groupIndex)
        {
            if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
            {
                if(editingMap.Groups.FirstOrDefault(x => x.Index == groupIndex, new MapGroup(-1, editingMap.Groups.Count + 1, Color.White, "Untitled group")) is MapGroup group)
                {
                    obj.Group = group;
                    if (!editingMap.Groups.Contains(group))
                        editingMap.Groups.Add(group);
                    textLabels[obj.Id].Text = $"Object #{obj.Id}\nGroup {obj.Group?.Name}";
                    textLabels[obj.Id].Color = group.ForeColor.GetValueOrDefault(Color.White);
                    player.SendClientMessage($"Object {obj.Id} has been put in group #{groupIndex} ({obj.Group.Name})");
                }
                UpdateGroupsHUD();
            }
            else
                player.SendClientMessage("Unknown object id");
        }

        private void UpdateGroupsHUD()
        {
            foreach(MapGroup mapGroup in editingMap.Groups)
            {
                string hudPrefix = "group" + mapGroup.Index;
                hud.Show(hudPrefix + "_bg");
                hud.SetText(hudPrefix + "_name", mapGroup.Name);
                hud.Show(hudPrefix + "_name");
                hud.Hide("^" + hudPrefix + "_item.*$");
                int idxObj = 0;
                foreach(MapObject mapObject in editingMap.Objects.Where(o => o.Group?.DbId == mapGroup.DbId))
                {
                    if(mapObject != null)
                    {
                        if (++idxObj < MAX_ITEMS_PER_GROUP)
                        {
                            hud.SetPreviewModel(hudPrefix + "_item" + idxObj, mapObject.ModelId);
                            hud.Show(hudPrefix + "_item" + idxObj);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Open the in-game position editor for an object
        /// </summary>
        /// <param name="objectid">Object ID to edit</param>
        public void EditObject(int objectid)
        {
            if (editingMap.Objects.Find(obj => obj.Id == objectid) is MapObject obj)
            {
                obj.Edit(player);
                obj.Edited += (sender, e) =>
                {
                    obj.Position = e.Position;
                    obj.Rotation = e.Rotation;
                    textLabels[obj.Id].Position = e.Position;
                };
            }
        }

        public void EditMarker(int marker)
        {
            if (marker > 0 && marker < markers.Length + 1)
            {
                marker--;
                markers[marker] ??= new PlayerObject(player, 19133, player.Position + new Vector3(5.0, 0.0, 0.0), Vector3.Zero);
                markers[marker].Edit();
                markers[marker].Edited += (sender, e) =>
                {
                    markers[marker].Position = e.Position;
                };
            }
        }

        public void GetMarkersDistance()
        {
            if (markers[0] is null || markers[1] is null)
                player.SendClientMessage("You must set 2 markers to use this command");
            else
            {
                player.SendClientMessage("Distance: " + (markers[0].Position - markers[1].Position).ToString());
            }
        }

        public void SetTime(TimeOnly _time)
        {
            player.mapCreator.editingMap.Time = _time;
            player.SetTime(_time.Hour, _time.Minute);
            player.Notificate($"Time set to {_time}");
        }

        public static void ShowMapList(Player p, string[] keywords)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            string whereStatement = "1=1";
            Dictionary<string, object> param = new Dictionary<string, object>();
            for (int i = 0; i < keywords.Length; i++)
            {
                if (keywords[i].Length > 0)
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
