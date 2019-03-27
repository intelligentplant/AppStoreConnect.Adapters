using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore {
    public class AdapterServicesOptionsBuilder {

        public HostInfo HostInfo { get; set; }

        internal Type AdapterAccessorType { get; set; }

        internal Type FeatureAuthorizationHandlerType { get; set; }

        internal bool UseAuthorization { get; set; }


        public AdapterServicesOptionsBuilder UseAdapterAccessor<TAccessor>() where TAccessor : class, IAdapterAccessor {
            AdapterAccessorType = typeof(TAccessor);

            return this;
        }


        public AdapterServicesOptionsBuilder UseFeatureAuthorizationHandler<THandler>() where THandler : FeatureAuthorizationHandler {
            FeatureAuthorizationHandlerType = typeof(THandler);
            UseAuthorization = true;

            return this;
        }

    }
}
