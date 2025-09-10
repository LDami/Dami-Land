using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SampSharpGameMode1.Display
{
    public class HUD
    {
        // The name of the textdraw will manage on which layer it should be set
        // If there is a : in the textdraw name, the first part (before the :) will be the layer name.
        // For example, all textdraws having name starting with `group:` will be in the layer "group"
        // Textdraws named `group#` are not in the same layer as `group[1]`
        // So if a method is called with a layer name, all textdraw inside will be changed, hidden, shown ...

        public bool HasError { get; private set; }
        /// <summary>
        /// The mode for the DynamicDuplicate method
        /// </summary>
        public enum DuplicateMode { ROWS_COLS, ROWS, COLS }

        protected BasePlayer player;
        protected Dictionary<string, TextdrawLayer> layers; // <name, layer>
        protected string filename;

        /// <summary>
        /// Create a HUD for a player from a JSON file
        /// </summary>
        /// <param name="player">The target Player</param>
        /// <param name="jsonFilename">The JSON file name, with ".json" extension</param>
        public HUD(BasePlayer _player, string jsonFilename)
        {
            Stopwatch sw = new();
            sw.Start();
            player = _player;
            TextdrawLayer baseLayer = new TextdrawLayer
            {
                AutoUpdate = false
            };
            layers = new Dictionary<string, TextdrawLayer>
            {
                { "base", baseLayer }
            };
            filename = $@"{Directory.GetCurrentDirectory()}/scriptfiles/{jsonFilename}";
            this.Load();
            Console.WriteLine("Time for load HUD: " + sw.ElapsedMilliseconds + "ms (filename: " + jsonFilename + ")");
        }

        public void Load()
        {
            if (File.Exists(filename))
            {
                try
                {
                    using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
                    {
                        byte[] output = new byte[fs.Length];
                        int idx = 0;
                        int blockLength = 1;
                        byte[] tmp = new byte[blockLength];
                        int readBytes;
                        while ((readBytes = fs.Read(tmp, 0, blockLength)) > 0)
                        {
                            for (int i = 0; i < readBytes; i++)
                                output[idx + i] = tmp[i];
                            idx += readBytes;
                        }
                        string jsonData = new UTF8Encoding(true).GetString(output);
                        List<textdraw> textdraws = JsonConvert.DeserializeObject<List<textdraw>>(jsonData);
                        string lastTDName = "";
                        TextdrawLayer layer;
                        foreach (textdraw textdraw in textdraws)
                        {
                            if (textdraw.Name.Contains(':'))
                            {
                                string layerName = textdraw.Name[..textdraw.Name.IndexOf(":")];
                                layer = layers.FirstOrDefault(l => l.Key.StartsWith(layerName)).Value;
                                if(layer == null)
                                {
                                    layer = new TextdrawLayer() { AutoUpdate = false };
                                    layers.Add(layerName, layer);
                                }
                            }
                            else
                                layer = layers.First().Value;
                            if (textdraw.Type.Equals("background"))
                            {
                                layer.CreateBackground(player, textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY), new Vector2(textdraw.Width, textdraw.Height), textdraw.Color);
                                layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY)); // Fixes position
                            }
                            if (textdraw.Type.Equals("box"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                                layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                                layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layer.SetTextdrawBoxColor(textdraw.Name, textdraw.BackColor);
                            }
                            if (textdraw.Type.Equals("text"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                layer.SetTextdrawText(textdraw.Name, textdraw.Text);
                                layer.SetTextdrawAlignment(textdraw.Name, textdraw.Alignment);
                                layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layer.SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
                                layer.SetTextdrawFont(textdraw.Name, textdraw.Font);
                                if (textdraw.Font == 4)
                                    layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                                if (textdraw.LetterWidth > 0 && textdraw.LetterHeight > 0)
                                    layer.SetTextdrawLetterSize(textdraw.Name, textdraw.LetterWidth, textdraw.LetterHeight);
                            }
                            if (textdraw.Type.Equals("previewmodel"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.PreviewModel);
                                layer.SetTextdrawPreviewModel(textdraw.Name, textdraw.PreviewModel);
                                layer.SetTextdrawAlignment(textdraw.Name, textdraw.Alignment);
                                layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layer.SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
                            }
                            layer.UpdateTextdraw(textdraw.Name);
                            if (!layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY)))
                                player.SendClientMessage(Color.Red, "Cannot set position for '" + textdraw.Name + "'");
                            if (!layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height))
                                player.SendClientMessage(Color.Red, "Cannot set size for '" + textdraw.Name + "'");
                            lastTDName = textdraw.Name;
                        }
                        foreach(TextdrawLayer layertmp in layers.Values)
                            layertmp.UnselectAllTextdraw();
                        fs.Close();
                    }
                }
                catch (IOException e)
                {
                    Logger.WriteLineAndClose("HUD.cs - HUD:_:E: Cannot load HUD: " + filename);
                    Logger.WriteLineAndClose(e.Message);
                    HasError = true;
                }
                catch (JsonReaderException e)
                {
                    Logger.WriteLineAndClose("HUD.cs - HUD:_:E: Cannot load HUD: " + filename);
                    Logger.WriteLineAndClose(e.Message);
                    HasError = true;
                }
            }
            else
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:_:E: Cannot load HUD: " + filename);
                Logger.WriteLineAndClose("The file " + filename + " does not exists.");
                HasError = true;
            }
            foreach (TextdrawLayer layertmp in layers.Values)
                layertmp.AutoUpdate = true;
        }

        public void Unload()
        {
            foreach (TextdrawLayer layer in layers.Values)
                layer.Dispose();
        }

        /// <summary>
        /// Returns true if the textdraw exist in it's layer
        /// </summary>
        /// <param name="element">Element to find</param>
        public bool ElementExistsInALayer(string element, out TextdrawLayer elementLayer)
        {
            // remove regex characters
            // https://regex101.com/r/9GBkL2/1
            List<string> str = Regex.Match(element, @"[\^]?(?'all'(?'layer'[a-zA-Z#_0-9]*(\[\d*\])*)[:]?[a-zA-Z#_0-9]*)[$]?", RegexOptions.ExplicitCapture).Groups.Select((Group g) => g.Value).ToList();
            string elementName = str[1];
            string layerName = str[2];
            if (layerName == elementName)
                layerName = "base";

            // The Regex remove the : (for example with duplicated layers: group[1].name, we get `group`)
            TextdrawLayer layer = layers.FirstOrDefault(l => l.Key.StartsWith(layerName)).Value;
            elementLayer = layer;
            if (layer == null)
                return false;
            return layer.Exists(elementName);
        }

        /// <summary>
        /// Show all textdraw of all HUD layer, the <paramref name="element"/> textdraw, or the all the layer passed in <paramref name="element"/> param
        /// <param name="element">The textdraw or layer name. All textdraws of all layers if empty</param>
        /// </summary>
        public void Show(string element = "")
        {
#if DEBUG
            Logger.WriteLineAndClose("HUD.cs - HUD:Show:D: Called for '" + element + "'");
#endif
            if (element == "")
            {
                foreach (TextdrawLayer layer in layers.Values)
                    layer.ShowAll();
            }
            else
            {
                if (ElementExistsInALayer(element, out TextdrawLayer layer))
                    layer.Show(element);
                else
                {
                    if(layer == null)
                    {
                        throw new Exception("The given element is not inside an existing layer");
                    }

                    // If element does not exist, it's probably a regex
                    // element will be now used as pattern, and the [] must be managed differently
                    element = element.Replace("[", @"\[").Replace("]", @"\]");
                    List<string> tdList;

                    if (element.Contains(':')) // If regex is in a layer
                    {
                        tdList = new List<string>(layer.GetTextdrawList().Keys);
                        foreach (string td in Utils.GetStringsMatchingRegex(tdList, element))
                        {
                            layer.Show(td);
                        }
                    }
                    else
                    {
                        foreach (TextdrawLayer layer2 in layers.Values)
                        {
                            tdList = new List<string>(layer2.GetTextdrawList().Keys);
                            foreach (string td in Utils.GetStringsMatchingRegex(tdList, element))
                            {
                                layer2.Show(td);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Hide all textdraw of this HUD layer or only the <paramref name="element"/> textdraw
        /// <param name="element">The textdraw name. All textdraws if empty</param>
        /// </summary>
        public void Hide(string element = "")
        {
#if DEBUG
            Logger.WriteLineAndClose("HUD.cs - HUD:Hide:D: Called for '" + element + "'");
#endif
            if (element == "")
            {
                foreach (TextdrawLayer layer in layers.Values)
                    layer.HideAll();
            }
            else
            {
                if (ElementExistsInALayer(element, out TextdrawLayer layer))
                    layer.Hide(element);
                else
                {
                    if (layer == null)
                    {
                        throw new Exception("The given element is not inside an existing layer");
                    }

                    // If element does not exist, it's probably a regex
                    // element will be now used as pattern, and the [] must be managed differently
                    element = element.Replace("[", @"\[").Replace("]", @"\]");
                    List<string> tdList;

                    if (element.Contains(':')) // If regex is in a layer
                    {
                        tdList = new List<string>(layer.GetTextdrawList().Keys);
                        foreach (string td in Utils.GetStringsMatchingRegex(tdList, element))
                        {
                            layer.Hide(td);
                        }
                    }
                    else
                    {
                        foreach (TextdrawLayer layer2 in layers.Values)
                        {
                            tdList = new List<string>(layer2.GetTextdrawList().Keys);
                            foreach (string td in Utils.GetStringsMatchingRegex(tdList, element))
                            {
                                layer2.Hide(td);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Hide and Show again all textdraw of this HUD layer or only the <paramref name="element"/> textdraw
        /// <param name="element">The textdraw name. All textdraws if empty</param>
        /// </summary>
        public void Refresh(string element = "")
        {
            this.Hide(element);
            this.Show(element);
        }

        /// <summary>
        /// Set text to a Textdraw element
        /// </summary>
        /// <param name="element">The textdraw name</param>
        /// <param name="value">The value to display</param>
        public void SetText(string element, string value)
        {
            try
            {
                if (ElementExistsInALayer(element, out TextdrawLayer layer))
                    layer.SetTextdrawText(element, value);
            }
            catch (TextdrawNameNotFoundException e)
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:SetText:E: " + e.Message);
                HasError = true;
            }
        }

        /// <summary>
        /// Set Textdraw's size
        /// </summary>
        /// <param name="element">The textdraw name</param>
        /// <param name="width">The width to set</param>
        /// <param name="height">The height to set</param>
        public void SetSize(string element, float width, float height)
        {
            try
            {
                if (ElementExistsInALayer(element, out TextdrawLayer layer))
                    layer.SetTextdrawSize(element, width, height);
            }
            catch (TextdrawNameNotFoundException e)
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:SetSize:E: " + e.Message);
                HasError = true;
            }
        }

        /// <summary>
        /// Set color to a Textdraw element
        /// </summary>
        /// <param name="element">The textdraw name</param>
        /// <param name="color">The color to set</param>
        public void SetColor(string element, Color color)
        {
            try
            {
                if (ElementExistsInALayer(element, out TextdrawLayer layer))
                    layer.SetTextdrawColor(element, color);
            }
            catch (TextdrawNameNotFoundException e)
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:SetColor:E: " + e.Message);
                HasError = true;
            }
        }

        /// <summary>
        /// Set the model ID to a preview
        /// </summary>
        /// <param name="element">The textdraw name</param>
        /// <param name="model">The model ID to preview</param>
        public void SetPreviewModel(string element, int model)
        {
            try
            {
                if (ElementExistsInALayer(element, out TextdrawLayer layer))
                {
                    if (layer.GetTextdrawType(element) == TextdrawLayer.TextdrawType.PreviewModel)
                    {
                        layer.SetTextdrawPreviewModel(element, model);
                    }
                }
            }
            catch (TextdrawNameNotFoundException e)
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:SetPreviewModel:E: " + e.Message);
                HasError = true;
            }
        }

        /// <summary>
        /// Duplicate all textdraws from a <paramref name="layer"/> inside a container element
        /// </summary>
        /// <param name="layer">The layer to duplicate</param>
        /// <param name="number">Number of duplicate textdraws</param>
        /// <param name="containerElement">The container textdraw</param>
        /// <param name="mode">The mode of duplicate</param>
        /// <exception cref="Exception">Throw an exception if the element textdraw name does not contain #</exception>
        /// <returns>The number of item that has been correctly displayed</returns>
        public int DynamicDuplicateLayer(string layerName, int number, string containerElement, DuplicateMode mode = DuplicateMode.ROWS_COLS)
        {
            if (!layerName.Contains('#'))
            {
                throw new Exception("Can't duplicate a layout having name without # char");
            }

            Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:D: Called for layer " + layerName);
            Stopwatch sw = new();
            sw.Start();
            float spacing = 5f;

            Vector2 bgElementPosition;
            Vector2 bgElementSize;
            Vector2 containerPosition;
            Vector2 containerSize;

            if (layers.ContainsKey(layerName))
            {
                // Find the biggest object of this layer to duplicate (usually the background)
                string bgElement = layerName + ":bg";
                if(ElementExistsInALayer(bgElement, out TextdrawLayer layer) && ElementExistsInALayer(containerElement, out TextdrawLayer layerContainer))
                {
#if DEBUG
                    Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:D: bgElement and containerElement exists");
#endif
                    bgElementPosition = layer.GetTextdrawPosition(bgElement);
                    bgElementSize = layer.GetTextdrawSize(bgElement);
                    containerPosition = layerContainer.GetTextdrawPosition(containerElement);
                    containerSize = layerContainer.GetTextdrawSize(containerElement);

#if DEBUG
                    Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:D: Hiding original layout");
#endif
                    this.Hide("^" + layerName + ".*$");
                    Dictionary<string, Textdraw> elements = new(layer.GetTextdrawList().Where(td => td.Key.Contains('#')));

                    int index = 0;
                    int itemsPerRow = (int)Math.Truncate(containerSize.X / (bgElementSize.X + spacing));
                    if (mode == DuplicateMode.COLS)
                        itemsPerRow = 1;
#if DEBUG
                    Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:D: itemsPerRow = " + itemsPerRow);
#endif

                    void AddItemsToRow(int row)
                    {
                        Vector2 newPosition;
                        Vector2 elementPosition;
                        Vector2 elementSize;
                        for (int i = 0; i < itemsPerRow; i++)
                        {
                            if (index < number)
                            {
                                string indexLayerName = layerName.Replace("#", $"[{index}]");
                                if (!layers.ContainsKey(indexLayerName))
                                    layers.Add(indexLayerName, new TextdrawLayer { AutoUpdate = false });

                                TextdrawLayer layerOfIndex = layers.First(l => l.Key == indexLayerName).Value;
                                foreach (KeyValuePair<string, Textdraw> td in elements)
                                {
                                    string newName = td.Key.Replace("#", $"[{index}]");
                                    elementPosition = layer.GetTextdrawPosition(td.Key);
                                    elementSize = layer.GetTextdrawSize(td.Key);
                                    newPosition = new(
                                        containerPosition.X + (bgElementSize.X + spacing) * i + spacing +(elementPosition.X - bgElementPosition.X) * (i + 1),
                                        containerPosition.Y + (bgElementSize.Y + spacing) * row + (elementPosition.Y - bgElementPosition.Y) * (i + 1)
                                    );
                                    Textdraw newTD = layer.GetCopy(player, td.Key, newName);
                                    layerOfIndex.Add(newTD, layer.GetTextdrawType(td.Key));
                                    _ = layerOfIndex.SetTextdrawPosition(newName, newPosition);
                                    if (layer.GetTextdrawType(td.Key) == TextdrawLayer.TextdrawType.Text) // The text textdraws manage width differently
                                        _ = layerOfIndex.SetTextdrawSize(newName, elementSize.X + (elementSize.X + spacing) * i, elementSize.Y);
                                    else
                                        _ = layerOfIndex.SetTextdrawSize(newName, elementSize.X, elementSize.Y);
#if DEBUG
                                    Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:I: Adding " + newName + " at pos: " + layerOfIndex.GetTextdrawPosition(newName).ToString());
#endif
                                }
                                index++;
                            }
                        }
                    }
                    ;

                    if (number > itemsPerRow)
                    {
                        int possibleRows = (int)Math.Truncate((containerSize.Y - (bgElementPosition.Y - containerPosition.Y)) / (bgElementSize.Y + spacing));
                        if (mode == DuplicateMode.ROWS)
                            possibleRows = 1;
#if DEBUG
                        Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:D: possibleRows = " + possibleRows);
#endif
                        for (int i = 0; i < possibleRows; i++)
                        {
                            AddItemsToRow(i);
                        }
                    }
                    else
                        AddItemsToRow(0);
                    Console.WriteLine("HUD.cs - HUD:DynamicDuplicateLayer:D: Time for duplicate layer: " + sw.ElapsedMilliseconds + "ms (layer: " + layerName + ")");
                    return index;

                }
                else
                {
                    Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:E: Can't duplicate the layer '" + layerName + "' because it does not contain 'bg' textdraw");
                    throw new Exception("The layer " + layerName + " does not contain any background layer");
                }
            }
            else
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:E: Can't duplicate the layer '" + layerName + "' because it does exist");
                throw new Exception("The layer " + layerName + " does not exist");
            }
        }

        /// <summary>
        /// Duplicate the element textdraw inside a container element
        /// </summary>
        /// <param name="element">The textdraw to duplicate</param>
        /// <param name="number">Number of duplicate textdraws</param>
        /// <param name="containerElement">The container textdraw</param>
        /// <param name="mode">The mode of duplicate</param>
        /// <exception cref="Exception">Throw an exception if the element textdraw name does not contain #</exception>
        /// <returns>The number of item that has been correctly displayed</returns>
        public int DynamicDuplicateElement(string element, int number, string containerElement, DuplicateMode mode = DuplicateMode.ROWS_COLS)
        {
            if (!element.Contains('#'))
            {
                throw new Exception("Can't duplicate an element having name without # char");
            }

            float spacing = 5f;

            Vector2 elementPosition;
            Vector2 elementSize;
            Vector2 containerPosition;
            Vector2 containerSize;

            if (ElementExistsInALayer(element, out TextdrawLayer layer))
            {
                elementPosition = layer.GetTextdrawPosition(element);
                elementSize = layer.GetTextdrawSize(element);
                if (layer.GetTextdrawType(element) == TextdrawLayer.TextdrawType.Text) // The text textdraws manage width differently
                    elementSize = new Vector2(elementSize.X - elementPosition.X, elementSize.Y);
                containerPosition = layer.GetTextdrawPosition(containerElement);
                containerSize = layer.GetTextdrawSize(containerElement);

                layer.Hide(element);

                int index = 0;
                int itemsPerRow = (int)Math.Truncate(containerSize.X / (elementSize.X + spacing));
                if (mode == DuplicateMode.COLS)
                    itemsPerRow = 1;
#if DEBUG
                Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateElement:D: itemsPerRow = " + itemsPerRow);
#endif

                void AddItemsToRow(int row)
                {
                    for (int i = 0; i < itemsPerRow; i++)
                    {
                        if (index < number)
                        {
                            string newName = element.Replace("#", $"[{index++}]");
                            Vector2 newPosition = new(
                                containerPosition.X + (elementSize.X + spacing) * i + spacing,
                                elementPosition.Y + (elementSize.Y + spacing) * row
                            );
                            layer.Duplicate(player, element, newName);
                            _ = layer.SetTextdrawPosition(newName, newPosition);
                            if (layer.GetTextdrawType(element) == TextdrawLayer.TextdrawType.Text) // The text textdraws manage width differently
                                _ = layer.SetTextdrawSize(newName, elementSize.X + (elementSize.X + spacing) * i, elementSize.Y);
                            else
                                _ = layer.SetTextdrawSize(newName, elementSize.X, elementSize.Y);
#if DEBUG
                            Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateElement:I: Adding " + newName + " at pos: " + layer.GetTextdrawPosition(newName).ToString());
#endif
                        }
                    }
                }
            ;

                if (number > itemsPerRow)
                {
                    int possibleRows = (int)Math.Truncate((containerSize.Y - (elementPosition.Y - containerPosition.Y)) / (elementSize.Y + spacing));
                    if (mode == DuplicateMode.ROWS)
                        possibleRows = 1;
#if DEBUG
                    Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateElement:D: possibleRows = " + possibleRows);
#endif
                    for (int i = 0; i < possibleRows; i++)
                    {
                        AddItemsToRow(i);
                    }
                }
                else
                    AddItemsToRow(0);
                return index;

            }
            else
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateElement:E: Can't duplicate the element '" + element + "' because it does not exists");
                throw new Exception("The element " + element + " does not exist in this layer");
            }
        }
    }
}
