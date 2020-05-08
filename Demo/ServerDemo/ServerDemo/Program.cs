using System.Threading;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Server;

namespace ServerDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            var server = new Server("EOCServerDemo", "127.0.0.1", 1350);

            // create 2 users without password
            server.MailCenter.Update(new User() { Name = "Mary" });
            server.MailCenter.Update(new User() { Name = "Tom" });

            // run the server
            server.Run();

            while (true) Thread.Sleep(1);
        }
    }
}
