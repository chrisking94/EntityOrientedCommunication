# EntityOrientedCommunication
A C/S framework library which enables object2object communication based on TCP/IP written in C#. 'EntityOrientedCommunication' is Called 'EOC' for short.

<!--![EOC Demo Ani](https://github.com/chrisking94/EntityOrientedCommunication/blob/master/.doc/EOCDemo_Ani.gif?raw=true "EOC Demo Ani")-->

## Highlights
:star: Easy to use
:star: Lightweight
:star: Flexible

## Table of Contents
1. [Quick Start](#QuickStart) </br>
   1.1 [Server Console](#section1_1) </br>
   1.2 [Client Console](#section1_2) </br>
   1.3 [Excution Results](#section1_3) </br>
2. [Introduction](./.doc/chapter2.md) </br>
3. [API Reference](https://chrisking94.github.io/EntityOrientedCommunication/api/index.html) </br>
4. [FAQ](./.doc/chapter4.md) </br>
<!-- 5. [Client Postoffice](./.doc/chapter5.md) </br> -->
<!-- 6. [Client Mailbox](./.doc/chapter6.md) </br> -->


<a name="QuickStart"></a>
## 1.Quick Start
Assume there are 2 objects **A** and **B** on different computers, now they need to send messages(objects of any type) to each other.

* It is able to use the EOC libraray without downloading source code of which. Since the compiled library has been published on [nuget.org](nuget.org), part of users might be pleased to use **EntityOrientedCommunication** library by installing the nuget package through some facilities. e.g. **PackageManager**.
```code
PM> Install-Package EntityOrientedCommunication -Source nuget.org
```

- - -
<a name="section1_1"></a>
### 1.1 Server Console
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

            // append 2 users without password
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

<a name="section1_2"></a>
### 1.2 Client Console

Similiar to section 1.1, we need to create a client console application first. Then declare a class that implements the interface named **IEntity** in the application. In order to make this quick start conciser, let's suppose the objects **A** and **B** are of same type named **SignalStation**.

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

        public LetterContent Pickup(ILetter letter)  // handle the incoming message
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
            boxB.Post("A@Mary", "hello A!", "put the content here.");

            Console.ReadKey();
        }
    }
}
```

Certainly the code block 1 and 2 could be placed on two diffrenct applications, and the two applications could run on different computers. Just make the agents point to same server if you want to connect the entities.

Sometimes an entity might want to send an object to another entity, please mark [SerializableAttribute] to the class of the object which is going to be sent.

For example:
```c#
using System;

namespace ClientDemo
{
    [Serializable]
    class Person
    {
        public int Age;

        public string Name;

        public Person(int age, string name)
        {
            this.Age = age;
            this.Name = name;
        }

        public override string ToString()
        {
            return $"Name: {this.Name}, Age: {this.Age}";
        }
    }
}
```

Then post a message of type 'Person' to the remote entity.

```c#
boxA.Post("B@Tom", "hello B, this is a information card of Jerry!", new Person(20, "Jerry"));
```

- - -

<a name="section1_3"></a>
### 1.3 Execution Results

The execution result of the client console seems like following.

![Client console execution result](https://github.com/chrisking94/EntityOrientedCommunication/blob/master/.doc/client_console_snapshot.png?raw=true "Client console execution result")

And similiar the server console is.

![Server console execution result](https://github.com/chrisking94/EntityOrientedCommunication/blob/master/.doc/server_console_snapshot.png?raw=true "Server console execution result")