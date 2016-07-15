using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleDI
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly ServiceDescriptor[] _services;

        public ServiceProvider(IEnumerable<ServiceDescriptor> services)
        {
            _services = services.ToArray();
        }

        public object GetService(Type serviceType)
        {
            var descriptor = _services.FirstOrDefault(service => service.ServiceType == serviceType);
            if (descriptor == null)
            {
                return null;
            }

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
                return CreateInstance(descriptor.ImplementationType);
            }
            // we should never get here
            throw new NotImplementedException();
        }

        private object CreateInstance(Type implementationType)
        {
            var constructors = implementationType.GetTypeInfo().
                DeclaredConstructors.OrderByDescending(c => c.GetParameters().Length);

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
