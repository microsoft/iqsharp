// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Quantum.QsCompiler;
using Newtonsoft.Json;
using static Microsoft.Quantum.QsCompiler.TargetCapabilityModule;

namespace Microsoft.Quantum.IQSharp.AzureClient;

/// <summary>
///     Writes values of type <see cref="TargetCapability"/> to JSON.
/// </summary>
public class TargetCapabilityConverter : JsonConverter<TargetCapability>
{
    /// <inheritdoc/>
    public override TargetCapability? ReadJson(JsonReader reader, Type objectType, TargetCapability? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, TargetCapability? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
            if (Name(value).AsObj() is {} name)
            {
                writer.WritePropertyName(nameof(value.Name));
                writer.WriteValue(name);
            }

            writer.WritePropertyName(nameof(value.ClassicalCompute));
            writer.WriteValue(value.ClassicalCompute.ToString());

            writer.WritePropertyName(nameof(value.ResultOpacity));
            writer.WriteValue(value.ResultOpacity.ToString());
        writer.WriteEndObject();
    }
}

/// <summary>
///     Formats target capability information for display in notebooks and
///     other rich-text contexts.
/// </summary>
public class TargetCapabilityToHtmlEncoder : IResultEncoder
{
    /// <inheritdoc/>
    public string MimeType => MimeTypes.Html;

    /// <inheritdoc/>
    public EncodedData? Encode(object displayable) =>
        displayable is TargetCapability capability
        ? $@"
            <table>
                <caption>Target capability {(TargetCapabilityModule.Name(capability).AsObj() is {} n ? $"<tt>{n.Trim()}</tt>" : "")}</caption>
                <tr>
                    <th>Classical computation</th>
                    <td>{capability.ClassicalCompute}</td>
                </tr>
                <tr>
                    <th>Result opacity</th>
                    <td>{capability.ResultOpacity}</td>
                </tr>
            </table>
          ".ToEncodedData()
        : (EncodedData?)null;
}
