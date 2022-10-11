// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text;

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
/// Encodes a <see cref="ResourceEstimationResult"/> object as HTML.
/// </summary>
public record class ResourceEstimationToHtmlEncoder(ILogger? Logger = null) : IResultEncoder
{
    /// <inheritdoc/>
    public string MimeType => MimeTypes.Html;

    /// <inheritdoc/>
    public EncodedData? Encode(object displayable)
    {
        if (displayable is ResourceEstimationResult result)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(@"
                    <style>
                        td:first-child {
                            font-weight: bold
                        }

                        .aqre-tooltip {
                            position: relative;
                            border-bottom: 1px dotted black;
                        }

                        .aqre-tooltip .aqre-tooltiptext {
                            visibility: hidden;
                            width: 200px;
                            background-color: black;
                            color: #fff;
                            text-align: center;
                            border-radius: 6px;
                            padding: 5px 0;
                            position: absolute;
                            z-index: 1;
                            top: 150%;
                            left: 50%;
                            margin-left: -100px;
                        }

                        .aqre-tooltip .aqre-tooltiptext::after {
                            content: "";
                            position: absolute;
                            bottom: 100%;
                            left: 50%;
                            margin-left: -5px;
                            border-width: 5px;
                            border-style: solid;
                            border-color: transparent transparent black transparent;
                        }

                        .aqre-tooltip:hover .aqre-tooltiptext {
                            visibility: visible;
                        }
                    </style>");
                var groups = result.GetValueFromPath("reportData/groups");
                foreach (var group in groups)
                {
                    sb.AppendLine($@"
                        <details {(group.Value<bool>("alwaysVisible") ? "open" : "")}>
                            <summary style='display:list-item'>
                                {group["title"]}
                            </summary>
                            <table>
                    ");
                    foreach (var entry in group.GetValue<JArray>("entries"))
                    {
                        var value = result.GetValueFromPath(entry.GetValue<string>("path"));
                        sb.AppendLine($@"
                            <tr class=aqre-tooltip>
                                <td>{entry["label"]}</td>
                                <td class=aqre-tooltip>{value}<span class=aqre-tooltiptext>{entry["description"]}</span></td>
                            </tr>");
                    }
                    sb.AppendLine("</table></details>");
                }
                sb.AppendLine(@"<details>
                        <summary style='display:list-item'>Assumptions</summary>
                        <ul>");
                foreach (var assumption in result.GetValueFromPath("reportData/assumptions"))
                {
                    sb.AppendLine($"<li>{assumption}</li>");
                }
                sb.AppendLine("</ul></details>");
                return sb.ToString().ToEncodedData();
            }
            catch (Exception e)
            {
                Logger?.LogError($"Failed to deserialize resource estimator output. {e.Message}");
                return AzureClientError.JobOutputDisplayFailed.ToDescription().ToEncodedData();
            }
        } 
        else return null;
    }
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