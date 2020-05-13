using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication
{
    public interface ILetter
    {
        /// <summary>
        /// title of the letter, similiar header in http protocal
        /// </summary>
        string Title { get; }

        /// <summary>
        /// content carried by the letter
        /// </summary>
        object Content { get; }

        /// <summary>
        /// the recipient's EOC mail address
        /// </summary>
        string Recipient { get; }

        /// <summary>
        /// the sender's EOC mail address
        /// </summary>
        string Sender { get; }
    }
}
