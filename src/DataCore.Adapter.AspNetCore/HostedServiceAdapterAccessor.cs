using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter {
    public class HostedServiceAdapterAccessor : IAdapterAccessor {

        private readonly IAdapter[] _adapters;


        public HostedServiceAdapterAccessor(IEnumerable<IHostedService> hostedServices) {
            _adapters = hostedServices?.Select(x => x as IAdapter).Where(x => x != null).ToArray() ?? new IAdapter[0];
        }


        Task<IEnumerable<IAdapter>> IAdapterAccessor.GetAdapters(IDataCoreContext context, CancellationToken cancellationToken) {
            return GetAdapters(context, cancellationToken);
        }


        protected virtual Task<IEnumerable<IAdapter>> GetAdapters(IDataCoreContext context, CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<IAdapter>>(_adapters);
        }


        Task<IAdapter> IAdapterAccessor.GetAdapter(IDataCoreContext context, string adapterId, CancellationToken cancellationToken) {
            return GetAdapter(context, adapterId, cancellationToken);
        }


        protected virtual Task<IAdapter> GetAdapter(IDataCoreContext context, string adapterId, CancellationToken cancellationToken) {
            var adapter = _adapters.FirstOrDefault(x => x.Descriptor.Id.Equals(adapterId));
            return Task.FromResult(adapter);
        }

    }
}
