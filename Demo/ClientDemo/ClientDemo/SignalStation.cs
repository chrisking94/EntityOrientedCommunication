/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/8/2020 3:49:42 PM					
\********************************************************/
using EntityOrientedCommunication;
using System;


namespace ClientDemo
{
    class SignalStation : IEntity
    {
        public string EntityName { get; }

        public SignalStation(string name)
        {
            this.EntityName = name;
        }

        public LetterContent Pickup(ILetter letter)
        {
            Console.WriteLine($"{this.EntityName} received message from {letter.Sender}: title={letter.Title}, content={letter.Content}");
            return new LetterContent("HAHAHAHA");
        }
    }
}
