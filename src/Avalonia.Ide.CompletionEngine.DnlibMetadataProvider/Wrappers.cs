﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using dnlib.DotNet;

namespace Avalonia.Ide.CompletionEngine.DnlibMetadataProvider
{
    class AssemblyWrapper : IAssemblyInformation
    {
        private readonly AssemblyDef _asm;

        public AssemblyWrapper(AssemblyDef asm)
        {
            _asm = asm;
        }

        public string Name => _asm.Name;

        public IEnumerable<ITypeInformation> Types
            => _asm.Modules.SelectMany(m => m.Types).Select(TypeWrapper.FromDef);

        public IEnumerable<ICustomAttributeInformation> CustomAttributes
            => _asm.CustomAttributes.Select(a => new CustomAttributeWrapper(a));
    }

    class TypeWrapper : ITypeInformation
    {
        private readonly TypeDef _type;

        public static TypeWrapper FromDef(TypeDef def) => def == null ? null : new TypeWrapper(def);

        TypeWrapper(TypeDef type)
        {
            if (type == null)
                throw new ArgumentNullException();
            _type = type;
        }

        public string FullName => _type.FullName;
        public string Name => _type.Name;
        public string Namespace => _type.Namespace;
        public ITypeInformation GetBaseType() => FromDef(_type.GetBaseType().ResolveTypeDef());

        public IEnumerable<IMethodInformation> Methods => _type.Methods.Select(m => new MethodWrapper(m));
        public IEnumerable<IPropertyInformation> Properties => _type.Properties.Select(p => new PropertyWrapper(p));
        public bool IsEnum => _type.IsEnum;
        public bool IsStatic => _type.IsAbstract && _type.IsSealed;
        public bool IsInterface => _type.IsInterface;
        public bool IsPublic => _type.IsPublic;

        public IEnumerable<string> EnumValues
        {
            get
            {
                return _type.Fields.Where(f => f.IsStatic).Select(f => f.Name.String).ToArray();
            }
        }
    }

    class CustomAttributeWrapper : ICustomAttributeInformation
    {
        private Lazy<IList<IAttributeConstructorArgumentInformation>> _args;
        public CustomAttributeWrapper(CustomAttribute attr)
        {
            TypeFullName = attr.TypeFullName;
            _args = new Lazy<IList<IAttributeConstructorArgumentInformation>>(() =>
                attr.ConstructorArguments.Select(
                    ca => (IAttributeConstructorArgumentInformation)
                        new ConstructorArgumentWrapper(ca)).ToList());
        }

        public string TypeFullName { get; }
        public IList<IAttributeConstructorArgumentInformation> ConstructorArguments => _args.Value;
    }

    class ConstructorArgumentWrapper : IAttributeConstructorArgumentInformation
    {
        public ConstructorArgumentWrapper(CAArgument ca)
        {
            Value = ca.Value;
        }

        public object Value { get; }
    }

    class PropertyWrapper : IPropertyInformation
    {
        public PropertyWrapper(PropertyDef prop)
        {
            Name = prop.Name;
            var setMethod = prop.SetMethod;
            var getMethod = prop.GetMethod;

            IsStatic = setMethod?.IsStatic ?? getMethod?.IsStatic ?? false;



            if (setMethod != null)
            {
                HasPublicSetter = true;

                TypeFullName = setMethod.Parameters[setMethod.IsStatic ? 0 : 1].Type.FullName;
            }
            if (getMethod != null)
            {

            }
        }

        public bool IsStatic { get; }
        public bool HasPublicSetter { get; }
        public bool HasPublicGetter { get; }
        public string TypeFullName { get; }
        public string Name { get; }
    }

    class MethodWrapper : IMethodInformation
    {
        private readonly MethodDef _method;
        private readonly Lazy<IList<IParameterInformation>> _parameters;

        public MethodWrapper(MethodDef method)
        {
            _method = method;
            _parameters = new Lazy<IList<IParameterInformation>>(() =>
                _method.Parameters.Select(p => (IParameterInformation)new ParameterWrapper(p)).ToList() as
                    IList<IParameterInformation>);
        }

        public bool IsStatic => _method.IsStatic;
        public bool IsPublic => _method.IsPublic;
        public string Name => _method.Name;
        public IList<IParameterInformation> Parameters => _parameters.Value;
    }

    class ParameterWrapper : IParameterInformation
    {
        private readonly Parameter _param;

        public ParameterWrapper(Parameter param)
        {
            _param = param;
        }
        public string TypeFullName => _param.Type.FullName;
    }
}
