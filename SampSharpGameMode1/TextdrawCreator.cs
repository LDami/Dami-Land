using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1
{
    class TextdrawCreator
    {

        class TextdrawHUD
        {
            TextdrawLayer layer;

            public TextdrawHUD(Player player)
            {
                layer = new TextdrawLayer();
                string filename = BaseMode.Instance.Client.ServerPath + "\\scriptfiles\\tdcreator.json";
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
                            layer.SetTextdrawText("layer", "Layer: None");
                            layer.SetTextdrawText("tdselected", "TD: None");
                            layer.SetTextdrawText("tdmode", "Mode: None");
                            layer.UnselectAllTextdraw();
                            fs.Close();
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("TextdrawCreator.cs - TextdrawHUD._:E: Cannot load Textdraw HUD:");
                        Console.WriteLine(e.Message);
                    }
                }
            }

            public void Hide()
            {
                layer.HideAll();
            }

            public void Destroy()
            {
                layer.HideAll();
                layer = null;
            }

            public void SetSelectedTextdrawName(string name)
            {
                layer.SetTextdrawText("tdselected", "TD: " + name);
            }

            public void SetMode(string mode)
            {
                layer.SetTextdrawText("tdmode", "Mode: " + mode);
            }
        }

        const int MAX_LAYERS = 10;
        TextdrawHUD tdHUD;
        List<TextdrawLayer> layers;
        int layerIndex;
        string editingTDName;

        Player player;

        private Boolean isEditing;

        private float moveSpeed;


        public TextdrawCreator(Player _player)
        {
            layers = new List<TextdrawLayer>();
            player = _player;
        }

        public void Init()
        {
            if(tdHUD == null) player.KeyStateChanged += Player_KeyStateChanged; // la condition est juste pour éviter d'ajouter plusieurs fois l'event
            tdHUD = new TextdrawHUD(player);
            layers.Clear();
            layerIndex = 0;
            editingTDName = "";
            isEditing = true;
            moveSpeed = 5.0f;
            player.SendClientMessage("/td to see all commands");
            player.SendClientMessage("Fire key to change edition mode");
            player.SendClientMessage("Y/N to increase/decrease move speed");
        }

        private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            if (editingTDName != "")
            {
                //if(!e.NewKeys.Equals("0")) Console.WriteLine("TextdrawCreator.cs - Player_KeyStateChanged:I: key: " + e.NewKeys.ToString());
                switch (e.NewKeys)
                {
                    case SampSharp.GameMode.Definitions.Keys.Fire:
                        {
                            layers[layerIndex].SwitchTextdrawMode(editingTDName);
                            tdHUD.SetMode(layers[layerIndex].GetEditingMode(editingTDName).ToString());
                            break;
                        }
                    case SampSharp.GameMode.Definitions.Keys.No:
                        {
                            moveSpeed -= 0.5f;
                            player.GameText("Speed: " + moveSpeed.ToString(), 500, 3);
                            break;
                        }
                    case SampSharp.GameMode.Definitions.Keys.Yes:
                        {
                            moveSpeed += 0.5f;
                            player.GameText("Speed: " + moveSpeed.ToString(), 500, 3);
                            break;
                        }
                    case SampSharp.GameMode.Definitions.Keys.AnalogLeft:
                        {
                            if(layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(-1.0 * moveSpeed, 0.0));
                            }
                            else if(layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(-1.0 * moveSpeed, 0.0));
                            }
                            break;
                        }
                    case SampSharp.GameMode.Definitions.Keys.AnalogRight:
                        {
                            if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(1.0 * moveSpeed, 0.0));
                            }
                            else if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(1.0 * moveSpeed, 0.0));
                            }
                            break;
                        }
                    case SampSharp.GameMode.Definitions.Keys.AnalogUp:
                        {
                            if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(0.0, -1.0 * moveSpeed));
                            }
                            else if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(0.0, -1.0 * moveSpeed));
                            }
                            break;
                        }
                    case SampSharp.GameMode.Definitions.Keys.AnalogDown:
                        {
                            if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(0.0, 1.0 * moveSpeed));
                            }
                            else if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(0.0, 1.0 * moveSpeed));
                            }
                            break;
                        }
                }
            }
        }

        public void Close()
        {
            foreach (TextdrawLayer layer in layers)
                layer.HideAll();
            layers.Clear();
            isEditing = false;
            player.ToggleControllable(true);
            player.KeyStateChanged -= Player_KeyStateChanged;
            tdHUD.Destroy();
        }

        public void Update()
        {
            if (isEditing)
            {

            }
        }
        public void Load(string name)
        {
            string filename = BaseMode.Instance.Client.ServerPath + "\\scriptfiles\\" + name + ".json";
            string jsonData = "";
            if (File.Exists(filename))
            {
                player.SendClientMessage("File " + filename + " exists");
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
                        Console.WriteLine("Load: ");
                        Console.WriteLine(jsonData);
                        List<textdraw> textdraws = JsonConvert.DeserializeObject<List<textdraw>>(jsonData);
                        layers.Add(new TextdrawLayer());
                        layerIndex = layers.Count - 1;
                        foreach(textdraw textdraw in textdraws)
                        {
                            if (textdraw.Type.Equals("box"))
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                            if (textdraw.Type.Equals("text"))
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                            layers[layerIndex].SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                            layers[layerIndex].SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                            editingTDName = textdraw.Name;
                        }
                        this.Select(editingTDName);
                        fs.Close();
                    }
                    player.SendClientMessage("File read successfully");
                }
                catch (IOException e)
                {
                    Console.WriteLine("TextdrawCreator.cs - TextdrawCreator.Save:E: Cannot write Textdraw data in file:");
                    Console.WriteLine(e.Message);
                }
            }
            else
                player.SendClientMessage("File " + filename + " does not exists");
        }
        public void Save(string name)
        {
            Textdraw td;
            string output = "";
            List<textdraw> listOfTextdraw = new List<textdraw>();
            textdraw textdraw;
            foreach(KeyValuePair<string, Textdraw> kvp in layers[layerIndex].GetTextdrawList())
            {
                textdraw.Name = kvp.Value.name;
                textdraw.PosX = kvp.Value.Position.X;
                textdraw.PosY = kvp.Value.Position.Y;
                textdraw.Width = kvp.Value.Width;
                textdraw.Height = kvp.Value.Height;
                textdraw.Text = kvp.Value.text;
                textdraw.Color = kvp.Value.ForeColor;
                textdraw.BackColor = kvp.Value.BackColor;
                textdraw.Type = kvp.Value.type;
                listOfTextdraw.Add(textdraw);
            }
            output = JsonConvert.SerializeObject(listOfTextdraw, Formatting.Indented);
            player.SendClientMessage(output);

            string filename = BaseMode.Instance.Client.ServerPath + "\\scriptfiles\\" + name + ".json";
            
            try
            {
                using (FileStream fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] data = new UTF8Encoding(true).GetBytes(output);
                    foreach (byte databyte in data)
                        fs.WriteByte(databyte);
                    fs.FlushAsync();
                    fs.Close();
                }
                player.SendClientMessage("File saved in " + filename);
            }
            catch (IOException e)
            {
                Console.WriteLine("TextdrawCreator.cs - TextdrawCreator.Save:E: Cannot write Textdraw data in file:");
                Console.WriteLine(e.Message);
            }
        }

        public void AddBox(string name)
        {
            if (isEditing)
            {
                if (layers.Count == 0)
                {
                    layerIndex = 0;
                    layers.Add(new TextdrawLayer());
                }
                layers[layerIndex].CreateTextdraw(player, name, TextdrawLayer.TextdrawType.Box);
                editingTDName = name;
                Select(editingTDName);
                tdHUD.SetMode(layers[layerIndex].GetEditingMode(editingTDName).ToString());
            }
        }
        public void AddText(string name)
        {
            if (isEditing)
            {
                if (layers.Count == 0)
                {
                    layerIndex = 0;
                    layers.Add(new TextdrawLayer());
                }
                layers[layerIndex].CreateTextdraw(player, name, TextdrawLayer.TextdrawType.Text);
                editingTDName = name;
                Select(editingTDName);
                tdHUD.SetMode(layers[layerIndex].GetEditingMode(editingTDName).ToString());
            }
        }

        public void Select(string name)
        {
            if(layers.Count > 0)
            {
                if (layers[layerIndex].SelectTextdraw(name))
                    tdHUD.SetSelectedTextdrawName(name);
                else
                    tdHUD.SetSelectedTextdrawName("None");
            }
        }
    }
}
