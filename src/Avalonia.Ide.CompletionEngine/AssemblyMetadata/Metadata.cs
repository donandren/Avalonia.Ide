﻿using System.Collections.Generic;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine
{

    public class Metadata
    {
        public Dictionary<string, Dictionary<string, MetadataType>> Namespaces { get; } = new Dictionary<string, Dictionary<string, MetadataType>>();

        public void AddType(string ns, MetadataType type) => Namespaces.GetOrCreate(ns)[type.Name] = type;
    }

    public class MetadataType
    {
        public bool IsEnum { get; set; }
        public bool IsMarkupExtension { get; set; }
        public bool IsStatic { get; set; }
        public bool HasHintValues { get; set; }
        public string[] HintValues { get; set; }
        public string Name { get; set; }
        public List<MetadataProperty> Properties { get; set; } = new List<MetadataProperty>();
        public bool HasAttachedProperties { get; set; }
        public bool HasStaticGetProperties { get; set; }
        public bool HasSetProperties { get; set; }
        public bool IsAvaloniaObjectType { get; set; }
        public MetadataTypeCtorArgument SupportCtorArgument { get; set; }
        public bool IsCompositeValue { get; set; }
    }

    public enum MetadataTypeCtorArgument
    {
        None,
        Type,
        Object,
        TypeAndObject,
        HintValues
    }

    public class MetadataProperty
    {
        public string Name { get; set; }
        public MetadataType Type { get; set; }
        public bool IsAttached { get; set; }
        public bool IsStatic { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
    }
}