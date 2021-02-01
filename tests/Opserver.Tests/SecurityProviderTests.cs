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
            Assert.IsType<ActiveDirectoryProvider>(sm.CurrentProvider);
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
        public void OIDCProvider_TryValidateToken_InvalidJwt()
        {
            var oidcSettings = new OIDCSecuritySettings();
            var oidcProvider = new OIDCProvider(oidcSettings);
            Assert.False(oidcProvider.TryValidateToken(new OIDCToken("invalid"), out _));
        }

        [Fact]
        public void OIDCProvider_TryValidateToken_ValidJwt()
        {
            var oidcSettings = new OIDCSecuritySettings();
            var oidcProvider = new OIDCProvider(oidcSettings);
            var validJwt = "eyJraWQiOiJLNW4zeHplQ3M4MWRIRVJpd0xSV0hDRmh4UHlZZmdNdUswT2VYNlVZZmFrIiwiYWxnIjoiUlMyNTYifQ.eyJzdWIiOiIwMHVybnM3YzNkeHpkT0t6UjR4NiIsInZlciI6MSwiaXNzIjoiaHR0cHM6Ly9kZXYtNjQ0ODgxLm9rdGEuY29tL29hdXRoMi9hdXMyMHpocHB4YU9qRDZiZjR4NyIsImF1ZCI6IjBvYTIwejB1a2s4dU1YNG1HNHg3IiwiaWF0IjoxNjEyMjE5MzQ2LCJleHAiOjE2MTIyMjI5NDYsImp0aSI6IklELkVIOF83QVFKSXJBV0htOXZyRE1kOUhabFVlVzVJOHozNVlKRUlBcEFrTnciLCJhbXIiOlsicHdkIl0sImlkcCI6IjAwb3Juczc4cUtQV1NKV1ZDNHg2Iiwibm9uY2UiOiJhZmJjZTc2M2QyNjc0Mjc5ODZmMjgxYzQwN2I4Njg3MyIsImF1dGhfdGltZSI6MTYxMjIxNjEyMiwiYXRfaGFzaCI6ImQ0NVo1LWZYSW9ZdnRGODNBVUpjaHciLCJncm91cHMiOlsiRXZlcnlvbmUiLCJPcHNlcnZlci1BZG1pbnMiLCJPcHNlcnZlci1WaWV3Il0sIm5hbWVpZGVudGlmaWVyIjoiZHdhcmQifQ.hTdXfqKkXOqxhuTHkbM3kEG-bbuNv9kBX8_WRZ9Xq-MpTspMTlWAVbLaqpX4uLUpli5XaUGPFv1bEuwMBOP4LU2tphpPgkn1-Y9yTCjEDQG7IdInmkPmRlZq6z1INErUHlJTTT3TmHluNKv_ToI9NMfEcMD_AhGB0H2-WmTcOz6YNcY01coKQwcEBHrHpqhU-jyih8g_GtP6hTyqSsdN1RoSvQabiFM3XG0a6jCeRAo-QMNKqtiYLE7LRmWF2gwKYEuT9m4nHqtoUGzb5G5Roeuzjont610SZHt5dYe4GM1behIIWcnslHjWkwaAyGeEmZVFQRA52R87-68Kt1Kvkg";
            Assert.True(oidcProvider.TryValidateToken(new OIDCToken(validJwt), out _));
        }

        [Fact]
        public void OIDCProvider_TryValidateToken_ExtractsClaims()
        {
            var oidcSettings = new OIDCSecuritySettings();
            var oidcProvider = new OIDCProvider(oidcSettings);
            var validJwt = "eyJraWQiOiJLNW4zeHplQ3M4MWRIRVJpd0xSV0hDRmh4UHlZZmdNdUswT2VYNlVZZmFrIiwiYWxnIjoiUlMyNTYifQ.eyJzdWIiOiIwMHVybnM3YzNkeHpkT0t6UjR4NiIsInZlciI6MSwiaXNzIjoiaHR0cHM6Ly9kZXYtNjQ0ODgxLm9rdGEuY29tL29hdXRoMi9hdXMyMHpocHB4YU9qRDZiZjR4NyIsImF1ZCI6IjBvYTIwejB1a2s4dU1YNG1HNHg3IiwiaWF0IjoxNjEyMjE5MzQ2LCJleHAiOjE2MTIyMjI5NDYsImp0aSI6IklELkVIOF83QVFKSXJBV0htOXZyRE1kOUhabFVlVzVJOHozNVlKRUlBcEFrTnciLCJhbXIiOlsicHdkIl0sImlkcCI6IjAwb3Juczc4cUtQV1NKV1ZDNHg2Iiwibm9uY2UiOiJhZmJjZTc2M2QyNjc0Mjc5ODZmMjgxYzQwN2I4Njg3MyIsImF1dGhfdGltZSI6MTYxMjIxNjEyMiwiYXRfaGFzaCI6ImQ0NVo1LWZYSW9ZdnRGODNBVUpjaHciLCJncm91cHMiOlsiRXZlcnlvbmUiLCJPcHNlcnZlci1BZG1pbnMiLCJPcHNlcnZlci1WaWV3Il0sIm5hbWVpZGVudGlmaWVyIjoiZHdhcmQifQ.hTdXfqKkXOqxhuTHkbM3kEG-bbuNv9kBX8_WRZ9Xq-MpTspMTlWAVbLaqpX4uLUpli5XaUGPFv1bEuwMBOP4LU2tphpPgkn1-Y9yTCjEDQG7IdInmkPmRlZq6z1INErUHlJTTT3TmHluNKv_ToI9NMfEcMD_AhGB0H2-WmTcOz6YNcY01coKQwcEBHrHpqhU-jyih8g_GtP6hTyqSsdN1RoSvQabiFM3XG0a6jCeRAo-QMNKqtiYLE7LRmWF2gwKYEuT9m4nHqtoUGzb5G5Roeuzjont610SZHt5dYe4GM1behIIWcnslHjWkwaAyGeEmZVFQRA52R87-68Kt1Kvkg";
            Assert.True(oidcProvider.TryValidateToken(new OIDCToken(validJwt), out var claimsPrincipal));
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
