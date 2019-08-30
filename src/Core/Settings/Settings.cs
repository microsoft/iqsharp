using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    public class RuntimeSettings : IRuntimeSettings
    {
        private Dictionary<string, string> storage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<(string, string)> All() =>
            storage.Select(kv => (kv.Key, kv.Value));

        public string One(string key) =>
            storage.ContainsKey(key) ? storage[key] : null;

        public void Set(string key, string value) =>
            storage[key] = value;
    }
}
