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
using Experimental = Microsoft.Quantum.Experimental;

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
                new ResultConverter(),

                // Make sure to use the base type for each open systems / CHP
                // state, since these are effectively a discriminated union.
                new DelegatedConverter<Experimental.PureState, Experimental.State>(new Experimental.StateConverter()),
                new DelegatedConverter<Experimental.MixedState, Experimental.State>(new Experimental.StateConverter()),
                new DelegatedConverter<Experimental.StabilizerState, Experimental.State>(new Experimental.StateConverter())
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
                var value = a.Value?.ToString(Formatting.None);
                if (value == null)
                {
                    throw new JsonSerializationException($"JToken for value for property {a.Key} is null; expect JSON null values to be JValue.CreateNull() instead.");
                }
                result.Add(a.Key, value);
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

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // Create an instance of the base Data type and populate it with the jObject:
            var dataProperty = objectType.GetProperty("Data");
            if (dataProperty == null)
            {
                throw new JsonSerializationException($"Attempted to deserialize a UDT, but C# type {objectType} does not have a Data property.");
            }
            var data = Activator.CreateInstance(dataProperty.PropertyType);
            if (data == null)
            {
                throw new JsonSerializationException($"Could not create new instance of type {objectType} using its default parameterless constructor.");
            }
            serializer.Populate(reader, data);
            return Activator.CreateInstance(objectType, data);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                throw new JsonSerializationException("UDTs cannot be null.");
            }
            var objectType = value.GetType();
            var dataProperty = objectType.GetProperty("Data");
            if (dataProperty == null)
            {
                throw new JsonSerializationException($"Attempted to deserialize a UDT, but C# type {objectType} does not have a Data property.");
            }
            var dataType = dataProperty.PropertyType;
            var tempWriter = new StringWriter();
            serializer.Serialize(tempWriter, dataProperty.GetValue(value), dataType);
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
            if (value == null) throw new ArgumentNullException(nameof(value));
            var tokenData = new Dictionary<string, object?>
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

                var rest = type.GetField("Rest")?.GetValue(value);
                Debug.Assert(rest != null, "Tuple to be serialized either had more than 7 type parameters but did not have a Rest property, or had a Rest property whose value is null; this should never happen.");
                value = rest;
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
            (
                value?.AsDictionary() is {} dict
                ? JToken.FromObject(dict)
                : JValue.CreateNull()
            )
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

    /// <summary>
    ///     Delegates JSON conversion from Newtonsoft.Json to a converter
    ///     for System.Text.Json.
    /// </summary>
    internal class DelegatedConverter<TTarget, TDelegated> : JsonConverter<TTarget?>
    where TTarget: class, TDelegated
    {
        private readonly System.Text.Json.Serialization.JsonConverter<TDelegated> converter;
        private readonly System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions();
        public DelegatedConverter(System.Text.Json.Serialization.JsonConverter<TDelegated> converter)
        {
            this.converter = converter;
            this.options.Converters.Add(converter);
        }

        public override TTarget? ReadJson(JsonReader reader, Type objectType, [AllowNull] TTarget existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var asJson = JToken.ReadFrom(reader).ToString();
            var delegated = System.Text.Json.JsonSerializer.Deserialize<TDelegated>(asJson, options);
            return (TTarget?) delegated;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] TTarget value, JsonSerializer serializer)
        {
            var asJson = System.Text.Json.JsonSerializer.Serialize<TDelegated?>(value, options);
            var token = JToken.Parse(asJson);
            token.WriteTo(writer);
        }
    }

}
