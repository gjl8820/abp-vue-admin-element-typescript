﻿using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.IdentityServer.Localization;
using Volo.Abp.Localization;

namespace LINGYUN.Abp.IdentityServer
{
    public class AbpIdentityServerPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var identityServerGroup = context.AddGroup(AbpIdentityServerPermissions.GroupName, L("Permissions:IdentityServer"));

            var clientPermissions = identityServerGroup.AddPermission(AbpIdentityServerPermissions.Clients.Default, L("Permissions:Clients"));
            clientPermissions.AddChild(AbpIdentityServerPermissions.Clients.Create, L("Permissions:Create"));
            clientPermissions.AddChild(AbpIdentityServerPermissions.Clients.Update, L("Permissions:Update"));
            clientPermissions.AddChild(AbpIdentityServerPermissions.Clients.Enabled, L("Permissions:Enabled"));
            clientPermissions.AddChild(AbpIdentityServerPermissions.Clients.Disabled, L("Permissions:Disabled"));
            clientPermissions.AddChild(AbpIdentityServerPermissions.Clients.Delete, L("Permissions:Delete"));
            var clientClaimPermissiosn = clientPermissions.AddChild(AbpIdentityServerPermissions.Clients.Claims.Default, L("Permissions:Clients:Claims"));
            clientClaimPermissiosn.AddChild(AbpIdentityServerPermissions.Clients.Claims.Create, L("Permissions:Create"));
            clientClaimPermissiosn.AddChild(AbpIdentityServerPermissions.Clients.Claims.Update, L("Permissions:Update"));
            clientClaimPermissiosn.AddChild(AbpIdentityServerPermissions.Clients.Claims.Delete, L("Permissions:Delete"));
        }

        protected virtual LocalizableString L(string name)
        {
            return LocalizableString.Create<AbpIdentityServerResource>(name);
        }
    }
}