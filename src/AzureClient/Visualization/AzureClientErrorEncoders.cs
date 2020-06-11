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
                ["errorCode"] = System.Convert.ToInt32(error),
                ["errorName"] = error.ToString(),
                ["errorDescription"] = error.ToDescription(),
            };
    }

    public class AzureClientErrorToHtmlEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable) => (displayable as AzureClientError?)?.ToDescription().ToEncodedData();
    }

    public class AzureClientErrorToTextEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable) => (displayable as AzureClientError?)?.ToDescription().ToEncodedData();
    }

    public class AzureClientErrorJsonConverter : JsonConverter<AzureClientError>
    {
        public override AzureClientError ReadJson(JsonReader reader, Type objectType, AzureClientError existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, AzureClientError value, JsonSerializer serializer) =>
            JToken.FromObject(value.ToDictionary()).WriteTo(writer);
    }
}
