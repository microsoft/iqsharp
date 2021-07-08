// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Microsoft.Quantum.IQSharp.Kernel
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
            if (message.Content is UnknownContent content)
            {
                var result = Activator.CreateInstance<T>();
                foreach (var property in typeof(T).GetProperties().Where(p => p.CanWrite))
                {
                    var jsonPropertyAttribute = property.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().FirstOrDefault();
                    var propertyName = jsonPropertyAttribute?.PropertyName ?? property.Name;
                    var propertyType = property.PropertyType;
                    if (content.Data.TryGetValue(propertyName, out var value))
                    {
                        var data = content.Data[propertyName];
                        // If the unknown content's data is a JToken, that
                        // indicates that we need to further deserialize it.
                        if (data is JToken tokenData)
                        {
                            property.SetValue(result, tokenData.ToObject(propertyType));
                        }
                        else
                        {
                            property.SetValue(result, data);
                        }
                    }
                }
                return result;
            }
            else if (message.Content is T decoded)
            {
                return decoded;
            }
            else
            {
                throw new Exception($"Attempted to convert a message with content type {message.Content.GetType()} to content type {typeof(T)}; can only convert from unknown content or subclasses of the desired content type.");
            }
        }
    }
}
