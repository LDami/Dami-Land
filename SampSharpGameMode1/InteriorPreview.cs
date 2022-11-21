using SampSharp.GameMode;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    class InteriorPreview
    {
        class HUD : Display.HUD
        {
            public event EventHandler<Display.TextdrawLayer.TextdrawEventArgs> Clicked;
            public HUD(Player player) : base(player, "interiorpreview.json")
            {
                layer.SetTextdrawText("interiorid", "Unknown ID");
                layer.SetTextdrawText("interiorname", "Unknown Name");
                layer.UnselectAllTextdraw();
                layer.SetClickable("leftbutton");
                layer.SetClickable("rightbutton");
                layer.TextdrawClicked += Layer_TextdrawClicked;
            }

            private void Layer_TextdrawClicked(object sender, Display.TextdrawLayer.TextdrawEventArgs e)
            {
                Clicked?.Invoke(sender, e);
            }

            public void Destroy()
            {
                layer.HideAll();
            }

            public void SetInterior(int id, string name)
            {
                layer.SetTextdrawText("interiorid", id.ToString());
                layer.SetTextdrawText("interiorname", name);
            }
        }

        Player player;
        HUD hud;
        int selectedIdx = 0;
        public InteriorPreview(Player player)
        {
            this.player = player;
            hud = new HUD(player);
            hud.Clicked += Hud_Clicked;
            player.CancelClickTextDraw += Player_CancelClickTextDraw;
        }

        public void Display()
        {
            hud.Show();
            player.SelectTextDraw(ColorPalette.Primary.Main.GetColor());
        }

        public void Hide()
        {
            hud.Destroy();
        }

        public CustomDatas.InteriorData GetInterior()
        {
            return CustomDatas.InteriorData.Interiors[selectedIdx];
        }

        private void Player_CancelClickTextDraw(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
        {
            Hide();
        }

        private void Hud_Clicked(object sender, Display.TextdrawLayer.TextdrawEventArgs e)
        {
            if (e.TextdrawName == "leftbutton")
            {
                selectedIdx = (selectedIdx == 0) ? 0 : selectedIdx - 1;
            }
            if (e.TextdrawName == "rightbutton")
            {
                selectedIdx = (selectedIdx >= CustomDatas.InteriorData.Interiors.Count - 1) ? selectedIdx : selectedIdx + 1;
            }
            CustomDatas.InteriorData interior = CustomDatas.InteriorData.Interiors[selectedIdx];
            hud.SetInterior(interior.Id, interior.Name);
            player.Interior = interior.Id;
            player.Teleport(interior.Position);
            player.Angle = interior.Rotation;
            player.PutCameraBehindPlayer();
        }
    }
}
