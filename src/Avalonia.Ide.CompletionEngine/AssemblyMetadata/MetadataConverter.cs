﻿using System.Collections.Generic;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine
{
    public static class MetadataConverter
    {
        internal static bool IsMarkupExtension(ITypeInformation def)
        {
            while (def != null)
            {
                if (def.Name == "MarkupExtension")
                    return true;
                def = def.GetBaseType();
            }
            return false;
        }

        public static MetadataType ConvertTypeInfomation(ITypeInformation type)
        {
            var mt = new MetadataType
            {
                Name = type.Name,
                IsStatic = type.IsStatic,
                IsMarkupExtension = MetadataConverter.IsMarkupExtension(type),
                IsEnum = type.IsEnum

            };
            if (mt.IsEnum)
                mt.EnumValues = type.EnumValues.ToArray();
            return mt;
        }

        public static Metadata ConvertMetadata(IMetadataReaderSession provider)
        {
            var types = new Dictionary<string, MetadataType>();
            var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
            var metadata = new Metadata();

            types.Add(typeof(bool).FullName, new MetadataType()
            {
                Name = typeof(bool).FullName,
                IsEnum = true,
                EnumValues = new[] { "True", "False" }
            });


            foreach (var asm in provider.Assemblies)
            {
                var aliases = new Dictionary<string, string>();

                ProcessWellKnowAliases(asm, aliases);
                ProcessCustomAttributes(asm, aliases);

                foreach (var type in asm.Types.Where(x => !x.IsInterface && x.IsPublic))
                {
                    var mt = types[type.FullName] = ConvertTypeInfomation(type);
                    typeDefs[mt] = type;
                    metadata.AddType("clr-namespace:" + type.Namespace + ";assembly=" + asm.Name, mt);
                    string alias = null;
                    if (aliases.TryGetValue(type.Namespace, out alias))
                        metadata.AddType(alias, mt);
                }
            }

            foreach (var type in types.Values)
            {
                ITypeInformation typeDef;
                typeDefs.TryGetValue(type, out typeDef);

                var ctors = typeDef?.Methods
                    .Where(m => m.IsPublic && !m.IsStatic && m.Name == ".ctor" && m.Parameters.Count == 1);

                if (ctors?.Any() == true)
                {
                    bool supportType = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Type");
                    bool supportOther = ctors.Any(m => m.Parameters[0].TypeFullName != "System.Type");

                    if (supportType && supportOther)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Any;
                    else if (supportType)
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Type;
                    else
                        type.SupportCtorArgument = MetadataTypeCtorArgument.Object;
                }

                while (typeDef != null)
                {
                    foreach (var prop in typeDef.Properties)
                    {
                        if (!prop.HasPublicGetter && !prop.HasPublicSetter)
                            continue;

                        var p = new MetadataProperty
                        {
                            Name = prop.Name,
                            Type = types.GetValueOrDefault(prop.TypeFullName),
                            IsStatic = prop.IsStatic,
                            HasGetter = prop.HasPublicGetter,
                            HasSetter = prop.HasPublicSetter
                        };

                        type.Properties.Add(p);
                    }
                    foreach (var methodDef in typeDef.Methods)
                    {
                        if (methodDef.Name.StartsWith("Set") && methodDef.IsStatic && methodDef.IsPublic
                            && methodDef.Parameters.Count == 2)
                        {
                            type.Properties.Add(new MetadataProperty()
                            {
                                Name = methodDef.Name.Substring(3),
                                IsAttached = true,
                                Type = types.GetValueOrDefault(methodDef.Parameters[1].TypeFullName)
                            });
                        }
                    }
                    typeDef = typeDef.GetBaseType();
                }

                type.HasAttachedProperties = type.Properties.Any(p => p.IsAttached);
                type.HasStaticGetProperties = type.Properties.Any(p => p.IsStatic && p.HasGetter);
            }

            return metadata;
        }

        private static void ProcessCustomAttributes(IAssemblyInformation asm, Dictionary<string, string> aliases)
        {
            foreach (
                var attr in
                asm.CustomAttributes.Where(a => a.TypeFullName == "Avalonia.Metadata.XmlnsDefinitionAttribute"))
                aliases[attr.ConstructorArguments[1].Value.ToString()] =
                    attr.ConstructorArguments[0].Value.ToString();
        }

        private static void ProcessWellKnowAliases(IAssemblyInformation asm, Dictionary<string, string> aliases)
        {
            //some internal portable xaml support for aliases
            aliases["Portable.Xaml.Markup"] = "http://schemas.microsoft.com/winfx/2006/xaml";
        }
    }
}
