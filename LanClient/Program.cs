using System.IO.Pipes;

namespace LanClient
{
    internal static class Program
    {
        private const string MutexName = "LanClientSingleInstance_v2";
        private const string PipeName  = "LanClientActivate";

        [STAThread]
        static void Main()
        {
            var mutex = new Mutex(true, MutexName, out bool isNew);

            if (!isNew)
            {
                // Another instance is running — signal it to show itself then exit
                try
                {
                    using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    pipe.Connect(1000);
                    pipe.WriteByte(1);
                }
                catch { }
                mutex.Dispose();
                return;
            }

            ApplicationConfiguration.Initialize();
            var app = new TrayApp();

            // Listen for activation signals from subsequent launches
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                        server.WaitForConnection();
                        server.ReadByte();
                        // Bring to front on UI thread
                        app.BringToFront();
                    }
                    catch { break; }
                }
            });

            Application.Run(app);
            mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
