using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    struct textdraw
    {
        public string Name;
        public float PosX;
        public float PosY;
        public float Width;
        public float Height;
        public string Text;
        public int Font;
        public int Alignment;
        public Color Color;
        public Color BackColor;
        public string Type;
    }
    public class Textdraw : PlayerTextDraw
    {
        public string name;
        public Vector2 position = Vector2.Zero;
        public float width = 0.0f;
        public float height = 0.0f;
        public string text = "";
        public int font = 1;
        public int alignment;
        public Color backColor = Color.SkyBlue;
        public string type; // Utilisé uniquement pour la sauvegarde
        public Textdraw(BasePlayer owner, string name) : base(owner)
        {
            this.name = name;
        }
    }
}
