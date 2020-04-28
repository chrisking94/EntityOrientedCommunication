using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace EntityOrientedCommunication.Messages
{
    public struct Envelope
    {
        public readonly uint ID;

        public Envelope(uint envelope)
        {
            ID = envelope;
        }
        public Envelope(TMessage msg)
        {
            ID = msg.ID;
        }

        public override string ToString()
        {
            return $"Envelope id={ID}";
        }
    }
}
