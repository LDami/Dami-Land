using SampSharp.GameMode.World;
using SampSharpGameMode1.CustomDatas;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SampSharpGameMode1.Map
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
        int currentPage;
        int nbrOfPages;
        public MapObjectSelector(BasePlayer player, MapObjectGroupData group) : base(player, "mapobjectlist.json")
        {
            layer.SetTextdrawText("category", group.Name);
            layer.SetTextdrawText("description", group.Description);
            List<MapObjectData> objList = MapObjectData.MapObjects.Where(x => x.Group.Name == group.Name).ToList();
            int nbrOfItems = DynamicDuplicateLayer("model#", objList.Count, "background");
            Console.WriteLine("MapObjectSelector.cs - MapObjectSelector._:I: objList = " + objList.Count);
            Console.WriteLine("MapObjectSelector.cs - MapObjectSelector._:I: nbrOfItems = " + nbrOfItems);
            nbrOfPages = objList.Count / nbrOfItems;
            Console.WriteLine("MapObjectSelector.cs - MapObjectSelector._:I: nbrOfPages = " + nbrOfPages);
            if (nbrOfItems != objList.Count)
                player.SendClientMessage("Warning: All objects cannot be displayed");
            shownObjects = new();
            for (int i = 0; i < objList.Count; i++)
            {
                if (i < nbrOfItems)
                {
                    layer.SetTextdrawPreviewModel($"model[{i}]", objList[i].Id);
                    layer.SetClickable($"model[{i}]");
                    shownObjects.Add(objList[i].Id);
                    Console.WriteLine($"MapObjectSelector.cs - MapObjectSelector._:I: model[{i}] = " + objList[i].Id);
                }
            }
            currentPage = 1;
            layer.SetClickable("prevPage");
            layer.SetClickable("nextPage");
            layer.SetTextdrawText("page", "01");
            layer.TextdrawClicked += Layer_TextdrawClicked;
        }

        private void Layer_TextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            if (e.TextdrawName == "prevPage")
            {
                currentPage = Math.Clamp(--currentPage, 1, nbrOfPages);
                layer.SetTextdrawText("page", string.Format("{0,2}", currentPage));
            }
            else if (e.TextdrawName == "nextPage")
            {
                currentPage = Math.Clamp(++currentPage, 1, nbrOfPages);
                layer.SetTextdrawText("page", string.Format("{0,2}", currentPage));
            }
            else
            {
                Regex regex = new(@"model\[(\d)*\]");
                try
                {
                    if (int.TryParse(regex.Matches(e.TextdrawName).First().Groups[1].Value, out int index))
                    {
                        Selected.Invoke(this, new MapObjectSelected(shownObjects[index]));
                    }
                }
                catch(Exception ex)
                {
                    Logger.WriteLineAndClose("MapObjectSelector.cs - MapObjectSelect.Layer_TextdrawClicked:E: " + ex.Message);
                }
            }
        }
    }
}
