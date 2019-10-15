using System;
using DataCore.Adapter.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting information about the hosting application.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/host-info")]
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

    }

}
