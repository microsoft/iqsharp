using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Implements the RuntimeSettings.
    /// In its constuctor it receives an IConfiguration class with the
    /// set of default values.
    /// If values change at runtime, the SettingSet event is triggered.
    /// Notice these chages are only kept in memory.
    /// </summary>
    public class RuntimeSettings : IRuntimeSettings
    {
        public static readonly string SETTING_USER_AGENT = "USER_AGENT";
        public static readonly string SETTING_HOSTING_ENV = "HOSTING_ENV";

        private readonly Dictionary<string, string> _storage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public RuntimeSettings(IConfiguration settings)
        {
            if (settings != null)
            {
                foreach (var (key, value) in Flatten(settings))
                {
                    _storage[key] = value;
                }
            }
        }

        public event EventHandler<SettingSetEventArgs> SettingSet;

        public string this[string key]
        {
            get => _storage.ContainsKey(key) ? _storage[key] : null;

            set
            {
                _storage[key] = value;
                SettingSet?.Invoke(this, new SettingSetEventArgs(key, value));
            }
        }

        public IEnumerable<(string, string)> All =>
            _storage.Select(kv => (kv.Key, kv.Value));

        public static IEnumerable<(string, string)> Flatten(IConfiguration settings)
        {
            foreach (var child in settings.GetChildren())
            {
                if (child.GetChildren().Any())
                {
                    yield return (child.Key, string.Join(";", child.GetChildren().Select(c => c.Value)));
                }
                else
                {
                    yield return (child.Key, child.Value);
                }
            }
        }
    }
}
