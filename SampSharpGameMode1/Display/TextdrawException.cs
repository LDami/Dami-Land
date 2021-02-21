using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class TextdrawNameNotFound : Exception
    {
        public TextdrawNameNotFound() :
            base("The given Textdraw name has not been found.")
        { }
    }
}
