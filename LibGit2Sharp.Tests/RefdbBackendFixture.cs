﻿using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefdbBackendFixture : BaseFixture
    {
        [Fact]
        public void CanWriteToRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);
                repo.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);
                Assert.Equal(backend.Refs["refs/heads/newref"], new RefdbBackend.ReferenceData("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644")));
            }
        }

        [Fact]
        public void CanReadFromRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);
                backend.Refs["HEAD"] = new RefdbBackend.ReferenceData("HEAD", "refs/heads/testref");
                backend.Refs["refs/heads/testref"] = new RefdbBackend.ReferenceData("refs/heads/testref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Assert.Equal("refs/heads/testref", repo.Refs["HEAD"].TargetIdentifier);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", repo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.Equal("refs/heads/testref", repo.Head.CanonicalName);
            }
        }

        private class MockRefdbBackend : RefdbBackend
        {
            public MockRefdbBackend(Repository repository) : base(repository)
            {
            }

            public SortedDictionary<string, ReferenceData> Refs { get; } = new SortedDictionary<string, ReferenceData>();

            public override bool Exists(string refName)
            {
                return Refs.ContainsKey(refName);
            }

            public override RefIterator Iterate(string glob)
            {
                return new MockRefIterator(this);
            }

            public override bool Lookup(string refName, out ReferenceData data)
            {
                return Refs.TryGetValue(refName, out data);
            }

            public override bool TryWrite(ReferenceData newRef, ReferenceData oldRef, bool force, Signature signature, string message)
            {
                ReferenceData existingRef;
                if (this.Refs.TryGetValue(newRef.RefName, out existingRef))
                {
                    Assert.NotNull(oldRef);
                    if (!existingRef.Equals(oldRef))
                    {
                        return false;
                    }
                }
                else
                {
                    Assert.Null(oldRef);
                }

                this.Refs[newRef.RefName] = newRef;
                return true;
            }

            private class MockRefIterator : RefIterator
            {
                private readonly IEnumerator<ReferenceData> enumerator;

                public MockRefIterator(MockRefdbBackend parent)
                {
                    this.enumerator = parent.Refs.Values.GetEnumerator();
                }

                public override ReferenceData GetNext()
                {
                    if (this.enumerator.MoveNext())
                    {
                        return this.enumerator.Current;
                    }

                    return null;
                }
            }
        }
    }
}
