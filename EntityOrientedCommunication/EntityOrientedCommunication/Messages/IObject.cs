using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Messages
{
    internal interface IObject<T>
    {
        T Object { get; }
    }
}
