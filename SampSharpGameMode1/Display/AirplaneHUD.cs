using SampSharp.GameMode.World;
using System;

namespace SampSharpGameMode1.Display
{
    public class AirplaneHUD
    {
        public bool IsDisplayed { get; private set; }
        private readonly HUD hud;
        private readonly Player player;

        public AirplaneHUD(Player player)
        {
            hud = new HUD(player, "airplanehud.json");
            hud.Hide();
            this.player = player;
            IsDisplayed = false;
        }

        public void Show()
        {
            Hide();
            if (!hud.HasError && player.InAnyVehicle && VehicleModelInfo.ForVehicle(player.Vehicle).Category == SampSharp.GameMode.Definitions.VehicleCategory.Airplane)
            {
                hud.SetText("landing_gear", "Landing gear: " + (player.Vehicle.IsGearUp ? "Up" : "Down"));
                hud.Show();
                if (player.Vehicle.Model == SampSharp.GameMode.Definitions.VehicleModelType.Hydra)
                {
                    float hydraReactorAngle = player.Vehicle.HydraReactorAngle;
                    hydraReactorAngle = (hydraReactorAngle - 0) / (327685000 - 0) * (90 - 0);
                    hud.SetText("hydra_reactor_angle", "Reactor angle: " + hydraReactorAngle.ToString("F0"));
                }
                else
                {
                    hud.Hide("hydra_reactor_angle");
                }
                hud.Hide("landing_gear_alert");
                IsDisplayed = true;
            }
        }

        public void Hide()
        {
            hud?.Hide();
            IsDisplayed = false;
        }

        public void Update()
        {
            if (IsDisplayed)
            {
                if (player.Vehicle == null)
                    return;

                hud.SetText("landing_gear", "Landing gear: " + (player.Vehicle.IsGearUp ? "Up" : "Down"));

                if (player.Vehicle.Model == SampSharp.GameMode.Definitions.VehicleModelType.Hydra)
                {
                    float hydraReactorAngle = player.Vehicle.HydraReactorAngle;
                    hydraReactorAngle = (hydraReactorAngle - 0) / (327685000 - 0) * (90 - 0);
                    hud.SetText("hydra_reactor_angle", "Reactor angle: " + hydraReactorAngle.ToString("F0"));
                }

            }
        }
    }
}
