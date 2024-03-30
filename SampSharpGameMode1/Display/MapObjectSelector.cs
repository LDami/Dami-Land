using SampSharp.GameMode.World;
using SampSharpGameMode1.CustomDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SampSharpGameMode1.Display
{
    public class MapObjectSelected : EventArgs
    {
        public int Id { get; set; }
        public MapObjectSelected(int id)
        {
            Id = id;
        }
    }
    public class MapObjectSelector : HUD
    {
        public event EventHandler<MapObjectSelected> Selected;
        List<int> shownObjects;
        public MapObjectSelector(BasePlayer player, MapObjectGroupData group) : base(player, "mapobjectlist.json")
        {
            layer.SetTextdrawText("category", group.Name);
            layer.SetTextdrawText("description", group.Description);
            this.DynamicDuplicateLayer("model#", 5, "background");
            List<MapObjectData> objList = MapObjectData.MapObjects.Where(x => x.Group.Name == group.Name).ToList();
            shownObjects = new();
            for (int i = 0; i < objList.Count; i++)
            {
                if (i < 5)
                {
                    layer.SetTextdrawPreviewModel($"model[{i}]", objList[i].Id);
                    layer.SetClickable($"model[{i}]");
                    shownObjects.Add(objList[i].Id);
                }
            }
            layer.TextdrawClicked += Layer_TextdrawClicked;
        }

        private void Layer_TextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            Regex regex = new(@"model\[(\d)\]");
            if (int.TryParse(regex.Matches(e.TextdrawName).First().Groups[1].Value, out int index))
            {
                Selected.Invoke(this, new MapObjectSelected(shownObjects[index]));
            }
        }
    }
}
