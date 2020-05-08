# EntityOrientedCommunication
A C/S framework library that allows object2object communication based on TCP/IP.

## 0.Quick Start
Assume there are 2 objects **A** and **B** on different computers, now they need to send messages to each other.

- - -

### 0.1 Server Console
Firstly we need to create a server console application, the code of the **Mail(string[])** is shown as follwing.

```c#
using System.Threading;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Server;

namespace ServerDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
        	// create a server named 'EOCServerDemo' at 127.0.0.1:1350
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
```

Now the code of server program is done, we will get a server execution file after the code is compiled, execute the file to run the server.

- - -

### 0.2 Client Console

Similiar to section 0.1, we need to create a client console application firstly. Then declare a class that implements the interface named **IEntity** in the application. In order to make this quick start conciser, we suppose the objects **A** and **B** are of same type named **SignalStation**.

```c#
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

        public object Pickup(ILetter letter)
        {
            Console.WriteLine($"{this.EntityName} received message from {letter.Sender}: {letter.Title}, {letter.Content}");
            return null;
        }
    }
}
```

Hitherto we have a class named **SignalStation** in the client console assembly, then code the **Mail(string[])** function of client program.

```c#
using EntityOrientedCommunication.Client;
using System;

namespace ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            /****** A@Mary START ******/  // Code block 1
            var agent1 = new ClientAgent("127.0.0.1", 1350);  // create a client agent with specified server IP and port
            agent1.Login("Mary", "", 10000);  // login with account 'Mary' without password
            var objA = new SignalStation("A");  // create a 'SignalStation' instance named 'A'
            var boxA = agent1.PostOffice.Register(objA);  // register a mailbox for 'A' to grant it to communicate with other entities
            /****** A@Mary END ******/

            /****** B@Tom START ******/  // Code block 2
            var agent2 = new ClientAgent("127.0.0.1", 1350);
            agent2.Login("Tom", "", 10000);
            var objB = new SignalStation("B");
            var boxB = agent2.PostOffice.Register(objB);
            /****** B@Tom END ******/

            // 'B' send a message to 'A'
            boxB.Send("A@Mary", "hello A!", "put the content here.");

            Console.ReadKey();
        }
    }
}
```

Certainly the code block 1 and 2 could be put at two diffrenct applications, and the two applications could run on different computers. Just make the agents point to same server if you want to connect the objects.

- - -

### 0.3 Execution results

The execution result of the client console seems like following.

![Client console execution result](https://github.com/chrisking94/EntityOrientedCommunication/raw/master/.img/client_console_snapshot.png "Client console execution result")

And similiar the server console is.

![Server console execution result](https://github.com/chrisking94/EntityOrientedCommunication/raw/master/.img/server_console_snapshot.png "Server console execution result")