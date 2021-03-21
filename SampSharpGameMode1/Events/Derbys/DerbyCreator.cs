using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1.Events.Derbys
{
    public class DerbyCreator
    {
        class HUD
        {
            TextdrawLayer layer;
            public HUD(Player player)
            {
                layer = new TextdrawLayer();
                string filename = BaseMode.Instance.Client.ServerPath + "\\scriptfiles\\derbycreator.json";
                string jsonData = "";
                if (File.Exists(filename))
                {
                    try
                    {
                        using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
                        {
                            byte[] output = new byte[fs.Length];
                            int idx = 0;
                            int blockLength = 1;
                            byte[] tmp = new byte[blockLength];
                            int readBytes;
                            while ((readBytes = fs.Read(tmp, 0, blockLength)) > 0)
                            {
                                for (int i = 0; i < readBytes; i++)
                                    output[idx + i] = tmp[i];
                                idx += readBytes;
                            }
                            jsonData = new UTF8Encoding(true).GetString(output);
                            List<textdraw> textdraws = JsonConvert.DeserializeObject<List<textdraw>>(jsonData);
                            foreach (textdraw textdraw in textdraws)
                            {
                                if (textdraw.Type.Equals("box"))
                                {
                                    layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                                    layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                    layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                                }
                                if (textdraw.Type.Equals("text"))
                                {
                                    layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                    layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                }
                            }
                            layer.SetTextdrawText("Derbynamelabel", "Derby Name:");
                            layer.SetTextdrawText("Derbyname", "None");
                            layer.SetTextdrawText("editingmode", "Mode: None");
                            layer.UnselectAllTextdraw();
                            fs.Close();
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("DerbyCreator.cs - DerbyCreator.HUD._:E: Cannot load Derby Creator HUD:");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            public void Destroy()
            {
                layer.HideAll();
            }
            public void SetDerbyName(string name)
            {
                layer.SetTextdrawText("Derbyname", name);
            }

            public void SetEditingMode(EditingMode editingMode)
            {
                layer.SetTextdrawText("editingmode", editingMode.ToString());
            }
        }
        enum EditingMode { Mapping, SpawnPos }

        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();

        Player player;
        HUD hud;

        public Derby editingDerby = null;
        EditingMode editingMode;

        PlayerObject moverObject;
        const int moverObjectModelID = 19133;
        Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);

        BaseVehicle[] spawnVehicles;
        int spawnIndex;

        #region PlayerEvents
        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            switch (e.NewKeys)
            {
                case Keys.AnalogLeft:
                    {
                        switch (editingMode)
                        {
                            case EditingMode.Mapping:
                                {
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case Keys.AnalogRight:
                    {
                        switch (editingMode)
                        {
                            case EditingMode.Mapping:
                                {
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case Keys.Submission:
                    {
                        ShowDerbyDialog();
                        break;
                    }
            }
        }

        #endregion

        public DerbyCreator(Player _player)
        {
            player = _player;
            player.KeyStateChanged += OnPlayerKeyStateChanged;
            if (!player.InAnyVehicle)
            {
                BaseVehicle veh = BaseVehicle.Create(VehicleModelType.Infernus, player.Position + new Vector3(0.0, 5.0, 0.0), 0.0f, 1, 1);
                player.PutInVehicle(veh);
            }
            editingDerby = null;
            spawnVehicles = new BaseVehicle[Derby.MAX_PLAYERS_IN_DERBY];
        }

        public void Create()
        {
            hud.SetDerbyName("Unsaved");
            editingMode = EditingMode.Mapping;
            hud.SetEditingMode(editingMode);

            editingDerby = new Derby();
            editingDerby.StartingVehicle = VehicleModelType.Infernus;
        }
        public void Load(int id)
        {
            hud = new HUD(player);
            if (id > 0)
            {
                Derby loadingDerby = new Derby();
                loadingDerby.Loaded += LoadingDerby_Loaded;
                loadingDerby.Load(id);
            }
            else player.SendClientMessage(Color.Red, "Error loading Derby #" + id);
        }

        private void LoadingDerby_Loaded(object sender, DerbyLoadedEventArgs e)
        {
            editingDerby = e.derby;
            hud.SetDerbyName(editingDerby.Name);
            editingMode = EditingMode.Mapping;
            hud.SetEditingMode(editingMode);
            player.SendClientMessage(Color.Green, "Derby #" + editingDerby.Id + " loaded successfully in creation mode");
        }
        public void Unload()
        {
            editingDerby = null;
            if (hud != null)
                hud.Destroy();
            hud = null;
            if (moverObject != null)
            {
                moverObject.Edited -= moverObject_Edited;
                moverObject.Dispose();
            }
            moverObject = null;
            //TODO: cancel edit ?
        }
        public Boolean Save()
        {
            if (editingDerby != null)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", editingDerby.Id }
                };
                mySQLConnector.Execute("DELETE FROM derby_spawnpos WHERE race_id=@id", param);
                for (int i = 0; i < editingDerby.StartingSpawn.Count; i++)
                {
                    param = new Dictionary<string, object>
                    {
                        { "@id", editingDerby.Id },
                        { "@spawn_index",  i },
                        { "@spawn_pos_x",  editingDerby.StartingSpawn[i].Position.X },
                        { "@spawn_pos_y",  editingDerby.StartingSpawn[i].Position.Y },
                        { "@spawn_pos_z",  editingDerby.StartingSpawn[i].Position.Z },
                        { "@spawn_rot",  editingDerby.StartingSpawn[i].Rotation },
                    };
                    mySQLConnector.Execute("INSERT INTO derby_spawnpos " +
                        "(derby_id, spawn_index, spawnpos_x, spawnpos_y, spawnpos_z, spawnpos_rot) VALUES " +
                        "(@id, @spawn_index, @spawn_pos_x, @spawn_pos_y, @spawn_pos_z, @spawn_rot)", param);

                }
                return (mySQLConnector.RowsAffected > 0);
            }
            return false;
        }

        public Boolean Save(string name) // Only if the derby does not already exist
        {
            if (editingDerby != null && name.Length > 0)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@derby_name", name },
                    { "@derby_creator", player.Name },
                    { "@derby_startvehicle", editingDerby.StartingVehicle }
                };
                mySQLConnector.Execute("INSERT INTO derbys " +
                    "(derby_name, derby_creator, derby_startvehicle) VALUES" +
                    "(@derby_name, @derby_creator, @derby_startvehicle)", param);
                if (mySQLConnector.RowsAffected > 0)
                {
                    hud.SetDerbyName(name);
                    return this.Save();
                }
                else return false;
            }
            return false;
        }
        #region Dialogs
        private void ShowDerbyDialog()
        {
            ListDialog derbyDialog = new ListDialog("Derby options", "Select", "Cancel");
            derbyDialog.AddItem("Select starting vehicle [" + editingDerby.StartingVehicle + "]");
            derbyDialog.AddItem("Edit derby name");

            derbyDialog.Show(player);
            derbyDialog.Response += DerbyDialog_Response;
        }

        private void DerbyDialog_Response(object sender, DialogResponseEventArgs e)
        {
            if (e.Player.Equals(player))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    switch (e.ListItem)
                    {
                        case 0: // Select starting vehicle
                            {
                                InputDialog startCarNameDialog = new InputDialog("Vehicle name", "Enter the vehicle name:", false, "Find", "Cancel");
                                startCarNameDialog.Show(player);
                                startCarNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingDerby.StartingVehicle = Utils.GetVehicleModelType(eventArgs.InputText);
                                        player.Notificate("Updated");
                                        ShowDerbyDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowDerbyDialog();
                                    }
                                };
                                break;
                            }
                        case 1: // Edit derby name
                            {
                                InputDialog raceNameDialog = new InputDialog("Derby name", "Enter the derby name:", false, "Edit", "Cancel");
                                raceNameDialog.Show(player);
                                raceNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingDerby.Name = eventArgs.InputText;
                                        player.Notificate("Updated");
                                        ShowDerbyDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowDerbyDialog();
                                    }
                                };
                                break;
                            }
                    }
                }
            }
        }
        #endregion
        private void moverObject_Edited(object sender, EditPlayerObjectEventArgs e)
        {
            throw new NotImplementedException();
        }
        public static Dictionary<string, string> GetDerbyInfo(int id)
        {
            // id, name, creator, type, number of spawn points
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string, string> row;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT derby_id, derby_name, derby_creator FROM derbys WHERE derby_id = @id", param);

            row = mySQLConnector.GetNextRow();
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                "FROM derby_spawnpos WHERE derby_id = @id", param);
            int nbrOfSpawnPoints = 0;
            row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                nbrOfSpawnPoints++;
                row = mySQLConnector.GetNextRow();
            }
            results.Add("Nombre de spawn points", nbrOfSpawnPoints.ToString());
            mySQLConnector.CloseReader();

            return results;
        }
    }
}
