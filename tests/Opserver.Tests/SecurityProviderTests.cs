using System;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Opserver.Security;
using Xunit;

namespace Opserver.Tests
{
    public class SecurityProviderTests
    {
        [Fact]
        public void UnconfiguredProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "";
                })
                .BuildServiceProvider();

            var sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);

            serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = null;
                })
                .BuildServiceProvider();

            sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);

            serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "gibberish";
                })
                .BuildServiceProvider();
            sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void ActiveDirectoryProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "ActiveDirectory";
                })
                .BuildServiceProvider();

            var sm = new SecurityManager(serviceProvider, memCache);
            if (OperatingSystem.IsWindows())
            {
                Assert.IsType<ActiveDirectoryProvider>(sm.CurrentProvider);
            }
        }

        [Fact]
        public void EveryonesAnAdminProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "EveryonesAnAdmin";
                })
                .BuildServiceProvider();

            var sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<EveryonesAnAdminProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void EveryonesReadOnlyProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "EveryonesReadOnly";
                })
                .BuildServiceProvider();
            var sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<EveryonesReadOnlyProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void OIDCProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "OIDC";
                })
                .BuildServiceProvider();

            var sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<OIDCProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void OIDCProvider_TryValidateToken_ExtractsClaims()
        {
            var oidcSettings = new OIDCSecuritySettings();
            var oidcProvider = new OIDCProvider(oidcSettings);
            var claims = new[]
            {
                new Claim(oidcSettings.GroupsClaim, "Everyone"),
                new Claim(oidcSettings.GroupsClaim, "Opserver-Admins"),
                new Claim(oidcSettings.GroupsClaim, "Opserver-View"),
                new Claim(oidcSettings.NameClaim, "dward"),
            };

            Assert.True(oidcProvider.TryValidateToken(new OIDCToken(claims), out var claimsPrincipal));
            Assert.NotNull(claimsPrincipal);
            Assert.True(claimsPrincipal.HasClaim(c => c.Type == ClaimTypes.Name));
            Assert.True(claimsPrincipal.HasClaim(c => c.Type == Security.OIDCProvider.GroupsClaimType));
            Assert.Collection(
                claimsPrincipal.Claims,
                c =>
                {
                    Assert.Equal(Security.OIDCProvider.GroupsClaimType, c.Type);
                    Assert.Equal("Everyone", c.Value);
                },
                c =>
                {
                    Assert.Equal(Security.OIDCProvider.GroupsClaimType, c.Type);
                    Assert.Equal("Opserver-Admins", c.Value);
                },
                c =>
                {
                    Assert.Equal(Security.OIDCProvider.GroupsClaimType, c.Type);
                    Assert.Equal("Opserver-View", c.Value);
                },
                c =>
                {
                    Assert.Equal(ClaimTypes.Name, c.Type);
                    Assert.Equal("dward", c.Value);
                }
            );
        }

        [Fact]
        public void DefaultProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings => { })
                .BuildServiceProvider();
            var sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);

            serviceProvider = new ServiceCollection()
                .Configure<SecuritySettings>(settings =>
                {
                    settings.Provider = "admjnfajkfnajsklfnasfd";
                })
                .BuildServiceProvider();
            sm = new SecurityManager(serviceProvider, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);
        }
    }
}
