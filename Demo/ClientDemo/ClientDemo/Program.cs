using EntityOrientedCommunication.Client;
using System;

namespace ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            __clientTest.__main__();

            /****** A@Mary START ******/  // Code block 1
            var postoffice1 = new ClientPostOffice();
            var agent1 = postoffice1.Connect("127.0.0.1", 1350);  // create a client agent with specified server IP and port
            agent1.Login("Mary", "", 10000);  // login with account 'Mary' without password
            var objA = new SignalStation("A");  // create a 'SignalStation' instance named 'A'
            var boxA = postoffice1.Register(objA);  // register a mailbox for 'A' to grant it to communicate with other entities
            /****** A@Mary END ******/

            /****** B@Tom START ******/  // Code block 2
            var postoffice2 = new ClientPostOffice();
            var agent2 = postoffice2.Connect("127.0.0.1", 1350);
            agent2.Login("Tom", "", 10000);
            var objB = new SignalStation("B");
            var boxB = postoffice2.Register(objB);
            /****** B@Tom END ******/

            // 'B' send a message to 'A'
            //var result = boxB.Get("A@Mary", "hello A!", "put the content here.");
            boxB.Post("A@Mary", "hello A!", "put the content here.");

            Console.ReadKey();
        }
    }
}
