﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Microsoft.IIS.Administration.Core.Utils;
    using Sites;
    using System;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    [Route("api/webserver/url-rewrite/outbound/rules")]
    public class OutboundRulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.OutboundRulesName)]
        public object Get()
        {
            string outboundRulesId = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(outboundRulesId)) {
                return NotFound();
            }

            var sectionId = new RewriteId(outboundRulesId);

            // Get site rule is for if applicable
            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            OutboundRulesCollection rules = OutboundRulesHelper.GetSection(site, sectionId.Path).Rules;

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(rules.Count());

            return new
            {
                rules = rules.Select(rule => OutboundRulesHelper.RuleToJsonModelRef((OutboundRule)rule, site, sectionId.Path, Context.Request.GetFields()))
            };
        }

        [HttpGet("{id}")]
        [ResourceInfo(Name = Defines.OutboundRuleName)]
        public object Get(string id)
        {
            var outboundRuleId = new OutboundRuleId(id);

            Site site = outboundRuleId.SiteId == null ? null : SiteHelper.GetSite(outboundRuleId.SiteId.Value);

            if (outboundRuleId.SiteId != null && site == null) {
                return NotFound();
            }

            OutboundRule rule = (OutboundRule)OutboundRulesHelper.GetSection(site, outboundRuleId.Path).Rules.FirstOrDefault(r => r.Name.Equals(outboundRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            return OutboundRulesHelper.RuleToJsonModel(rule, site, outboundRuleId.Path, Context.Request.GetFields());
        }

        [HttpPatch("{id}")]
        [ResourceInfo(Name = Defines.OutboundRuleName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            model = DynamicHelper.ToJObject(model);
            var outboundRuleId = new OutboundRuleId(id);

            Site site = outboundRuleId.SiteId == null ? null : SiteHelper.GetSite(outboundRuleId.SiteId.Value);

            if (outboundRuleId.SiteId != null && site == null) {
                return NotFound();
            }

            OutboundRulesSection section = OutboundRulesHelper.GetSection(site, outboundRuleId.Path);
            OutboundRule rule = (OutboundRule)section.Rules.FirstOrDefault(r => r.Name.Equals(outboundRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            OutboundRulesHelper.UpdateRule(model, rule, section);

            ManagementUnit.Current.Commit();

            dynamic updatedRule = OutboundRulesHelper.RuleToJsonModel(rule, site, outboundRuleId.Path, Context.Request.GetFields(), true);

            if (updatedRule.id != id) {
                return LocationChanged(OutboundRulesHelper.GetRuleLocation(updatedRule.id), updatedRule);
            }

            return updatedRule;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.OutboundRuleName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            model = DynamicHelper.ToJObject(model);
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model);

            if (parentId == null) {
                throw new ApiArgumentException("url_rewrite");
            }

            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            OutboundRulesSection section = OutboundRulesHelper.GetSection(site, parentId.Path, configPath);

            OutboundRule rule = OutboundRulesHelper.CreateRule(model, section);

            OutboundRulesHelper.AddRule(rule, section, model);

            ManagementUnit.Current.Commit();

            dynamic r = OutboundRulesHelper.RuleToJsonModel(rule, site, parentId.Path, Context.Request.GetFields(), true);
            return Created(OutboundRulesHelper.GetRuleLocation(r.id), r);
        }

        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            OutboundRule rule = null;
            var outboundRuleId = new OutboundRuleId(id);

            Site site = outboundRuleId.SiteId == null ? null : SiteHelper.GetSite(outboundRuleId.SiteId.Value);

            if (outboundRuleId.SiteId == null || site != null) {
                rule = (OutboundRule)OutboundRulesHelper.GetSection(site, outboundRuleId.Path).Rules.FirstOrDefault(r => r.Name.Equals(outboundRuleId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (rule != null) {
                var section = OutboundRulesHelper.GetSection(site, outboundRuleId.Path, ManagementUnit.ResolveConfigScope());

                OutboundRulesHelper.DeleteRule(rule, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
