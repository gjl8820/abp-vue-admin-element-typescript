﻿using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.IdentityServer.Clients;
using Volo.Abp.Security.Encryption;
using Client = Volo.Abp.IdentityServer.Clients.Client;

namespace LINGYUN.Abp.IdentityServer.Clients
{
    [Authorize(AbpIdentityServerPermissions.Clients.Default)]
    public class ClientAppService : AbpIdentityServerAppServiceBase, IClientAppService
    {
        private IStringEncryptionService _encryptionService;
        protected IStringEncryptionService EncryptionService => LazyGetRequiredService(ref _encryptionService);

        protected IClientRepository ClientRepository { get; }

        public ClientAppService(IClientRepository clientRepository)
        {
            ClientRepository = clientRepository;
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Claims.Create)]
        public virtual async Task<ClientClaimDto> AddClaimAsync(ClientClaimCreateDto clientClaimCreate)
        {
            var client = await ClientRepository.GetAsync(clientClaimCreate.ClientId);

            client.AddClaim(clientClaimCreate.Value, clientClaimCreate.Type);
            var clientClaim = client.FindClaim(clientClaimCreate.Value, clientClaimCreate.Type);

            return ObjectMapper.Map<ClientClaim, ClientClaimDto>(clientClaim);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Properties.Create)]
        public virtual async Task<ClientPropertyDto> AddPropertyAsync(ClientPropertyCreateDto clientPropertyCreate)
        {
            var client = await ClientRepository.GetAsync(clientPropertyCreate.ClientId);

            client.AddProperty(clientPropertyCreate.Key, clientPropertyCreate.Value);
            var clientProperty = client.FindProperty(clientPropertyCreate.Key, clientPropertyCreate.Value);

            return ObjectMapper.Map<ClientProperty, ClientPropertyDto>(clientProperty);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Secrets.Create)]
        public virtual async Task<ClientSecretDto> AddSecretAsync(ClientSecretCreateDto clientSecretCreate)
        {
            var client = await ClientRepository.GetAsync(clientSecretCreate.ClientId);

            var clientSecretValue = clientSecretCreate.Value;

            // 如果是 SharedSecret 类型的密钥
            // 采用 IdentityServer4 服务器扩展方法加密
            if (IdentityServerConstants.SecretTypes.SharedSecret.Equals(clientSecretCreate.Type))
            {
                if(clientSecretCreate.HashType == HashType.Sha256)
                {
                    clientSecretValue = clientSecretCreate.Value.Sha256();
                }
                else if (clientSecretCreate.HashType == HashType.Sha512)
                {
                    clientSecretValue = clientSecretCreate.Value.Sha512();
                }
            }
            else
            {
                clientSecretValue = EncryptionService.Encrypt(clientSecretCreate.Value);
            }
            client.AddSecret(clientSecretValue, clientSecretCreate.Expiration, 
                clientSecretCreate.Type, clientSecretCreate.Description);
            
            var clientSecret = client.FindSecret(clientSecretValue, clientSecretCreate.Type);

            return ObjectMapper.Map<ClientSecret, ClientSecretDto>(clientSecret);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Create)]
        public virtual async Task<ClientDto> CreateAsync(ClientCreateDto clientCreate)
        {
            var clientIdExists = await ClientRepository.CheckClientIdExistAsync(clientCreate.ClientId);
            if(clientIdExists)
            {
                throw new UserFriendlyException(L[AbpIdentityServerErrorConsts.ClientIdExisted, clientCreate.ClientId]);
            }
            var client = new Client(GuidGenerator.Create(), clientCreate.ClientId);
            client.ClientName = clientCreate.ClientName;
            client.Description = clientCreate.Description;
            foreach(var grantType in clientCreate.AllowedGrantTypes)
            {
                client.AddGrantType(grantType.GrantType);
            }

            client = await ClientRepository.InsertAsync(client, true);

            return ObjectMapper.Map<Client, ClientDto>(client);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Delete)]
        public virtual async Task DeleteAsync(ClientGetByIdInputDto clientGetByIdInput)
        {
            await ClientRepository.DeleteAsync(clientGetByIdInput.Id);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Claims.Delete)]
        public virtual async Task DeleteClaimAsync(ClientClaimGetByKeyInputDto clientClaimGetByKey)
        {
            var client = await ClientRepository.GetAsync(clientClaimGetByKey.ClientId);
            client.RemoveClaim(clientClaimGetByKey.Value, clientClaimGetByKey.Type);
            await ClientRepository.UpdateAsync(client);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Properties.Delete)]
        public virtual async Task DeletePropertyAsync(ClientPropertyGetByKeyDto clientPropertyGetByKey)
        {
            var client = await ClientRepository.GetAsync(clientPropertyGetByKey.ClientId);
            client.RemoveProperty(clientPropertyGetByKey.Key, clientPropertyGetByKey.Value);
            await ClientRepository.UpdateAsync(client);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Secrets.Delete)]
        public virtual async Task DeleteSecretAsync(ClientSecretGetByTypeDto clientSecretGetByType)
        {
            var client = await ClientRepository.GetAsync(clientSecretGetByType.ClientId);
            client.RemoveSecret(clientSecretGetByType.Value, clientSecretGetByType.Type);
            await ClientRepository.UpdateAsync(client);
        }


        public virtual async Task<ClientDto> GetAsync(ClientGetByIdInputDto clientGetById)
        {
            var client = await ClientRepository.GetAsync(clientGetById.Id);

            return ObjectMapper.Map<Client, ClientDto>(client);
        }

        public virtual async Task<PagedResultDto<ClientDto>> GetAsync(ClientGetByPagedInputDto clientGetByPaged)
        {
            // Abp官方IdentityServer项目不支持Filter过滤...
            var clients = await ClientRepository.GetListAsync(clientGetByPaged.Sorting,
                clientGetByPaged.SkipCount, clientGetByPaged.MaxResultCount, true);

            var clientCount = await ClientRepository.GetCountAsync();

            return new PagedResultDto<ClientDto>(clientCount, 
                ObjectMapper.Map<List<Client>, List<ClientDto>>(clients));
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Update)]
        public virtual async Task<ClientDto> UpdateAsync(ClientUpdateInputDto clientUpdateInput)
        {
            var client = await ClientRepository.GetAsync(clientUpdateInput.Id);

            #region Basic Property
            client.ConcurrencyStamp = clientUpdateInput.Client.ConcurrencyStamp;
            client.ClientId = clientUpdateInput.Client.ClientId ?? client.ClientId;
            client.ClientUri = clientUpdateInput.Client.ClientUri ?? client.ClientUri;
            client.ClientName = clientUpdateInput.Client.ClientName ?? client.ClientName;
            client.AbsoluteRefreshTokenLifetime = clientUpdateInput.Client.AbsoluteRefreshTokenLifetime
                ?? client.AbsoluteRefreshTokenLifetime;
            client.AccessTokenLifetime = clientUpdateInput.Client.AccessTokenLifetime
                ?? client.AccessTokenLifetime;
            client.AccessTokenType = clientUpdateInput.Client.AccessTokenType ?? client.AccessTokenType;
            client.AllowAccessTokensViaBrowser = clientUpdateInput.Client.AllowAccessTokensViaBrowser
                ?? client.AllowAccessTokensViaBrowser;
            client.AllowOfflineAccess = clientUpdateInput.Client.AllowOfflineAccess
                ?? client.AllowOfflineAccess;
            client.AllowPlainTextPkce = clientUpdateInput.Client.AllowPlainTextPkce ?? client.AllowPlainTextPkce;
            client.AllowRememberConsent = clientUpdateInput.Client.AllowRememberConsent ?? client.AllowRememberConsent;
            client.AlwaysIncludeUserClaimsInIdToken = clientUpdateInput.Client.AlwaysIncludeUserClaimsInIdToken
                ?? client.AlwaysIncludeUserClaimsInIdToken;
            client.AlwaysSendClientClaims = clientUpdateInput.Client.AlwaysSendClientClaims ?? client.AlwaysSendClientClaims;
            client.AuthorizationCodeLifetime = clientUpdateInput.Client.AuthorizationCodeLifetime
                ?? client.AuthorizationCodeLifetime;
            client.BackChannelLogoutSessionRequired = clientUpdateInput.Client.BackChannelLogoutSessionRequired
                ?? client.BackChannelLogoutSessionRequired;

            client.BackChannelLogoutUri = clientUpdateInput.Client.BackChannelLogoutUri
                ?? client.BackChannelLogoutUri;
            client.ClientClaimsPrefix = clientUpdateInput.Client.ClientClaimsPrefix ?? client.ClientClaimsPrefix;
            client.ConsentLifetime = clientUpdateInput.Client.ConsentLifetime ?? client.ConsentLifetime;
            client.Description = clientUpdateInput.Client.Description ?? client.Description;
            client.DeviceCodeLifetime = clientUpdateInput.Client.DeviceCodeLifetime ?? client.DeviceCodeLifetime;
            client.Enabled = clientUpdateInput.Client.Enabled ?? client.Enabled;
            client.EnableLocalLogin = clientUpdateInput.Client.EnableLocalLogin ?? client.EnableLocalLogin;
            client.FrontChannelLogoutSessionRequired = clientUpdateInput.Client.FrontChannelLogoutSessionRequired
                ?? client.FrontChannelLogoutSessionRequired;
            client.FrontChannelLogoutUri = clientUpdateInput.Client.FrontChannelLogoutUri ?? client.FrontChannelLogoutUri;

            client.IdentityTokenLifetime = clientUpdateInput.Client.IdentityTokenLifetime ?? client.IdentityTokenLifetime;
            client.IncludeJwtId = clientUpdateInput.Client.IncludeJwtId ?? client.IncludeJwtId;
            client.LogoUri = clientUpdateInput.Client.LogoUri ?? client.LogoUri;
            client.PairWiseSubjectSalt = clientUpdateInput.Client.PairWiseSubjectSalt ?? client.PairWiseSubjectSalt;
            client.ProtocolType = clientUpdateInput.Client.ProtocolType ?? client.ProtocolType;
            client.RefreshTokenExpiration = clientUpdateInput.Client.RefreshTokenExpiration ?? client.RefreshTokenExpiration;
            client.RefreshTokenUsage = clientUpdateInput.Client.RefreshTokenUsage ?? client.RefreshTokenUsage;
            client.RequireClientSecret = clientUpdateInput.Client.RequireClientSecret ?? client.RequireClientSecret;
            client.RequireConsent = clientUpdateInput.Client.RequireConsent ?? client.RequireConsent;

            client.RequirePkce = clientUpdateInput.Client.RequirePkce ?? client.RequirePkce;
            client.SlidingRefreshTokenLifetime = clientUpdateInput.Client.SlidingRefreshTokenLifetime
                ?? client.SlidingRefreshTokenLifetime;
            client.UpdateAccessTokenClaimsOnRefresh = clientUpdateInput.Client.UpdateAccessTokenClaimsOnRefresh
                ?? client.UpdateAccessTokenClaimsOnRefresh;

            client.UserCodeType = clientUpdateInput.Client.UserCodeType ?? client.UserCodeType;
            client.UserSsoLifetime = clientUpdateInput.Client.UserSsoLifetime ?? client.UserSsoLifetime;
            #endregion

            #region AllowScope

            client.RemoveAllScopes();
            foreach (var scope in clientUpdateInput.Client.AllowedScopes)
            {
                client.AddScope(scope.Scope);
            }

            #endregion

            #region RedirectUris

            client.RemoveAllRedirectUris();
            foreach (var redirect in clientUpdateInput.Client.RedirectUris)
            {
                client.AddRedirectUri(redirect.RedirectUri);
            }

            #endregion

            #region AllowedGrantTypes

            client.RemoveAllAllowedGrantTypes();
            foreach (var grantType in clientUpdateInput.Client.AllowedGrantTypes)
            {
                client.AddGrantType(grantType.GrantType);
            }

            #endregion

            #region AllowedCorsOrigins

            client.RemoveAllCorsOrigins();
            foreach (var corgOrigin in clientUpdateInput.Client.AllowedCorsOrigins)
            {
                client.AddCorsOrigin(corgOrigin.Origin);
            }

            #endregion

            #region PostLogoutRedirectUris

            client.RemoveAllPostLogoutRedirectUris();
            foreach (var logoutRedirect in clientUpdateInput.Client.PostLogoutRedirectUris)
            {
                client.AddPostLogoutRedirectUri(logoutRedirect.PostLogoutRedirectUri);
            }

            #endregion

            #region IdentityProviderRestrictions

            client.RemoveAllIdentityProviderRestrictions();
            foreach (var provider in clientUpdateInput.Client.IdentityProviderRestrictions)
            {
                client.AddIdentityProviderRestriction(provider.Provider);
            }

            #endregion

            client = await ClientRepository.UpdateAsync(client, true);

            return ObjectMapper.Map<Client, ClientDto>(client);
        }

        /// <summary>
        /// 克隆客户端
        /// </summary>
        /// <remarks>
        /// 实现参考 Skoruba.IdentityServer4.Admin 项目
        /// https://github.com/skoruba/IdentityServer4.Admin.git
        /// </remarks>
        /// <param name="clientCloneInput"></param>
        /// <returns></returns>
        [Authorize(AbpIdentityServerPermissions.Clients.Clone)]
        public virtual async Task<ClientDto> CloneAsync(ClientCloneInputDto clientCloneInput)
        {
            var clientIdExists = await ClientRepository.CheckClientIdExistAsync(clientCloneInput.ClientId);
            if (clientIdExists)
            {
                throw new UserFriendlyException(L[AbpIdentityServerErrorConsts.ClientIdExisted, clientCloneInput.ClientId]);
            }
            var srcClient = await ClientRepository.GetAsync(clientCloneInput.SourceClientId);

            var client = new Client(GuidGenerator.Create(), clientCloneInput.ClientId);
            client.ClientName = clientCloneInput.ClientName;
            client.Description = clientCloneInput.Description;
            client.AbsoluteRefreshTokenLifetime = srcClient.AbsoluteRefreshTokenLifetime;
            client.AccessTokenLifetime = srcClient.AccessTokenLifetime;
            client.AccessTokenType = srcClient.AccessTokenType;
            client.AllowAccessTokensViaBrowser = srcClient.AllowAccessTokensViaBrowser;
            client.AllowOfflineAccess = srcClient.AllowOfflineAccess;
            client.AllowPlainTextPkce = srcClient.AllowPlainTextPkce;
            client.AllowRememberConsent = srcClient.AllowRememberConsent;
            client.AlwaysIncludeUserClaimsInIdToken = srcClient.AlwaysIncludeUserClaimsInIdToken;
            client.AlwaysSendClientClaims = srcClient.AlwaysSendClientClaims;
            client.AuthorizationCodeLifetime = srcClient.AuthorizationCodeLifetime;
            client.BackChannelLogoutSessionRequired = srcClient.BackChannelLogoutSessionRequired;

            client.BackChannelLogoutUri = srcClient.BackChannelLogoutUri;
            client.ClientClaimsPrefix = srcClient.ClientClaimsPrefix;
            client.ConsentLifetime = srcClient.ConsentLifetime;
            client.DeviceCodeLifetime = srcClient.DeviceCodeLifetime;
            client.Enabled = srcClient.Enabled;
            client.EnableLocalLogin = srcClient.EnableLocalLogin;
            client.FrontChannelLogoutSessionRequired = srcClient.FrontChannelLogoutSessionRequired;
            client.FrontChannelLogoutUri = srcClient.FrontChannelLogoutUri;

            client.IdentityTokenLifetime = srcClient.IdentityTokenLifetime;
            client.IncludeJwtId = srcClient.IncludeJwtId;
            client.LogoUri = srcClient.LogoUri;
            client.PairWiseSubjectSalt = srcClient.PairWiseSubjectSalt;
            client.ProtocolType = srcClient.ProtocolType;
            client.RefreshTokenExpiration = srcClient.RefreshTokenExpiration;
            client.RefreshTokenUsage = srcClient.RefreshTokenUsage;
            client.RequireClientSecret = srcClient.RequireClientSecret;
            client.RequireConsent = srcClient.RequireConsent;

            client.RequirePkce = srcClient.RequirePkce;
            client.SlidingRefreshTokenLifetime = srcClient.SlidingRefreshTokenLifetime;
            client.UpdateAccessTokenClaimsOnRefresh = srcClient.UpdateAccessTokenClaimsOnRefresh;

            client.UserCodeType = srcClient.UserCodeType;
            client.UserSsoLifetime = srcClient.UserSsoLifetime;

            if (clientCloneInput.CopyAllowedCorsOrigin)
            {
                foreach(var corsOrigin in srcClient.AllowedCorsOrigins)
                {
                    client.AddCorsOrigin(corsOrigin.Origin);
                }
            }
            if (clientCloneInput.CopyAllowedGrantType)
            {
                foreach (var grantType in srcClient.AllowedGrantTypes)
                {
                    client.AddGrantType(grantType.GrantType);
                }
            }
            if (clientCloneInput.CopyAllowedScope)
            {
                foreach (var scope in srcClient.AllowedScopes)
                {
                    client.AddScope(scope.Scope);
                }
            }
            if (clientCloneInput.CopyClaim)
            {
                foreach (var claim in srcClient.Claims)
                {
                    client.AddClaim(claim.Value, claim.Type);
                }
            }
            if (clientCloneInput.CopyIdentityProviderRestriction)
            {
                foreach (var provider in srcClient.IdentityProviderRestrictions)
                {
                    client.AddIdentityProviderRestriction(provider.Provider);
                }
            }
            if (clientCloneInput.CopyPostLogoutRedirectUri)
            {
                foreach (var uri in srcClient.PostLogoutRedirectUris)
                {
                    client.AddPostLogoutRedirectUri(uri.PostLogoutRedirectUri);
                }
            }
            if (clientCloneInput.CopyPropertie)
            {
                foreach (var property in srcClient.Properties)
                {
                    client.AddProperty(property.Key, property.Value);
                }
            }
            if (clientCloneInput.CopyRedirectUri)
            {
                foreach (var uri in srcClient.RedirectUris)
                {
                    client.AddRedirectUri(uri.RedirectUri);
                }
            }
            client = await ClientRepository.InsertAsync(client);
            return ObjectMapper.Map<Client, ClientDto>(client);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Claims.Update)]
        public virtual async Task<ClientClaimDto> UpdateClaimAsync(ClientClaimUpdateDto clientClaimUpdate)
        {
            var client = await ClientRepository.GetAsync(clientClaimUpdate.ClientId);

            var clientClaim = client.Claims.FirstOrDefault(claim => claim.Type.Equals(clientClaimUpdate.Type));
            if(clientClaim == null)
            {
                throw new UserFriendlyException(L[AbpIdentityServerErrorConsts.ClientClaimNotFound, clientClaimUpdate.Type]);
            }
            clientClaim.Value = clientClaimUpdate.Value;

            return ObjectMapper.Map<ClientClaim, ClientClaimDto>(clientClaim);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Properties.Update)]
        public virtual async Task<ClientPropertyDto> UpdatePropertyAsync(ClientPropertyUpdateDto clientPropertyUpdate)
        {
            var client = await ClientRepository.GetAsync(clientPropertyUpdate.ClientId);

            var clientProperty = client.Properties
                .FirstOrDefault(property => property.Key.Equals(clientPropertyUpdate.Key));
            if (clientProperty == null)
            {
                throw new UserFriendlyException(
                    L[AbpIdentityServerErrorConsts.ClientPropertyNotFound, clientPropertyUpdate.Key]);
            }
            clientProperty.Value = clientPropertyUpdate.Value;

            return ObjectMapper.Map<ClientProperty, ClientPropertyDto>(clientProperty);
        }

        [Authorize(AbpIdentityServerPermissions.Clients.Secrets.Update)]
        public virtual async Task<ClientSecretDto> UpdateSecretAsync(ClientSecretUpdateDto clientSecretUpdate)
        {
            var client = await ClientRepository.GetAsync(clientSecretUpdate.ClientId);

            var clientSecret = client.ClientSecrets
                .FirstOrDefault(secret => secret.Type.Equals(clientSecretUpdate.Type));
            if (clientSecret == null)
            {
                throw new UserFriendlyException(
                    L[AbpIdentityServerErrorConsts.ClientSecretNotFound, clientSecretUpdate.Type]);
            }
            var clientSecretValue = clientSecret.Value;

            // 如果是 SharedSecret 类型的密钥
            // 采用 IdentityServer4 服务器扩展方法加密
            if (IdentityServerConstants.SecretTypes.SharedSecret.Equals(clientSecretUpdate.Type))
            {
                if (clientSecretUpdate.HashType == HashType.Sha256)
                {
                    clientSecretValue = clientSecretUpdate.Value.Sha256();
                }
                else if (clientSecretUpdate.HashType == HashType.Sha512)
                {
                    clientSecretValue = clientSecretUpdate.Value.Sha512();
                }
            }
            else
            {
                clientSecretValue = EncryptionService.Encrypt(clientSecretUpdate.Value);
            }
            clientSecret.Value = clientSecretValue;

            return ObjectMapper.Map<ClientSecret, ClientSecretDto>(clientSecret);
        }
    }
}
