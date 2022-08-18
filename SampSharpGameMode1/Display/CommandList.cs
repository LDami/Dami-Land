using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public  class CommandList
    {
        private string title;
        private Dictionary<string, string> commands = new Dictionary<string, string>();
        public CommandList(string title)
        {
            commands.Clear();
            this.title = title; 
        }

        public void Add(string command, string description)
        {
            if(commands.ContainsKey(command))
                commands[command] = description;
            else
                commands.Add(command, description);
        }

        public void Show(BasePlayer player)
        {
            string list = "";
            int index = 0;
            foreach(KeyValuePair<string, string> kvp in commands)
            {
                int nbrOfTabs = kvp.Key.Length <= 16 ? 4 : 3;
                string tabs = string.Concat(Enumerable.Repeat("\t", nbrOfTabs));
                list += 
                    Color.Lerp(ColorPalette.Primary.Main.GetColor(), Color.Black, (index % 2 == 0) ? 0 : 0.25f) + 
                    kvp.Key + tabs +
                    Color.Lerp(ColorPalette.Secondary.Main.GetColor(), Color.Black, (index % 2 == 0) ? 0 : 0.25f) +
                    kvp.Value + "\n";
                index++;
            }
            MessageDialog dialog = new MessageDialog(title, list, "Close");
            dialog.Show(player);
        }
    }
}
