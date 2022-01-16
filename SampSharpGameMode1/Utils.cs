using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using System;

namespace SampSharpGameMode1
{
    class Utils
    {
        public static VehicleModelType GetVehicleModelType(string model)
        {
            int minLevDistance = 999;
            VehicleModelType nearestModel = new VehicleModelType();
            foreach(VehicleModelType m in (VehicleModelType[])Enum.GetValues(typeof(VehicleModelType)))
            {
                if (GetLevenshteinDistance(m.ToString(), model) < minLevDistance)
                {
                    minLevDistance = GetLevenshteinDistance(m.ToString(), model);
                    nearestModel = m;
                }
            }
            Console.WriteLine("Utils.cs - Utils.GetVehicleModelType:I: Found model \"" + nearestModel + "\" for keyword \"" + model + "\", distance: " + minLevDistance);
            return nearestModel;
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int GetLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static Boolean IsPlayerInRangeOfPoint(Player player, float range, Vector3 target)
        {
            //return Vector3.Distance(player.Position, target) <= range;
            return (
                player.Position.X > (target.X + range) &&
                player.Position.X < (target.X - range) &&
                player.Position.Y > (target.Y + range) &&
                player.Position.Y < (target.Y - range)
            );
        }
    }
}