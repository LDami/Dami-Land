﻿using Google.Protobuf;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharpGameMode1.Events.Races;
using SampSharpGameMode1.Events.Derbys;
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

        public Event openedEvent;
        private List<Event> eventList;

        public EventManager()
        {
            openedEvent = null;
            eventList = new List<Event>();
        }

        public void ShowManagerDialog(Player player)
        {
            ListDialog managerDialog = new ListDialog("Event manager", "Select", "Cancel");
            managerDialog.AddItem(Color.Green + "Create event");
            foreach (Event evt in eventList)
            {
                managerDialog.AddItem(Color.White + "[" + evt.Status.ToString() + "]" + evt.Name);
            }

            managerDialog.Show(player);
            managerDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    if(eventArgs.ListItem == 0) // Create event
                    {
                        ShowCreateEventTypeDialog(player);
                    }
                    else
                        ShowEventOptionDialog(player, eventList.ElementAt(eventArgs.ListItem - 1));
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
                    createEventDialog.AddItem(t.ToString());
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
            if (evt.Status == EventStatus.Waiting)
                managerOptionDialog.AddItem("Start event");
            managerOptionDialog.AddItem(Color.Red + "Abort event");

            managerOptionDialog.Show(player);
            managerOptionDialog.Response += (sender, eventArgs) =>
            {
                if (eventArgs.DialogButton == DialogButton.Left)
                {
                    if (eventArgs.ListItem == 0) // Open / Start
                    {
                        if (evt.Status == EventStatus.Loaded)
                        {
                            evt.Open();
                        }
                        if (evt.Status == EventStatus.Waiting)
                        {
                            evt.Start();
                        }
                    }
                    else if(eventArgs.ListItem == 1) // Abort
                    {
                        evt.End();
                    }
                }
            };
        }

        public void ShowCreateEventNameDialog(Player player, EventType eventType)
        {
            InputDialog createEventNameDialog = new InputDialog(eventType.ToString() + " name", "Type the " + eventType.ToString() + " name you are looking for, or empty for random. You can also search by creator name", false, "Search", "Cancel");
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
			}
            Dictionary<string, string> row;
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str }
                };
            GameMode.mySQLConnector.OpenReader(query, param);
            row = GameMode.mySQLConnector.GetNextRow();
            if(row.Count == 0)
            {
                player.Notificate("No results");
                GameMode.mySQLConnector.CloseReader();
                ShowCreateEventNameDialog(player, eventType);
            }
            else
            {
                List<int> foundEvents = new List<int>();
                ListDialog eventSearchDialog = new ListDialog("Found " + eventType.ToString() + "s", "Launch", "Cancel");
                while (row.Count > 0)
                {
                    foundEvents.Add(Convert.ToInt32(row[key_id]));
                    eventSearchDialog.AddItem(row[key_id] + "_" + Display.ColorPalette.Primary.Main + row[key_name] + Display.ColorPalette.Primary.Lighten + " by " + Display.ColorPalette.Primary.Main + row[key_creator]);
                    row = GameMode.mySQLConnector.GetNextRow();
                }
                GameMode.mySQLConnector.CloseReader();
                eventSearchDialog.Show(player);
                eventSearchDialog.Response += (sender, eventArgs) =>
                {
                    if (eventArgs.DialogButton == DialogButton.Left)
                    {
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
            switch(type)
            {
                case EventType.Race:
                    {
                        Event newEvent = new RaceEvent(id);
                        newEvent.Loaded += (sender, eventArgs) =>
                        {
                            if (eventArgs.ErrorMessage == null)
                            {
                                if (player.IsConnected) player.SendClientMessage(Color.Green, "Event " + Display.ColorPalette.Primary.Main + eventArgs.EventLoaded.Id + Color.Green + " loaded ! (Race)");
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
                        player.SendClientMessage(Color.Green, "Loading Race #" + id);
                        newEvent.Load();
                        break;
                    }
                case EventType.Derby:
                    {
                        Event newEvent = new DerbyEvent(id);
                        newEvent.Loaded += (sender, eventArgs) =>
                        {
                            if (eventArgs.ErrorMessage == null)
                            {
                                if (player.IsConnected) player.SendClientMessage(Color.Green, "Event " + Display.ColorPalette.Primary.Main + eventArgs.EventLoaded.Id + Color.Green + " loaded ! (Derby)");
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
                        player.SendClientMessage(Color.Green, "Loading Derby #" + id);
                        newEvent.Load();
                        break;
                    }
            }
        }

    }
}
