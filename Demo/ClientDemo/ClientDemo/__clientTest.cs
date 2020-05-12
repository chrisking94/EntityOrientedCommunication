/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/11/2020 11:58:43 AM					
\********************************************************/
using EntityOrientedCommunication;
using EntityOrientedCommunication.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDemo
{
    class __clientTest
    {
        public class Entity : IEntity
        {
            public string EntityName { get; }

            public Entity(string name)
            {
                this.EntityName = name;
            }

            public LetterContent Pickup(ILetter letter)
            {
                Console.WriteLine($"new letter received, title={letter.Title}, content={letter.Content}");

                return null;
            }
        }

        public static void __main__()
        {
            var agent = new ClientAgent("127.0.0.1", 1350);

            var ea = new Entity("A");
            var mba = agent.PostOffice.Register(ea);

            mba.Get("A@localhost", "hellpA", "fire in the hole");

            Console.ReadKey(false);
        }
    }
}
