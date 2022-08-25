using Newtonsoft.Json;
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
    public class TextdrawCreator
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
                string filename = Directory.GetCurrentDirectory() + "/scriptfiles/tdcreator.json";
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
                        Logger.WriteLineAndClose("TextdrawCreator.cs - TextdrawHUD._:E: Cannot load Textdraw HUD:");
                        Logger.WriteLineAndClose(e.Message);
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

        public Boolean IsEditing { get { return isEditing; } }
        private Boolean isEditing;

        private float moveSpeed;


        public TextdrawCreator(Player _player)
        {
            layers = new List<TextdrawLayer>();
            player = _player;
        }

        public void Init()
        {
            player.Speedometer.Hide();
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
            ListDialog textdrawDialog = new ListDialog($"Textdraw options [{editingTDName}]", "Select", "Cancel");
            textdrawDialog.AddItem("Color [" + layers[layerIndex].GetTextdrawColor(editingTDName) + layers[layerIndex].GetTextdrawColor(editingTDName).ToString() + Color.White + "]");
            textdrawDialog.AddItem("Back color [" + layers[layerIndex].GetTextdrawBackColor(editingTDName) + layers[layerIndex].GetTextdrawBackColor(editingTDName).ToString() + Color.White + "]");
            textdrawDialog.AddItem("Text [" + layers[layerIndex].GetTextdrawText(editingTDName) + "]");
            textdrawDialog.AddItem("Font [" + layers[layerIndex].GetTextdrawFont(editingTDName) + "]");
            textdrawDialog.AddItem("Alignment [" + layers[layerIndex].GetTextdrawAlignment(editingTDName) + "]");
            textdrawDialog.AddItem("Position [" + layers[layerIndex].GetTextdrawPosition(editingTDName).ToString() + "]");
            textdrawDialog.AddItem("Size [" + layers[layerIndex].GetTextdrawSize(editingTDName).ToString() + "]");
            textdrawDialog.AddItem("Letter Size [" + layers[layerIndex].GetTextdrawLetterSize(editingTDName).ToString() + "]");
            textdrawDialog.AddItem("Preview Model [" + layers[layerIndex].GetTextdrawPreviewModel(editingTDName) + "]");

            textdrawDialog.Show(player);
            textdrawDialog.Response += (object sender, SampSharp.GameMode.Events.DialogResponseEventArgs eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    switch (eventArgs.ListItem)
                    {
                        case 0: // Color
                            InputDialog colorDialog = new InputDialog("Enter color", "Supported formats: 0xFF0000 ; 0xFF0000FF ; rbg(255, 0, 0)", false, "Set", "Cancel");
                            colorDialog.Show(player);
                            colorDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    Color? newColor = Utils.GetColorFromString(input);
                                    if(newColor != null)
                                    {
                                        layers[layerIndex].SetTextdrawColor(editingTDName, newColor ?? Color.AliceBlue);
                                        player.SendClientMessage("Color set to " + newColor + input);
                                    }
                                    else
                                    {
                                        player.SendClientMessage(Color.Red, "Format error");
                                    }
                                }
                                ShowTextdrawDialog();
                            };
                            break;
                        case 1: // Back color
                            player.SendClientMessage(Color.Red, "Not supported yet");
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
                                }
                                ShowTextdrawDialog();
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
                                }
                                ShowTextdrawDialog();
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
                                }
                                ShowTextdrawDialog();
                            };
                            break;
                        case 5: // Position
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
                                        if (double.TryParse(posStr[0], out double posX) && double.TryParse(posStr[1], out double posY))
                                        {
                                            layers[layerIndex].SetTextdrawPosition(editingTDName, new Vector2(posX, posY));
                                            UpdateRuler();
                                        }
                                        else player.SendClientMessage(Color.Red, "Format error");
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                }
                                ShowTextdrawDialog();
                            };
                            break;
                        case 6: // Size
                            InputDialog sizeDialog = new InputDialog("Enter size", $"Current value: {layers[layerIndex].GetTextdrawSize(editingTDName)}\n Format: x;y (example: 100;40)\n0 <= x <= 640 | 0 <= y <= 480", false, "Set", "Cancel");
                            sizeDialog.Show(player);
                            sizeDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        string[] posStr = input.Split(";");
                                        if (float.TryParse(posStr[0], out float posX) && float.TryParse(posStr[1], out float posY))
                                        {
                                            layers[layerIndex].SetTextdrawSize(editingTDName, posX, posY);
                                            UpdateRuler();
                                        }
                                        else player.SendClientMessage(Color.Red, "Format error");
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                }
                                ShowTextdrawDialog();
                            };
                            break;
                        case 7: // Letter Size
                            InputDialog sizeDialog2 = new InputDialog("Enter size", $"Current value: {layers[layerIndex].GetTextdrawLetterSize(editingTDName)}\n Format: x;y (example: 0.3;1.2)\n0 <= x <= 640 | 0 <= y <= 480", false, "Set", "Cancel");
                            sizeDialog2.Show(player);
                            sizeDialog2.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        string[] posStr = input.Split(";");
                                        if (float.TryParse(posStr[0], out float posX) && float.TryParse(posStr[1], out float posY))
                                        {
                                            layers[layerIndex].SetTextdrawLetterSize(editingTDName, posX, posY);
                                            UpdateRuler();
                                        }
                                        else player.SendClientMessage(Color.Red, "Format error");
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                }
                                ShowTextdrawDialog();
                            };
                            break;
                        case 8: // PreviewModel
                            InputDialog modelDialog = new InputDialog("Enter model id", $"Current value: {layers[layerIndex].GetTextdrawLetterSize(editingTDName)}", false, "Set", "Cancel");
                            modelDialog.Show(player);
                            modelDialog.Response += (sender, eventArgs) =>
                            {
                                if (eventArgs.DialogButton == DialogButton.Left)
                                {
                                    string input = eventArgs.InputText;
                                    if (input.Length > 0)
                                    {
                                        if (int.TryParse(input, out int modelid))
                                        {
                                            layers[layerIndex].SetTextdrawPreviewModel(editingTDName, modelid);
                                        }
                                        else player.SendClientMessage(Color.Red, "Format error");
                                    }
                                    else player.SendClientMessage(Color.White, "Not modified");
                                }
                                ShowTextdrawDialog();
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
            tdHUD = null;
            if(player.InAnyVehicle)
                player.Speedometer.Show();
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
            string filename = Directory.GetCurrentDirectory() + "/scriptfiles/" + name + ".json";
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
                            if (textdraw.Type.Equals("background"))
                            {
                                layers[layerIndex].CreateBackground(player, textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY), new Vector2(textdraw.Width, textdraw.Height), textdraw.Color);
                            }
                            if (textdraw.Type.Equals("box"))
                            {
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                                layers[layerIndex].SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layers[layerIndex].SetTextdrawBoxColor(textdraw.Name, textdraw.BackColor);
                            }
                            if (textdraw.Type.Equals("text"))
                            {
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                layers[layerIndex].SetTextdrawText(textdraw.Name, textdraw.Text);
                                layers[layerIndex].SetTextdrawFont(textdraw.Name, textdraw.Font);
                                layers[layerIndex].SetTextdrawAlignment(textdraw.Name, textdraw.Alignment);
                                layers[layerIndex].SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layers[layerIndex].SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
                                if (textdraw.LetterWidth > 0 && textdraw.LetterHeight > 0)
                                    layers[layerIndex].SetTextdrawLetterSize(textdraw.Name, textdraw.LetterWidth, textdraw.LetterHeight);
                            }
                            if (textdraw.Type.Equals("previewmodel"))
                            {
                                layers[layerIndex].CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.PreviewModel);
                                layers[layerIndex].SetTextdrawText(textdraw.Name, textdraw.Text);
                                layers[layerIndex].SetTextdrawFont(textdraw.Name, (int)TextDrawFont.PreviewModel);
                                layers[layerIndex].SetTextdrawPreviewModel(textdraw.Name, textdraw.PreviewModel);
                                layers[layerIndex].SetTextdrawAlignment(textdraw.Name, textdraw.Alignment);
                                layers[layerIndex].SetTextdrawColor(textdraw.Name, textdraw.Color);
                                layers[layerIndex].SetTextdrawBackColor(textdraw.Name, textdraw.BackColor);
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
                    Logger.WriteLineAndClose("TextdrawCreator.cs - TextdrawCreator.Save:E: Cannot read Textdraw data in file:");
                    Logger.WriteLineAndClose(e.Message);
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
                textdraw.LetterWidth = kvp.Value.LetterSize.X;
                textdraw.LetterHeight = kvp.Value.LetterSize.Y;
                textdraw.Text = kvp.Value.text;
                textdraw.Font = kvp.Value.font;
                textdraw.PreviewModel = kvp.Value.PreviewModel;
                textdraw.Alignment = kvp.Value.alignment;
                textdraw.Color = kvp.Value.ForeColor;
                textdraw.BackColor = kvp.Value.BackColor;
                textdraw.Type = kvp.Value.type;
                listOfTextdraw.Add(textdraw);
            }
            output = JsonConvert.SerializeObject(listOfTextdraw, Formatting.Indented);
            player.SendClientMessage(output);

            string filename = Directory.GetCurrentDirectory() + "/scriptfiles/" + name + ".json";
            
            try
            {
                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = File.Open(filename, FileMode.CreateNew, FileAccess.Write))
                {
                    
                    byte[] data = new UTF8Encoding(true).GetBytes(output);
                    foreach (byte databyte in data)
                        fs.WriteByte(databyte);
                    fs.FlushAsync();
                    fs.Close();
                }
                player.SendClientMessage("File saved in " + filename);
                Logger.WriteLineAndClose($"{player.Name} updated {filename}");
            }
            catch (IOException e)
            {
                Logger.WriteLineAndClose("TextdrawCreator.cs - TextdrawCreator.Save:E: Cannot write Textdraw data in file:");
                Logger.WriteLineAndClose(e.Message);
            }
        }

        public void AddBackground(string name)
        {
            if (isEditing)
            {
                CreateLayerIfNotExist();
                if (layers[layerIndex].GetTextdrawList().ContainsKey(name))
                {
                    player.SendClientMessage(Color.Red, "The following textdraw already exists: " + name);
                    return;
                }
                layers[layerIndex].CreateBackground(player, name, new Vector2(320.0f, 240.0f), new Vector2(320.0f, 240.0f), Color.White);
                player.SendClientMessage("Background Textdraw created: " + name);
                Select(name);
            }
        }

        public void AddBox(string name)
        {
            if (isEditing)
            {
                CreateLayerIfNotExist();
                if (layers[layerIndex].GetTextdrawList().ContainsKey(name))
                {
                    player.SendClientMessage(Color.Red, "The following textdraw already exists: " + name);
                    return;
                }
                layers[layerIndex].CreateTextdraw(player, name, TextdrawLayer.TextdrawType.Box);
                player.SendClientMessage("Box Textdraw created: " + name);
                Select(name);
            }
        }
        public void AddText(string name)
        {
            if (isEditing)
            {
                CreateLayerIfNotExist();
                if (layers[layerIndex].GetTextdrawList().ContainsKey(name))
                {
                    player.SendClientMessage(Color.Red, "The following textdraw already exists: " + name);
                    return;
                }
                layers[layerIndex].CreateTextdraw(player, name, TextdrawLayer.TextdrawType.Text);
                player.SendClientMessage("Text Textdraw created: " + name);
                Select(name);
            }
        }

        public void AddPreviewModel(string name)
        {
            if (isEditing)
            {
                CreateLayerIfNotExist();
                if (layers[layerIndex].GetTextdrawList().ContainsKey(name))
                {
                    player.SendClientMessage(Color.Red, "The following textdraw already exists: " + name);
                    return;
                }
                layers[layerIndex].CreateTextdraw(player, name, TextdrawLayer.TextdrawType.PreviewModel);
                player.SendClientMessage("PreviewModel Textdraw created: " + name);
                Select(name);
            }
        }

        private void CreateLayerIfNotExist()
        {
            if (layers.Count == 0)
            {
                layerIndex = 0;
                layers.Add(new TextdrawLayer());
            }
        }

        /// <summary>
        /// Replaces Text textdraw by Box
        /// </summary>
        /// <param name="name">Name of the textdraw to set as Box</param>
        public void SetAsBox(string name)
        {
            if (isEditing)
            {
                layers[layerIndex].SetTextdrawSize(name, 250, 20);
                layers[layerIndex].SetTextdrawFont(name, 0);
                layers[layerIndex].SetTextdrawText(name, "_");
                layers[layerIndex].SetTextdrawType(name, TextdrawLayer.TextdrawType.Box);
            }
        }

        /// <summary>
        /// Replaces Box textdraw by Text
        /// </summary>
        /// <param name="name">Name of the textdraw to set as Text</param>
        public void SetAsText(string name)
        {
            if (isEditing)
            {
                layers[layerIndex].SetTextdrawSize(name, 0, 0);
                layers[layerIndex].SetTextdrawFont(name, 1);
                layers[layerIndex].SetTextdrawType(name, TextdrawLayer.TextdrawType.Text);
            }
        }


        public void Select(string name)
        {
            if (layers.Count > 0)
            {
                try
                {
                    layers[layerIndex].SelectTextdraw(name);
                    tdHUD.SetSelectedTextdrawName(name);
                    tdHUD.SetMode(layers[layerIndex].GetEditingMode(name).ToString());
                    editingTDName = name;
                }
                catch(TextdrawNameNotFoundException)
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
