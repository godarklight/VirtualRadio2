using System;
using Gtk;

using VirtualRadio.Common;
using VirtualRadio.Network;

namespace VirtualRadio.Client
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //Options
            AudioInterface audio = null;
            bool showGui = true;
            ModeType? mode = null;
            string setVFO = null;
            string server = "godarklight.privatedns.org";
            int port = 5973;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--nogui")
                {
                    showGui = false;
                }
                if (args[i] == "--vfo")
                {
                    setVFO = args[i + 1];
                }
                if (args[i] == "--mode")
                {
                    mode = Enum.Parse<Common.ModeType>(args[i + 1]);
                }
                if (args[i] == "--file")
                {
                    audio = new FileDriver(args[i + 1]);
                }
                if (args[i] == "--server")
                {
                    server = args[i + 1];
                    //TODO: IPv6 literals
                    if (server.Contains(":"))
                    {
                        string[] split = server.Split(':');
                        server = split[0];
                        port = Int32.Parse(split[1]);
                    }
                }
            }

            //Program setup
            if (audio == null)
            {
                audio = new MicDriver();
            }
            NetworkHandler handler = new NetworkHandler();
            NetworkEndpoint network = new NetworkEndpoint(handler, 0);
            ClientSender sender = new ClientSender(network, server, port);
            audio.SetMic(sender.MicEvent);
            ClientReceiver receiver = new ClientReceiver(sender, handler);

            //Tell the server our modes
            if (mode != null)
            {
                sender.SetMode(mode.Value);
            }
            if (setVFO != null)
            {
                sender.SetVFO(setVFO);
            }

            Console.WriteLine("Virtual Client Ready!");

            if (showGui)
            {
                Application.Init();

                var app = new Application("org.privatedns.godarklight", GLib.ApplicationFlags.None);
                app.Register(GLib.Cancellable.Current);

                var win = new MainWindow(sender);
                app.AddWindow(win);

                win.Show();
                Application.Run();
            }
            else
            {
                Console.WriteLine("Press enter to quit");
                Console.ReadLine();
            }

            sender.Stop();
            network.Stop();
            audio.Stop();
        }
    }
}
