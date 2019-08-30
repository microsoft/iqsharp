using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    public interface IRuntimeSettings
    {
        IEnumerable<(string, string)> All();

        string One(string key);

        void Set(string key, string value);
    }
}
