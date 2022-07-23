using System;
using Gtk;

using VirtualRadio.Network;

namespace VirtualRadio.Client
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            NetworkHandler handler = new NetworkHandler();
            NetworkEndpoint network = new NetworkEndpoint(handler, 0);
            ClientSender sender = new ClientSender(network);
            AudioInterface audio = new AudioInterface(sender.MicEvent);
            ClientReceiver receiver = new ClientReceiver(sender, handler);
            Console.WriteLine("Virtual Client Ready!");

            if (args.Length == 0)
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
