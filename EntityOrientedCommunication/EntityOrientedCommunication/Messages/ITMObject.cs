using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Messages
{
    public interface ITMObject<T>
    {
        T Object { get; }
    }
}
