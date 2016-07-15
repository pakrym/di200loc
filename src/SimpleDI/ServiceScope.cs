using System;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleDI
{
    internal class ServiceScope : IServiceScope
    {
        private ServiceProvider _serviceProvider;

        public ServiceScope(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}