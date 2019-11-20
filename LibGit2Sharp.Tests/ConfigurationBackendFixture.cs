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

        private class MockReadOnlyConfigBackend : ReadOnlyConfigBackend
        {
            private readonly IReadOnlyDictionary<string, string> entries;

            public MockReadOnlyConfigBackend(IReadOnlyDictionary<string, string> entries)
            {
                this.entries = entries;
            }

            public override string Get(string key)
            {
                if (entries.TryGetValue(key, out var value))
                {
                    return value;
                }

                return null;
            }

            public override IEnumerable<string> Iterate()
            {
                return entries.Keys;
            }
        }

        private class MockConfigBackend : ReadWriteConfigBackend
        {
            public readonly Dictionary<string, string> Entries = new Dictionary<string, string>();

            public override void Del(string key)
            {
                throw new NotImplementedException();
            }

            public override void DelMultiVar(string name, string regexp)
            {
                throw new NotImplementedException();
            }

            public override string Get(string key)
            {
                if (Entries.TryGetValue(key, out var value))
                {
                    return value;
                }

                return null;
            }

            public override IEnumerable<string> Iterate()
            {
                throw new NotImplementedException();
            }

            public override void Lock()
            {
                throw new NotImplementedException();
            }

            public override void Set(string key, string value)
            {
                throw new NotImplementedException();
            }

            public override void SetMultiVar(string name, string regexp, string value)
            {
                throw new NotImplementedException();
            }

            public override ReadOnlyConfigBackend Snapshot()
            {
                return new MockReadOnlyConfigBackend(Entries);
            }

            public override void Unlock(bool success)
            {
                throw new NotImplementedException();
            }
        }
    }
}
