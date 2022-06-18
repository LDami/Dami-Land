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
			public override string ToString()
			{
				return "{" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2") + "}";
			}
		}
		public static class Primary
		{
			public static SAMPColor Main = new SAMPColor(Color.FromArgb(231, 213, 164));
			public static SAMPColor Lighten = new SAMPColor(Color.FromArgb(112, 145, 197));
			public static SAMPColor Darken = new SAMPColor(Color.FromArgb(53, 82, 128));
		}
		public static class Secondary
		{
			public static SAMPColor Main = new SAMPColor(Color.FromArgb(56, 106, 98));
			public static SAMPColor Lighten = new SAMPColor(Color.FromArgb(243, 245, 247));
			public static SAMPColor Darken = new SAMPColor(Color.FromArgb(194, 207, 214));
		}

	}
}
