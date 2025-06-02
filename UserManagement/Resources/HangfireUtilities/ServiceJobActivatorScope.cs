using Hangfire;
using System.Diagnostics.CodeAnalysis;

namespace UserManagement.Resources.HangfireUtilities
{
    public class ServiceJobActivatorScope : JobActivatorScope
    {
        private readonly IServiceScope _serviceScope;

        public ServiceJobActivatorScope([NotNull] IServiceScope serviceScope)
        {
            _serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
        }

        public override object Resolve(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(_serviceScope.ServiceProvider, type);
        }

        public override void DisposeScope()
        {
            _serviceScope.Dispose();
        }
    }
}
