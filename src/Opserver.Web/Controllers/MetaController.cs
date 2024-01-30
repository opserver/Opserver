using Opserver.Views.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Opserver.Helpers;
using Opserver.Models;

namespace Opserver.Controllers
{
    [AlsoAllow(Roles.Anonymous)]
    public class MetaController : StatusController
    {
        public MetaController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        [Route("meta/healthcheck")]
        public IActionResult MetaHealthcheck() => Ok();
    }
}
