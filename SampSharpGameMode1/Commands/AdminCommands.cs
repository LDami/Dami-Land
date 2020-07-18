using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using System;

namespace SampSharpGameMode1.Commands
{
    class AdminCommands : Player
    {
        [Command("getmodel")]
        private void GetModel(string model)
        {
            SendClientMessage("Found model: " + Utils.GetVehicleModelType(model).ToString());
        }

        [Command("vehicle", "veh", "v", DisplayName = "v")]
        private void SpawnVehicleCommand(VehicleModelType model)
        {
            Random rndColor = new Random();
            BaseVehicle v = BaseVehicle.Create(model, new Vector3(this.Position.X + 5.0, this.Position.Y, this.Position.Z), 0.0f, rndColor.Next(0, 255), rndColor.Next(0, 255));
            this.PutInVehicle(v, 0);
        }


        [Command("tp")]
        private void TP()
        {
            this.Position = new Vector3(1431.6393, 1519.5398, 10.5988);
        }
    }
}
