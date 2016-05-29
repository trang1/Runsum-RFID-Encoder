using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RfidEncoder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            // Initialize and start our tracer
            Tracer.StartTracing();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                Trace.TraceError("UnhandledException", (Exception)e.ExceptionObject);
            }
            else
            {
                Trace.TraceError("UnhandledException: '{0}'", e.ExceptionObject);
            }
        }
    }
}
