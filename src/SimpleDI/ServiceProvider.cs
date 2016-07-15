using System;
using System.Collections.Generic;
using System.Linq;
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
                return Activator.CreateInstance(descriptor.ImplementationType);
            }
            // we should never get here
            throw new NotImplementedException();
        }
    }
}
