using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    class TextdrawLayer
    {
        private Color editingColor = new Color(180, 50, 50);

        string name = "";
        Dictionary<string, Textdraw> textdrawList = new Dictionary<string, Textdraw>();
        Dictionary<string, TextdrawType> textdrawType = new Dictionary<string, TextdrawType>();
        Dictionary<string, EditingMode> textdrawEditMode = new Dictionary<string, EditingMode>();
        List<string> textdrawOrder = new List<string>();


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

        public void HideAll()
        {
            foreach (string tdname in textdrawList.Keys)
                textdrawList[tdname].Hide();
        }

        public Boolean SetTextdrawPosition(string name, Vector2 position)
        {
            try
            {
                textdrawList[name].Position = new Vector2(position.X, position.Y);
                return true;
            }
            catch(KeyNotFoundException e)
            {
                Console.WriteLine("TextdrawLayer.cs - TextdrawLayer.SetTextdrawPosition:E: " + e.Message);
                return false;
            }
        }
        public Boolean SetTextdrawSize(string name, float width, float height)
        {
            try
            {
                textdrawList[name].Width = width;
                textdrawList[name].Height = height;
                return true;
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine("TextdrawLayer.cs - TextdrawLayer.SetTextdrawSize:E: " + e.Message);
                return false;
            }
        }
        public void Move(string name, Vector2 offset)
        {
            textdrawList[name].Position = new Vector2(textdrawList[name].Position.X + offset.X, textdrawList[name].Position.Y + offset.Y);
        }

        public void Resize(string name, Vector2 offset)
        {
            textdrawList[name].Width += offset.X;
            textdrawList[name].Height += offset.Y;
        }

        public void SetTextdrawText(string name, string text)
        {
            textdrawList[name].text = text;
            this.ChangeTextdrawMode(name, textdrawEditMode[name]);
        }

        public void SetTextdrawColor(string name, Color color)
        {
            textdrawList[name].Color = color;
        }
        public Color GetTextdrawColor(string name)
        {
            return textdrawList[name].Color;
        }

        public void SetTextdrawBackColor(string name, Color color)
        {
            textdrawList[name].BackColor = color;
        }
        public Color GetTextdrawBackColor(string name)
        {
            return textdrawList[name].BackColor;
        }

        public bool SelectTextdraw(string name)
        {
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
            switch (textdrawEditMode[name])
            {
                case EditingMode.Position:
                    {
                        textdrawEditMode[name] = EditingMode.WidthHeight;
                        textdrawList[name].Text = "Width/Height";
                        break;
                    }
                case EditingMode.WidthHeight:
                    {
                        textdrawEditMode[name] = EditingMode.Position;
                        textdrawList[name].Text = "Position";
                        break;
                    }
            }
        }

        public void ChangeTextdrawMode(string name, EditingMode mode)
        {
            switch (mode)
            {
                case EditingMode.Unselected:
                    {
                        textdrawList[name].BackColor = textdrawList[name].backColor;
                        textdrawList[name].Text = textdrawList[name].text;
                        break;
                    }
                case EditingMode.Position:
                    {
                        textdrawList[name].BackColor = editingColor;
                        textdrawList[name].Text = "Position";
                        break;
                    }
                case EditingMode.WidthHeight:
                    {
                        textdrawList[name].BackColor = editingColor;
                        textdrawList[name].Text = "Width/Height";
                        break;
                    }
            }
            textdrawEditMode[name] = mode;
        }

        public EditingMode GetEditingMode(string name)
        {
            return textdrawEditMode[name];
        }

        public void UnselectAllTextdraw()
        {
            foreach (string tdname in textdrawList.Keys)
                ChangeTextdrawMode(tdname, EditingMode.Unselected);
        }
    }
}
