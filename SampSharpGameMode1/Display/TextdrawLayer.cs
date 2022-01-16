using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    class TextdrawLayer
    {
        private Color editingColor = new Color(180, 50, 50);

        string name = "";
        Dictionary<string, Textdraw> textdrawList = new Dictionary<string, Textdraw>();
        Dictionary<string, TextdrawType> textdrawType = new Dictionary<string, TextdrawType>();
        Dictionary<string, EditingMode> textdrawEditMode = new Dictionary<string, EditingMode>();
        List<string> textdrawOrder = new List<string>();
        private Action onClickAction;


        public enum TextdrawType { Box, Text };
        public enum EditingMode { Unselected, Position, WidthHeight };

        public Dictionary<string, Textdraw> GetTextdrawList()
        {
            return textdrawList;
        }

        public Textdraw CreateTextdraw(BasePlayer owner, string name, TextdrawType type)
        {
            textdrawList.Add(name, new Textdraw(owner, name));
            textdrawList[name].Position = new Vector2(320.0f, 240.0f);

            if (type == TextdrawType.Box)
            {
                textdrawList[name].UseBox = true;
                textdrawList[name].Width = 50;
                textdrawList[name].Height = 20;
                textdrawList[name].text = "_";
                textdrawList[name].type = "box";
                textdrawType[name] = TextdrawType.Box;
            }
            else if (type == TextdrawType.Text)
            {
                textdrawList[name].text = name;
                textdrawList[name].type = "text";
                textdrawType[name] = TextdrawType.Text;
            }

            textdrawEditMode[name] = EditingMode.Position;
            textdrawOrder.Add(name);

            textdrawList[name].Text = "Position";
            textdrawList[name].BackColor = editingColor;
            textdrawList[name].Shadow = 0;
            textdrawList[name].Show();
            return textdrawList[name];
        }

		public void Show(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Show();
        }
        public void ShowAll()
        {
            foreach (KeyValuePair<string, Textdraw> td in textdrawList)
                td.Value.Show();
        }
        public void Hide(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
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

        public Boolean SetTextdrawPosition(string name, Vector2 position)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            float newPosX, newPosY;
            newPosX = (position.X >= 0) ? position.X : textdrawList[name].Position.X;
            newPosY = (position.Y >= 0) ? position.Y : textdrawList[name].Position.Y;
            textdrawList[name].Position = new Vector2(newPosX, newPosY);
            return true;
        }
        public Boolean SetTextdrawSize(string name, float width, float height)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Width = width;
            textdrawList[name].Height = height;
            return true;
        }

        public void Move(string name, Vector2 offset)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Position = new Vector2(textdrawList[name].Position.X + offset.X, textdrawList[name].Position.Y + offset.Y);
        }

        public void Resize(string name, Vector2 offset)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Width += offset.X;
            textdrawList[name].Height += offset.Y;
        }
        public Vector2 GetTextdrawPosition(string name)
        {
            if (!textdrawType.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawList[name].Position;
        }
        public void SetTextdrawPos(string name, Vector2 pos)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Position = pos;
        }

        public TextdrawType GetTextdrawType(string name)
        {
            if (!textdrawType.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawType[name];
        }

        public void SetTextdrawText(string name, string text)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            if (text is null)
                throw new ArgumentNullException();
            textdrawList[name].text = text;
            this.ChangeTextdrawMode(name, textdrawEditMode[name]);
            this.UpdateTextdraw(name);
        }
        public string GetTextdrawText(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawList[name].text;
        }

        public void SetTextdrawFont(string name, int font)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            if (Enum.IsDefined(typeof(SampSharp.GameMode.Definitions.TextDrawFont), font))
            {
                textdrawList[name].font = font;
                this.UpdateTextdraw(name);
            }
        }
        public int GetTextdrawFont(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return (int)textdrawList[name].font;
        }

        public void SetTextdrawAlignment(string name, int alignment)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            if (Enum.IsDefined(typeof(SampSharp.GameMode.Definitions.TextDrawAlignment), alignment))
            {
                textdrawList[name].Alignment = (SampSharp.GameMode.Definitions.TextDrawAlignment)alignment;
                this.UpdateTextdraw(name);
            }
        }
        public string GetTextdrawAlignment(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawList[name].Alignment.ToString();
        }

        public void SetTextdrawColor(string name, Color color)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Color = color;
            this.UpdateTextdraw(name);
        }
        public Color GetTextdrawColor(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawList[name].Color;
        }

        public void SetTextdrawBackColor(string name, Color color)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].BackColor = color;
            this.UpdateTextdraw(name);
        }
        public Color GetTextdrawBackColor(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawList[name].BackColor;
        }

        public void UpdateTextdraw(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            textdrawList[name].Hide();
            textdrawList[name].Show();
        }

        public void SetOnClickCallback(string name, Action callback)
        {
            textdrawList[name].Selectable = true;
            textdrawList[name].Click += (object sender, SampSharp.GameMode.Events.ClickPlayerTextDrawEventArgs e) =>
            {
                callback();
            };
		}

        public bool SelectTextdraw(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();

            foreach (string tdname in textdrawList.Keys)
                this.ChangeTextdrawMode(tdname, EditingMode.Unselected);

            if (textdrawList.ContainsKey(name))
            {
                Console.WriteLine("Setting mode Position for td " + name);
                this.ChangeTextdrawMode(name, EditingMode.Position);
                return true;
            }
            else return false;
        }

        public void SwitchTextdrawMode(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
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
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            switch (mode)
            {
                case EditingMode.Unselected:
                    {
                        textdrawList[name].BackColor = textdrawList[name].backColor;
                        textdrawList[name].Text = textdrawList[name].text;
                        textdrawList[name].Font = (SampSharp.GameMode.Definitions.TextDrawFont)textdrawList[name].font;
                        break;
                    }
                case EditingMode.Position:
                    {
                        textdrawList[name].BackColor = editingColor;
                        //textdrawList[name].Text = "Position";
                        //textdrawList[name].Font = SampSharp.GameMode.Definitions.TextDrawFont.Normal;
                        break;
                    }
                case EditingMode.WidthHeight:
                    {
                        textdrawList[name].BackColor = editingColor;
                        //textdrawList[name].Text = "Width/Height";
                        //textdrawList[name].Font = SampSharp.GameMode.Definitions.TextDrawFont.Normal;
                        break;
                    }
            }
            textdrawEditMode[name] = mode;
        }

        public EditingMode GetEditingMode(string name)
        {
            if (!textdrawList.ContainsKey(name))
                throw new TextdrawNameNotFoundException();
            return textdrawEditMode[name];
        }

        public void UnselectAllTextdraw()
        {
            foreach (string tdname in textdrawList.Keys)
                ChangeTextdrawMode(tdname, EditingMode.Unselected);
        }
    }
}
