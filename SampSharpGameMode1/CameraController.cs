using SampSharp.GameMode;
using SampSharp.GameMode.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    public class CameraController
    {
        Player player;
        public CameraController(Player player)
        {
            this.player = player;
            this.player.KeyStateChanged += OnPlayerKeyStateChanged;
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
            player.PutCameraBehindPlayer();
        }

        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            if(player.CameraMode == SampSharp.GameMode.Definitions.CameraMode.Fixed)
            {
                switch (e.NewKeys)
                {
                    case SampSharp.GameMode.Definitions.Keys.AnalogLeft:
                        player.CameraPosition += new Vector3(-10.0, 0.0, 0.0);
                        break;
                    case SampSharp.GameMode.Definitions.Keys.AnalogRight:
                        player.CameraPosition += new Vector3(10.0, 0.0, 0.0);
                        break;
                    case SampSharp.GameMode.Definitions.Keys.AnalogUp:
                        player.CameraPosition += new Vector3(0.0, 10.0, 0.0);
                        break;
                    case SampSharp.GameMode.Definitions.Keys.AnalogDown:
                        player.CameraPosition += new Vector3(0.0, -10.0, 0.0);
                        break;
                }
            }
        }
    }
}
