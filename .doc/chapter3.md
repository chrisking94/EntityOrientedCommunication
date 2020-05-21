## 4. Entity

**Entity** is one of the primal components in EOC which users need to care about. It is a kind of object that has its own name and is able to process messages from another entity. In the EOC library, entity is defined as the C# interface -- 'IEntity'.

```c#
namespace EntityOrientedCommunication
{
    public interface IEntity
    {
        string EntityName { get; }
        
        LetterContent Pickup(ILetter letter);  // message handler
    }
}
```

