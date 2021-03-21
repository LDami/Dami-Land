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
        private TextdrawLayer layer;
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
                            if (textdraw.Type.Equals("box"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                                layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                            }
                            if (textdraw.Type.Equals("text"))
                            {
                                layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                            }
                        }
                        layer.UnselectAllTextdraw();
                        fs.Close();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("HUD.cs - HUD:_:E: Cannot load HUD: " + jsonFilename);
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Hide all textdraw of this HUD layer
        /// </summary>
        public void Hide()
        {
            layer.HideAll();
        }

        /// <summary>
        /// Set text to a Textdraw element
        /// </summary>
        /// <param name="element">The textdraw name</param>
        /// <param name="value">The value to display</param>
        public void SetText(string element, string value)
        {
            layer.SetTextdrawText(element, value);
        }

        /// <summary>
        /// Set color to a Textdraw element
        /// </summary>
        /// <param name="element">The textdraw name</param>
        /// <param name="color">The color to set</param>
        public void SetColor(string element, Color color)
        {
            layer.SetTextdrawColor(element, color);
        }
    }
}
