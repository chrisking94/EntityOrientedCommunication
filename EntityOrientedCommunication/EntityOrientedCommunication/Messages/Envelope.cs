namespace EntityOrientedCommunication.Messages
{
    internal struct Envelope
    {
        public readonly uint ID;

        public Envelope(uint envelope)
        {
            ID = envelope;
        }

        public override string ToString()
        {
            return $"Envelope id={ID}";
        }
    }
}
