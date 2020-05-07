using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Mail
{
    public interface ILetter
    {
        string Title { get; }

        object Content { get; }

        LetterType LetterType { get; }

        string Recipient { get; }

        string Sender { get; }
    }
}
