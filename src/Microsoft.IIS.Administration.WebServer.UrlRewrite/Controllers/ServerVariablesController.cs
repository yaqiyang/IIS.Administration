// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Microsoft.IIS.Administration.Core.Utils;
    using Sites;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    [Route("api/webserver/url-rewrite/allowed-server-variables")]
    public class ServerVariablesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ServerVariablesName)]
        public object Get()
        {
            RewriteHelper.ResolveRewrite(Context, out Site site, out string path);

            if (path == null) {
                return NotFound();
            }

            dynamic d = ServerVariablesHelper.ToJsonModel(site, path);
            return LocationChanged(ServerVariablesHelper.GetLocation(d.id), d);
        }

        [HttpGet("{id}")]
        [ResourceInfo(Name = Defines.ServerVariablesName)]
        public object Get(string id)
        {
            var serverVariablesId = new RewriteId(id);

            Site site = serverVariablesId.SiteId == null ? null : SiteHelper.GetSite(serverVariablesId.SiteId.Value);

            if (serverVariablesId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return ServerVariablesHelper.ToJsonModel(site, serverVariablesId.Path);
        }

        [HttpPatch("{id}")]
        [Audit]
        [ResourceInfo(Name = Defines.ServerVariablesName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            model = DynamicHelper.ToJObject(model);
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId serverVariablesId = new RewriteId(id);

            Site site = serverVariablesId.SiteId == null ? null : SiteHelper.GetSite(serverVariablesId.SiteId.Value);

            if (serverVariablesId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            ServerVariablesHelper.UpdateFeatureSettings(model, site, serverVariablesId.Path, configPath);

            ManagementUnit.Current.Commit();

            return ServerVariablesHelper.ToJsonModel(site, serverVariablesId.Path);
        }

        [HttpDelete("{id}")]
        [Audit]
        public void Delete(string id)
        {
            var serverVariablesId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (serverVariablesId.SiteId != null) ? SiteHelper.GetSite(serverVariablesId.SiteId.Value) : null;

            if (site != null) {
                var section = ServerVariablesHelper.GetSection(site, serverVariablesId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
        }
    }
}

