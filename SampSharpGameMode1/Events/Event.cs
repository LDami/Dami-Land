using SampSharpGameMode1.Events.Races;

namespace SampSharpGameMode1.Events
{
    public enum EventType
    {
        Race,
        Derby
    }

    public enum EventStatus
    {
        NotLoaded,
        Loaded,
        Running,
        Finished
    }

    public interface Event
    {
        public string name { get; set; }
        public EventStatus status { get; set; }
        public void Load();
        public void Start();
        public void End();

        public void Join(Player player);

    }
}
