using SampSharp.GameMode.World;
using SampSharpGameMode1.CustomDatas;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events;
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
        List<int> allObjects;
        List<int> shownObjects;
        int currentPage;
        int nbrOfPages;
        int nbrOfItems;
        public MapObjectSelector(BasePlayer player, MapObjectGroupData group) : base(player, "mapobjectlist.json")
        {
            layer.SetTextdrawText("category", group.Name);
            layer.SetTextdrawText("description", group.Description);
            allObjects = MapObjectData.MapObjects.Where(x => x.Group.Name == group.Name).Select(x => x.Id).ToList();
            nbrOfItems = DynamicDuplicateLayer("model#", allObjects.Count, "background");
            Console.WriteLine("MapObjectSelector.cs - MapObjectSelector._:I: allObjects = " + allObjects.Count);
            Console.WriteLine("MapObjectSelector.cs - MapObjectSelector._:I: nbrOfItems = " + nbrOfItems);
            nbrOfPages = allObjects.Count / nbrOfItems;
            Console.WriteLine("MapObjectSelector.cs - MapObjectSelector._:I: nbrOfPages = " + nbrOfPages);
            if (nbrOfItems != allObjects.Count)
                player.SendClientMessage("Warning: All objects cannot be displayed on one page");
            shownObjects = new();
            for (int i = 0; i < allObjects.Count; i++)
            {
                if (i < nbrOfItems)
                {
                    layer.SetTextdrawPreviewModel($"model[{i}]", allObjects[i]);
                    layer.SetClickable($"model[{i}]");
                    shownObjects.Add(allObjects[i]);
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
                UpdatePage();
            }
            else if (e.TextdrawName == "nextPage")
            {
                currentPage = Math.Clamp(++currentPage, 1, nbrOfPages);
                UpdatePage();
            }
            else
            {
                Regex regex = new(@"model\[(\d*)\]");
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
        private void UpdatePage()
        {
            layer.SetTextdrawText("page", string.Format("{0,2}", currentPage));
            shownObjects = new();
            for (int i = 0; i < nbrOfItems - 1; i++)
            {
                if ((nbrOfItems * currentPage) - 1 + i >= allObjects.Count)
                    layer.Hide($"model[{i}]");
                else
                {
                    int id = allObjects[(nbrOfItems * currentPage) - 1 + i];
                    layer.SetTextdrawPreviewModel($"model[{i}]", id);
                    layer.SetClickable($"model[{i}]");
                    shownObjects.Add(id);
                }
            }
        }
    }
}
