using System;

namespace HydraEngine.Runtimes.Exceptions.Runtime
{
    internal static class ExceptionManager
    {

        private static void Initialize()
        {
            ExcepList = new System.Collections.Generic.List<string>();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                System.Windows.Forms.Application.ThreadException += Application_Exception_Handler;
                System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException, false);
            }
            catch
            {
            }

        }

        public static void FirstChanceExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            System.Exception ex = (System.Exception)e.Exception;
            WriteLogError(ex);
        }

        public static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            System.Exception ex = (System.Exception)e.ExceptionObject;
            WriteLogError(ex);
        }

        private static void Application_Exception_Handler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            System.Exception ex = (System.Exception)e.Exception;
            WriteLogError(ex);
        }

        private static void WriteLogError(System.Exception Excep)
        {
            try {
                ExcepList.Add(Excep.Message);
                ConsoleColor CurrentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Excep.Message);
                Console.ForegroundColor = CurrentColor;
            } catch { }
        }

        static System.Collections.Generic.List<string> ExcepList;

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            System.IO.File.WriteAllText("HydraExceptions.log", string.Join(Environment.NewLine, ExcepList));
        }
    }


    internal static class ExceptionManagerCore
    {
        private static void Initialize()
        {
            ExcepList = new System.Collections.Generic.List<string>();

            // Captura excepciones de primer nivel y no manejadas
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        }

        public static void FirstChanceExceptionHandler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            System.Exception ex = (System.Exception)e.Exception;
            WriteLogError(ex);
        }

        public static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            System.Exception ex = (System.Exception)e.ExceptionObject;
            WriteLogError(ex);
        }

        private static void Application_Exception_Handler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            System.Exception ex = (System.Exception)e.Exception;
            WriteLogError(ex);
        }

        private static void WriteLogError(System.Exception excep)
        {
            try
            {
                ExcepList.Add(excep.Message);
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(excep.Message);
                Console.ForegroundColor = currentColor;
            }
            catch { }
        }

        static System.Collections.Generic.List<string> ExcepList;

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            System.IO.File.WriteAllText("HydraExceptions.log", string.Join(Environment.NewLine, ExcepList));
        }
    }


}
