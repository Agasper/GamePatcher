using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Patcher
{
    public class PatcherException : System.Exception
    {

        public PatcherException()
        {
        }

        public PatcherException(string message)
            : base(message)
        {
        }
    }
}
