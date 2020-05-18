using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication.Mail;

namespace EntityOrientedCommunication.Client
{
    internal delegate EMLetter IncomingLetterEventHandler(EMLetter letter);

    internal delegate void ResetedEventHandler();

    internal delegate void TransmissionErrorEventHandler(EMLetter letter, string errorMessage);

    internal interface IClientMailTransceiver : IMailTransceiver, IDisposable
    {
        /// <summary>
        /// provide a datetime which represents the now time of communication system
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// this event will be emitted when the dispatcher receives a new letter
        /// </summary>
        event IncomingLetterEventHandler IncomingLetterEvent;

        /// <summary>
        /// emitted once after this dispatcher was reseted
        /// </summary>
        event ResetedEventHandler ResetedEvent;

        /// <summary>
        /// emitted when a letter encountered a async transmission error
        /// </summary>
        event TransmissionErrorEventHandler TransmissionErrorEvent;
        
        /// <summary>
        /// activate mailboxes, notify other mailboxes that these 'mailBoxes' are online
        /// </summary>
        /// <param name="mailBoxes"></param>
        void Activate(params ClientMailBox[] mailBoxes);
    }
}
