using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class HUD
    {
        public bool HasError { get; private set; }

        protected BasePlayer player;
        protected TextdrawLayer layer;
        protected string filename;

        /// <summary>
        /// Create a HUD for a player from a JSON file
        /// </summary>
        /// <param name="player">The target Player</param>
        /// <param name="jsonFilename">The JSON file name, with ".json" extension</param>
        public HUD(BasePlayer _player, string jsonFilename)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            player = _player;
            layer = new TextdrawLayer
            {
                AutoUpdate = false
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
                        foreach (textdraw textdraw in textdraws)
                        {
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
                        layer.UnselectAllTextdraw();
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
            layer.AutoUpdate = true;
        }

        public void Unload()
        {
            layer.Dispose();
        }

        /// <summary>
        /// Show all textdraw of this HUD layer or only the <paramref name="element"/> textdraw
        /// <param name="element">The textdraw name. All textdraws if empty</param>
        /// </summary>
        public void Show(string element = "")
        {
            if (element == "")
                layer.ShowAll();
            else
            {
                if (layer.Exists(element))
                    layer.Show(element);
                else
                {
                    foreach (string td in Utils.GetStringsMatchingRegex(new List<string>(layer.GetTextdrawList().Keys), element))
                    {
                        layer.Show(td);
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
            if (element == "")
                layer.HideAll();
            else
            {
                if(layer.Exists(element))
                    layer.Hide(element);
                else
                {
                    foreach (string td in Utils.GetStringsMatchingRegex(new List<string>(layer.GetTextdrawList().Keys), element))
                    {
                        //Console.WriteLine("Hiding td " + td);
                        layer.Hide(td);
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
            if (element == "")
            {
                layer.HideAll();
                layer.ShowAll();
            }
            else
            {
                if (layer.Exists(element))
                {
                    layer.Hide(element);
                    layer.Show(element);
                }
                else
                {
                    foreach (string td in Utils.GetStringsMatchingRegex(new List<string>(layer.GetTextdrawList().Keys), element))
                    {
                        if(td.EndsWith("bg"))
                            Logger.WriteLineAndClose("HUD.cs - HUD.Refresh:I: " + td + " color: " + layer.GetTextdrawColor(td).ToString() + " " + layer.GetTextdrawColor(td).A);
                        layer.Hide(td);
                        layer.Show(td);
                    }
                }
            }
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
                if (layer.GetTextdrawType(element) == TextdrawLayer.TextdrawType.PreviewModel)
                {
                    layer.SetTextdrawPreviewModel(element, model);
                }
            }
            catch (TextdrawNameNotFoundException e)
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:SetPreviewModel:E: " + e.Message);
                HasError = true;
            }
        }

        /// <summary>
        /// Duplicate the element textdraw inside a container element
        /// </summary>
        /// <param name="element">The textdraw to duplicate</param>
        /// <param name="number">Number of duplicate textdraws</param>
        /// <param name="containerElement">The container textdraw</param>
        /// <exception cref="Exception">Throw an exception if the element textdraw name does not contain #</exception>
        /// <returns>The number of item that has been correctly displayed</returns>
        public int DynamicDuplicateLayer(string element, int number, string containerElement)
        {
            float spacing = 5f;
            Vector2 elementPosition = layer.GetTextdrawPosition(element);
            Vector2 elementSize = layer.GetTextdrawSize(element);
            if (layer.GetTextdrawType(element) == TextdrawLayer.TextdrawType.Text) // The text textdraws manage width differently
                elementSize = new Vector2(elementSize.X - elementPosition.X, elementSize.Y);
            Vector2 containerPosition = layer.GetTextdrawPosition(containerElement);
            Vector2 containerSize = layer.GetTextdrawSize(containerElement);

            if(!element.Contains('#'))
            {
                throw new Exception("Can't duplicate an element having name without # char");
            }

            layer.Hide(element);

            int index = 0;
            int itemsPerRow = (int)Math.Truncate(containerSize.X / (elementSize.X + spacing));
#if DEBUG
            Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:D: itemsPerRow = " + itemsPerRow);
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
                        Logger.WriteLineAndClose("HUD.cs - HUD:DynamicDuplicateLayer:I: Adding " + newName + " at pos: " + layer.GetTextdrawPosition(newName).ToString());
#endif
                    }
                }
            };

            if (number > itemsPerRow)
            {
                int possibleRows = (int)Math.Truncate((containerSize.Y - (elementPosition.Y - containerPosition.Y)) / (elementSize.Y + spacing));
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
            return index;
        }
    }
}
