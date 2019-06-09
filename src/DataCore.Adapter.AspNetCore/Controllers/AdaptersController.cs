using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting information about the hosted adapters.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/adapters")]
    public class AdaptersController : ControllerBase {

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the calling user.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AdaptersController"/> object.
        /// </summary>
        /// <param name="callContext">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public AdaptersController(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Gets information about all registered adapters that are visible to the caller.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="AdapterDescriptorExtended"/> 
        ///   objects.
        /// </returns>
        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<AdapterDescriptorExtended>), 200)]
        public async Task<IActionResult> GetAllAdapters(ApiVersion apiVersion, CancellationToken cancellationToken) {
            var adapters = await _adapterAccessor.GetAdapters(_callContext, cancellationToken).ConfigureAwait(false);
            var result = adapters.Select(x => x.CreateExtendedAdapterDescriptor()).OrderBy(x => x.Name).ToArray();
            return Ok(result); // 200
        }


        /// <summary>
        /// Gets information about the specified adapter.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the <see cref="AdapterDescriptorExtended"/> for the 
        ///   requested adapter.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(AdapterDescriptorExtended), 200)]
        public async Task<IActionResult> GetAdapterById(ApiVersion apiVersion, string adapterId, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            return Ok(adapter.CreateExtendedAdapterDescriptor()); // 200
        }

    }

}
