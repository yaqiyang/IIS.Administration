// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Scm
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            _ = Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}", new { controller = "scm" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", _ => new { href = $"/{Defines.PATH}" });
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
        }
    }
}
