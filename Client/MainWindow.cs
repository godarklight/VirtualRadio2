using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

using VirtualRadio.Common;

namespace VirtualRadio.Client
{
    class MainWindow : Window
    {
        [UI] private Button btnTransmitCW = null;
        [UI] private Button btnSetFreq = null;
        [UI] private Entry vfoFreq = null;
        [UI] private RadioButton radioAM = null;
        [UI] private RadioButton radioFM = null;
        [UI] private RadioButton radioWFM = null;
        [UI] private RadioButton radioLSB = null;
        [UI] private RadioButton radioUSB = null;
        private ClientSender clientSender = null;

        public MainWindow(ClientSender clientSender) : this(new Builder("MainWindow.glade"))
        {
            this.clientSender = clientSender;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;

            btnTransmitCW.Pressed += cwPressed;
            btnTransmitCW.Released += cwReleased;
            btnSetFreq.Clicked += setFreq;
            radioAM.Toggled += ModeAM;
            radioFM.Toggled += ModeFM;
            radioWFM.Toggled += ModeWFM;
            radioLSB.Toggled += ModeLSB;
            radioUSB.Toggled += ModeUSB;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void cwPressed(object sender, EventArgs a)
        {
            Console.WriteLine("Pressed");
        }

        private void cwReleased(object sender, EventArgs a)
        {
            Console.WriteLine("Released");
        }

        private void setFreq(object sender, EventArgs a)
        {
            int newVfo = clientSender.SetVFO(vfoFreq.Text);
            vfoFreq.Text = newVfo.ToString();
        }

        private void ModeAM(object sender, EventArgs a)
        {
            if (((RadioButton)sender).Active)
            {
                clientSender.SetMode(ModeType.AM);
            }

        }

        private void ModeFM(object sender, EventArgs a)
        {
            if (((RadioButton)sender).Active)
            {
                clientSender.SetMode(ModeType.FM);
            }
        }

        private void ModeWFM(object sender, EventArgs a)
        {
            if (((RadioButton)sender).Active)
            {
                clientSender.SetMode(ModeType.WFM);
            }
        }

        private void ModeLSB(object sender, EventArgs a)
        {
            if (((RadioButton)sender).Active)
            {
                clientSender.SetMode(ModeType.LSB);
            }
        }

        private void ModeUSB(object sender, EventArgs a)
        {
            if (((RadioButton)sender).Active)
            {
                clientSender.SetMode(ModeType.USB);
            }
        }
    }
}
