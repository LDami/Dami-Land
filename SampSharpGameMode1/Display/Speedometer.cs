using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class Speedometer
    {
        private HUD hud;
        private Player player;
        private int health_icon_index = 0; // 0 does not exist: means that the icon is not displayed yet
        private int health_icon_delay = 100; // 100ms between frames
        private DateTime health_icon_lastUpdate; // Last time the frame changed

        public Speedometer(Player player)
        {
            hud = new HUD(player, "speedometer.json");
            hud.Hide();
            this.player = player;
        }

        public void Show()
        {
            Hide();
            if (!hud.HasError)
            {
                hud.SetText("speed", "0 km/h");
                hud.SetText("health", "0 %");
                hud.SetText("vehiclename", player.Vehicle?.ModelInfo.Name ?? "Unknown model");
                hud.Show();
                hud.Hide("health_icon");
            }
            else
                Logger.WriteLineAndClose("has error");
        }

        public void Hide()
        {
            if (hud != null)
            {
                hud.Hide();
            }
        }

        public void Update()
        {
            double vel = Math.Sqrt(player.Vehicle.Velocity.LengthSquared) * 181.5;
            hud.SetText("speed", vel.ToString(@"N0") + " km/h");
            hud.SetText("health", (player.Vehicle.Health / 10).ToString(@"N0") + " %");
            if (player.Vehicle.Health < 300)
            {
                if(health_icon_index == 0)
                {
                    health_icon_index = 1;
                    hud.SetText("health_icon", "LD_DUAL:ex1");
                    hud.Show("health_icon");
                    health_icon_lastUpdate = DateTime.Now;
                }
                if((DateTime.Now - health_icon_lastUpdate).TotalMilliseconds > health_icon_delay)
                {
                    health_icon_index++;
                    if (health_icon_index > 4)
                        health_icon_index = 1;
                    hud.SetText("health_icon", "LD_DUAL:ex" + health_icon_index);
                }
            }
            else
            {
                if(health_icon_index != 0)
                {
                    hud.Hide("health_icon");
                    health_icon_index = 0;
                }
            }
        }
    }
}
