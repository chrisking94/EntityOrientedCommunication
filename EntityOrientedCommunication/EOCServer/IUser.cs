using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EOCServer
{
    public interface IUser
    {
        long ID { get; }

        string Name { get; }

        string Password { get; }

        string NickName { get; }

        object Detail { get; }
    }
}
