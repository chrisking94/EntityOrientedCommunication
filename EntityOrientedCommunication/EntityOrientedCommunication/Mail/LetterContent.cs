/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/12/2020 10:13:44 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication
{
    public enum TransmissionMode
    {
        /// <summary>
        /// ignore the message when recipient entity is offline
        /// </summary>
        Post,

        /// <summary>
        /// communicate in succession, any error will be reported if the recipient entity is offline
        /// </summary>
        Get,
    }

    /// <summary>
    /// user-oriented intermediate letter class
    /// </summary>
    public class LetterContent
    {
        /// <summary>
        /// title of the letter to be transferred
        /// </summary>
        public readonly string Title;

        /// <summary>
        /// content of the letter to be transferred
        /// </summary>
        public readonly object Content;

        /// <summary>
        /// transmission mode
        /// </summary>
        public TransmissionMode Mode;

        /// <summary>
        /// instantiate a LetterContent
        /// </summary>
        /// <param name="title">title of the letter to be transferred</param>
        /// <param name="content">content of the letter to be transferred</param>
        /// <param name="transmissionMode">transimission mode</param>
        public LetterContent(string title, object content = null, TransmissionMode transmissionMode = TransmissionMode.Post)
        {
            this.Title = title;
            this.Content = content;
            this.Mode = transmissionMode;
        }
    }
}
