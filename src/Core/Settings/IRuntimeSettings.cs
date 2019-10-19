using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// List of arguments for the SettingUpdated event.
    /// </summary>
    public class SettingSetEventArgs : EventArgs
    {
        public SettingSetEventArgs(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// The key of the setting updated.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The value of the setting updated.
        /// </summary>
        public string Value { get; }
    }

    /// <summary>
    ///     Runtime settings for global execution values that can be configured by users.
    ///     It provides a flat list of key=value elements.
    /// </summary>
    public interface IRuntimeSettings
    {
        /// <summary>
        /// This event is triggered for each setting that gets updated at runtime.
        /// </summary>
        event EventHandler<SettingSetEventArgs> SettingSet;

        /// <summary>
        ///     Reads/sets a single value.
        /// </summary>
        string this[string key] { get; set; }

        /// <summary>
        /// Returns all existing values.
        /// </summary>
        IEnumerable<(string, string)> All { get; }
    }
}
