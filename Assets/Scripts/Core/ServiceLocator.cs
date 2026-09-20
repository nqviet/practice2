using System;
using System.Collections.Generic;

namespace Game.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> s_Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            s_Services[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            s_Services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (s_Services.TryGetValue(typeof(T), out object obj))
            {
                service = (T)obj;
                return true;
            }

            service = null;
            return false;
        }

        public static void Clear()
        {
            s_Services.Clear();
        }
    }
}
