using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        public static Vector3 GetPositionFrontOfPlayer(BasePlayer player, float distance = 8f)
        {
            float angle = player.InAnyVehicle ? player.Vehicle.Angle : player.Angle;
            angle = (float)(angle * Math.PI) / 180;
            return player.Position + new Vector3(-distance * Math.Sin(angle), distance * Math.Cos(angle), 0);
        }

        /// <summary>
        ///  This function returns True if <paramref name="check"/> is between <paramref name="pos1"/> and <paramref name="pos2"/>
        /// </summary>
        /// <param name="pos1">The first position</param>
        /// <param name="pos2">The second position</param>
        /// <param name="check">The position to check</param>
        /// <returns>True if <paramref name="check"/> is between <paramref name="pos1"/> and <paramref name="pos2"/></returns>
        public static bool IsInTwoVectors(Vector3 pos1, Vector3 pos2, Vector3 check)
        {
            float minX = Math.Min(pos1.X, pos2.X);
            float maxX = Math.Max(pos1.X, pos2.X);
            float minY = Math.Min(pos1.Y, pos2.Y);
            float maxY = Math.Max(pos1.Y, pos2.Y);
            float minZ = Math.Min(pos1.Z, pos2.Z);
            float maxZ = Math.Max(pos1.Z, pos2.Z);
            return check.X >= minX && check.X <= maxX && check.Y >= minY && check.Y <= maxY
                && check.Z >= minZ && check.Z <= maxZ;
        }

        public static Color? GetColorFromString(string str)
        {
            Color? result = null;
            if (str.Length > 0)
            {
                if (str.StartsWith("0x"))
                {
                    if (str.Length == 8 || str.Length == 10)
                    {
                        string r, g, b, a = "255";
                        r = str.Substring(2, 2);
                        g = str.Substring(4, 2);
                        b = str.Substring(6, 2);

                        if (str.Length == 10)
                            a = str.Substring(8, 2);

                        try
                        {
                            result = new Color(
                                int.Parse(r, System.Globalization.NumberStyles.HexNumber),
                                int.Parse(g, System.Globalization.NumberStyles.HexNumber),
                                int.Parse(b, System.Globalization.NumberStyles.HexNumber),
                                int.Parse(a, System.Globalization.NumberStyles.HexNumber)
                            );
                        }
                        catch (FormatException)
                        {
                            result = null;
                        }
                    }
                }
                else if (str.StartsWith("rgb("))
                {
                    Regex regex = new Regex(@"[r][g][b][(](\d{1,3})[,;]\s*(\d{1,3})[,;]\s*(\d{1,3})(?>[,;]\s*(\d{1,3}))?[)]", RegexOptions.IgnoreCase);
                    Match match = regex.Match(str);
                    if (match.Success)
                    {
                        int r, g, b, a = 255;
                        r = int.Parse(match.Groups[1].Value);
                        g = int.Parse(match.Groups[2].Value);
                        b = int.Parse(match.Groups[3].Value);

                        if (match.Groups[4].Success)
                            a = int.Parse(match.Groups[4].Value);

                        result = new Color(r, g, b, a);
                    }
                }
            }
            return result;
        }

        public static List<string> GetStringsMatchingRegex(List<string> inputs, string pattern)
        {
            List<string> result = new List<string>();
            string inputsStr = string.Join("\n", inputs);
            Console.WriteLine("inputStr: " + inputsStr);
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection collection = rgx.Matches(inputsStr);
            Console.WriteLine("Matches for " + pattern + ": " + collection.Count);
            foreach (Match match in collection)
            {
                result.Add(match.Value);
            }
            return result;
        }
    }
}