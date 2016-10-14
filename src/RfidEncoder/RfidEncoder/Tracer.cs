using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RfidEncoder
{
    public static class Tracer
    {
        //Init logging
        public static void StartTracing()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

            var traceDirName = Path.Combine(Path.GetDirectoryName(assembly.CodeBase).Replace("file:\\", ""), "Log");
            Directory.CreateDirectory(traceDirName);

            var traceFileName = Path.Combine(traceDirName,
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
            var newmsg = "Time: " + DateTime.Now + ". " + message;
            base.WriteLine(newmsg);
        }
    }
}