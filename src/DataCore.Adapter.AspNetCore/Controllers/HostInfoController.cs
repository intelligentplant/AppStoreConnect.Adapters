using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {


    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/host-info")]
    public class HostInfoController: ControllerBase {

        private readonly HostInfo _hostInfo;


        public HostInfoController(HostInfo hostInfo) {
            _hostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
        }


        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(HostInfo), 200)]
        public IActionResult GetHostInfo() {
            return Ok(_hostInfo); // 200
        }

    }

}
