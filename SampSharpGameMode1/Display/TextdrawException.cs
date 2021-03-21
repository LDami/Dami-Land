using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class TextdrawNameNotFoundException : Exception
    {
        public TextdrawNameNotFoundException() :
            base("The given Textdraw name has not been found.")
        { }
    }
}
