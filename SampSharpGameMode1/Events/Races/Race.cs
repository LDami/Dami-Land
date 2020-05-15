using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Events.Races
{
    class Race
    {
        public const int MIN_PLAYERS_IN_RACE = 2;
        public const int MAX_PLAYERS_IN_RACE = 100;

        private int id;
        private string name;
        private int laps;
        public Dictionary<int, Checkpoint> checkpoints = new Dictionary<int, Checkpoint>();
        public Vector3R[] startingSpawn;
        private VehicleModelType startingVehicle;
        private bool isStartingVehicleNull;


        public int Id { get => id; private set => id = value; }
        public string Name { get => name; set => name = value; }
        public int Laps { get => laps; set => laps = value; }
        public VehicleModelType StartingVehicle { get => startingVehicle; set => startingVehicle = value; }
        public bool IsStartingVehicleNull { get => isStartingVehicleNull; set => isStartingVehicleNull = value; }

        // Launcher only

        private bool isStarted;

        public bool IsStarted { get => isStarted; set => isStarted = value; }

        public void Load(int id)
        {
            if (GameMode.mySQLConnector != null)
            {
                Dictionary<string, string> row;
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };
                GameMode.mySQLConnector.OpenReader("SELECT * FROM races WHERE race_id=@id", param);
                row = GameMode.mySQLConnector.GetNextRow();
                if (row.Count > 0)
                {
                    this.Id = Convert.ToInt32(row["race_id"]);
                    this.Name = row["race_name"].ToString();
                    this.Laps = Convert.ToInt32(row["race_laps"]);
                    if(Convert.ToInt32(row["race_startvehicle"]) >= 400 && Convert.ToInt32(row["race_startvehicle"]) <= 611)
                    {
                        this.StartingVehicle = (VehicleModelType)Convert.ToInt32(row["race_startvehicle"]);
                        this.IsStartingVehicleNull = false;
                    }
                    else
                    {
                        this.StartingVehicle = VehicleModelType.Landstalker;
                        this.IsStartingVehicleNull = true;
                    }
                }
                GameMode.mySQLConnector.CloseReader();

                GameMode.mySQLConnector.OpenReader("SELECT checkpoint_number, checkpoint_pos_x, checkpoint_pos_y, checkpoint_pos_z, checkpoint_size, checkpoint_type " +
                    "FROM race_checkpoints " +
                    "WHERE race_id=@id ORDER BY checkpoint_number", param);
                row = GameMode.mySQLConnector.GetNextRow();

                this.checkpoints.Clear();
                Checkpoint checkpoint;
                int idx = 0;
                while (row.Count > 0)
                {
                    checkpoint = new Checkpoint(new Vector3(
                            (float)Convert.ToDouble(row["checkpoint_pos_x"]),
                            (float)Convert.ToDouble(row["checkpoint_pos_y"]),
                            (float)Convert.ToDouble(row["checkpoint_pos_z"])
                        ), (CheckpointType)Convert.ToInt32(row["checkpoint_type"]), Convert.ToDouble(row["checkpoint_size"]));
                    this.checkpoints.Add(idx++, checkpoint);
                    row = GameMode.mySQLConnector.GetNextRow();
                }
                GameMode.mySQLConnector.CloseReader();

                GameMode.mySQLConnector.OpenReader("SELECT spawn_index, spawnpos_x, spawnpos_y, spawnpos_z " +
                    "FROM race_spawnpos " +
                    "WHERE race_id=@id ORDER BY spawn_index", param);
                row = GameMode.mySQLConnector.GetNextRow();

                this.startingSpawn = new Vector3R[MAX_PLAYERS_IN_RACE];
                Vector3R pos;
                idx = 0;
                while (row.Count > 0)
                {
                    pos = new Vector3R(new Vector3(
                                (float)Convert.ToDouble(row["spawnpos_x"]),
                                (float)Convert.ToDouble(row["spawnpos_y"]),
                                (float)Convert.ToDouble(row["spawnpos_z"])
                            ),
                            (float)Convert.ToDouble(row["spawnpos_rot"])
                        );
                    this.startingSpawn[idx++] = pos;
                    row = GameMode.mySQLConnector.GetNextRow();
                }
                GameMode.mySQLConnector.CloseReader();
            }
        }

        public Boolean IsPlayable()
        {
            return (checkpoints.Count > 0 && !IsStartingVehicleNull) ? true : false;
        }
    }
}
