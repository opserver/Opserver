using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
            var options = Options.Create(new SecuritySettings
            {
                Provider = ""
            });
            var sm = new SecurityManager(options, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);

            options = Options.Create(new SecuritySettings
            {
                Provider = null
            });
            sm = new SecurityManager(options, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);

            options = Options.Create(new SecuritySettings
            {
                Provider = "gibberish"
            });
            sm = new SecurityManager(options, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void ActiveDirectoryProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var options = Options.Create(new SecuritySettings
            {
                Provider = "ActiveDirectory"
            });
            var sm = new SecurityManager(options, memCache);
            Assert.IsType<ActiveDirectoryProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void EveryonesAnAdminProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var options = Options.Create(new SecuritySettings
            {
                Provider = "EveryonesAnAdmin"
            });
            var sm = new SecurityManager(options, memCache);
            Assert.IsType<EveryonesAnAdminProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void EveryonesReadOnlyProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var options = Options.Create(new SecuritySettings
            {
                Provider = "EveryonesReadOnly"
            });
            var sm = new SecurityManager(options, memCache);
            Assert.IsType<EveryonesReadOnlyProvider>(sm.CurrentProvider);
        }

        [Fact]
        public void DefaultProvider()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var options = Options.Create(new SecuritySettings());
            var sm = new SecurityManager(options, memCache);
            Assert.IsType<UnconfiguredProvider>(sm.CurrentProvider);

            var bsOptions = Options.Create(new SecuritySettings()
            {
                Provider = "admjnfajkfnajsklfnasfd"
            });
            var bsSm = new SecurityManager(bsOptions, memCache);
            Assert.IsType<UnconfiguredProvider>(bsSm.CurrentProvider);
        }
    }
}
