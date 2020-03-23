// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    /// Extensions for the Jupyter Core Message
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// Deserializes the message content into the requested type.
        /// </summary>
        /// <typeparam name="T">Type to deserialize the content of the message as</typeparam>
        /// <param name="message">The Jupyter core message</param>
        /// <returns>The message deserialized as the requested type</returns>
        public static T To<T>(this Message message)
            where T : MessageContent
        {
            var content = (message.Content as UnknownContent);
            var result = Activator.CreateInstance<T>();
            foreach (var property in typeof(T).GetProperties().Where(p => p.CanWrite))
            {
                var jsonPropertyAttribute = property.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().FirstOrDefault();
                var propertyName = jsonPropertyAttribute?.PropertyName ?? property.Name;
                if (content.Data.TryGetValue(propertyName, out var value))
                {
                    property.SetValue(result, content.Data[propertyName]);
                }
            }
            return result;
        }
    }
}
