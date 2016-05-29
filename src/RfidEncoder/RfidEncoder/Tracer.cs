using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RfidEncoder
{
    public static class Tracer
    {
        //Инициализация лога
        public static void StartTracing()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

            string traceDirName = Path.Combine(Path.GetDirectoryName(assembly.CodeBase).Replace("file:\\", ""), "Log");
            Directory.CreateDirectory(traceDirName);

            string traceFileName = Path.Combine(traceDirName,
                Path.GetFileNameWithoutExtension(assembly.Location) + ".log");
            if (!File.Exists(traceFileName))
                File.CreateText(traceFileName).Close();
            var myTraceListener = new MyListener(traceFileName);
            Trace.Listeners.Add(myTraceListener);
            Trace.AutoFlush = true;
            Trace.TraceInformation("Logging started. " +
                                   "OS version: " + Environment.OSVersion.VersionString);
        }
    }

    internal class MyListener : TextWriterTraceListener
    {
        public MyListener(string filename) : base(filename)
        {
        }

        public override void WriteLine(string message)
        {
            string newmsg = "Time: " + DateTime.Now + ". " + message;
            base.WriteLine(newmsg);
        }
    }
}
