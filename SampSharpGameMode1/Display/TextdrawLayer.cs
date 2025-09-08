using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class TextdrawLayer
    {
        public class TextdrawEventArgs : EventArgs
        {
            public string TextdrawName { get; set; }
        }

        public bool AutoUpdate = true;
        public bool IsDisposed = false;

        private Color editingColor = new Color(180, 50, 50);
        private const bool DEBUG_TEXTDRAW_LAYER = false;
        private bool isTextdrawCreator = false;

        Dictionary<string, Textdraw> textdrawList = new Dictionary<string, Textdraw>();
        Dictionary<string, TextdrawType> textdrawType = new Dictionary<string, TextdrawType>();
        Dictionary<string, EditingMode> textdrawEditMode = new Dictionary<string, EditingMode>();
        List<string> textdrawOrder = new List<string>();


        public enum TextdrawType { Background, Box, Text, PreviewModel };
        public enum EditingMode { Unselected, Position, WidthHeight };


        public event EventHandler<TextdrawEventArgs> TextdrawClicked;
        protected virtual void OnTextdrawClicked(TextdrawEventArgs e)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.OnTextdrawClicked:D: Called for {e.TextdrawName}");
            TextdrawClicked?.Invoke(this, e);
        }

        public bool Exists(string name)
        {
            return textdrawList.ContainsKey(name);
        }

        public Dictionary<string, Textdraw> GetTextdrawList()
        {
            return textdrawList;
        }

        public void Dispose()
        {
            IsDisposed = true;
            foreach (Textdraw td in textdrawList.Values)
            {
                if (!td.IsDisposed)
                    td.Dispose();
            }
        }
        public void EnableCreationMode()
        {
            isTextdrawCreator = true;
        }

        public Textdraw CreateBackground(BasePlayer owner, string name, Vector2 position, Vector2 size, Color color)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.CreateBackground:D: Creating background '{name}' at pos {position} with size {size}and color {color}");
            //Console.WriteLine($"Creating background '{name}' at pos {position} with size {size} and color {color}");
            textdrawList.Add(name, new Textdraw(owner, name));

            textdrawList[name].Position = new Vector2(position.X - (size.X/2), position.Y - (size.Y/2));
            textdrawList[name].font = (int)SampSharp.GameMode.Definitions.TextDrawFont.DrawSprite;
            textdrawList[name].Proportional = true;
            textdrawList[name].Width = size.X;
            textdrawList[name].Height = size.Y;
            textdrawList[name].Alignment = SampSharp.GameMode.Definitions.TextDrawAlignment.Center;
            textdrawList[name].text = "LD_DRV:BLKDOT";
            textdrawList[name].type = "background";
            textdrawList[name].ForeColor = color;


            textdrawType[name] = TextdrawType.Background;
            textdrawEditMode[name] = EditingMode.Unselected;
            textdrawOrder.Add(name);
            textdrawList[name].Show();
            return textdrawList[name];
        }
        public void CreateTextdraw(BasePlayer owner, string name, TextdrawType type, string text = "")
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.CreateTextdraw:D: Creating textdraw '{name}' of type {type}");
            if (textdrawList.ContainsKey(name))
            {
                Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.CreateTextdraw:E: The textdraw {name} already exists");
                return;
            }
            textdrawList.Add(name, new Textdraw(owner, name));
            textdrawList[name].Position = new Vector2(320.0f, 240.0f);

            if (type == TextdrawType.Box)
            {
                if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.CreateTextdraw:D: Type = Box");
                textdrawList[name].UseBox = true;
                textdrawList[name].text = "_";
                textdrawList[name].type = "box";
                textdrawList[name].Width = 50;
                textdrawList[name].Height = 40;
            }
            else if (type == TextdrawType.Text)
            {
                if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.CreateTextdraw:D: Type = Text");
                textdrawList[name].text = text;
                textdrawList[name].type = "text";
                textdrawList[name].Width = 500;
                textdrawList[name].Height = 20;
            }
            else if (type == TextdrawType.PreviewModel)
            {
                if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.CreateTextdraw:D: Type = PreviewModel");
                textdrawList[name].font = (int)SampSharp.GameMode.Definitions.TextDrawFont.PreviewModel;
                textdrawList[name].PreviewModel = 1;
                textdrawList[name].type = "previewmodel";
                textdrawList[name].Width = 50;
                textdrawList[name].Height = 50;
            }
            textdrawType[name] = type;

            textdrawEditMode[name] = EditingMode.Unselected;
            textdrawOrder.Add(name);

            textdrawList[name].BackColor = editingColor;
            textdrawList[name].Shadow = 0;
            textdrawList[name].Show();
            return;
        }

        /// <summary>
        /// Recreates the Textdraw in front of the other
        /// </summary>
        /// <param name="owner">BasePlayer to which will be displayed the Textdraw</param>
        /// <param name="name">Name of the textdraw</param>
        /// <exception cref="TextdrawNameNotFoundException"></exception>
        public void PutInFront(BasePlayer owner, string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.PutInFront:D: Called for {name}");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);

            Textdraw tmp = new(owner, "tmp")
            {
                Position = textdrawList[name].Position,
                LetterSize = textdrawList[name].LetterSize,
                Width = textdrawList[name].Width,
                Height = textdrawList[name].Height,
                Text = textdrawList[name].Text,
                Font = textdrawList[name].Font,
                ForeColor = textdrawList[name].ForeColor,
                BackColor = textdrawList[name].BackColor,
                Alignment = textdrawList[name].Alignment,
                UseBox = textdrawList[name].UseBox,
                Shadow = textdrawList[name].Shadow,
                type = textdrawList[name].type,
                Selectable = textdrawList[name].Selectable
            };
            if(tmp.Selectable)
            {
                tmp.Click += (object sender, SampSharp.GameMode.Events.ClickPlayerTextDrawEventArgs e) =>
                {
                    OnTextdrawClicked(new TextdrawEventArgs { TextdrawName = name });
                };
            }

            textdrawList[name].Dispose();
            textdrawList.Remove(name);
            textdrawList.Add(name, tmp);
            textdrawList[name].Show();
        }

        public void Duplicate(BasePlayer owner, string name, string targetName)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.Duplicate:D: Duplicating '{name}' to '{targetName}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);

            textdrawList.Add(targetName, new Textdraw(owner, targetName));
            textdrawList[targetName].Position = textdrawList[name].Position;
            textdrawList[targetName].LetterSize = textdrawList[name].LetterSize;
            textdrawList[targetName].Width = textdrawList[name].Width;
            textdrawList[targetName].Height = textdrawList[name].Height;
            textdrawList[targetName].Text = textdrawList[name].Text;
            textdrawList[targetName].Font = textdrawList[name].Font;
            textdrawList[targetName].ForeColor = textdrawList[name].ForeColor;
            textdrawList[targetName].BackColor = textdrawList[name].BackColor;
            textdrawList[targetName].Alignment = textdrawList[name].Alignment;
            textdrawList[targetName].UseBox = textdrawList[name].UseBox;
            textdrawList[targetName].Shadow = textdrawList[name].Shadow;
            textdrawList[targetName].type = textdrawList[name].type;
            textdrawList[targetName].Show();
            textdrawType[targetName] = textdrawType[name];
            textdrawEditMode.Add(targetName, EditingMode.Unselected);
        }

        public void Delete(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.Delete:D: Deleting '{name}'");
            if (textdrawList.ContainsKey(name))
            {
                textdrawList[name].Dispose();
                textdrawList.Remove(name);
            }
        }

		public void Show(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.Show:D: Showing '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Show();
        }
        public void ShowAll()
        {
            foreach (KeyValuePair<string, Textdraw> td in textdrawList)
                td.Value.Show();
        }
        public void Hide(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.Hide:D: Hiding '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Hide();
        }

        public void HideAll()
        {
            foreach (KeyValuePair<string, Textdraw> td in textdrawList)
                td.Value.Hide();
        }
        public void Destroy()
        {
            foreach (KeyValuePair<string, Textdraw> td in textdrawList)
                td.Value.AutoDestroy = true;
            this.HideAll();
        }
        public Vector2 GetTextdrawSize(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawSize:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return new Vector2(textdrawList[name].Width, textdrawList[name].Height);
        }
        public Boolean SetTextdrawSize(string name, float width, float height)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawSize:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Width = width;
            textdrawList[name].Height = height;
            if(AutoUpdate) this.UpdateTextdraw(name);
            return true;
        }
        public Vector2 GetTextdrawLetterSize(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawLetterSize:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].LetterSize;
        }
        public Boolean SetTextdrawLetterSize(string name, float width, float height)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawLetterSize:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].LetterSize = new Vector2(width, height);
            return true;
        }

        public void Move(string name, Vector2 offset)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.Move:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Position = new Vector2(textdrawList[name].Position.X + offset.X, textdrawList[name].Position.Y + offset.Y);
        }

        public void Resize(string name, Vector2 offset)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.Resize:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Width += offset.X;
            textdrawList[name].Height += offset.Y;
            if(AutoUpdate) this.UpdateTextdraw(name);
        }
        public Vector2 GetTextdrawPosition(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawPosition:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].Position;
        }
        public Boolean SetTextdrawPosition(string name, Vector2 position)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawPosition:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            float newPosX, newPosY;
            newPosX = (position.X >= 0) ? position.X : textdrawList[name].Position.X;
            newPosY = (position.Y >= 0) ? position.Y : textdrawList[name].Position.Y;
            textdrawList[name].Position = new Vector2(newPosX, newPosY);
            return true;
        }

        public void SetTextdrawType(string name, TextdrawType type)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawType:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            if (type == TextdrawType.Background)
                textdrawList[name].type = "background";
            if (type == TextdrawType.Box)
                textdrawList[name].type = "box";
            if (type == TextdrawType.Text)
                textdrawList[name].type = "text";
            if (type == TextdrawType.PreviewModel)
                textdrawList[name].type = "previewmodel";
            if (AutoUpdate) this.UpdateTextdraw(name);
            textdrawType[name] = type;
        }
        public TextdrawType GetTextdrawType(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawType:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawType[name];
        }

        public void SetTextdrawText(string name, string text)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawText:D: Called for '{name}', text = '{text}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            if (text is null)
                throw new ArgumentNullException();
            textdrawList[name].text = text;
            this.ChangeTextdrawMode(name, textdrawEditMode[name]);
            if (AutoUpdate) this.UpdateTextdraw(name);
        }
        public string GetTextdrawText(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawText:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].text;
        }

        public void SetTextdrawFont(string name, int font)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawFont:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            if (Enum.IsDefined(typeof(SampSharp.GameMode.Definitions.TextDrawFont), font))
            {
                textdrawList[name].font = font;
                if (AutoUpdate) this.UpdateTextdraw(name);
            }
        }
        public int GetTextdrawFont(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawFont:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return (int)textdrawList[name].font;
        }

        public void SetTextdrawPreviewModel(string name, int model)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawPreviewModel:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].PreviewModel = model;
            if (AutoUpdate) this.UpdateTextdraw(name);
        }
        public int GetTextdrawPreviewModel(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawPreviewModel:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].PreviewModel;
        }

        public void SetTextdrawAlignment(string name, int alignment)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawAlignment:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            if (Enum.IsDefined(typeof(SampSharp.GameMode.Definitions.TextDrawAlignment), alignment))
            {
                textdrawList[name].Alignment = (SampSharp.GameMode.Definitions.TextDrawAlignment)alignment;
                if (AutoUpdate) this.UpdateTextdraw(name);
            }
        }
        public string GetTextdrawAlignment(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawAlignment:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].Alignment.ToString();
        }

        public void SetTextdrawColor(string name, Color color)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawColor:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].ForeColor = color;
            if (AutoUpdate) this.UpdateTextdraw(name);
        }
        public Color GetTextdrawColor(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawColor:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].ForeColor;
        }

        public void SetTextdrawBackColor(string name, Color color)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawBackColor:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].BackColor = color;
            textdrawList[name].backColor = color;
            if (AutoUpdate) this.UpdateTextdraw(name);
        }
        public Color GetTextdrawBackColor(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawBackColor:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].BackColor;
        }

        public void SetTextdrawBoxColor(string name, Color color)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetTextdrawBoxColor:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].BoxColor = color;
            textdrawList[name].backColor = color;
            if (AutoUpdate) this.UpdateTextdraw(name);
        }
        public Color GetTextdrawBoxColor(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.GetTextdrawBoxColor:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawList[name].BoxColor;
        }

        public void UpdateTextdraw(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.UpdateTextdraw:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Hide();
            textdrawList[name].Show();
        }

        public void SetClickable(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SetClickable:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Selectable = true;
            textdrawList[name].Click += TextdrawLayer_Click;
        }

        public void ResetClickEvent(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.ResetClickEvent:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            textdrawList[name].Selectable = false;
            textdrawList[name].Click -= TextdrawLayer_Click;
        }

        private void TextdrawLayer_Click(object sender, SampSharp.GameMode.Events.ClickPlayerTextDrawEventArgs e)
        {
            Textdraw t = (Textdraw)sender;
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.TextdrawLayer_Click:D: Called for '{t.name}'");
            OnTextdrawClicked(new TextdrawEventArgs { TextdrawName = t.name });
        }

        public bool SelectTextdraw(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);

            foreach (string tdname in textdrawList.Keys)
                this.ChangeTextdrawMode(tdname, EditingMode.Unselected);

            if (textdrawList.ContainsKey(name))
            {
                this.ChangeTextdrawMode(name, EditingMode.Position);
                return true;
            }
            else return false;
        }

        public void SwitchTextdrawMode(string name)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.SwitchTextdrawMode:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            switch (textdrawEditMode[name])
            {
                case EditingMode.Position:
                    {
                        textdrawEditMode[name] = EditingMode.WidthHeight;
                        //textdrawList[name].Text = "Width/Height";
                        break;
                    }
                case EditingMode.WidthHeight:
                    {
                        textdrawEditMode[name] = EditingMode.Position;
                        //textdrawList[name].Text = "Position";
                        break;
                    }
            }
        }

        public void ChangeTextdrawMode(string name, EditingMode mode)
        {
            if (DEBUG_TEXTDRAW_LAYER) Logger.WriteLineAndClose($"TextdrawLayer.cs - TextdrawLayer.ChangeTextdrawMode:D: Called for '{name}'");
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            switch (mode)
            {
                case EditingMode.Unselected:
                    {
                        textdrawList[name].BoxColor = textdrawList[name].backColor;
                        textdrawList[name].Text = textdrawList[name].text;
                        textdrawList[name].Font = (SampSharp.GameMode.Definitions.TextDrawFont)textdrawList[name].font;
                        break;
                    }
                case EditingMode.Position:
                    {
                        textdrawList[name].BoxColor = editingColor;
                        if (textdrawType[name] == TextdrawType.Background)
                            break;
                        //textdrawList[name].Text = "Position";
                        //textdrawList[name].Font = SampSharp.GameMode.Definitions.TextDrawFont.Normal;
                        break;
                    }
                case EditingMode.WidthHeight:
                    {
                        textdrawList[name].BoxColor = editingColor;
                        if (textdrawType[name] == TextdrawType.Background)
                            break;
                        //textdrawList[name].Text = "Width/Height";
                        //textdrawList[name].Font = SampSharp.GameMode.Definitions.TextDrawFont.Normal;
                        break;
                    }
            }
            UpdateTextdraw(name);
            textdrawEditMode[name] = mode;
        }

        public EditingMode GetEditingMode(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException(name);
            return textdrawEditMode[name];
        }

        public void UnselectAllTextdraw()
        {
            foreach (string tdname in textdrawList.Keys)
                ChangeTextdrawMode(tdname, EditingMode.Unselected);
        }
    }
}
