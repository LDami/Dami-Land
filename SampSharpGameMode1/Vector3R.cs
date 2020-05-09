using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    public struct Vector3R
    {
        public Vector3 Position;
        public float Rotation;

        public Vector3R(Vector3 _position)
        {
            this.Position = _position;
            this.Rotation = 0.0f;
        }
        public Vector3R(Vector3 _position, float _rotation)
        {
            this.Position = _position;
            this.Rotation = _rotation;
        }
    }
}
