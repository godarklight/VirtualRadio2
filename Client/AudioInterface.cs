using System;
using System.Diagnostics;
using System.Threading;

namespace VirtualRadio.Client
{
    class AudioInterface
    {
        bool running = true;
        Process p;
        Thread audioReadLoop;
        int audioPos = 0;
        byte[] audioBuffer = new byte[512];
        Action<byte[]> micEvent;

        public AudioInterface(Action<byte[]> micEvent)
        {
            this.micEvent = micEvent;
            audioReadLoop = new Thread(new ThreadStart(AudioReadLoop));
            audioReadLoop.Name = "Audio Read Loop";
            audioReadLoop.Start();
        }

        public void AudioReadLoop()
        {
            ProcessStartInfo psi = new ProcessStartInfo("pacat", "-r --raw --channels 1 --rate 8000 --format u8 -n \"VirtualRadio2\"");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            p = Process.Start(psi);
            while (running)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = p.StandardOutput.BaseStream.Read(audioBuffer, audioPos, audioBuffer.Length - audioPos);
                }
                catch (ObjectDisposedException)
                {
                    //The program was closed, we're exiting.
                    return;
                }
                if (bytesRead == 0)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    audioPos += bytesRead;
                    if (audioPos == audioBuffer.Length)
                    {
                        micEvent(audioBuffer);
                        audioPos = 0;
                    }
                }
            }
        }

        public void Stop()
        {
            p.Kill();
            p.WaitForExit();
            p.Dispose();
        }
    }
}