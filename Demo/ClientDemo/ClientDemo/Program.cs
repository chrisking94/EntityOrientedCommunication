using ClientDemo;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Client;
using EntityOrientedCommunication.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TAPACSTest
{
    class Program
    {
        private static void ClientEventListener(object sender, ClientAgentEventArgs args)
        {
            if(args.EventType.HasFlag(ClientAgentEventType.Error))
            {
                throw new Exception($"{args.Title}: {args.Message}");
            }
        }

        private class ObjectA : IMailReceiver
        {
            public string EntityName => "ObjA";

            public void Pickup(TMLetter letter)
            {
                Console.WriteLine($"{letter.Title}");
            }
        }

        private class ObjectB : IMailReceiver
        {
            public string EntityName => "ObjB";

            public void Pickup(TMLetter letter)
            {
                Console.WriteLine($"{letter.Title}");
            }
        }

        static void Main(string[] args)
        {
            
            var agent1 = TAPAClient.Agent;
            agent1.Login("user1", "", 10000);
            var agent2 = new ClientLoginAgent(TAPAClient.DefaultIP, TAPAClient.DefaultPort);
            agent2.Login("user2", "", 10000);

            var objA = new ObjectA();
            var box1 = agent1.PostOffice.Register(objA);

            var objB = new ObjectB();
            var box2 = agent2.PostOffice.Register(objB);

            Thread.Sleep(1000);

            box1.Send("ObjB@user2", "hello objB", null);


            Console.ReadKey();
        }
    }
}
