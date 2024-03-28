using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampSharpGameMode1.CustomDatas
{
    public class MapObjectGroupData
    {
        public string Name { get; }
        public string Description { get; }

        public MapObjectGroupData(string name, string description)
        {
            Name = name;
            Description = description;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    public class MapObjectData
    {
        public static List<MapObjectData> MapObjects { get; private set; }
        public static List<MapObjectGroupData> MapObjectCategories { get; private set; }
        public static void UpdateMapObject(List<MapObjectData> mapObjects)
        {
            MapObjects = mapObjects;
            MapObjectCategories = mapObjects.Select(x => x.Group).ToList();
        }
        public int Id { get; }
        public string Name { get; }
        public MapObjectGroupData Group { get; }

        public MapObjectData(int id, string name, MapObjectGroupData group)
        {
            Id = id;
            Name = name;
            Group = group;
        }
        public override string ToString()
        {
            return $"{Id} {Name}";
        }
    }
}
