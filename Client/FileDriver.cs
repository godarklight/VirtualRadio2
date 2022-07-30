using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace VirtualRadio.Client
{
    class FileDriver : AudioInterface
    {
        bool running = true;
        Thread fileReadLoop;
        byte[] fileBytes;
        int filePos = 0;
        byte[] audioBuffer = new byte[1024];
        Action<byte[]> micEvent;

        public FileDriver(string inputFile)
        {
            fileBytes = File.ReadAllBytes(inputFile);
            fileReadLoop = new Thread(new ThreadStart(FileReadLoop));
            fileReadLoop.Name = "File Read Loop";
            fileReadLoop.Start();
        }

        public void SetMic(Action<byte[]> micEvent)
        {
            this.micEvent = micEvent;
        }

        public void FileReadLoop()
        {
            long startTime = DateTime.UtcNow.Ticks;
            long sentBytes = 0;
            while (micEvent == null)
            {
                Thread.Sleep(5);
            }
            while (running)
            {
                long currentTime = DateTime.UtcNow.Ticks;
                double targetBytes = (441000 * ((currentTime - startTime) / (double)TimeSpan.TicksPerSecond)) - sentBytes;
                if (targetBytes > audioBuffer.Length)
                {
                    Array.Copy(fileBytes, filePos, audioBuffer, 0, audioBuffer.Length);
                    sentBytes += audioBuffer.Length;
                    filePos += audioBuffer.Length;
                    micEvent(audioBuffer);
                    if (fileBytes.Length - filePos < audioBuffer.Length)
                    {
                        filePos = 0;
                    }
                }
                Thread.Sleep(5);
            }
        }

        public void SetFile(string file)
        {
            filePos = 0;
            fileBytes = File.ReadAllBytes(file);
        }

        public void Stop()
        {
            running = false;
        }
    }
}