using System;
using System.Collections.Generic;

namespace ThanhHoang.Bomberman
{
    public class Service
    {
        private static Dictionary<Type, object> _services = new Dictionary<Type, object>();
        public static void Register<T>(T service)
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type} is already registered.");
            }
            _services[type] = service;
        }

        public static T Get<T>()
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out object service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service of type {type} is not registered.");
        }
    }
}