using System;
using System.Collections.Concurrent;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.Common;
using System.Collections.Generic;

namespace Xbim.IDS.Validator.Core.Configuration
{
    public class IdsValueMapper : IValueMapper
    {

        ConcurrentDictionary<Type, Func<object, object>> _typeMap = new ConcurrentDictionary<Type, Func<object, object>>();
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
            _typeMap.TryAdd(typeof(T), accessor);
            
        }

        public bool MapValue<T>(T value, out object? mappedValue)
        {
            if(value == null)
            {
                mappedValue = null;
                return false;
            }
            if(!TryGetType(value.GetType(), out var accessor))
            {
                mappedValue = null;
                return false;
            }
            mappedValue = accessor!(value);
            return true;
        }

        private bool TryGetType(Type type, out Func<object,object>? accessor)
        {
            if(_typeMap.ContainsKey(type))
            {
                return _typeMap.TryGetValue(type, out accessor);
            }
            else
            {
                foreach(var interfaceType in type.GetInterfaces())
                {
                    if(_typeMap.ContainsKey(interfaceType))
                    {
                        // TODO: Consider caching matching interface to a type
                        return _typeMap.TryGetValue(interfaceType, out accessor);
                    }
                }
            }
            accessor = null;
            return false;
        }

        public bool ContainsMap<T>()
        {
            return _typeMap.ContainsKey(typeof(T));
        }
    }
}
