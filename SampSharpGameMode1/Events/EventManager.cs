using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharpGameMode1.Events.Races;
using SampSharpGameMode1.Events.Derbys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events.Missions;

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

        public Event openedEvent;
        private List<Event> eventList;
        private List<int> usedIds;

        public EventManager()
        {
            openedEvent = null;
            eventList = new List<Event>();
            usedIds = new List<int>();
        }

        public void PurgeEvents(Player player)
		{
            MessageDialog confirmation = new MessageDialog("Confirmation", "Are you sure you want to delete upcoming events ?", "Yes", "No/Cancel");
			confirmation.Response += (object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e) =>
            {
                if(e.DialogButton == DialogButton.Left)
                {
                    foreach(Event evt in eventList.Where(ev => ev.Status == EventStatus.Loaded))
                    {
                        usedIds.RemoveAll(x => x == evt.Id);
                    }
                    int nbr = eventList.RemoveAll(ev => ev.Status == EventStatus.Loaded);
                    player.Notificate(nbr + " event(s) removed");
                }
            };
            confirmation.Show(player);
		}

		public void ShowManagerDialog(Player player)
        {
            ListDialog managerDialog = new ListDialog("Event manager", "Select", "Cancel");
            managerDialog.AddItem(Color.Green + "Create event");
            if (player.Adminlevel > 0)
            {
                foreach (Event evt in eventList)
                {
                    managerDialog.AddItem(Color.White + "[" + evt.Status.ToString() + "]" + evt.Name);
                }
            }
            else
                managerDialog.AddItem(Color.Red + "Only admins can manage already opened events");

            managerDialog.Show(player);
            managerDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    if(eventArgs.ListItem == 0) // Create event
                    {
                        if(openedEvent == null)
                            ShowCreateEventTypeDialog(player);
                        else
                            player.SendClientMessage(ColorPalette.Error.Main + "There is already an opened event");
                    }
                    else
                        if (player.Adminlevel > 0) ShowEventOptionDialog(player, eventList.ElementAt(eventArgs.ListItem - 1));
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
                if(t.ToString() != "Unknown")
                {
                    createEventDialog.AddItem(t.ToString());
                }
            }

            createEventDialog.Show(player);
            createEventDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    EventType evtType;
                    if (Enum.TryParse(eventArgs.InputText, out evtType))
                        ShowCreateEventNameDialog(player, evtType);
                    else
                    {
                        ShowCreateEventTypeDialog(player);
                        player.Notificate("Unable to parse event type: " + eventArgs.InputText);
                    }
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
            ListDialog managerOptionDialog = new ListDialog(evt.Name, "Select", "Cancel");
            if (evt.Status == EventStatus.Loaded)
                managerOptionDialog.AddItem("Open event to players");
            else if (evt.Status == EventStatus.Waiting)
                managerOptionDialog.AddItem("Start event");
            managerOptionDialog.AddItem(Color.Yellow + "Restart event");
            managerOptionDialog.AddItem(Color.Red + "Abort event");

            managerOptionDialog.Show(player);
            managerOptionDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    if(evt.Status == EventStatus.Loaded || evt.Status == EventStatus.Waiting)
                    {
                        if (eventArgs.ListItem == 0) // Open / Start
                        {
                            if (evt.Status == EventStatus.Loaded)
                            {
                                if(openedEvent == null)
                                {
                                    evt.Open();
                                    player.Notificate("Event opened");
                                }
                                else
								{
                                    player.SendClientMessage(Color.Red + "You cannot open this event because there is already an event in Waiting status");
								}
                            }
                            else if (evt.Status == EventStatus.Waiting)
                            {
                                if (evt.Start(evt.Slots)) player.Notificate("Event started");
                                else player.SendClientMessage(Color.Red + "The event cannot be started (there are maybe no player)");
                            }
                        }
                        else if (eventArgs.ListItem == 1) // Restart
                        {
                            evt.End(EventFinishedReason.Terminated);
                            evt.Start(evt.Slots);
                            player.Notificate("Event restarted");
                        }
                        else if (eventArgs.ListItem == 2) // Abort
                        {
                            eventList.Remove(evt);
                            if (openedEvent == evt)
                                openedEvent = null;
                            evt.End(EventFinishedReason.Aborted);
                            player.Notificate("Event cancelled");
                        }
                    }
                    else
                    {
                        if (eventArgs.ListItem == 0) // Abort
                        {
                            eventList.Remove(evt);
                            if (openedEvent == evt)
                                openedEvent = null;
                            evt.End(EventFinishedReason.Aborted);
                            player.Notificate("Event cancelled");
                        }
                    }
                }
            };
        }

        public void ShowCreateEventNameDialog(Player player, EventType eventType)
        {
            InputDialog createEventNameDialog = new InputDialog(eventType.ToString() + " name", "Type the " + eventType.ToString() + " name you are looking for, or empty for all. You can also search by creator name", false, "Search", "Cancel");
            createEventNameDialog.Show(player);
            createEventNameDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    ShowCreateEventSearchDialog(player, eventType, eventArgs.InputText);
                }
                else
                {
                    player.Notificate("Cancelled");
                    ShowCreateEventTypeDialog(player);
                }
            };
        }

        public void ShowCreateEventSearchDialog(Player player, EventType eventType, string str)
        {
            string query = "";
            string key_id = "", key_name = "", key_creator = "";
            switch(eventType)
			{
                case EventType.Race:
					{
                        query = "SELECT race_id, race_name, race_creator FROM races WHERE race_name LIKE @name OR race_creator LIKE @name";
                        key_id = "race_id";
                        key_name = "race_name";
                        key_creator = "race_creator";
                        break;
                    }
                case EventType.Derby:
                    {
                        query = "SELECT derby_id, derby_name, derby_creator FROM derbys WHERE derby_name LIKE @name OR derby_creator LIKE @name";
                        key_id = "derby_id";
                        key_name = "derby_name";
                        key_creator = "derby_creator";
                        break;
                    }
                case EventType.Mission:
                    {
                        query = "SELECT derby_id, derby_name, derby_creator FROM derbys WHERE derby_name LIKE @name OR derby_creator LIKE @name";
                        key_id = "derby_id";
                        key_name = "derby_name";
                        key_creator = "derby_creator";
                        break;
                    }
            }
            Dictionary<string, string> row;
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str }
                };
            GameMode.MySQLConnector.OpenReader(query, param);
            row = GameMode.MySQLConnector.GetNextRow();
            if(row.Count == 0)
            {
                player.Notificate("No results");
                GameMode.MySQLConnector.CloseReader();
                ShowCreateEventNameDialog(player, eventType);
            }
            else
            {
                // Check if someone is editing a race or a derby
                List<int> editingEvents = new List<int>();
                foreach(Player p in Player.All)
                {
                    if(p.eventCreator != null)
                    {
                        if(p.eventCreator.EventId > -1)
                        {
                            editingEvents.Add(p.eventCreator.EventId);
                        }
                    }
                }

                List<int> foundEvents = new List<int>();
                ListDialog eventSearchDialog = new ListDialog("Found " + eventType.ToString() + "s", "Launch", "Cancel");
                while (row.Count > 0)
                {
                    if(!editingEvents.Contains(Convert.ToInt32(row[key_id])))
                    {
                        foundEvents.Add(Convert.ToInt32(row[key_id]));
                        eventSearchDialog.AddItem(row[key_id] + "_" + ColorPalette.Primary.Main + row[key_name] + ColorPalette.Primary.Lighten + " by " + ColorPalette.Primary.Main + row[key_creator]);
                    }
                    row = GameMode.MySQLConnector.GetNextRow();
                }
                GameMode.MySQLConnector.CloseReader();
                if(foundEvents.Count == 0)
                {
                    player.Notificate("No results");
                    ShowCreateEventNameDialog(player, eventType);
                }
                eventSearchDialog.Show(player);
                eventSearchDialog.Response += (sender, eventArgs) =>
                {
                    if (eventArgs.DialogButton == DialogButton.Left)
                    {
                        if (eventType == EventType.Mission)
                            CreateEvent(player, eventType, 1);
                        else
                            CreateEvent(player, eventType, foundEvents[eventArgs.ListItem]);
                    }
                    else
                    {
                        player.Notificate("Cancelled");
                        ShowCreateEventTypeDialog(player);
                    }
                };
            }
        }

        public void CreateEvent(Player player, EventType type, int id)
        {
            int eventId = CheckForAvailableId(usedIds);
            switch (type)
            {
                case EventType.Race:
                    {
                        Event newEvent = new RaceEvent(eventId);
                        newEvent.Loaded += (sender, eventArgs) =>
                        {
                            if (eventArgs.ErrorMessage == null)
                            {
                                if (player.IsConnected) player.SendClientMessage(ColorPalette.Primary.Main + $"Race {eventArgs.EventLoaded.Id} {Color.Green}loaded !");
                                eventList.Add(eventArgs.EventLoaded);
                                if (openedEvent == null)
                                {
                                    openedEvent = eventArgs.EventLoaded;
                                    eventArgs.EventLoaded.Open();
                                    eventArgs.EventLoaded.Started += (sender, eventArgs) => { openedEvent = null; };
                                    eventArgs.EventLoaded.Ended += (sender, eventArgs) => { eventList.Remove((Event)sender); };
                                }
                            }
                            else
                                if (player.IsConnected) player.SendClientMessage(Color.Red, "Cannot load the race: " + eventArgs.ErrorMessage);
                        };
                        player.SendClientMessage(ColorPalette.Primary.Main + $"Loading Race #{eventId}");
                        newEvent.Load(id);
                        usedIds.Add(eventId);
                        break;
                    }
                case EventType.Derby:
                    {
                        Event newEvent = new DerbyEvent(eventId);
                        newEvent.Loaded += (sender, eventArgs) =>
                        {
                            if (eventArgs.ErrorMessage == null)
                            {
                                if (player.IsConnected) player.SendClientMessage(ColorPalette.Primary.Main + $"Derby {eventArgs.EventLoaded.Id} {Color.Green}loaded !");
                                eventList.Add(eventArgs.EventLoaded);
                                if (openedEvent == null)
                                {
                                    openedEvent = eventArgs.EventLoaded;
                                    eventArgs.EventLoaded.Open();
                                    eventArgs.EventLoaded.Started += (sender, eventArgs) => { openedEvent = null; };
                                    eventArgs.EventLoaded.Ended += (sender, eventArgs) => { eventList.Remove((Event)sender); };
                                }
                            }
                            else
                                if (player.IsConnected) player.SendClientMessage(Color.Red, "Cannot load the derby: " + eventArgs.ErrorMessage);
                        };
                        player.SendClientMessage(ColorPalette.Primary.Main + $"Loading Derby #{eventId}");
                        newEvent.Load(id);
                        usedIds.Add(eventId);
                        break;
                    }
                case EventType.Mission:
                    {
                        Event newEvent = new MissionEvent(eventId);
                        newEvent.Loaded += (sender, eventArgs) =>
                        {
                            if (eventArgs.ErrorMessage == null)
                            {
                                if (player.IsConnected) player.SendClientMessage(ColorPalette.Primary.Main + $"Mission {eventArgs.EventLoaded.Id} {Color.Green}loaded !");
                                eventList.Add(eventArgs.EventLoaded);
                                if (openedEvent == null)
                                {
                                    openedEvent = eventArgs.EventLoaded;
                                    eventArgs.EventLoaded.Open();
                                    eventArgs.EventLoaded.Started += (sender, eventArgs) => { openedEvent = null; };
                                    eventArgs.EventLoaded.Ended += (sender, eventArgs) => { eventList.Remove((Event)sender); };
                                }
                            }
                            else
                                if (player.IsConnected) player.SendClientMessage(Color.Red, "Cannot load the mission: " + eventArgs.ErrorMessage);
                        };
                        player.SendClientMessage(ColorPalette.Primary.Main + $"Loading mission #{eventId}");
                        newEvent.Load(id);
                        usedIds.Add(eventId);
                        break;
                    }
            }
        }

        private int CheckForAvailableId(List<int> alreadyUsedIds)
        {
            alreadyUsedIds.Sort();
            if (alreadyUsedIds.Count > 0)
            {
                int a = alreadyUsedIds.First();
                int b = alreadyUsedIds.Last();
                List<int> list2 = Enumerable.Range(a, b - a + 1).ToList();
                List<int> remaining = list2.Except(alreadyUsedIds).ToList();
                return remaining.Count > 0 ? remaining[0] : b + 1;
            }
            else
                return 1;

        }

    }
}
