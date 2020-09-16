// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Jupyter.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal static class AzureClientErrorExtensions
    {
        /// <summary>
        ///     Returns the string value of the <see cref="DescriptionAttribute"/> for the given
        ///     <see cref="AzureClientError"/> enumeration value.
        /// </summary>
        internal static string ToDescription(this AzureClientError error)
        {
            var attributes = error
                .GetType()
                .GetField(error.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            return attributes?.Length > 0 ? attributes[0].Description : string.Empty;
        }

        /// <summary>
        ///     Returns a dictionary representing the properties of the <see cref="AzureClientError"/>.
        /// </summary>
        internal static Dictionary<string, object> ToDictionary(this AzureClientError error) =>
            new Dictionary<string, object>()
            {
                ["error_code"] = System.Convert.ToInt32(error),
                ["error_name"] = error.ToString(),
                ["error_description"] = error.ToDescription(),
            };
    }

    /// <summary>
    /// Encodes an <see cref="AzureClientError"/> object as HTML.
    /// </summary>
    public class AzureClientErrorToHtmlEncoder : IResultEncoder
    {
        /// <inheritdoc/>
        public string MimeType => MimeTypes.Html;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) => (displayable as AzureClientError?)?.ToDescription().ToEncodedData();
    }

    /// <summary>
    /// Encodes an <see cref="AzureClientError"/> object as plain text.
    /// </summary>
    public class AzureClientErrorToTextEncoder : IResultEncoder
    {
        /// <inheritdoc/>
        public string MimeType => MimeTypes.PlainText;

        /// <inheritdoc/>
        public EncodedData? Encode(object displayable) => (displayable as AzureClientError?)?.ToDescription().ToEncodedData();
    }

    /// <summary>
    /// Encodes an <see cref="AzureClientError"/> object as JSON.
    /// </summary>
    public class AzureClientErrorJsonConverter : JsonConverter<AzureClientError>
    {
        /// <inheritdoc/>
        public override AzureClientError ReadJson(JsonReader reader, Type objectType, AzureClientError existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, AzureClientError value, JsonSerializer serializer) =>
            JToken.FromObject(value.ToDictionary()).WriteTo(writer);
    }
}
