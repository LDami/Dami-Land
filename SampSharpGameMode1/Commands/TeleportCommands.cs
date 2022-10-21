using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
    class MapHUD : HUD
    {
        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();
        private Dictionary<int, Vector3R> teleportations = new Dictionary<int, Vector3R>();
        public MapHUD(Player player) : base(player, "mapteleport.json")
        {
            if (mySQLConnector != null)
            {
                float scaleX = 6000 / layer.GetTextdrawSize("mapbox").X;
                float scaleY = 6000 / layer.GetTextdrawSize("mapbox").Y;

                Dictionary<string, object> param = new Dictionary<string, object>();
                mySQLConnector.OpenReader("SELECT * FROM teleportations WHERE 1=1", param);
                Dictionary<string, string> row = mySQLConnector.GetNextRow();
                int tlpID;
                string tlpName;
                Vector3 tlpPosition;
                Vector2 tlpDisplayedPosition;
                teleportations.Clear();
                while (row.Count > 0)
                {
                    tlpID = Convert.ToInt32(row["teleport_id"]);
                    tlpName = row["teleport_name"];
                    tlpPosition = new Vector3(
                        (float)Convert.ToDouble(row["teleport_pos_x"]),
                        (float)Convert.ToDouble(row["teleport_pos_y"]),
                        (float)Convert.ToDouble(row["teleport_pos_z"])
                    );
                    teleportations[tlpID] = new Vector3R(tlpPosition, (float)Convert.ToDouble(row["teleport_angle"]));
                    tlpDisplayedPosition = new Vector2(
                        (tlpPosition.X + 3000) / scaleX + layer.GetTextdrawPosition("mapbox").X,
                        ((tlpPosition.Y * -1) + 3000) / scaleY + layer.GetTextdrawPosition("mapbox").Y
                    );
                    layer.CreateBackground(player, "tlps_" + tlpID, tlpDisplayedPosition, new Vector2(5, 5), ColorPalette.Secondary.Main.GetColor());
                    layer.SetTextdrawText("tlps_" + tlpID, "LD_POOL:nib");
                    layer.UpdateTextdraw("tlps_" + tlpID);
                    layer.SetClickable("tlps_" + tlpID);

                    Color color = ColorPalette.Primary.Main.GetColor();
                    color = new Color(color.R, color.G, color.B, 0.8f);
                    layer.CreateTextdraw(player, "info_" + tlpID, TextdrawLayer.TextdrawType.Box);
                    layer.SetTextdrawText("info_" + tlpID, $"{tlpName}");
                    layer.SetTextdrawSize("info_" + tlpID, 5, 50);
                    layer.SetTextdrawColor("info_" + tlpID, ColorPalette.Secondary.Main.GetColor());
                    layer.SetTextdrawBoxColor("info_" + tlpID, new Color(100, 100, 100, 0.8f));
                    layer.SetTextdrawPosition("info_" + tlpID, layer.GetTextdrawPosition("tlps_" + tlpID) + new Vector2(0, 8));
                    layer.SetTextdrawLetterSize("info_" + tlpID, 0.15f, 0.6f);
                    layer.SetTextdrawFont("info_" + tlpID, 1);
                    layer.SetTextdrawAlignment("info_" + tlpID, 2);
                    layer.SetClickable("info_" + tlpID);

                    row = mySQLConnector.GetNextRow();
                }
                mySQLConnector.CloseReader();
                layer.UnselectAllTextdraw();

                layer.TextdrawClicked += OnTextdrawClicked;
            }
            else
            {
                Logger.WriteLineAndClose("TeleportCommands.cs - MapHUD._:E: MySQL not started");
            }
        }

        private void OnTextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            if(Int32.TryParse(e.TextdrawName.Substring(5), out int tlpID))
            {
                (player as Player).Teleport(teleportations[tlpID].Position);
                player.Angle = teleportations[tlpID].Rotation;
                player.CancelSelectTextDraw();
            }
        }

        public void ForceHide()
        {
            //layer.Hide("mapbox");
        }
    }
    class TeleportCommands
    {
        [Command("tlps")]
        private static void TlpsCommand(BasePlayer player)
        {
            MapHUD mapHUD = new MapHUD(player as Player);
            mapHUD.Show();
            mapHUD.ForceHide();
            player.SelectTextDraw(ColorPalette.Primary.Main.GetColor());
            player.CancelClickTextDraw += (sender, e) =>
            {
                if(mapHUD != null)
                {
                    mapHUD.Hide();
                    mapHUD = null;
                }
            };
        }

        [CommandGroup("tlps")]
        class TlpsCommandGroup
        {
            [Command("add", PermissionChecker = typeof(AdminPermissionChecker))]
            private static void AddCommand(BasePlayer player)
            {
                InputDialog dialog = new InputDialog("Creating Teleport point", "Type the name of the teleport point", false, "Save", "Cancel");
                dialog.Response += (sender, e) =>
                {
                    if(e.DialogButton == DialogButton.Left)
                    {
                        MySQLConnector mySQLConnector = MySQLConnector.Instance();
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add("@name", e.InputText);
                        param.Add("@pos_x", e.Player.Position.X);
                        param.Add("@pos_y", e.Player.Position.Y);
                        param.Add("@pos_z", e.Player.Position.Z);
                        param.Add("@angle", e.Player.Angle);
                        param.Add("@zone", Zone.GetZoneName(e.Player.Position));
                        mySQLConnector.Execute("INSERT INTO teleportations (teleport_name, teleport_pos_x, teleport_pos_y, teleport_pos_z, teleport_angle, teleport_zone) VALUES" +
                            "(@name, @pos_x, @pos_y, @pos_z, @angle, @zone)", param);
                        if (mySQLConnector.RowsAffected > 0)
                        {
                            player.SendClientMessage($"{ColorPalette.Primary.Main}The teleport point {ColorPalette.Secondary.Main}\"{e.InputText}\"{ColorPalette.Primary.Main} has been created");
                        }
                    }
                };
                dialog.Show(player);
            }
        }
    }
}
