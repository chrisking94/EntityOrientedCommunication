using System.Threading;
using ServerDemo;

namespace EntityOrientedCommunication.Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            var server = new Server("EOCServerDemo", "127.0.0.1", 1350);

            // add 200 test users
            var testUCount = 200;
            for (var i = 0; i < testUCount; ++i)
            {
                var user = new UserInfo($"user{i}");

                server.UserManager.Update(user);
            }

            server.Run();

            while (true) Thread.Sleep(1);
        }
    }
}
