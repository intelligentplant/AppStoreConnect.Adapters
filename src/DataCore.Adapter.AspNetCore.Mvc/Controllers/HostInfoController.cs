using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting information about the hosting application.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/host-info")]
    // Legacy route for compatibility with v1 of the toolkit
    [Route("api/data-core/v1.0/host-info")] 
    public class HostInfoController: ControllerBase {

        /// <summary>
        /// The host information.
        /// </summary>
        private readonly HostInfo _hostInfo;


        /// <summary>
        /// Creates a new <see cref="HostInfoController"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The host information.
        /// </param>
        public HostInfoController(HostInfo hostInfo) {
            _hostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
        }


        /// <summary>
        /// Gets information about the hosting application.
        /// </summary>
        /// <returns>
        ///   Successful responses contain a <see cref="HostInfo"/> object describing the hosting 
        ///   application.
        /// </returns>
        [HttpGet]
        [Route("")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HostInfo), 200)]
        public IActionResult GetHostInfo() {
            return Ok(_hostInfo); // 200
        }


        /// <summary>
        /// Gets descriptors for the standard adapter features.
        /// </summary>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="FeatureDescriptor"/> objects
        ///   describing the standard adapter features.
        /// </returns>
        [HttpGet]
        [Route("adapter-features")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<FeatureDescriptor>), 200)]
        public IActionResult GetStandardFeatureDescriptors() {
            return Ok(TypeExtensions.GetStandardAdapterFeatureTypes().Select(x => x.CreateFeatureDescriptor()).ToArray()); // 200
        }


        /// <summary>
        /// Gets the available APIs for the host.
        /// </summary>
        /// <param name="apiService">
        ///   The <see cref="IAvailableApiService"/> that returns information about the enabled APIs.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="ApiDescriptor"/> objects 
        ///   describing the available APIs.
        /// </returns>
        [HttpGet]
        [Route("available-apis")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ApiDescriptor>), 200)]
        public IActionResult GetAvailableApis([FromServices] IAvailableApiService apiService) {
            return Ok(apiService.GetApiDescriptors().Where(x => x.Enabled).ToArray()); // 200
        }

    }

}
