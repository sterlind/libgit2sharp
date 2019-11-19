using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ConfigurationBackendFixture : BaseFixture
    {
        [Fact]
        public void Get()
        {
            var backend = new MockConfigBackend();
            backend.Entries["foo.bar"] = "baz";
            using (var repo = new Repository(SandboxBareTestRepo()))
            {
                repo.Config.AddBackend(backend, ConfigurationLevel.System, true);
                Assert.Equal("baz", repo.Config.Get<string>("foo.bar").Value);
            }
        }

        private class MockConfigBackend : ConfigBackend
        {
            public readonly Dictionary<string, string> Entries = new Dictionary<string, string>();

            public override string Get(string key)
            {
                if (Entries.TryGetValue(key, out var value))
                {
                    return value;
                }

                return null;
            }
        }
    }
}
