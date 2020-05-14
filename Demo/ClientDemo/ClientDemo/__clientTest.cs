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

        static void DuplicateLoginTest()
        {
            /****** A@Mary START ******/  // Code block 1
            var postoffice1 = new ClientPostOffice();
            var agent1 = postoffice1.Connect("127.0.0.1", 1350);  // create a client agent with specified server IP and port
            agent1.Login("Mary", "", 10000);  // login with account 'Mary' without password
            /****** A@Mary END ******/

            /****** B@Tom START ******/  // Code block 2
            var postoffice2 = new ClientPostOffice();
            var agent2 = postoffice2.Connect("127.0.0.1", 1350);
            agent2.Login("Mary", "", 10000);

            var mb = postoffice1.Register(new Entity("CBV"));

            Console.ReadLine();
        }

        public static void __main__()
        {
            DuplicateLoginTest();

            var office = new ClientPostOffice("");
            var agent = office.Connect("127.0.0.1", 1350);

            var ea = new Entity("A");
            var mba = office.Register(ea);

            mba.Get("A@localhost", "hellpA", "fire in the hole");

            Console.ReadKey(false);
        }
    }
}
