/*
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
 * ScapeCoreDeserializer.cs
 * This is the default deserializer used in ScapeCore. Includes
 * all the logic used for deserialize and compress objects at 
 * runtime.
 */

using ScapeCore.Core.Tools;
using System;

namespace ScapeCore.Core.Serialization.Streamers
{
    public interface IScapeCoreDeserializer : IScapeCoreService
    {
        ScapeCoreDeserializer.DeserializationOutput Deserialize(Type type, byte[] serialized, object? obj = null, bool decompress = false);
        ScapeCoreDeserializer.DeserializationOutput Deserialize(Type type, string path, object? obj = null, bool decompress = false);
        ScapeCoreDeserializer.DeserializationOutput Deserialize<T>(byte[] serialized, T? obj = default, bool decompress = false);
        ScapeCoreDeserializer.DeserializationOutput Deserialize<T>(string path, T? obj = default, bool decompress = false);
    }
}