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
 * ScapeCoreSerializer.cs
 * This is the default serializer used in ScapeCore. Includes
 * all the logic used for serialize and compress objects at 
 * runtime.
 */

using ScapeCore.Core.Tools;
using System;

namespace ScapeCore.Core.Serialization.Streamers
{
    public interface IScapeCoreSerializer : IScapeCoreService
    {
        ScapeCoreSerializer.SerializationOutput Serialize(Type type, object? obj, bool compress = false);
        ScapeCoreSerializer.SerializationOutput Serialize(Type type, object? obj, string path, bool compress = false);
        ScapeCoreSerializer.SerializationOutput Serialize<T>(T obj, bool compress = false);
        ScapeCoreSerializer.SerializationOutput Serialize<T>(T obj, string path, bool compress = false);
    }
}