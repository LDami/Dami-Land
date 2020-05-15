using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharpGameMode1.Events.Races;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampSharpGameMode1.Events
{
    public class EventManager
    {
        private static EventManager _instance = null;

        public static EventManager Instance()
        {
            if (_instance == null)
                _instance = new EventManager();
            return _instance;
        }

        private Event nextEvent;
        private Queue<Event> eventList;

        public EventManager()
        {
            nextEvent = null;
        }

        public void ShowManagerDialog(Player player)
        {
            ListDialog managerDialog = new ListDialog("Event manager", "Select", "Cancel");
            managerDialog.AddItem(Color.Green + "Create event");
            foreach (Event evt in eventList)
            {
                managerDialog.AddItem(Color.White + evt.name);
            }

            managerDialog.Show(player);
            managerDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    if(eventArgs.ListItem == 0) // Create event
                    {
                        if (nextEvent == null)
                            ShowCreateEventTypeDialog(player);
                        else
                            player.SendClientMessage("There is already an event to join");
                    }
                    else
                        ShowEventOptionDialog(player, eventList.ElementAt(eventArgs.ListItem + 1));
                }
                else
                {
                    player.Notificate("Cancelled");
                }
            };
        }

        public void ShowCreateEventTypeDialog(Player player)
        {
            ListDialog createEventDialog = new ListDialog("Create an event", "Create", "Cancel");
            foreach (EventType t in (EventType[])Enum.GetValues(typeof(EventType)))
            {
                createEventDialog.AddItem(t.ToString());
            }

            createEventDialog.Show(player);
            createEventDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    ShowCreateEventNameDialog(player, (EventType)Convert.ToInt32(eventArgs.InputText));
                }
                else
                {
                    player.Notificate("Cancelled");
                    ShowManagerDialog(player);
                }
            };
        }

        public void ShowEventOptionDialog(Player player, Event evt)
        {
            ListDialog managerOptionDialog = new ListDialog(evt.name, "Select", "Cancel");
            if(evt.status == EventStatus.Loaded)
                managerOptionDialog.AddItem("Start event");
            managerOptionDialog.AddItem(Color.Red + "Abort event");

            managerOptionDialog.Show(player);
            managerOptionDialog.Response += (sender, eventArgs) =>
            {

            };
        }

        public void ShowCreateEventNameDialog(Player player, EventType eventType)
        {
            switch(eventType)
            {
                case EventType.Race:
                    {
                        InputDialog createEventNameDialog = new InputDialog("Race name", "Type the race name you are looking for, or empty for random", false, "Search", "Cancel");
                        createEventNameDialog.Show(player);
                        createEventNameDialog.Response += (sender, eventArgs) =>
                        {
                            if (eventArgs.DialogButton == DialogButton.Left)
                            {
                                ShowCreateEventSearchDialog(player, (EventType)Convert.ToInt32(eventArgs.InputText), eventArgs.InputText);
                            }
                            else
                            {
                                player.Notificate("Cancelled");
                                ShowCreateEventTypeDialog(player);
                            }
                        };
                        break;
                    }
            }
        }

        public void ShowCreateEventSearchDialog(Player player, EventType eventType, string str)
        {
            Dictionary<string, string> row;
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str }
                };
            GameMode.mySQLConnector.OpenReader("SELECT race_id, race_name FROM races WHERE race_name LIKE @name", param);
            row = GameMode.mySQLConnector.GetNextRow();
            if(row.Count == 0)
            {
                player.Notificate("No race found");
                GameMode.mySQLConnector.CloseReader();
                ShowCreateEventNameDialog(player, eventType);
            }
            else
            {
                List<int> foundRaces = new List<int>();
                ListDialog eventSearchDialog = new ListDialog("Found races", "Launch", "Cancel");
                while (row.Count > 0)
                {
                    foundRaces.Add(Convert.ToInt32(row["race_id"]));
                    eventSearchDialog.AddItem(row["race_id"] + "_" + row["race_name"]);
                    row = GameMode.mySQLConnector.GetNextRow();
                }
                eventSearchDialog.Show(player);
                eventSearchDialog.Response += (sender, eventArgs) =>
                {
                    if (eventArgs.DialogButton == DialogButton.Left)
                    {
                        CreateEvent(player, eventType, foundRaces[eventArgs.ListItem]);
                    }
                    else
                    {
                        player.Notificate("Cancelled");
                        ShowCreateEventTypeDialog(player);
                    }
                };

                GameMode.mySQLConnector.CloseReader();
            }
        }

        public void CreateEvent(Player player, EventType type, int id)
        {
            switch(type)
            {
                case EventType.Race:
                    {
                        RaceEvent race = new RaceEvent(id);
                        player.SendClientMessage(Color.Green, "Loading Race #" + id);
                        race.Loaded += (sender, eventArgs) =>
                        {
                            player.SendClientMessage(Color.Green, "Race #" + eventArgs.RaceID + " loaded with " + eventArgs.CheckpointsCount + " checkpoints");
                        };
                        eventList.Enqueue(race);
                        break;
                    }
            }
        }

    }
}
