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
 * RuntimeModelFactory.cs
 * An abstraction for a ProtoBuffer RuntimeTypeModel used on the
 * Serialization Manager.
 */

using ProtoBuf.Meta;
using ScapeCore.Core.Tools;
using System;
using System.Linq;
using System.Reflection;

using static ScapeCore.Core.Debug.Debugger;
using static ScapeCore.Traceability.Logging.LoggingColor;

namespace ScapeCore.Core.Serialization
{
    /// <summary>
    /// Creates a configured model for the serializer and 
    /// </summary>
    /// <remarks>
    /// Inherit from this class to create a simple behaviour that can be added and registered in the engine.
    /// </remarks>
    public sealed class RuntimeModelFactory
    {
        private const string SCAPE_CORE_NAME = "ScapeCore";
        private const int FIELD_PROTOBUF_INDEX = 1;
        private const int SUBTYPE_PROTOBUF_INDEX = 556;

        private RuntimeTypeModel? _model = null;
        public RuntimeTypeModel? Model { get => _model; }

        public RuntimeModelFactory(Type[] types)
        {
            var runtimeModel = CreateRuntimeModel();
            foreach (var type in types)
                ConfigureType(runtimeModel, type);
            runtimeModel.MakeDefault();
            _model = runtimeModel;
        }

        public void AddType(Type type)
        {
            if (_model == null)
            {
                SCLog.Log(WARNING, $"Serialization Manager can not add a type {Yellow}{type.FullName}{Traceability.Logging.LoggingColor.Default} because serialization model is null.");
                return;
            }
            var fieldIndex = FIELD_PROTOBUF_INDEX;
            var metaType = _model.Add(type, false);
            metaType.IgnoreUnknownSubTypes = false;
            SCLog.Log(DEBUG, $"Type {type.Name} was configured for [de]Serialization...");
            SetTypeFields(metaType, type, ref fieldIndex);
            SetSubType(type, _model);
            if (type == typeof(DeeplyMutableType) || type.BaseType == typeof(DeeplyMutableType)) return;
            SetTypeProperties(metaType, type, ref fieldIndex);
        }

        private static RuntimeTypeModel CreateRuntimeModel()
        {
            var runtimeModel = RuntimeTypeModel.Create(SCAPE_CORE_NAME);
            runtimeModel.AllowParseableTypes = true;
            runtimeModel.AutoAddMissingTypes = true;
            runtimeModel.MaxDepth = 100;
            return runtimeModel;
        }

        private void ConfigureType(RuntimeTypeModel runtimeModel, Type type)
        {
            var fieldIndex = FIELD_PROTOBUF_INDEX;
            var metaType = runtimeModel.Add(type, false);
            SCLog.Log(DEBUG, $"Type {type.Name} was configured for [de]Serialization...");
            if (type.IsEnum) return;
            SetFieldsSubTypesAndProperties(runtimeModel, type, metaType, ref fieldIndex);
        }

        private void SetFieldsSubTypesAndProperties(RuntimeTypeModel runtimeModel, Type type, MetaType metaType, ref int fieldIndex)
        {
            SetTypeFields(metaType, type, ref fieldIndex);
            SetSubType(type, runtimeModel);
            if (CheckForDeeplyMutableType(runtimeModel, type)) return;
            SetTypeProperties(metaType, type, ref fieldIndex);
        }

        private static void SetTypeFields(MetaType metaType, Type type, ref int fieldIndex)
        {
            foreach (var field in type.GetFields())
            {
                try
                {
                    if (field.FieldType.Name == typeof(object).Name)
                    {
                        SCLog.Log(WARNING, $"Serialization Manager tried to configure an object/dynamic field named {field.Name} from Type {type.Name}, serializer does not support deeply mutable types, try changing field type to {typeof(DeeplyMutableType).FullName}.");
                        continue;
                    }
                    AddField(metaType, field, type, ref fieldIndex);
                }
                catch (Exception ex)
                {
                    SCLog.Log(WARNING, $"Serialization Manager can not determine type of field {field.Name} from {type}.");
                    SCLog.Log(VERBOSE, $"{ex}", substitutions: ex.Message);
                }
            }
        }

        private static void AddField(MetaType metaType, FieldInfo field, Type type, ref int fieldIndex)
        {
            metaType.Add(fieldIndex++, field.Name);
            SCLog.Log(VERBOSE, $"\tField [{fieldIndex - 1}]{field.Name}[{field.FieldType}] from Type {type.Name}");
        }

        private static void SetSubType(Type type, RuntimeTypeModel runtimeModel)
        {
            foreach (var runtimeType in runtimeModel.GetTypes().Cast<MetaType>())
            {
                if (runtimeType.Type != type.BaseType) continue;
                var subTypeIndex = runtimeType.GetSubtypes().Length + SUBTYPE_PROTOBUF_INDEX;
                runtimeType.AddSubType(subTypeIndex, type);
                break;
            }
        }

        private bool CheckForDeeplyMutableType(RuntimeTypeModel runtimeModel, Type type)
        {
            if (type == typeof(DeeplyMutableType) || type.BaseType == typeof(DeeplyMutableType))
            {
                runtimeModel.MakeDefault();
                _model = runtimeModel;
                return true;
            }
            return false;
        }

        private static void SetTypeProperties(MetaType metaType, Type type, ref int fieldIndex)
        {
            foreach (var property in type.GetProperties())
            {
                try
                {
                    if (property.PropertyType.Name == typeof(object).Name)
                    {
                        SCLog.Log(WARNING, $"Serialization Manager tried to configure an object/dynamic field named {property.Name} from Type {type.Name}, serializer does not support deeply mutable types, try changing field type to {typeof(DeeplyMutableType).FullName}.");
                        continue;
                    }
                    AddProperty(metaType, property, type, ref fieldIndex);
                }
                catch (Exception ex)
                {
                    SCLog.Log(WARNING, $"Serialization Manager can not determine type of property {property.Name} from {type}.");
                    SCLog.Log(VERBOSE, ex.Message);
                }
            }
        }

        private static void AddProperty(MetaType metaType, PropertyInfo property, Type type, ref int fieldIndex)
        {
            metaType.Add(fieldIndex++, property.Name);
            SCLog.Log(VERBOSE, $"\tProperty [{fieldIndex - 1}]{property.Name}[{property.PropertyType}] from Type {type.Name}");
        }

        #region Change Serialization Model
        public enum ChangeModelError
        {
            None,
            NullModel,
            InvalidModelDependencies
        }
        public readonly record struct ChangeModelOutput(ChangeModelError Error);
        public ChangeModelOutput ChangeModel(RuntimeTypeModel model)
        {
            if (model == null)
            {
                SCLog.Log(WARNING, "Cannot change to a null serialization model. Serialization model remains the same.");
                return new() { Error = ChangeModelError.NullModel };
            }
            _model = model;
            _model.CompileInPlace();
            SCLog.Log(DEBUG, "Serialization model was successfully updated.");
            return new() { Error = ChangeModelError.None };
        }
        #endregion Change Serialization Model
    }
}