using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkaroundUtilities
{
    abstract internal class StringFactory<T> where T : class
    {
        private Dictionary<string, Type> _KnownTypes = new Dictionary<string, Type>();

        public bool KnowsType(string key)
        {
            return _KnownTypes.ContainsKey(key);
        }

        protected StringFactory()
        {
            var baseType = typeof(T);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(s => s.GetTypes())
                                .Where(p => baseType.IsAssignableFrom(p) && p != baseType);

            foreach (var type in types)
            {
                _KnownTypes.Add(type.Name, type);
            }
        }

        public IEnumerable<T> Create(ICollection<string> _sTypeNames)
        {
            foreach (var inst in _sTypeNames)
            {
                yield return Create(inst);
            }
        }

        public T Create(string _sTypeName)
        {
            Type knownType;
            if (_KnownTypes.TryGetValue(_sTypeName, out knownType))
            {
                return (T)Activator.CreateInstance(knownType);
            }

            throw new KeyNotFoundException();
        }
    }
}