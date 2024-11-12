using System;
using System.Collections.Concurrent;
using Xbim.IDS.Validator.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Xbim.IDS.Validator.Core.Configuration
{
    public class IdsValueMapper : IValueMapper
    {

        ConcurrentDictionary<Type, TypeMapper> _typeMap = new ConcurrentDictionary<Type, TypeMapper>();
        private readonly IEnumerable<IValueMapProvider> providers;

        public IdsValueMapper(IEnumerable<IValueMapProvider> providers)
        {
            this.providers = providers ?? throw new ArgumentNullException(nameof(providers));
            InitialiseDefaults();
        }

        private void InitialiseDefaults()
        {
            foreach(var provider in providers)
            {
                provider.CreateMappings(this);
            }
        }

        public void AddMap<T>(Func<T, object> fn)
        {

            Func<object, object> accessor = (t) => fn((T)t);
            var type = typeof(T);
            _typeMap.TryAdd(typeof(T), new TypeMapper(accessor));
            
        }

        public bool MapValue<T>(T value, out object? mappedValue)
        {
            if(value == null)
            {
                mappedValue = null;
                return false;
            }
            if(!TryGetType(value.GetType(), out var typeMapper))
            {
                mappedValue = null;
                return false;
            }
            mappedValue = typeMapper!.Accessor(value);
            return true;
        }

        private bool TryGetType(Type type, out TypeMapper? typeMapper)
        {
            if(_typeMap.ContainsKey(type))
            {
                return _typeMap.TryGetValue(type, out typeMapper);
            }
            else
            {
                typeMapper = null;
                var types = type.GetInterfaces().Union(GetAncestors(type));
                foreach (var implementedType in types)
                {
                    if(_typeMap.ContainsKey(implementedType))
                    {
                        // TODO: Consider caching matching interface to a type
                        if (_typeMap.TryGetValue(implementedType, out var mapper))
                        {
                            // Get the mapper with the lowest ordinal. 
                            if(typeMapper == null ||  mapper.Ordinal < typeMapper.Ordinal)
                            {
                                typeMapper = mapper;
                            }
                        }
                    }
                }
                
                if (typeMapper != null)
                {
                    return true;
                }
            }
            typeMapper = null;
            return false;
        }

        private IEnumerable<Type> GetAncestors(Type type)
        {
            if (type.BaseType != null)
            {
                yield return type.BaseType;
                foreach (Type b in GetAncestors(type.BaseType))
                {
                    yield return b;
                }
            }

        }

        public bool ContainsMap<T>()
        {
            return _typeMap.ContainsKey(typeof(T));
        }
    }

    internal class TypeMapper
    {
        public TypeMapper(Func<object, object> accessor)
        {
            Accessor = accessor;
            Ordinal = LastOrdinal++;
        }

        static int LastOrdinal = 0;
        public int Ordinal { get; }
        public Func<object, object> Accessor { get;  }
    }
}
