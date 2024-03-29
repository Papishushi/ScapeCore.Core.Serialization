﻿/*
 * -*- encoding: utf-8 with BOM -*-
 * .▄▄ ·  ▄▄·  ▄▄▄·  ▄▄▄·▄▄▄ .     ▄▄·       ▄▄▄  ▄▄▄ .
 * ▐█ ▀. ▐█ ▌▪▐█ ▀█ ▐█ ▄█▀▄.▀·    ▐█ ▌▪▪     ▀▄ █·▀▄.▀·
 * ▄▀▀▀█▄██ ▄▄▄█▀▀█  ██▀·▐▀▀▪▄    ██ ▄▄ ▄█▀▄ ▐▀▀▄ ▐▀▀▪▄
 * ▐█▄▪▐█▐███▌▐█ ▪▐▌▐█▪·•▐█▄▄▌    ▐███▌▐█▌.▐▌▐█•█▌▐█▄▄▌
 *  ▀▀▀▀ ·▀▀▀  ▀  ▀ .▀    ▀▀▀     ·▀▀▀  ▀█▄▀▪.▀  ▀ ▀▀▀ 
 * https://github.com/Papishushi/ScapeCore
 * 
 * Copyright (c) 2023 Daniel Molinero Lucas
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 * 
 * ScapeCoreserializer.cs
 * This is the default serializer used in ScapeCore. Includes
 * all the logic used for serialize and compress objects at 
 * runtime.
 */

using ProtoBuf.Meta;
using System;
using System.IO;

using static ScapeCore.Core.Debug.Debugger;

namespace ScapeCore.Core.Serialization.Streamers
{
    public sealed class ScapeCoreSerializer : ScapeCoreSeralizationStreamer , IScapeCoreSerializer
    {
        public string Name { get; } = "ScapeCoreSerializer";

        private readonly Guid _id = Guid.NewGuid();
        public Guid Id => _id;

        public ScapeCoreSerializer(RuntimeTypeModel model, int gzipBufferSize, string binName, string compressedBinName) :
            base(binName, compressedBinName, model, gzipBufferSize)
        { }

        public readonly record struct SerializationOutput(Type Type, SerializationError Error, byte[]? Data, long Size, string Path, bool Compressed);

        private SerializationOutput SerializeToPath(Type type, string path, bool compress, object? obj)
        {
            byte[]? data = null;
            SerializationOutput output;

            using (MemoryStream ms = new MemoryStream())
            {
                _model!.SerializeWithLengthPrefix(ms, obj, type, ProtoBuf.PrefixStyle.Base128, 0);
                data = ms.ToArray();
            }

            if (compress)
                data = Compress(data);

            var size = data.Length;

            using (var writer = File.Open(Path.Combine(path, GetFileName(type, compress)), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                writer.Write(data, 0, size);
            }

            SCLog.Log(VERBOSE, $"Serialized {size} bytes from {type} into {path}");

            output = new() { Type = type, Error = SerializationError.None, Data = data, Size = size, Path = path, Compressed = compress };
            return output;
        }
        private SerializationOutput SerializeToMemory(Type type, bool compress, object? obj)
        {
            byte[] data;
            SerializationOutput output;

            using (MemoryStream ms = new MemoryStream())
            {
                _model!.SerializeWithLengthPrefix(ms, obj, type, ProtoBuf.PrefixStyle.Base128, 0);
                data = ms.ToArray();
            }

            if (compress)
                data = Compress(data);

            var size = data.Length;

            SCLog.Log(VERBOSE, $"Serialized {size} bytes from {type}");

            output = new() { Type = type, Error = SerializationError.None, Data = data, Size = size, Path = string.Empty, Compressed = compress };
            return output;
        }

        public SerializationOutput Serialize<T>(T obj, string path, bool compress = false)
        {
            if (CheckForSerializationErrors(SERIALIZATION_ERROR_FORMAT, typeof(T), path, compress, out var output)) return new() { Error = output ?? default, Data = default, Size = 0, Path = path, Compressed = compress };
            if (string.IsNullOrEmpty(path)) return new() { Error = SerializationError.NullPath, Data = default, Size = 0, Path = path, Compressed = compress };
            try
            {
                return SerializeToPath(typeof(T), path, compress, obj);
            }
            catch (Exception ex)
            {
                return new() { Error = HandleSerializationError(SERIALIZATION_ERROR_FORMAT, path, ex), Data = default, Size = 0, Path = path, Compressed = compress };
            }
        }
        public SerializationOutput Serialize(Type type, object? obj, string path, bool compress = false)
        {
            if (CheckForSerializationErrors(SERIALIZATION_ERROR_FORMAT, type, path, compress, out var output)) return new() { Error = output ?? default, Data = default, Size = 0, Path = path, Compressed = compress };
            if (string.IsNullOrEmpty(path)) return new() { Error = SerializationError.NullPath, Data = default, Size = 0, Path = path, Compressed = compress };
            try
            {
                return SerializeToPath(type, path, compress, obj);
            }
            catch (Exception ex)
            {
                return new() { Error = HandleSerializationError(SERIALIZATION_ERROR_FORMAT, path, ex), Data = default, Size = 0, Path = path, Compressed = compress };
            }
        }
        public SerializationOutput Serialize<T>(T obj, bool compress = false)
        {
            if (CheckForSerializationErrors(SERIALIZATION_ERROR_FORMAT, typeof(T), string.Empty, compress, out var output)) return new() { Error = output ?? default, Data = default, Size = 0, Path = string.Empty, Compressed = compress };
            try
            {
                return SerializeToMemory(typeof(T), compress, obj);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new() { Error = HandleSerializationError(SERIALIZATION_ERROR_FORMAT, string.Empty, ex), Data = default, Size = 0, Path = string.Empty, Compressed = compress };
            }
        }
        public SerializationOutput Serialize(Type type, object? obj, bool compress = false)
        {
            if (CheckForSerializationErrors(SERIALIZATION_ERROR_FORMAT, type, string.Empty, compress, out var output)) return new() { Error = output ?? default, Data = default, Size = 0, Path = string.Empty, Compressed = compress };
            try
            {
                return SerializeToMemory(type, compress, obj);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new() { Error = HandleSerializationError(SERIALIZATION_ERROR_FORMAT, string.Empty, ex), Data = default, Size = 0, Path = string.Empty, Compressed = compress };
            }
        }
    }
}