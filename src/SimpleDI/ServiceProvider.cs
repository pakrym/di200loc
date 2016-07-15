using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleDI
{
    public class ServiceProvider : IServiceProvider, IServiceScopeFactory, IDisposable
    {
        private readonly Dictionary<Type, object> _scoped = new Dictionary<Type, object>();
        private readonly List<object> _transient = new List<object>();

        private readonly ServiceDescriptor[] _services;
        private readonly ServiceProvider _root;

        private bool _disposed;

        public ServiceProvider(IEnumerable<ServiceDescriptor> services)
        {
            _services = services.ToArray();
            _root = this;
        }

        public ServiceProvider(ServiceProvider parent)
        {
            _services = parent._services;
            _root = parent._root;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }

            var descriptor = _services.FirstOrDefault(service => service.ServiceType == serviceType);
            if (descriptor == null)
            {
                if (serviceType.IsConstructedGenericType)
                {
                    var genericType = serviceType.GetGenericTypeDefinition();
                    descriptor = _services.FirstOrDefault(service => service.ServiceType == genericType);
                    if (descriptor != null)
                    {
                        return Resolve(descriptor, serviceType, serviceType.GenericTypeArguments);
                    }
                }
                return null;
            }
            return Resolve(descriptor, serviceType, null);
        }

        private object Resolve(ServiceDescriptor descriptor, Type serviceType, Type[] typeArguments)
        {
            switch (descriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return Singleton(serviceType, () => Create(descriptor, typeArguments));
                case ServiceLifetime.Scoped:
                    return Scoped(serviceType, () => Create(descriptor, typeArguments));
                case ServiceLifetime.Transient:
                    return Transient(Create(descriptor, typeArguments));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object Transient(object o)
        {
            _transient.Add(o);
            return o;
        }

        private object Singleton(Type type, Func<object> factory)
        {
            return Scoped(type, factory, _root);
        }

        private object Scoped(Type type, Func<object> factory)
        {
            return Scoped(type, factory, this);
        }

        private static object Scoped(Type type, Func<object> factory, ServiceProvider provider)
        {
            object value;
            if (!provider._scoped.TryGetValue(type, out value))
            {
                value = factory();
                provider._scoped.Add(type, value);
            }
            return value;
        }

        private object Create(ServiceDescriptor descriptor, Type[] typeArguments)
        {
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }
            else if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(this);
            }
            else if (descriptor.ImplementationType != null)
            {
                return CreateInstance(descriptor.ImplementationType, typeArguments);
            }
            // we should never get here
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var o in _transient.Concat(_scoped.Values))
                {
                    (o as IDisposable)?.Dispose();
                }
            }
        }

        public IServiceScope CreateScope()
        {
            return new ServiceScope(new ServiceProvider(this));
        }

        private object CreateInstance(Type implementationType, Type[] typeArguments)
        {
            if (typeArguments != null)
            {
                implementationType = implementationType.MakeGenericType(typeArguments);
            }
            var constructors = implementationType.GetTypeInfo()
                .DeclaredConstructors.OrderByDescending(c => c.GetParameters().Length);
            foreach (var constructorInfo in constructors)
            {
                var parameters = constructorInfo.GetParameters();
                var arguments = new List<object>();

                foreach (var parameterInfo in parameters)
                {
                    var value = GetService(parameterInfo.ParameterType);
                    // Could not resolve parameter
                    if (value == null)
                    {
                        break;
                    }
                    arguments.Add(value);
                }

                if (parameters.Length != arguments.Count)
                {
                    continue;
                }
                // We got values for all paramters
                return Activator.CreateInstance(implementationType, arguments.ToArray());
            }
            throw new InvalidOperationException("Cannot find constructor");
        }
    }
}
