namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// contains the ID of a EOC message
    /// </summary>
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
