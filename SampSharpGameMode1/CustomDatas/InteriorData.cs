using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SampSharpGameMode1.CustomDatas
{
    class InteriorData
    {
        public int Id { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }

        public InteriorData(int id, Vector3 position, float rotation, string name)
        {
            Id = id;
            Name = name;
            Position = position;
            Rotation = rotation;
        }

        public override string ToString()
        {
            return $"Id: {this.Id}, Name: {this.Name}, Pos: {this.Position}, Rot: {this.Rotation}";
        }

        public static List<InteriorData> Interiors = new List<InteriorData>
        {
            new InteriorData(11, new Vector3(2003.1178, 1015.1948, 33.008), 351.5789f, "Four Dragons' Managerial Suite"),
            new InteriorData(5, new Vector3(770.8033, -0.7033, 1000.7267), 22.8599f, "Ganton Gym"),
            new InteriorData(3, new Vector3(974.0177, -9.5937, 1001.1484), 22.6045f, "Brothel"),
            new InteriorData(3, new Vector3(961.9308, -51.9071, 1001.1172), 95.5381f, "Brothel2"),
            new InteriorData(3, new Vector3(830.6016, 5.9404, 1004.1797), 125.8149f, "Inside Track Betting"),
            new InteriorData(3, new Vector3(1037.8276, 0.397, 1001.2845), 353.9335f, "Blastin' Fools Records"),
            new InteriorData(3, new Vector3(1212.1489, -28.5388, 1000.9531), 170.5692f, "The Big Spread Ranch"),
            new InteriorData(18, new Vector3(1290.4106, 1.9512, 1001.0201), 179.9419f, "Warehouse 1"),
            new InteriorData(1, new Vector3(1412.1472, -2.2836, 1000.9241), 114.661f, "Warehouse 2"),
            new InteriorData(3, new Vector3(1527.0468, -12.0236, 1002.0971), 350.0013f, "B Dup's Apartment"),
            new InteriorData(2, new Vector3(1523.5098, -47.8211, 1002.2699), 262.7038f, "B Dup's Crack Palace"),
            new InteriorData(3, new Vector3(612.2191, -123.9028, 997.9922), 266.5704f, "Wheel Arch Angels"),
            new InteriorData(3, new Vector3(512.9291, -11.6929, 1001.5653), 198.7669f, "OG Loc's House"),
            new InteriorData(3, new Vector3(418.4666, -80.4595, 1001.8047), 343.2358f, "Barber Shop"),
            new InteriorData(3, new Vector3(386.5259, 173.6381, 1008.3828), 63.7399f, "Planning Department"),
            new InteriorData(3, new Vector3(288.4723, 170.0647, 1007.1794), 22.0477f, "Las Venturas Police Department"),
            new InteriorData(3, new Vector3(206.4627, -137.7076, 1003.0938), 10.9347f, "Pro-Laps"),
            new InteriorData(3, new Vector3(-100.2674, -22.9376, 1000.7188), 17.285f, "Sex Shop"),
            new InteriorData(3, new Vector3(-201.2236, -43.2465, 1002.2734), 45.8613f, "Las Venturas Tattoo parlor"),
            new InteriorData(17, new Vector3(-202.9381, -6.7006, 1002.2734), 204.2693f, "Lost San Fierro Tattoo parlor"),
            new InteriorData(17, new Vector3(-25.7220, -187.8216, 1003.5469), 5.0760f, "24/7 (version 1)"),
            new InteriorData(5, new Vector3(454.9853, -107.2548, 999.4376), 309.0195f, "Diner 1"),
            new InteriorData(5, new Vector3(372.5565, -131.3607, 1001.4922), 354.2285f, "Pizza Stack"),
            new InteriorData(17, new Vector3(378.026, -190.5155, 1000.6328), 141.0245f, "Rusty Brown's Donuts"),
            new InteriorData(7, new Vector3(315.244, -140.8858, 999.6016), 7.4226f, "Ammu-nation"),
            new InteriorData(5, new Vector3(225.0306, -9.1838, 1002.218), 85.5322f, "Victim"),
            new InteriorData(2, new Vector3(611.3536, -77.5574, 997.9995), 320.9263f, "Loco Low Co"),
            new InteriorData(10, new Vector3(246.0688, 108.9703, 1003.2188), 0.2922f, "San Fierro Police Department"),
            new InteriorData(10, new Vector3(6.0856, -28.8966, 1003.5494), 5.0365f, "24/7 (version 2 - large)"),
            new InteriorData(7, new Vector3(773.7318, -74.6957, 1000.6542), 5.2304f, "Below The Belt Gym (Las Venturas)"),
            new InteriorData(1, new Vector3(621.4528, -23.7289, 1000.9219), 15.6789f, "Transfenders"),
            new InteriorData(1, new Vector3(445.6003, -6.9823, 1000.7344), 172.2105f, "World of Coq"),
            new InteriorData(1, new Vector3(285.8361, -39.0166, 1001.5156), 0.7529f, "Ammu-nation (version 2)"),
            new InteriorData(1, new Vector3(204.1174, -46.8047, 1001.8047), 357.5777f, "SubUrban"),
            new InteriorData(1, new Vector3(245.2307, 304.7632, 999.1484), 273.4364f, "Denise's Bedroom"),
            new InteriorData(3, new Vector3(290.623, 309.0622, 999.1484), 89.9164f, "Helena's Barn"),
            new InteriorData(5, new Vector3(322.5014, 303.6906, 999.1484), 8.1747f, "Barbara's Love nest"),
            new InteriorData(1, new Vector3(-2041.2334, 178.3969, 28.8465), 156.2153f, "San Fierro Garage"),
            new InteriorData(1, new Vector3(-1402.6613, 106.3897, 1032.2734), 105.1356f, "Oval Stadium"),
            new InteriorData(7, new Vector3(-1403.0116, -250.4526, 1043.5341), 355.8576f, "8-Track Stadium"),
            new InteriorData(2, new Vector3(1204.6689, -13.5429, 1000.9219), 350.0204f, "The Pig Pen (strip club 2)"),
            new InteriorData(10, new Vector3(2016.1156, 1017.1541, 996.875), 88.0055f, "Four Dragons"),
            new InteriorData(1, new Vector3(-741.8495, 493.0036, 1371.9766), 71.7782f, "Liberty City"),
            new InteriorData(2, new Vector3(2447.8704, -1704.4509, 1013.5078), 314.5253f, "Ryder's house"),
            new InteriorData(1, new Vector3(2527.0176, -1679.2076, 1015.4986), 260.9709f, "Sweet's House"),
            new InteriorData(10, new Vector3(-1129.8909, 1057.5424, 1346.4141), 274.5268f, "RC Battlefield"),
            new InteriorData(3, new Vector3(2496.0549, -1695.1749, 1014.7422), 179.2174f, "The Johnson House"),
            new InteriorData(10, new Vector3(366.0248, -73.3478, 1001.5078), 292.0084f, "Burger shot"),
            new InteriorData(1, new Vector3(2233.9363, 1711.8038, 1011.6312), 184.3891f, "Caligula's Casino"),
            new InteriorData(2, new Vector3(269.6405, 305.9512, 999.1484), 215.6625f, "Katie's Lovenest"),
            new InteriorData(2, new Vector3(414.2987, -18.8044, 1001.8047), 41.4265f, "Barber Shop 2 (Reece's)"),
            new InteriorData(2, new Vector3(1.1853, -3.2387, 999.4284), 87.5718f, "Angel Pine Trailer"),
            new InteriorData(18, new Vector3(-30.9875, -89.6806, 1003.5469), 359.8401f, "24/7 (version 3)"),
            new InteriorData(18, new Vector3(161.4048, -94.2416, 1001.8047), 0.7938f, "Zip"),
            new InteriorData(3, new Vector3(-2638.8232, 1407.3395, 906.4609), 94.6794f, "The Pleasure Domes"),
            new InteriorData(5, new Vector3(1267.8407, -776.9587, 1091.9063), 231.3418f, "Madd Dogg's Mansion"),
            new InteriorData(2, new Vector3(2536.5322, -1294.8425, 1044.125), 254.9548f, "Big Smoke's Crack Palace"),
            new InteriorData(5, new Vector3(2350.1597, -1181.0658, 1027.9766), 99.1864f, "Burning Desire Building"),
            new InteriorData(1, new Vector3(-2158.6731, 642.09, 1052.375), 86.5402f, "Wu-Zi Mu's"),
            new InteriorData(10, new Vector3(419.8936, 2537.1155, 10), 67.6537f, "Abandoned AC tower"),
            new InteriorData(14, new Vector3(256.9047, -41.6537, 1002.0234), 85.8774f, "Wardrobe/Changing room"),
            new InteriorData(14, new Vector3(204.1658, -165.7678, 1000.5234), 181.7583f, "Didier Sachs"),
            new InteriorData(12, new Vector3(1133.35, -7.8462, 1000.6797), 165.8482f, "Casino (Redsands West)"),
            new InteriorData(14, new Vector3(-1420.4277, 1616.9221, 1052.5313), 159.1255f, "Kickstart Stadium"),
            new InteriorData(17, new Vector3(493.1443, -24.2607, 1000.6797), 356.9864f, "Club"),
            new InteriorData(18, new Vector3(1727.2853, -1642.9451, 20.2254), 172.4193f, "Atrium"),
            new InteriorData(16, new Vector3(-202.842, -24.0325, 1002.2734), 252.8154f, "Los Santos Tattoo Parlor"),
            new InteriorData(5, new Vector3(2233.6919, -1112.8107, 1050.8828), 8.6483f, "Safe House group 1"),
            new InteriorData(6, new Vector3(1211.2484, 1049.0234, 359.941), 170.9341f, "Safe House group 2"),
            new InteriorData(9, new Vector3(2319.1272, -1023.9562, 1050.2109), 167.3959f, "Safe House group 3"),
            new InteriorData(10, new Vector3(2261.0977, -1137.8833, 1050.6328), 266.88f, "Safe House group 4"),
            new InteriorData(17, new Vector3(-944.2402, 1886.1536, 5.0051), 179.8548f, "Sherman Dam"),
            new InteriorData(16, new Vector3(-26.1856, -140.9164, 1003.5469), 2.9087f, "24/7 (version 4)"),
            new InteriorData(15, new Vector3(2217.281, -1150.5349, 1025.7969), 273.7328f, "Jefferson Motel"),
            new InteriorData(1, new Vector3(1.5491, 23.3183, 1199.5938), 359.9054f, "Jet Interior"),
            new InteriorData(1, new Vector3(681.6216, -451.8933, -25.6172), 166.166f, "The Welcome Pump"),
            new InteriorData(3, new Vector3(234.6087, 1187.8195, 1080.2578), 349.4844f, "Burglary House X1"),
            new InteriorData(2, new Vector3(225.5707, 1240.0643, 1082.1406), 96.2852f, "Burglary House X2"),
            new InteriorData(1, new Vector3(224.288, 1289.1907, 1082.1406), 359.868f, "Burglary House X3"),
            new InteriorData(5, new Vector3(239.2819, 1114.1991, 1080.9922), 270.2654f, "Burglary House X4"),
            new InteriorData(15, new Vector3(207.5219, -109.7448, 1005.1328), 358.62f, "Binco"),
            new InteriorData(15, new Vector3(295.1391, 1473.3719, 1080.2578), 352.9526f, "Burglary houses"),
            new InteriorData(15, new Vector3(-1417.8927, 932.4482, 1041.5313), 0.7013f, "Blood Bowl Stadium"),
            new InteriorData(12, new Vector3(446.3247, 509.9662, 1001.4195), 330.5671f, "Budget Inn Motel Room"),
            new InteriorData(0, new Vector3(2306.3826, -15.2365, 26.7496), 274.49f, "Palamino Bank"),
            new InteriorData(0, new Vector3(2331.8984, 6.7816, 26.5032), 100.2357f, "Palamino Diner"),
            new InteriorData(0, new Vector3(663.0588, -573.6274, 16.3359), 264.9829f, "Dillimore Gas Station"),
            new InteriorData(18, new Vector3(-227.5703, 1401.5544, 27.7656), 269.2978f, "Lil' Probe Inn"),
            new InteriorData(0, new Vector3(-688.1496, 942.0826, 13.6328), 177.6574f, "Torreno's Ranch"),
            new InteriorData(0, new Vector3(-1916.1268, 714.8617, 46.5625), 152.2839f, "Zombotech - lobby area"),
            new InteriorData(0, new Vector3(818.7714, -1102.8689, 25.794), 91.1439f, "Crypt in LS cemetery (temple)"),
            new InteriorData(0, new Vector3(255.2083, -59.6753, 1.5703), 1.4645f, "Blueberry Liquor Store"),
            new InteriorData(2, new Vector3(446.626, 1397.738, 1084.3047), 343.9647f, "Pair of Burglary Houses"),
            new InteriorData(5, new Vector3(227.3922, 1114.6572, 1080.9985), 267.459f, "Crack Den"),
            new InteriorData(5, new Vector3(227.7559, 1114.3844, 1080.9922), 266.2624f, "Burglary House X11"),
            new InteriorData(4, new Vector3(261.1165, 1287.2197, 1080.2578), 178.9149f, "Burglary House X12"),
            new InteriorData(4, new Vector3(291.7626, -80.1306, 1001.5156), 290.2195f, "Ammu-nation (version 3)"),
            new InteriorData(4, new Vector3(449.0172, -88.9894, 999.5547), 89.6608f, "Jay's Diner"),
            new InteriorData(4, new Vector3(-27.844, -26.6737, 1003.5573), 184.3118f, "24/7 (version 5)"),
            new InteriorData(0, new Vector3(2135.2004, -2276.2815, 20.6719), 318.59f, "Warehouse 3"),
            new InteriorData(4, new Vector3(306.1966, 307.819, 1003.3047), 203.1354f, "Michelle's Love Nest*"),
            new InteriorData(10, new Vector3(24.3769, 1341.1829, 1084.375), 8.3305f, "Burglary House X14"),
            new InteriorData(1, new Vector3(963.0586, 2159.7563, 1011.0303), 175.313f, "Sindacco Abatoir"),
            new InteriorData(0, new Vector3(2548.4807, 2823.7429, 10.8203), 270.6003f, "K.A.C.C. Military Fuels Depot"),
            new InteriorData(0, new Vector3(215.1515, 1874.0579, 13.1406), 177.5538f, "Area 69"),
            new InteriorData(4, new Vector3(221.6766, 1142.4962, 1082.6094), 184.9618f, "Burglary House X13"),
            new InteriorData(12, new Vector3(2323.7063, -1147.6509, 1050.7101), 206.5352f, "Unused Safe House"),
            new InteriorData(6, new Vector3(344.9984, 307.1824, 999.1557), 193.643f, "Millie's Bedroom"),
            new InteriorData(12, new Vector3(411.9707, -51.9217, 1001.8984), 173.3449f, "Barber Shop"),
            new InteriorData(4, new Vector3(-1421.5618, -663.8262, 1059.5569), 170.9341f, "Dirtbike Stadium"),
            new InteriorData(6, new Vector3(773.8887, -47.7698, 1000.5859), 10.7161f, "Cobra Gym"),
            new InteriorData(6, new Vector3(246.6695, 65.8039, 1003.6406), 7.9562f, "Los Santos Police Department"),
            new InteriorData(14, new Vector3(-1864.9434, 55.7325, 1055.5276), 85.8541f, "Los Santos Airport"),
            new InteriorData(4, new Vector3(-262.1759, 1456.6158, 1084.3672), 82.459f, "Burglary House X15"),
            new InteriorData(5, new Vector3(22.861, 1404.9165, 1084.4297), 349.6158f, "Burglary House X16"),
            new InteriorData(5, new Vector3(140.3679, 1367.8837, 1083.8621), 349.2372f, "Burglary House X17"),
            new InteriorData(3, new Vector3(1494.8589, 1306.48, 1093.2953), 196.065f, "Bike School"),
            new InteriorData(14, new Vector3(-1813.213, -58.012, 1058.9641), 335.3199f, "Francis International Airport"),
            new InteriorData(16, new Vector3(-1401.067, 1265.3706, 1039.8672), 178.6483f, "Vice Stadium"),
            new InteriorData(6, new Vector3(234.2826, 1065.229, 1084.2101), 4.3864f, "Burglary House X18"),
            new InteriorData(6, new Vector3(-68.5145, 1353.8485, 1080.2109), 3.5742f, "Burglary House X19"),
            new InteriorData(6, new Vector3(-2240.1028, 136.973, 1035.4141), 269.0954f, "Zero's RC Shop"),
            new InteriorData(6, new Vector3(297.144, -109.8702, 1001.5156), 20.2254f, "Ammu-nation (version 4)"),
            new InteriorData(6, new Vector3(316.5025, -167.6272, 999.5938), 10.3031f, "Ammu-nation (version 5)"),
            new InteriorData(15, new Vector3(-285.2511, 1471.197, 1084.375), 85.6547f, "Burglary House X20"),
            new InteriorData(6, new Vector3(-26.8339, -55.5846, 1003.5469), 3.9528f, "24/7 (version 6)"),
            new InteriorData(6, new Vector3(442.1295, -52.4782, 999.7167), 177.9394f, "Secret Valley Diner"),
            new InteriorData(2, new Vector3(2182.2017, 1628.5848, 1043.8723), 224.8601f, "Rosenberg's Office in Caligulas"),
            new InteriorData(6, new Vector3(748.4623, 1438.2378, 1102.9531), 0.6069f, "Fanny Batter's Whore House"),
            new InteriorData(8, new Vector3(2807.3604, -1171.7048, 1025.5703), 193.7117f, "Colonel Furhberger's"),
            new InteriorData(9, new Vector3(366.0002, -9.4338, 1001.8516), 160.528f, "Cluckin' Bell"),
            new InteriorData(1, new Vector3(2216.1282, -1076.3052, 1050.4844), 86.428f, "The Camel's Toe Safehouse"),
            new InteriorData(1, new Vector3(2268.5156, 1647.7682, 1084.2344), 99.7331f, "Caligula's Roof"),
            new InteriorData(2, new Vector3(2236.6997, -1078.9478, 1049.0234), 2.5706f, "Old Venturas Strip Casino"),
            new InteriorData(3, new Vector3(-2031.1196, -115.8287, 1035.1719), 190.1877f, "Driving School"),
            new InteriorData(8, new Vector3(2365.1089, -1133.0795, 1050.875), 177.3947f, "Verdant Bluffs Safehouse"),
            new InteriorData(0, new Vector3(1168.512, 1360.1145, 10.9293), 196.5933f, "Bike School"),
            new InteriorData(9, new Vector3(315.4544, 976.5972, 1960.8511), 359.6368f, "Andromada"),
            new InteriorData(10, new Vector3(1893.0731, 1017.8958, 31.8828), 86.1044f, "Four Dragons' Janitor's Office"),
            new InteriorData(11, new Vector3(501.9578, -70.5648, 998.7578), 171.5706f, "Bar"),
            new InteriorData(8, new Vector3(-42.5267, 1408.23, 1084.4297), 172.068f, "Burglary House X21"),
            new InteriorData(11, new Vector3(2283.3118, 1139.307, 1050.8984), 19.7032f, "Willowfield Safehouse"),
            new InteriorData(9, new Vector3(84.9244, 1324.2983, 1083.8594), 159.5582f, "Burglary House X22"),
            new InteriorData(9, new Vector3(260.7421, 1238.2261, 1084.2578), 84.3084f, "Burglary House X23"),
            new InteriorData(0, new Vector3(-1658.1656, 1215.0002, 7.25), 103.9074f, "Otto's Autos"),
            new InteriorData(0, new Vector3(-1961.6281, 295.2378, 35.4688), 264.4891f, "Wang Cars")
        };

        public static List<InteriorData> OnlyOnce = Interiors.GroupBy(x => x.Id, (key, g) => g.OrderBy(e => e.Id).First()).ToList();
    }
}
