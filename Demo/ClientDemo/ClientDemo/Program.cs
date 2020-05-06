using EntityOrientedCommunication;
using EntityOrientedCommunication.Client;
using EntityOrientedCommunication.Mail;
using System;

namespace TAPACSTest
{
    class ObjectA : IMailReceiver
    {
        public string EntityName => "ObjA";

        public object Pickup(EMLetter letter)
        {
            Console.WriteLine($"{letter.Content}");
            return null;
        }
    }

    class ObjectB : IMailReceiver
    {
        public string EntityName => "ObjB";

        public object Pickup(EMLetter letter)
        {
            Console.WriteLine($"--------------------------{letter.Title}");
            //return null;

            return "this message is from B, the item has been received";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            
            var agent1 = new ClientAgent("127.0.0.1", 1350);
            agent1.Login("user1", "", 10000);
            var agent2 = new ClientAgent("127.0.0.1", 1350);
            agent2.Login("user2", "", 10000);

            var objA = new ObjectA();
            var box1 = agent1.PostOffice.Register(objA);

            var objB = new ObjectB();
            var box2 = agent2.PostOffice.Register(objB);


            //var obj = box1.Get("ObjB@user2", "hello objB", null);
            var obj = box1.Get("entityA@server", "hello objB", null);


            Console.ReadKey();
        }
    }
}
