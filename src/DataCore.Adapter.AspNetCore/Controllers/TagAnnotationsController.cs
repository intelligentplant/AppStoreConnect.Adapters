using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource;
using DataCore.Adapter.DataSource.Features;
using DataCore.Adapter.DataSource.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/tag-annotations")]
    public class TagAnnotationsController: ControllerBase {

        private readonly IDataCoreContext _dataCoreContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public TagAnnotationsController(IDataCoreContext dataCoreContext, IAdapterAccessor adapterAccessor) {
            _dataCoreContext = dataCoreContext ?? throw new ArgumentNullException(nameof(dataCoreContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        [HttpPost]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(IEnumerable<TagValueAnnotations>), 200)]
        public async Task<IActionResult> ReadAnnotations(ApiVersion apiVersion, string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_dataCoreContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadTagValueAnnotations>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadTagValueAnnotations))); // 400
            }

            var annotations = await feature.ReadTagValueAnnotations(_dataCoreContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(annotations); // 200
        }

    }

}
