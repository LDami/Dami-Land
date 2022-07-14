using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class HUD
    {
        public bool HasError { get; private set; }

        protected TextdrawLayer layer;
        /// <summary>
        /// Create a HUD for a player from a JSON file
        /// </summary>
        /// <param name="player">The target Player</param>
        /// <param name="jsonFilename">The JSON file name, with ".json" extension</param>
        public HUD(Player player, string jsonFilename)
        {
            layer = new TextdrawLayer();
            string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\" + jsonFilename;
            string jsonData = "";
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
                        jsonData = new UTF8Encoding(true).GetString(output);
                        List<textdraw> textdraws = JsonConvert.DeserializeObject<List<textdraw>>(jsonData);
                        foreach (textdraw textdraw in textdraws)
                        {
                            if (textdraw.Type.Equals("background"))
                            {
                                layer.CreateBackground(player, textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY), new Vector2(textdraw.Width, textdraw.Height), textdraw.Color);
                                //layer.SetTextdrawText(textdraw.Name, textdraw.Text);
                            }
                            if (textdraw.Type.Equals("box"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                                layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                                layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layer.SetTextdrawBoxColor(textdraw.Name, textdraw.BackColor);
                            }
                            if (textdraw.Type.Equals("text"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                layer.SetTextdrawText(textdraw.Name, textdraw.Text);
                                layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                layer.SetTextdrawAlignment(textdraw.Name, textdraw.Alignment);
                                layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layer.SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
                                layer.SetTextdrawFont(textdraw.Name, textdraw.Font);
                                if(textdraw.Font == 4)
                                    layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                                if (textdraw.Width > 0 && textdraw.Height > 0)
                                    layer.SetTextdrawLetterSize(textdraw.Name, textdraw.Width, textdraw.Height);
                            }
                        }
                        layer.UnselectAllTextdraw();
                        fs.Close();
                    }
                }
                catch (IOException e)
                {
                    Logger.WriteLineAndClose("HUD.cs - HUD:_:E: Cannot load HUD: " + jsonFilename);
                    Logger.WriteLineAndClose(e.Message);
                    HasError = true;
                }
                catch(JsonReaderException e)
                {
                    Logger.WriteLineAndClose("HUD.cs - HUD:_:E: Cannot load HUD: " + jsonFilename);
                    Logger.WriteLineAndClose(e.Message);
                    HasError = true;
                }
            }
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
                layer.Show(element);
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
                layer.Hide(element);
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
            catch(TextdrawNameNotFoundException e)
            {
                Logger.WriteLineAndClose("HUD.cs - HUD:SetText:E: " + e.Message);
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
    }
}
