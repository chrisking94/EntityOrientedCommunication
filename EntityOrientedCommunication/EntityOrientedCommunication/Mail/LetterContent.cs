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
        /// ignore all errors while transferring
        /// </summary>
        Post,

        /// <summary>
        /// communicate in succession, any error during transmission will be reported
        /// </summary>
        Get,
    }

    /// <summary>
    /// user-oriented intermediate letter class
    /// </summary>
    public class LetterContent
    {
        public readonly string Title;

        public readonly object Content;

        public TransmissionMode Mode;

        public LetterContent(string title, object content = null, TransmissionMode transmissionMode = TransmissionMode.Post)
        {
            this.Title = title;
            this.Content = content;
            this.Mode = transmissionMode;
        }
    }
}
