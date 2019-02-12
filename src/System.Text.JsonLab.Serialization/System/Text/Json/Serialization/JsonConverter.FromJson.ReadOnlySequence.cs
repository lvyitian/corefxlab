﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json.Serialization
{
    public static partial class JsonConverter
    {
        public static T FromJson<T>(this in ReadOnlySequence<byte> json, JsonConverterSettings settings = null)
        {
            return (T)FromJson(json, typeof(T), settings);
        }

        public static object FromJson(this in ReadOnlySequence<byte> json, Type returnType, JsonConverterSettings settings = null)
        {
            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            if (settings == null)
                settings = s_DefaultSettings;

            var state = new JsonReaderState(settings.MaxDepth, settings.ReaderOptions);
            var reader = new Utf8JsonReader(json, isFinalBlock: true, state);
            return FromJson(reader, returnType, settings);
        }
    }
}
