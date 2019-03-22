using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/adapters")]
    public class AdaptersController: ControllerBase {

        private readonly IDataCoreContext _dataCoreContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public AdaptersController(IDataCoreContext dataCoreContext, IAdapterAccessor adapterAccessor) {
            _dataCoreContext = dataCoreContext ?? throw new ArgumentNullException(nameof(dataCoreContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<AdapterDescriptorExtended>), 200)]
        public async Task<IActionResult> GetAllAdapters(ApiVersion apiVersion, CancellationToken cancellationToken) {
            var adapters = await _adapterAccessor.GetAdapters(_dataCoreContext, cancellationToken).ConfigureAwait(false);
            var result = adapters.Select(x => new AdapterDescriptorExtended(x)).OrderBy(x => x.Name).ToArray();
            return Ok(result); // 200
        }


        [HttpGet]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(AdapterDescriptorExtended), 200)]
        public async Task<IActionResult> GetAdaptersById(ApiVersion apiVersion, string adapterId, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_dataCoreContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            return Ok(new AdapterDescriptorExtended(adapter)); // 200
        }

    }

}
