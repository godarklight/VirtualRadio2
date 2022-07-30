using System;

namespace VirtualRadio.Client
{
    interface AudioInterface
    {
        public void SetMic(Action<byte[]> micEvent);
        public void Stop();
    }
}