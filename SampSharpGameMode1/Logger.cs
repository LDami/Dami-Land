using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1
{
    class Logger
    {
        private static string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\gamemode.log";
        //private static string filename = BaseMode.Instance.Client.ServerPath + "..\\..\\scriptfiles\\gamemode.log";

        FileStream fileStream = null;

        public Logger()
        {
            fileStream = File.Open(filename, FileMode.Append, FileAccess.Write);
        }

        public void Write(string text, bool writeToConsole = true)
        {
            if (writeToConsole) Console.Write(text);
            if (fileStream != null)
            {
                byte[] data = new UTF8Encoding(true).GetBytes(text);
                foreach (byte databyte in data)
                    fileStream.WriteByte(databyte);
                fileStream.FlushAsync();
            }
        }
        public void WriteLine(string text, bool writeToConsole = true)
        {
            this.Write(text + "\r\n", writeToConsole);
        }

        public void Close()
        {
            if(fileStream != null)
            {
                fileStream.Close();
                fileStream.Dispose();
                fileStream = null;
            }
        }

        public static void Init()
        {
            try
            {
                if (File.Exists(filename))
                    File.Delete(filename);
                Logger.WriteLineAndClose("Logger.cs - Logger.Init:I: Logger initialized at " + DateTime.Now.ToString(), false);
                Console.WriteLine("Logger.cs - Logger.Init:I: Logger initialized");
            }
            catch (IOException e)
            {
                Console.WriteLine("Logger.cs - Logger.Init:E: Cannot delete logger file: ");
                Console.WriteLine(e.Message);
            }
        }
        public static void WriteAndClose(string text, bool writeToConsole = true)
        {
            if (writeToConsole) Console.Write(text);
            try
            {
                using (FileStream fs = File.Open(filename, FileMode.Append, FileAccess.Write))
                {
                    byte[] data = new UTF8Encoding(true).GetBytes(text);
                    foreach (byte databyte in data)
                        fs.WriteByte(databyte);
                    fs.FlushAsync();
                    fs.Close();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Logger.cs - Logger.Init:E: Cannot write in logger file: ");
                Console.WriteLine(e.Message);
            }
        }
        public static void WriteLineAndClose(string text, bool writeToConsole = true)
        {
            Logger.WriteAndClose(text + "\r\n", writeToConsole);
        }
    }
}
