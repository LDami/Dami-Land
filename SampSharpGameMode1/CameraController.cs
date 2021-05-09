using SampSharp.GameMode;
using SampSharp.GameMode.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    public class CameraController
    {
        public Player player;
        public bool LockTarget { get; set; }
        public Vector3 LockedTarget { get; set; }
        private Vector3 lastLookAtPos;
        public CameraController(Player player)
        {
            this.player = player;
            this.player.KeyStateChanged += OnPlayerKeyStateChanged;
            this.LockTarget = false;
            this.LockedTarget = Vector3.Zero;
            this.lastLookAtPos = Vector3.Zero;
        }

        public void Dispose()
        {
            if(this.player != null)
            {
                this.player.KeyStateChanged -= OnPlayerKeyStateChanged;
                this.player = null;
            }
        }

        public void SetFree()
        {
            player.ToggleSpectating(true);
            System.Threading.Thread.Sleep(100);
            player.CameraPosition = player.Position + new Vector3(0.0, 0.0, 5.0);
        }

        public void SetBehindPlayer()
        {
            player.ToggleSpectating(false);
            player.PutCameraBehindPlayer();
        }

        public void SetPosition(Vector3 position)
		{
            player.CameraPosition = position;
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.SetPosition:I: New position = " + position.ToString());
        }

        public void MoveTo(Vector3 position)
        {
            player.InterpolateCameraPosition(player.CameraPosition, position, 200, SampSharp.GameMode.Definitions.CameraCut.Move);
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.MoveTo:I: Moving to = " + position.ToString());
        }

        public void SetTarget(Vector3 target, bool locked = false)
		{
            player.SetCameraLookAt(target, SampSharp.GameMode.Definitions.CameraCut.Cut);
            lastLookAtPos = target;
            if (locked)
            {
                LockTarget = true;
                LockedTarget = target;
            }
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.SetTarget:I: Look at position = " + target.ToString());
        }

        public void MoveToTarget(Vector3 target)
        {
            player.InterpolateCameraLookAt(lastLookAtPos, target, 200, SampSharp.GameMode.Definitions.CameraCut.Move);
            lastLookAtPos = target;
        }

        private double GetAngle(Vector3 target, double distance)
        {
            Vector3 cameraPos = player.CameraPosition;
            double xDiff = Math.Abs(cameraPos.X - target.X);
            double yDiff = Math.Abs(cameraPos.Y - target.Y);
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.GetSinCos:I: xDiff = " + xDiff.ToString());
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.GetSinCos:I: yDiff = " + yDiff.ToString());
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.GetSinCos:I: dist = " + distance.ToString());
            Logger.WriteLineAndClose($"CameraController.cs - CameraController.GetSinCos:I: Math.Acos(xDiff / dist) = " + Math.Acos(xDiff / distance));
            return Math.Acos(xDiff / distance);
        }

        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            if(player.CameraMode == SampSharp.GameMode.Definitions.CameraMode.Fixed)
            {
                Vector3 cameraPos = player.CameraPosition;
                double angle;
                double dist = cameraPos.DistanceTo(LockedTarget);
                switch (e.NewKeys)
                {
                    case SampSharp.GameMode.Definitions.Keys.AnalogLeft:
                        if(LockTarget)
                        {
                            angle = GetAngle(LockedTarget, dist);
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: LockedTarget = " + LockedTarget.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: dist = " + dist.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: cameraPos = " + cameraPos.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: LockedTarget = " + LockedTarget.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: previous angle = " + angle.ToString());
                            angle -= Math.PI/4;
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: next angle = " + angle.ToString());
                            double cos = Math.Cos(angle);
                            double sin = Math.Sin(angle);
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: cos = " + cos.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: sin = " + sin.ToString());
                            MoveTo(new Vector3(
                                (cos * dist) + LockedTarget.X,
                                (sin * dist) + LockedTarget.Y,
                                cameraPos.Z
                            ));
                            MoveToTarget(LockedTarget);
                        }
                        else
                            MoveTo(cameraPos + new Vector3(10.0, 0.0, 0.0));
                        break;
                    case SampSharp.GameMode.Definitions.Keys.AnalogRight:
                        if (LockTarget)
                        {
                            angle = GetAngle(LockedTarget, dist);
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: LockedTarget = " + LockedTarget.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: dist = " + dist.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: cameraPos = " + cameraPos.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: LockedTarget = " + LockedTarget.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: previous angle = " + angle.ToString());
                            angle += Math.PI / 4;
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: next angle = " + angle.ToString());
                            double cos = Math.Cos(angle);
                            double sin = Math.Sin(angle);
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: cos = " + cos.ToString());
                            Logger.WriteLineAndClose($"CameraController.cs - CameraController.OnPlayerKeyStateChanged:I: sin = " + sin.ToString());
                            MoveTo(new Vector3(
                                (cos * dist) + LockedTarget.X,
                                (sin * dist) + LockedTarget.Y,
                                cameraPos.Z
                            ));
                            MoveToTarget(LockedTarget);
                        }
                        else
                            MoveTo(player.CameraPosition + new Vector3(-10.0, 0.0, 0.0));
                        break;
                    case SampSharp.GameMode.Definitions.Keys.AnalogUp:
                        MoveTo(player.CameraPosition + new Vector3(0.0, 10.0, 0.0));
                        break;
                    case SampSharp.GameMode.Definitions.Keys.AnalogDown:
                        MoveTo(player.CameraPosition + new Vector3(0.0, -10.0, 0.0));
                        break;
                }
            }
        }
    }
}
