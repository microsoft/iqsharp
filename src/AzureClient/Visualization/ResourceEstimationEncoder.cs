// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using Markdig;

namespace Microsoft.Quantum.IQSharp.AzureClient;

/// <summary>
/// The results of a resource estimation job.
/// </summary>
public record class ResourceEstimationResult(JToken RawJson)
{
    internal JToken GetValueFromPath(string path)
    {
        var value = RawJson;
        foreach (var p in path.Split("/"))
        {
            value = value.Value<JToken>(p) is {} newValue
                ? newValue
                : throw new JsonException($"Malformed JSON. Failed at '{p}' to retrieve value for '{path}'");
        }
        return value;
    }
}

internal static class ResourceEstimationResultExtensions
{
    internal static ResourceEstimationResult ToResourceEstimationResults(this Stream stream) =>
        new ResourceEstimationResult(JToken.Parse(new StreamReader(stream).ReadToEnd()));

    internal static T GetValue<T>(this JToken token, object key) =>
        token.Value<T>(key) is {} newValue
            ? newValue
            : throw new Exception($"Malformed JSON. Failed to retrieve value for '{key}' from '{token.Path}'");
}

/// <summary>
/// Encodes a <see cref="ResourceEstimationResult"/> object as JSON.
/// </summary>
public class ResourceEstimationResultConverter : JsonConverter<ResourceEstimationResult>
{
    /// <inheritdoc/>
    public override ResourceEstimationResult ReadJson(JsonReader reader, Type objectType, ResourceEstimationResult? existingValue, bool hasExistingValue, JsonSerializer serializer)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, ResourceEstimationResult? value, JsonSerializer serializer)
    {
        if (value != null) JToken.FromObject(value.RawJson).WriteTo(writer);
    }
}