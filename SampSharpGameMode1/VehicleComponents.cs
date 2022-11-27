using SampSharp.GameMode.World;
using SampSharpGameMode1.CustomDatas;

namespace SampSharpGameMode1
{
    public class VehicleComponents
    {
        private VehicleComponentData componentData;

        private VehicleComponents(VehicleComponentData componentData)
        {
            this.componentData = componentData;
        }

        public static VehicleComponents Get(int componentId)
        {
            return new VehicleComponents(VehicleComponentData.VehicleComponentDatas.Find(element => element.Id == componentId));
        }

        public bool IsCompatibleWithVehicle(BaseVehicle vehicle)
        {
            return componentData.CompatiblesModels.Contains((int)vehicle.Model);
        }
    }
}
