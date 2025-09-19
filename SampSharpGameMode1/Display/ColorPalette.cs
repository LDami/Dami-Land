using System.Drawing;

namespace SampSharpGameMode1.Display
{
	public class ColorPalette
	{
		public class SAMPColor
		{
			private Color color;
			public SAMPColor(Color clr)
			{
				this.color = clr;
			}
			public SAMPColor(SampSharp.GameMode.SAMP.Color clr)
			{
				this.color = Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
			}
			public SampSharp.GameMode.SAMP.Color GetColor()
            {
				return new SampSharp.GameMode.SAMP.Color(color.R, color.G, color.B, color.A);

			}
			public override string ToString()
			{
				return "{" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2") + "}";
			}
		}
		public static class Primary
		{
            public static readonly SAMPColor Main = new(Color.FromArgb(93, 183, 222));
            public static readonly SAMPColor Lighten = new (Color.FromArgb(112, 145, 197));
			public static readonly SAMPColor Darken = new (Color.FromArgb(53, 82, 128));
		}
		public static class Secondary
        {
            public static readonly SAMPColor Main = new(Color.FromArgb(231, 213, 164));
			public static readonly SAMPColor Lighten = new (Color.FromArgb(243, 245, 247));
			public static readonly SAMPColor Darken = new (Color.FromArgb(194, 207, 214));
		}
		public static class Error
		{
			public static SAMPColor Main = new SAMPColor(Color.FromArgb(223, 41, 53));
		}

	}
}
