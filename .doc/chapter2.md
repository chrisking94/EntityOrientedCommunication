## 2. Introduction

**Distributed Objects** are objects (in the sense of OOP) that are distributed across different address spaces, either in different processes on the same computer, or even in multiple computers connected via a network, but which work together by sharing data and invoking methods.

**Entity Oriented Communication** (**EOC** for short) is a kind of distributed object framework that make the communication between objects in diffrent processes possible. Specially, EOC is much more lightweight and flexible than traditional distributed object frameworks. There is only a *DLL* you need when you want to create a distributed system through EOC; and the method invocation between objects is not concealed, EOC only transfers message between objects, you can determine what to invoke and what to reply when an object received a message from the remote object.

As shown in the figure blow, EOC consists of 2 parts, respectively are **Server** and **Client**.

![Message flow of EOC](https://github.com/chrisking94/EntityOrientedCommunication/blob/master/.doc/MessageFlow.jpg?raw=true "Message flow of EOC")

The the main components of **server** are **Mail Center** and **Server Postoffice**. Mail center is responsible for routing messages passed from the server postoffice. While server postoffice records the state of entities in the corresponding client, meanwhile dispatches the messages issued by mail center to the client and pass messages sent from the client to mail center.

The **client** includes **Client PostOffice** and **Mailbox**. Mailbox is a communication device provided for entity, each entity has its own mailbox which could be used to send message to the remote entity, and when a mailbox receives a message from remote entity, it will pass the message to the entity it binds with. Client postoffice is a manager to the mailboxes, it route the messages from the server to the target mailboxes, and dispatch the messages from the mailboxes to the server.

Specially there is a **RAM** connection between 'ServerPostOffice' and 'ClientPostOffice' in the figure above. The reason is that the 'ClientPostOffice' is a local client inside the server application, which connects server through memory to provide high performance communication. Usually it is used to offer some services in the server application.
