// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Quantum.IQSharp;

internal static class Extensions
{
    internal static async Task<Dictionary<string, string>> ReadAsJsonDict(this HttpRequest request)
    {
        using var body = new StreamReader(request.Body);
        var json = await body.ReadToEndAsync();
        return JsonConverters.JsonToDict(json);
    }
}