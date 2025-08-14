using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic_Obfuscation.Arithmetic
{
    public class Logger
    {
        public void mark()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(@"   ___            _          ___   __    ___              _    ");
            Console.WriteLine(@"  / __\ ___    __| |  ___   /___\ / _|  /   \ __ _  _ __ | | __");
            Console.WriteLine(@" / /   / _ \  / _` | / _ \ //  //| |_  / /\ // _` || '__|| |/ /");
            Console.WriteLine(@"/ /___| (_) || (_| ||  __// \_// |  _|/ /_//| (_| || |   |   < ");
            Console.WriteLine(@"\____/ \___/  \__,_| \___|\___/  |_| /___,'  \__,_||_|   |_|\_\");
            Console.WriteLine("");
            Line();
        }
        public void Info(string str)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[Info] ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(str);
        }
        public void Custom(string message, string info, ConsoleColor infoColor, ConsoleColor messageColor)
        {
            Console.ForegroundColor = infoColor;
            Console.Write("[" + info + "] ");
            Console.ForegroundColor = messageColor;
            Console.Write(message);
            Console.ForegroundColor = infoColor;
            Console.WriteLine("[" + info + "] ");

        }
        public void Line()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("------------------------------------------");
        }

        public void Warning(string str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[Warning] ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(str);
        }
        public void Fixed(string str)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[Fixed] ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(str);
        }
        public void Error(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[Error] ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(str);
        }
        public void Success(string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[Success] ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(str);
        }
    }

}
