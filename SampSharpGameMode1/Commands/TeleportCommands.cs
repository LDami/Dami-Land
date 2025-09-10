using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;

#pragma warning disable IDE0051 // Disable useless private members

namespace SampSharpGameMode1.Commands
{
    class MapHUD : HUD
    {
        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();
        private readonly Dictionary<int, Vector3R> teleportations = new();
        public MapHUD(Player player) : base(player, "mapteleport.json")
        {
            if (mySQLConnector != null)
            {
                float scaleX = 6000 / layers["base"].GetTextdrawSize("mapbox").X;
                float scaleY = 6000 / layers["base"].GetTextdrawSize("mapbox").Y;

                Dictionary<string, object> param = new();
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
                        (tlpPosition.X + 3000) / scaleX + layers["base"].GetTextdrawPosition("mapbox").X,
                        ((tlpPosition.Y * -1) + 3000) / scaleY + layers["base"].GetTextdrawPosition("mapbox").Y
                    );
                    layers["base"].CreateBackground(player, "tlps_" + tlpID, tlpDisplayedPosition, new Vector2(5, 5), ColorPalette.Secondary.Main.GetColor());
                    layers["base"].SetTextdrawText("tlps_" + tlpID, "LD_POOL:nib");
                    layers["base"].UpdateTextdraw("tlps_" + tlpID);
                    layers["base"].SetClickable("tlps_" + tlpID);

                    Color color = ColorPalette.Primary.Main.GetColor();
                    color = new Color(color.R, color.G, color.B, 0.8f);
                    layers["base"].CreateTextdraw(player, "info_" + tlpID, TextdrawLayer.TextdrawType.Box);
                    layers["base"].SetTextdrawText("info_" + tlpID, $"{tlpName}");
                    layers["base"].SetTextdrawSize("info_" + tlpID, 5, 50);
                    layers["base"].SetTextdrawColor("info_" + tlpID, ColorPalette.Secondary.Main.GetColor());
                    layers["base"].SetTextdrawBoxColor("info_" + tlpID, new Color(100, 100, 100, 0.8f));
                    layers["base"].SetTextdrawPosition("info_" + tlpID, layers["base"].GetTextdrawPosition("tlps_" + tlpID) + new Vector2(0, 8));
                    layers["base"].SetTextdrawLetterSize("info_" + tlpID, 0.15f, 0.6f);
                    layers["base"].SetTextdrawFont("info_" + tlpID, 1);
                    layers["base"].SetTextdrawAlignment("info_" + tlpID, 2);
                    layers["base"].SetClickable("info_" + tlpID);

                    row = mySQLConnector.GetNextRow();
                }
                mySQLConnector.CloseReader();
                layers["base"].UnselectAllTextdraw();

                layers["base"].TextdrawClicked += OnTextdrawClicked;
            }
            else
            {
                Logger.WriteLineAndClose("TeleportCommands.cs - MapHUD._:E: MySQL not started");
            }
        }

        private void OnTextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            if(int.TryParse(e.TextdrawName.AsSpan(5), out int tlpID))
            {
                (player as Player).Teleport(teleportations[tlpID].Position);
                player.Angle = teleportations[tlpID].Rotation;
                player.CancelSelectTextDraw();
            }
        }
    }
    class TeleportCommands
    {
        [Command("tlps")]
        private static void TlpsCommand(BasePlayer player)
        {
            MapHUD mapHUD = new(player as Player);
            mapHUD.Show();
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
                InputDialog dialog = new("Creating Teleport point", "Type the name of the teleport point", false, "Save", "Cancel");
                dialog.Response += (sender, e) =>
                {
                    if(e.DialogButton == DialogButton.Left)
                    {
                        MySQLConnector mySQLConnector = MySQLConnector.Instance();
                        Dictionary<string, object> param = new()
                        {
                            { "@name", e.InputText },
                            { "@pos_x", e.Player.Position.X },
                            { "@pos_y", e.Player.Position.Y },
                            { "@pos_z", e.Player.Position.Z },
                            { "@angle", e.Player.Angle },
                            { "@zone", Zone.GetDetailedZoneName(e.Player.Position) }
                        };
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
