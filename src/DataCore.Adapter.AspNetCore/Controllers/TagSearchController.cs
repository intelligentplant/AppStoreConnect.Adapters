using System;
using System.Collections.Generic;
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
    [Route("api/[area]/v{version:apiVersion}/tags")]
    public class TagSearchController: ControllerBase {

        private readonly IDataCoreContext _dataCoreContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public TagSearchController(IDataCoreContext dataCoreContext, IAdapterAccessor adapterAccessor) {
            _dataCoreContext = dataCoreContext ?? throw new ArgumentNullException(nameof(dataCoreContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }



        [HttpPost]
        [Route("{adapterId}/find")]
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(ApiVersion apiVersion, string adapterId, FindTagsRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_dataCoreContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var tagSearch = adapter.Features.Get<ITagSearch>();
            if (tagSearch == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(ITagSearch))); // 400
            }

            var tags = await tagSearch.FindTags(_dataCoreContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(tags); // 200
        }


        [HttpGet]
        [Route("{adapterId}/find")]
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(ApiVersion apiVersion, CancellationToken cancellationToken, string adapterId, string name = null, string description = null, string units = null, int pageSize = 10, int page = 1) {
            return await FindTags(apiVersion, adapterId, new FindTagsRequest() {
                Name = name,
                Description = description,
                Units = units,
                PageSize = pageSize,
                Page = page
            }, cancellationToken).ConfigureAwait(false);
        }


        [HttpPost]
        [Route("{adapterId}/get-by-id")]
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(ApiVersion apiVersion, string adapterId, GetTagsRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_dataCoreContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            var tagSearch = adapter.Features.Get<ITagSearch>();
            if (tagSearch == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(ITagSearch))); // 400
            }

            var tags = await tagSearch.GetTags(_dataCoreContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(tags); // 200
        }


        [HttpGet]
        [Route("{adapterId}/get-by-id")]
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(ApiVersion apiVersion, string adapterId, [FromQuery] string[] tag, CancellationToken cancellationToken) {
            return await GetTags(apiVersion, adapterId, new GetTagsRequest() {
                Tags = tag
            }, cancellationToken).ConfigureAwait(false);
        }

    }

}
