using SampSharp.GameMode;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    struct textdraw
    {
        public string Name;
        public float PosX;
        public float PosY;
        public float Width;
        public float Height;
        public string Text;
        public Color Color;
        public Color BackColor;
        public string Type;
    }
    class Textdraw : PlayerTextDraw
    {
        public string name;
        public Vector2 position = Vector2.Zero;
        public float width = 0.0f;
        public float height = 0.0f;
        public string text = "";
        public Color Color = Color.SkyBlue;
        public Color backColor = Color.SkyBlue;
        public string type; // Utilisé uniquement pour la sauvegarde
        public Textdraw(BasePlayer owner, string name) : base(owner)
        {
            this.name = name;
        }
    }
}
