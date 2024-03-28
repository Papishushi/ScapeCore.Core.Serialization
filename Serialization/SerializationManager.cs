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
 * SerializationManager.cs
 * Provides methods for serializing and deserializing data.
 */

using Baksteen.Extensions.DeepCopy;
using ProtoBuf;
using ProtoBuf.Meta;
using ScapeCore.Core.Serialization.Streamers;
using ScapeCore.Core.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Xml.Linq;

using static ScapeCore.Core.Serialization.RuntimeModelFactory;
using static ScapeCore.Core.Debug.Debugger;
using ScapeCore.Core.SceneManagement;

namespace ScapeCore.Core.Serialization
{
    public class SerializationManager : IScapeCoreManager
    {
        private static SerializationManager? _defaultManager = null;
        public static SerializationManager? Default { get => _defaultManager; set => _defaultManager = value; }

        private RuntimeModelFactory? _modelFactory = null;
        private IScapeCoreSerializer? _serializer = null;
        private IScapeCoreDeserializer? _deserializer = null;
        private readonly List<IScapeCoreService?> _services = [];

        private readonly Type[] _buildInTypes;

        internal TypeModel? Model { get => _modelFactory?.Model; }

        public RuntimeTypeModel? GetRuntimeClone() => _modelFactory?.Model?.DeepCopy();
        public RuntimeTypeModel? GetRuntime() => _modelFactory?.Model;

        public IScapeCoreSerializer? Serializer => _serializer; 
        public IScapeCoreDeserializer? Deserializer => _deserializer;

        List<IScapeCoreService?> IScapeCoreManager.Services { get => _services; }

        public SerializationManager()
        {
            var assembly = typeof(SerializationManager).Assembly;
            var path = Path.Combine(assembly.Location[..assembly.Location.LastIndexOf('\\')], @"BuildinTypes.xml") ??
                throw new FileNotFoundException("Path to xml data base is null. SerializationManager configuration aborting...");

            var typesNames = ParseXmlToTypesArray(path);
            if (typesNames.Length == 0)
                throw new InvalidDataException("XML was on an invalid format, empty or failed to load.");

            var l = new List<Type>();
            var assemblyTypes = assembly.GetTypes();

            foreach (var type in assemblyTypes)
                if (typesNames.Contains(type.Name))
                    l.Add(type);

            _buildInTypes = [.. l];
            _modelFactory = new RuntimeModelFactory(_buildInTypes);
            _defaultManager ??= this;
        }

        bool IScapeCoreManager.InjectDependencies(params IScapeCoreService[] services)
        {
            if (services.Length <= 0 || services.Length > 2) return false;

            foreach (var service in services)
            {
                if (service is IScapeCoreSerializer serializer)
                {
                    if (_serializer != null) return false;
                    _services.Add(serializer);
                    _serializer = serializer;
                }
                else if (service is IScapeCoreDeserializer deserializer)
                {
                    if (_deserializer != null) return false;
                    _services.Add(deserializer);
                    _deserializer = deserializer;
                }
                else return false;
            }

            return true;
        }

        public void InjectDependencies(IScapeCoreSerializer? serializer = null, IScapeCoreDeserializer? deserializer = null)
        {
            var strs = new List<string>();
            if (serializer != null)
                strs.Add($"\"{serializer.Name}\"");
            if (deserializer != null)
                strs.Add($"\"{deserializer.Name}\"");
            if (!(this as IScapeCoreManager).InjectDependencies([serializer, deserializer]))
                throw new ArgumentException($"The serializers injected to this {nameof(SerializationManager)} are not valid. Check if they are correct and try again.");
            else
                SCLog?.Log(DEBUG, strs.Count == 1 ? $"{strs[0]} was succesfully injected to {nameof(SerializationManager)}." :
                                                    $"{string.Join(", ", strs)} were succesfully injected to {nameof(SerializationManager)}.");
        }

        private bool ExtractDependenciesLocal(params IScapeCoreService[] services)
        {
            if (services.Length <= 0 || services.Length > 2) return false;

            foreach (var service in services)
            {
                if (service is IScapeCoreSerializer serializer)
                {
                    if (_services.Remove(serializer)) _serializer = null;
                    else return false;
                }
                else if (service is IScapeCoreDeserializer deserializer)
                {
                    if (_services.Remove(deserializer)) _deserializer = null;
                    else return false;
                }
                else return false;
            }
            return true;
        }

        bool IScapeCoreManager.ExtractDependencies(params IScapeCoreService[] services)
        {
            var servicesToExtract = services.Length <= 0 ? [.. _services] : services;
            var result = ExtractDependenciesLocal(servicesToExtract);
            var strs = new List<string>();
            foreach (var service in servicesToExtract)
                strs.Add($"\"{service.Name}\"");
            if (result == false)
                throw new ArgumentException($"The serializers extracted from this {nameof(SerializationManager)} are not valid. Check if they are correct and try again. Alternatively dependencies could be null.");
            else
                SCLog?.Log(DEBUG, strs.Count == 1 ? $"{strs[0]} was succesfully extracted from {nameof(SerializationManager)}." :
                                                    $"{string.Join(", ", strs)} were succesfully extracted from {nameof(SerializationManager)}.");
            return result;
        }

        private string[] ParseXmlToTypesArray(string xmlFilePath)
        {
            var xmlDoc = XDocument.Load(xmlFilePath);
            XNamespace ns = "http://schemas.microsoft.com/powershell/2004/04";
            if (xmlDoc == null || xmlDoc!.Root == null) return [];
            var types = xmlDoc.Root.Elements(ns + "Type").Select(type => type.Value).ToArray();
            return types;
        }

        public void AddType(Type type) => _modelFactory?.AddType(type);

        public ChangeModelOutput ChangeModel(RuntimeTypeModel model, IScapeCoreSerializer serializer, IScapeCoreDeserializer deserializer)
        {
            try
            {
                InjectDependencies(serializer, deserializer);
            }
            catch (ArgumentException ex)
            {
                SCLog.Log(ERROR, ex.Message);
                return new ChangeModelOutput(ChangeModelError.InvalidModelDependencies);
            }

            var output = (_modelFactory ??= new(_buildInTypes)).ChangeModel(model);

            return output;
        }

        public void ResetFactory() => _modelFactory = new RuntimeModelFactory(_buildInTypes);
    }
}