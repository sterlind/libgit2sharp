using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections;
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
            using (var repo = Init(backend))
            {
                Assert.Equal("baz", repo.Config.Get<string>("foo.bar").Value);
            }
        }

        [Fact]
        public void Set()
        {
            var backend = new MockConfigBackend();
            using (var repo = Init(backend))
            {
                SetValue(repo, "foo.bar", "quux");
                Assert.Equal("quux", GetValue(repo, "foo.bar"));
            }
        }

        [Fact]
        public void Delete()
        {
            var backend = new MockConfigBackend();
            using (var repo = Init(backend))
            {
                SetValue(repo, "foo.bar", "quux");
                Assert.Equal("quux", GetValue(repo, "foo.bar"));
                DeleteValue(repo, "foo.bar");
                Assert.Null(GetValue(repo, "foo.bar"));
            }
        }

        [Fact]
        public void Iterate()
        {
            var backend = new MockConfigBackend();
            backend.Entries["foo.bar"] = "baz";
            backend.Entries["foo.baz"] = "quux";
            using (var repo = Init(backend))
            {
                AssertConfigsMatch(backend.Entries, repo.Config);
            }
        }

        [Fact]
        public void LockUnlock()
        {
            var backend = new MockConfigBackend();
            using (var repo = Init(backend))
            {
                // Set fails (and we roll back) because of an exception.
                Assert.Throws<InvalidOperationException>(() => repo.Config.WithinTransaction(() =>
                {
                    SetValue(repo, "foo.bar", "abc");
                    throw new InvalidOperationException();
                }));

                Assert.Null(GetValue(repo, "foo.bar"));

                // Set succeeds (and we commit).
                repo.Config.WithinTransaction(() =>
                {
                    SetValue(repo, "foo.bar", "abc");
                });

                Assert.Equal("abc", GetValue(repo, "foo.bar"));
            }
        }

        private Repository Init(MockConfigBackend backend)
        {
            var repo = new Repository(InitNewRepository(true));
            repo.Config.AddBackend(backend, ConfigurationLevel.System, true);
            return repo;
        }

        private string GetValue(Repository repo, string key)
        {
            var entry = repo.Config.Get<string>(key, ConfigurationLevel.System);
            if (entry == null)
            {
                return null;
            }

            Assert.Equal(ConfigurationLevel.System, entry.Level);
            return entry.Value;
        }

        private void SetValue(Repository repo, string key, string value)
        {
            repo.Config.Set(key, value, ConfigurationLevel.System);
        }

        private void DeleteValue(Repository repo, string key)
        {
            Assert.True(repo.Config.Unset(key, ConfigurationLevel.System));
        }

        private static void AssertConfigsMatch(IReadOnlyDictionary<string, string> expected, IEnumerable<ConfigurationEntry<string>> actual)
        {
            var sortedActual = actual
                .Where(entry => expected.ContainsKey(entry.Key))
                .Select(entry => new Tuple<string, string>(entry.Key, entry.Value))
                .ToArray();

            Array.Sort(sortedActual);
            var sortedExpected = expected.Select(entry => new Tuple<string, string>(entry.Key, entry.Value)).ToArray();
            Array.Sort(sortedExpected);
            Assert.Equal(sortedExpected, sortedActual);
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
            public readonly SortedDictionary<string, string> Entries = new SortedDictionary<string, string>();

            public override void Del(string key)
            {
                if (!Entries.Remove(key))
                {
                    throw ConfigBackendException.NotFound(key);
                }
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
                return Entries.Keys;
            }

            public override void Lock()
            {
                throw new NotImplementedException();
            }

            public override void Set(string key, string value)
            {
                Entries[key] = value;
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
