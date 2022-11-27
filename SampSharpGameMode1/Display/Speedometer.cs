using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class Speedometer
    {
        public bool IsDisplayed { get; private set; }
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
            IsDisplayed = false;
        }

        public void Show()
        {
            Hide();
            if (!hud.HasError)
            {
                hud.SetText("vehiclename", "Unknown model");
                hud.SetText("speed", "0 km/h");
                hud.SetText("health", "0 %");
                hud.Show();
                hud.Hide("health_icon");
                IsDisplayed = true;
            }
        }

        public void Hide()
        {
            if (hud != null)
            {
                hud.Hide();
            }
            IsDisplayed = false;
        }

        public void Update()
        {
            if(IsDisplayed)
            {
                string model = player.Vehicle?.ModelInfo.Name ?? "Unknown model";
                if (model.Length > 11)
                {
                    model = model.Insert(11, "\n");
                }
                hud.SetText("vehiclename", model);

                double vel = Math.Sqrt(player.Vehicle.Velocity.LengthSquared) * 181.5;
                hud.SetText("speed", vel.ToString(@"N0") + " km/h");

                hud.SetText("health", (player.Vehicle.Health / 10).ToString(@"N0") + " %");
                if (player.Vehicle.Health < 250)
                {
                    hud.SetColor("health", Color.Red);
                    if (health_icon_index == 0)
                    {
                        health_icon_index = 1;
                        hud.SetText("health_icon", "LD_DUAL:ex1");
                        hud.Show("health_icon");
                        health_icon_lastUpdate = DateTime.Now;
                    }
                    if ((DateTime.Now - health_icon_lastUpdate).TotalMilliseconds > health_icon_delay)
                    {
                        health_icon_index++;
                        if (health_icon_index > 4)
                            health_icon_index = 1;
                        hud.SetText("health_icon", "LD_DUAL:ex" + health_icon_index);
                    }
                }
                else
                {
                    hud.SetColor("health", Color.White);
                    if (health_icon_index != 0)
                    {
                        hud.Hide("health_icon");
                        health_icon_index = 0;
                    }
                }
            }
        }
    }
}
