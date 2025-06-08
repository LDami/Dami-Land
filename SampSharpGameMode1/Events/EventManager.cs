using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharpGameMode1.Events.Races;
using SampSharpGameMode1.Events.Derbys;
using System;
using System.Collections.Generic;
using System.Linq;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events.Missions;
using SampSharp.GameMode.World;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Threading;

namespace SampSharpGameMode1.Events
{
    public struct EventListObject
    {
        public int Id;
        public string Name;
        public string Author;
    }
    public class EventSelected : EventArgs
    {
        public int Id { get; set; }
        public EventSelected(int id)
        {
            Id = id;
        }
    }
    public class EventListHUD : HUD
    {
        public event EventHandler<EventSelected> Selected;
        List<EventListObject> eventList;
        List<int> shownObjects;
        int currentPage;
        int nbrOfPages;
        int nbrOfItems;
        public EventListHUD(BasePlayer _player, EventType _eventType, List<EventListObject> _eventList) : base(_player, "eventlist.json")
        {
            if (_eventList.Count == 0)
            {
                Logger.WriteLineAndClose("EventManager.cs : EventListHUD:_:W: No items in the list.");
                this.Hide();
                return;
            }
            layer.SetTextdrawText("title", $"Select a {_eventType}");
            nbrOfItems = DynamicDuplicateLayer("racename#", _eventList.Count, "bg");
            if (nbrOfItems == 0)
            {
                Logger.WriteLineAndClose("EventManager.cs : EventListHUD:_:E: The number of items that can be displayed is 0.");
                this.Hide();
                return;
            }
            currentPage = 1;
            nbrOfPages = _eventList.Count / nbrOfItems;
            layer.SetTextdrawText("page", string.Format("{0,2}", currentPage) + "/" + string.Format("{0,2}", nbrOfPages));

            eventList = _eventList;
            shownObjects = new();
            for (int i = 0; i < nbrOfItems; i++)
            {
                layer.SetTextdrawText($"racename[{i}]", $"{eventList[i].Name} ~r~by ~b~~h~~h~{eventList[i].Author}");
                layer.SetClickable($"racename[{i}]");
                shownObjects.Add(eventList[i].Id);
            }
            layer.SetClickable("prevPage");
            layer.SetClickable("nextPage");
            layer.SetClickable("gotolastpage");
            layer.TextdrawClicked += Layer_TextdrawClicked;

        }

        private void Layer_TextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
        {
            Logger.WriteLineAndClose("EventManager.cs : EventListHUD:Layer_TextdrawClicked:I: You clicked " + e.TextdrawName);
            if (e.TextdrawName == "prevPage")
            {
                currentPage = Math.Clamp(--currentPage, 1, nbrOfPages);
                UpdatePage();
            }
            else if (e.TextdrawName == "nextPage")
            {
                currentPage = Math.Clamp(++currentPage, 1, nbrOfPages);
                UpdatePage();
            }
            else if (e.TextdrawName == "gotolastpage")
            {
                currentPage = nbrOfPages;
                UpdatePage();
            }
            else
            {
                Regex regex = new(@"racename\[(\d*)\]");
                try
                {
                    if (int.TryParse(regex.Matches(e.TextdrawName).First().Groups[1].Value, out int index))
                    {
                        Selected.Invoke(this, new EventSelected(shownObjects[index]));
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLineAndClose("EventManager.cs - EventManager.Layer_TextdrawClicked:E: " + ex.Message);
                }
            }
        }

        private void UpdatePage()
        {
            layer.SetTextdrawText("page", string.Format("{0,2}", currentPage) + "/" + string.Format("{0,2}", nbrOfPages));
            shownObjects = new();
            for (int i = 0; i < nbrOfItems - 1; i++)
            {
                if ((nbrOfItems * currentPage) - 1 + i >= eventList.Count)
                    layer.Hide($"racename[{i}]");
                else
                {
                    EventListObject evt = eventList[(nbrOfItems * currentPage) - 1 + i];
                    layer.SetTextdrawText($"racename[{i}]", $"{evt.Name} by ~r~{evt.Author}");
                    layer.SetClickable($"racename[{i}]");
                    shownObjects.Add(evt.Id);
                }
            }
        }
    }
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
        public List<Event> RunningEvents { get { return eventList; } }
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
            managerOptionDialog.AddItem("Infos ...");
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
                    if (eventArgs.ListItem == 0) // Infos
                    {
                        TablistDialog infoDialog = new(evt.Name + " infos", 2, "Close")
                        {
                            new string[] { "Id", evt.Id.ToString() },
                            new string[] { "Name", evt.Name },
                            new string[] { "Players", evt.Slots.Count.ToString() },
                            new string[] { "Max players", evt.AvailableSlots.ToString() },
                            new string[] { "Status", evt.Status.ToString() },
                            new string[] { "VirtualWorld", evt.VirtualWorld.ToString() },
                            new string[] { "Type", evt.Type.ToString() },
                            new string[] { "Is playable ?", evt.Source.IsPlayable().ToString() },
                            new string[] { "Player slots:", "" },
                        };
                        for(int i = 0; i < evt.Slots.Count; i++)
                        {
                            infoDialog.Add(new string[] { "Slot #" + i, evt.Slots[i].Player.Name + " " + (evt.Slots[i].SpectateOnly ? "(spectator)" : "(runner)").ToString() });
                        }

                        infoDialog.Show(player);
                        infoDialog.Response += (sender, eventArgs) =>
                        {
                            ShowEventOptionDialog(player, evt);
                        };
                    }
                    if(evt.Status == EventStatus.Loaded || evt.Status == EventStatus.Waiting)
                    {
                        if (eventArgs.ListItem == 1) // Open / Start
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
                        else if (eventArgs.ListItem == 2) // Restart
                        {
                            evt.End(EventFinishedReason.Aborted);
                            openedEvent = null;
                            Thread.Sleep(2000);
                            CreateEvent(player, evt.Type, evt.Id);
                            player.Notificate("Event restarted");
                        }
                        else if (eventArgs.ListItem == 3) // Abort
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
                        if (eventArgs.ListItem == 1) // Abort
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
                List<int> editingEvents = new();
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

                List<int> foundEvents = new();
                List<EventListObject> events = new();
                while (row.Count > 0)
                {
                    if(!editingEvents.Contains(Convert.ToInt32(row[key_id])))
                    {
                        foundEvents.Add(Convert.ToInt32(row[key_id]));
                        events.Add(new EventListObject() { Id = Convert.ToInt32(row[key_id]), Name = row[key_name], Author = row[key_creator] });
                        Console.WriteLine("event added: " + row[key_name]);
                    }
                    row = GameMode.MySQLConnector.GetNextRow();
                }
                EventListHUD eventListHUD = new(player, eventType, events);
                GameMode.MySQLConnector.CloseReader();
                if(foundEvents.Count == 0)
                {
                    player.Notificate("No results");
                    ShowCreateEventNameDialog(player, eventType);
                }
                else
                {
                    eventListHUD.Selected += (sender, eventArgs) =>
                    {
                        player.CancelSelectTextDraw();
                        eventListHUD.Unload();
                        if (eventArgs.Id != 0)
                        {
                            if (eventType == EventType.Mission)
                                CreateEvent(player, eventType, 1);
                            else
                                CreateEvent(player, eventType, eventArgs.Id);
                        }
                        else
                        {
                            player.Notificate("Cancelled");
                            ShowCreateEventTypeDialog(player);
                        }
                    };
                    player.SelectTextDraw(ColorPalette.Primary.Main.GetColor());
                    player.CancelClickTextDraw += Player_CancelClickTextDraw;

                    void Player_CancelClickTextDraw(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
                    {
                        eventListHUD.Unload();
                        player.CancelClickTextDraw -= Player_CancelClickTextDraw;
                    }
                }
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
                                if (player.IsConnected) player.SendClientMessage(ColorPalette.Primary.Main + $"Race #{eventArgs.EventLoaded.Id} {Color.Green}loaded !");
                                eventList.Add(eventArgs.EventLoaded);
                                if (openedEvent == null)
                                {
                                    openedEvent = eventArgs.EventLoaded;
                                    openedEvent.Open();
                                    openedEvent.Started += (sender, eventArgs) => { openedEvent = null; };
                                    openedEvent.Ended += (sender, eventArgs) => { eventList.Remove((Event)sender); };
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
                                if (player.IsConnected) player.SendClientMessage(ColorPalette.Primary.Main + $"Derby #{eventArgs.EventLoaded.Id} {Color.Green}loaded !");
                                eventList.Add(eventArgs.EventLoaded);
                                if (openedEvent == null)
                                {
                                    openedEvent = eventArgs.EventLoaded;
                                    openedEvent.Open();
                                    openedEvent.Started += (sender, eventArgs) => { openedEvent = null; };
                                    openedEvent.Ended += (sender, eventArgs) => { eventList.Remove((Event)sender); };
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
                                    openedEvent.Open();
                                    openedEvent.Started += (sender, eventArgs) => { openedEvent = null; };
                                    openedEvent.Ended += (sender, eventArgs) => { eventList.Remove((Event)sender); };
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
