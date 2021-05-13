﻿using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SampSharpGameMode1.Display
{
    class TextdrawCreator
    {

        class TextdrawHUD
        {
            TextdrawLayer layer;
            bool isSwitched;
            Dictionary<string, Vector2> textdrawDefaultPos = new Dictionary<string, Vector2>();
            Dictionary<string, Vector2> textdrawDefaultSize = new Dictionary<string, Vector2>();

            public TextdrawHUD(Player player)
            {
                isSwitched = false;
                layer = new TextdrawLayer();
                string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\tdcreator.json";
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
                                    layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                    layer.SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
                                }
                                if (textdraw.Type.Equals("text"))
                                {
                                    layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                    layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                    layer.SetTextdrawColor(textdraw.Name, textdraw.Color);
                                    layer.SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
                                }
                                textdrawDefaultPos[textdraw.Name] = new Vector2(textdraw.PosX, textdraw.PosY);
                                textdrawDefaultSize[textdraw.Name] = new Vector2(textdraw.Width, textdraw.Height);
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

            public void Switch()
            {
                if(isSwitched)
                {
                    foreach(KeyValuePair<string, Textdraw> textdraw in layer.GetTextdrawList())
                    {
                        layer.SetTextdrawPosition(textdraw.Key, textdrawDefaultPos[textdraw.Key]);
                        layer.SetTextdrawSize(textdraw.Key, textdrawDefaultSize[textdraw.Key].X, textdrawDefaultSize[textdraw.Key].Y);
                    }
                    isSwitched = false;
                }
                else
                {
                    foreach (KeyValuePair<string, Textdraw> textdraw in layer.GetTextdrawList())
                    {
                        layer.SetTextdrawPosition(textdraw.Key, new Vector2(0, -1));
                        layer.SetTextdrawSize(textdraw.Key, 150.0f, textdrawDefaultSize[textdraw.Key].Y);
                    }
                    isSwitched = true;
                }
            }

            public void SetRuler(Vector2 pos)
            {
                layer.SetTextdrawPosition("x-axis-ruler", new Vector2(pos.X, 0));
                layer.SetTextdrawPosition("y-axis-ruler", new Vector2(0, pos.Y));
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
                    case Keys.Submission:
                        {
                            ShowTextdrawDialog();
                            break;
                        }
                    case Keys.Fire:
                        {
                            layers[layerIndex].SwitchTextdrawMode(editingTDName);
                            tdHUD.SetMode(layers[layerIndex].GetEditingMode(editingTDName).ToString());
                            break;
                        }
                    case Keys.No:
                        {
                            if(moveSpeed > 0.0f)
                            {
                                if (moveSpeed <= 1.0f)
                                    moveSpeed -= 0.1f;
                                else
                                    moveSpeed -= 0.5f;
                            }
                            player.GameText("Speed: " + moveSpeed.ToString(), 500, 3);
                            break;
                        }
                    case Keys.Yes:
                        {
                            if (moveSpeed <= 1.0f)
                                moveSpeed += 0.1f;
                            else
                                moveSpeed += 0.5f;
                            player.GameText("Speed: " + moveSpeed.ToString(), 500, 3);
                            break;
                        }
                    case Keys.AnalogLeft:
                        {
                            if(layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(-1.0 * moveSpeed, 0.0));
                            }
                            else if(layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(-1.0 * moveSpeed, 0.0));
                            }
                            UpdateRuler();
                            break;
                        }
                    case Keys.AnalogRight:
                        {
                            if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(1.0 * moveSpeed, 0.0));
                            }
                            else if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(1.0 * moveSpeed, 0.0));
                            }
                            UpdateRuler();
                            break;
                        }
                    case Keys.AnalogUp:
                        {
                            if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(0.0, -1.0 * moveSpeed));
                            }
                            else if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(0.0, -1.0 * moveSpeed));
                            }
                            UpdateRuler();
                            break;
                        }
                    case Keys.AnalogDown:
                        {
                            if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.Position)
                            {
                                layers[layerIndex].Move(editingTDName, new Vector2(0.0, 1.0 * moveSpeed));
                            }
                            else if (layers[layerIndex].GetEditingMode(editingTDName) == TextdrawLayer.EditingMode.WidthHeight)
                            {
                                layers[layerIndex].Resize(editingTDName, new Vector2(0.0, 1.0 * moveSpeed));
                                if (layers[layerIndex].GetTextdrawType(editingTDName) == TextdrawLayer.TextdrawType.Box)
                                {
                                    layers[layerIndex].SetTextdrawText(editingTDName, layers[layerIndex].GetTextdrawText(editingTDName) + "\n");
                                }
                            }
                            UpdateRuler();
                            break;
                        }
                }
            }
        }

        private void UpdateRuler()
		{
            tdHUD.SetRuler(new Vector2(layers[layerIndex].GetTextdrawPosition(editingTDName).X, layers[layerIndex].GetTextdrawPosition(editingTDName).Y));
        }

        private void ShowTextdrawDialog()
        {
            ListDialog textdrawDialog = new ListDialog("Textdraw options", "Select", "Cancel");
            textdrawDialog.AddItem("Color [" + layers[layerIndex].GetTextdrawColor(editingTDName) + layers[layerIndex].GetTextdrawColor(editingTDName).ToString() + Color.White + "]");
            textdrawDialog.AddItem("Back color [" + layers[layerIndex].GetTextdrawBackColor(editingTDName) + layers[layerIndex].GetTextdrawBackColor(editingTDName).ToString() + Color.White + "]");
            textdrawDialog.AddItem("Text [" + layers[layerIndex].GetTextdrawText(editingTDName) + "]");
            textdrawDialog.AddItem("Font [" + layers[layerIndex].GetTextdrawFont(editingTDName) + "]");
            textdrawDialog.AddItem("Alignment [" + layers[layerIndex].GetTextdrawAlignment(editingTDName) + "]");
            textdrawDialog.AddItem("Position [" + layers[layerIndex].GetTextdrawPosition(editingTDName).ToString() + "]");

            textdrawDialog.Show(player);
            textdrawDialog.Response += (object sender, SampSharp.GameMode.Events.DialogResponseEventArgs eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    switch (eventArgs.ListItem)
                    {
                        case 0: // Color
                            InputDialog colorDialog = new InputDialog("Enter color", "Supported formats: 0xFF0000 ; rbg(255, 0, 0)", false, "Set", "Cancel");
                            colorDialog.Show(player);
                            colorDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        if(input.StartsWith("0x"))
                                        {
                                            if (input.Length == 8)
                                            {
                                                string r, g, b;
                                                r = input.Substring(2, 2);
                                                g = input.Substring(4, 2);
                                                b = input.Substring(6, 2);
                                                Color newColor = new Color(
                                                    int.Parse(r, System.Globalization.NumberStyles.HexNumber),
                                                    int.Parse(g, System.Globalization.NumberStyles.HexNumber),
                                                    int.Parse(b, System.Globalization.NumberStyles.HexNumber)
                                                );
                                                layers[layerIndex].SetTextdrawColor(editingTDName, newColor);
                                                player.SendClientMessage("Color set to " + layers[layerIndex].GetTextdrawColor(editingTDName) + layers[layerIndex].GetTextdrawColor(editingTDName).ToString());
                                            }
                                            else player.SendClientMessage(Color.Red, "Format error");
                                            ShowTextdrawDialog();
                                        }
                                        else if(input.StartsWith("rgb("))
                                        {
                                            Regex regex = new Regex(@"[r][g][b][(](\d{1,3})[,;]\s*(\d{1,3})[,;]\s*(\d{1,3})[)]", RegexOptions.IgnoreCase);
                                            Match match = regex.Match(input);
                                            if(match.Success)
                                            {
                                                int r, g, b;
                                                r = int.Parse(match.Groups[0].Value);
                                                g = int.Parse(match.Groups[1].Value);
                                                b = int.Parse(match.Groups[2].Value);
                                                Color newColor = new Color(r, g, b);
                                                layers[layerIndex].SetTextdrawColor(editingTDName, newColor);
                                                player.SendClientMessage("Color set to " + layers[layerIndex].GetTextdrawColor(editingTDName) + layers[layerIndex].GetTextdrawColor(editingTDName).ToString());
                                            }
                                            else player.SendClientMessage(Color.Red, "Format error");
                                            ShowTextdrawDialog();
                                        }
                                    }
                                }
                            };
                            break;
                        case 1: // Back color
                                break;
                        case 2: // Text
                            InputDialog textDialog = new InputDialog("Enter text", "Max length: 1024 chars", false, "Set", "Cancel");
                            textDialog.Show(player);
                            textDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0 && input.Length < 1024)
                                    {
                                        layers[layerIndex].SetTextdrawText(editingTDName, input);
                                        Update();
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                    ShowTextdrawDialog();
                                }
                            };
                            break;
                        case 3: // Font
                            InputDialog fontDialog = new InputDialog("Enter font id", "Supported font: 0, 1, 2, 3, 4, 5", false, "Set", "Cancel");
                            fontDialog.Show(player);
                            fontDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        if (int.TryParse(input, out int font))
                                        {
                                            if (font >= 0 && font <= 5)
                                            {
                                                layers[layerIndex].SetTextdrawFont(editingTDName, font);
                                                if(font == 4 || font == 5)
												{
                                                    layers[layerIndex].SetTextdrawSize(editingTDName, 100, 100);
												}
                                                Update();
                                            }
                                            else player.SendClientMessage(Color.Red, "The font must be between 0 and 5");
                                            ShowTextdrawDialog();
                                        }
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                    ShowTextdrawDialog();
                                }
                            };
                            break;
                        case 4: // Alignment
                            InputDialog alignmentDialog = new InputDialog("Enter alignment", "Supported alignments: left, centered, right", false, "Set", "Cancel");
                            alignmentDialog.Show(player);
                            alignmentDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        switch(input)
                                        {
                                            case "left":
                                                layers[layerIndex].SetTextdrawAlignment(editingTDName, 1);
                                                break;
                                            case "centered":
                                                layers[layerIndex].SetTextdrawAlignment(editingTDName, 2);
                                                break;
                                            case "right":
                                                layers[layerIndex].SetTextdrawAlignment(editingTDName, 3);
                                                break;
                                            default:
                                                player.SendClientMessage(Color.Red, "Insupported value (left, centered, right)");
                                                ShowTextdrawDialog();
                                                break;
                                        }
                                        Update();
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                    ShowTextdrawDialog();
                                }
                            };
                            break;
                        case 5:
                            InputDialog posDialog = new InputDialog("Enter position", "Format: x;y (example: 105.1;400)\n0 <= x <= 640 | 0 <= y <= 480", false, "Set", "Cancel");
                            posDialog.Show(player);
                            posDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        string[] posStr = input.Split(";");
                                        if(double.TryParse(posStr[0], out double posX) && double.TryParse(posStr[1], out double posY))
                                        {
                                            layers[layerIndex].SetTextdrawPos(editingTDName, new Vector2(posX, posY));
                                            UpdateRuler();
                                        }
                                        else player.SendClientMessage(Color.Red, "Format error");
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                    ShowTextdrawDialog();
                                }
                            };
                            break;
                    }
                }
            };
        }

        public void Close()
        {
            foreach (TextdrawLayer layer in layers)
                layer.Destroy();
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
                string tmpname = editingTDName;
                Unselect();
                Select(tmpname);
            }
        }
        public void Load(string name)
        {
            string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\" + name + ".json";
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
                        List<textdraw> textdraws = JsonConvert.DeserializeObject<List<textdraw>>(jsonData);
                        this.Init();
                        layers.Add(new TextdrawLayer());
                        layerIndex = layers.Count - 1;
                        foreach (textdraw textdraw in textdraws)
                        {
                            if (textdraw.Type.Equals("box"))
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                            if (textdraw.Type.Equals("text"))
                            {
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                layers[layerIndex].SetTextdrawText(textdraw.Name, textdraw.Text);
                                layers[layerIndex].SetTextdrawFont(textdraw.Name, textdraw.Font);
                                layers[layerIndex].SetTextdrawAlignment(textdraw.Name, textdraw.Alignment);
                            }
                            if (!layers[layerIndex].SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY)))
                                player.SendClientMessage(Color.Red, "Cannot set position for '" + textdraw.Name + "'");
                            if (!layers[layerIndex].SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height))
                                player.SendClientMessage(Color.Red, "Cannot set size for '" + textdraw.Name + "'");
                            editingTDName = textdraw.Name;
                        }
                        this.Select(editingTDName);
                        UpdateRuler();
                        fs.Close();
                    }
                    player.SendClientMessage("File read successfully");
                }
                catch (IOException e)
                {
                    Console.WriteLine("TextdrawCreator.cs - TextdrawCreator.Save:E: Cannot read Textdraw data in file:");
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
                textdraw.Font = kvp.Value.font;
                textdraw.Alignment = kvp.Value.alignment;
                textdraw.Color = kvp.Value.ForeColor;
                textdraw.BackColor = kvp.Value.BackColor;
                textdraw.Type = kvp.Value.type;
                listOfTextdraw.Add(textdraw);
            }
            output = JsonConvert.SerializeObject(listOfTextdraw, Formatting.Indented);
            player.SendClientMessage(output);

            string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\" + name + ".json";
            
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
            if (layers.Count > 0)
            {
                if (layers[layerIndex].SelectTextdraw(name))
                {
                    tdHUD.SetSelectedTextdrawName(name);
                    editingTDName = name;
                }
                else
                {
                    tdHUD.SetSelectedTextdrawName("None");
                    player.SendClientMessage(Color.Red, "The following textdraw does not exists: " + name);
                }
            }
            else
                player.SendClientMessage(Color.Red, "There is no layer to select");
        }
        public void Unselect()
        {
            if (layers.Count > 0)
            {
                layers[layerIndex].UnselectAllTextdraw();
            }
        }

        public void HUDSwitch()
        {
            tdHUD.Switch();
        }
    }
}