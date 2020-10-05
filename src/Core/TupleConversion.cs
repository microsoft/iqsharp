// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Quantum.Simulation.Core;
using System.Diagnostics;
using System.Text;
using System.IO;
using Newtonsoft.Json.Converters;
using Microsoft.Quantum.Simulation.Simulators;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Quantum.IQSharp
{
    public static class JsonConverters
    {
        private static readonly ImmutableList<JsonConverter> tupleConverters;
        private static readonly ImmutableList<JsonConverter> otherConverters;
        public static JsonConverter[] TupleConverters => tupleConverters.ToArray();

        public static JsonConverter[] AllConverters => tupleConverters.Concat(otherConverters).ToArray();

        static JsonConverters()
        {
            tupleConverters = new JsonConverter[] {
                new QTupleConverter(),
                new QVoidConverter(),
                new UDTConverter(),
                new ResultConverter()
            }.ToImmutableList();
            otherConverters = ImmutableList.Create<JsonConverter>(
                new ResourcesEstimatorConverter()
            );
        }

        /// <summary>
        ///  A helper method to read a json object and return it as a dictionary.
        ///  Only the immediate elements of the object are used as keys. Their values
        ///  are returned as json objects themselves.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> JsonToDict(string json)
        {
            var result = new Dictionary<string, string>();

            var args = JObject.Parse(json);
            foreach (var a in args)
            {
                result.Add(a.Key, a.Value?.ToString(Formatting.None));
            }

            return result;
        }
    }

    public class UDTConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType) =>
            objectType.IsSubclassOfGenericType(typeof(UDTBase<>));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Create an instance of the base Data type and populate it with the jObject:
            var data = Activator.CreateInstance(objectType.GetProperty("Data").PropertyType);
            serializer.Populate(reader, data);
            return Activator.CreateInstance(objectType, data);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var objectType = value.GetType();
            var dataType = objectType.GetProperty("Data").PropertyType;
            var tempWriter = new StringWriter();
            serializer.Serialize(tempWriter, objectType.GetProperty("Data").GetValue(value), dataType);
            var token = JToken.Parse(tempWriter.ToString());
            token["@type"] = objectType.FullName;
            token.WriteTo(writer);
        }
    }

    public class QTupleConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(ValueTuple)) return true;

            // If we've survived, we either have a nongeneric type which isn't
            // a value tuple, or we have a generic value tuple.
            if (!objectType.IsGenericType) return false;

            // Now we can compare the generic type to each possible pattern for
            // value tuples.
            var genericType = objectType.GetGenericTypeDefinition();
            return genericType == typeof(ValueTuple<>)
                || genericType == typeof(ValueTuple<,>)
                || genericType == typeof(ValueTuple<,,>)
                || genericType == typeof(ValueTuple<,,,>)
                || genericType == typeof(ValueTuple<,,,,>)
                || genericType == typeof(ValueTuple<,,,,,>)
                || genericType == typeof(ValueTuple<,,,,,,>)
                || genericType == typeof(ValueTuple<,,,,,,,>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var tokenData = new Dictionary<string, object>
            {
                ["@type"] = "@tuple"
            };

            var itemOffset = 0;
            while (true)
            {
                var type = value.GetType();
                var nItems = type.GenericTypeArguments.Length;

                // For tuples of more than 7 items, the .NET type is ValueTuple<T1,T2,T3,T4,T5,T6,T7,TRest>
                const int maxTupleLength = 7;
                foreach (var idx in Enumerable.Range(0, System.Math.Min(nItems, maxTupleLength)))
                {
                    var field = type.GetField($"Item{idx + 1}");
                    Debug.Assert(field != null, $"Failed trying to look at field Item{idx + 1} of a value tuple with {nItems} type arguments, {type.FullName}.");
                    tokenData[$"Item{idx + itemOffset + 1}"] = field.GetValue(value);
                }

                if (nItems <= maxTupleLength)
                {
                    break;
                }

                value = type.GetField("Rest").GetValue(value);
                itemOffset += maxTupleLength;
            }

            // See https://github.com/JamesNK/Newtonsoft.Json/issues/386#issuecomment-421161191
            // for why this works to pass through.
            var token = JToken.FromObject(tokenData, serializer);
            token.WriteTo(writer);
        }
    }

    public class ResourcesEstimatorConverter : JsonConverter<ResourcesEstimator>
    {
        public override ResourcesEstimator ReadJson(JsonReader reader, Type objectType, [AllowNull] ResourcesEstimator existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ResourcesEstimator value, JsonSerializer serializer) =>
            JToken
            .FromObject(value.AsDictionary())
            .WriteTo(writer);
    }

    public class QVoidConverter : JsonConverter<QVoid>
    {
        public override QVoid ReadJson(JsonReader reader, Type objectType, QVoid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, QVoid value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(new Dictionary<string, object>
            {
                ["@type"] = "tuple"
            });
            token.WriteTo(writer);
        }
    }

    public class ResultConverter : JsonConverter<Result>
    {
        public override Result ReadJson(JsonReader reader, Type objectType, Result existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Result value, JsonSerializer serializer)
        {
            // See https://github.com/JamesNK/Newtonsoft.Json/issues/386#issuecomment-421161191
            // for why this works to pass through.
            var token = JToken.FromObject(value.GetValue(), serializer);
            token.WriteTo(writer);
        }
    }

}
