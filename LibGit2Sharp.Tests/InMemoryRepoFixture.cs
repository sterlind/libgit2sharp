using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace LibGit2Sharp.Tests
{
    public class InMemoryRepoFixture
    {
        private readonly ITestOutputHelper output;

        public InMemoryRepoFixture(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Basic()
        {
            using (var repo = new Repository())
            {
                var odb = new MockOdbBackend();
                var refdb = new MockRefdbBackend(repo);
                repo.ObjectDatabase.AddBackend(odb, 5);
                repo.Refs.SetBackend(refdb);

                var blob = repo.ObjectDatabase.Write<Blob>(Encoding.UTF8.GetBytes("hello world"));

                var td = new TreeDefinition();
                td.Add("hello.txt", blob, Mode.NonExecutableFile);
                var tree = repo.ObjectDatabase.CreateTree(td);
                var sig = new Signature("Alice Q. Unit-Test", "aqu@example.com", DateTime.Now);
                var commit = repo.ObjectDatabase.CreateCommit(
                    sig,
                    sig,
                    "First post!",
                    tree,
                    repo.Commits,
                    false);

                repo.Refs.Add("HEAD", commit.Id);

                output.WriteLine("Refs:");
                foreach (var reference in repo.Refs)
                {
                    output.WriteLine(reference.CanonicalName);
                }
            }
        }

        private class MockRefdbBackend : RefdbBackend
        {
            private readonly Dictionary<string, ReferenceData> refs = new Dictionary<string, ReferenceData>();

            public MockRefdbBackend(Repository repository) : base(repository)
            {
            }

            public override void Delete(ReferenceData existingRef)
            {
                if (!refs.Remove(existingRef.RefName))
                {
                    throw RefdbBackendException.NotFound(existingRef.RefName);
                }
            }

            public override bool Exists(string refName)
            {
                return refs.ContainsKey(refName);
            }

            public override IEnumerable<ReferenceData> Iterate(string glob)
            {
                if (string.IsNullOrEmpty(glob))
                {
                    return refs.Values;
                }
                else
                {
                    var globRegex = new Regex("^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$");
                    return refs.Values.Where(r => globRegex.IsMatch(r.RefName));
                }
            }

            public override bool Lookup(string refName, out ReferenceData data)
            {
                return refs.TryGetValue(refName, out data);
            }

            public override ReferenceData Rename(string oldName, string newName, bool force, Signature signature, string message)
            {
                throw new NotImplementedException();
            }

            public override void Write(ReferenceData newRef, ReferenceData oldRef, bool force, Signature signature, string message)
            {
                ReferenceData existingRef;
                if (!force && refs.TryGetValue(newRef.RefName, out existingRef) && !existingRef.Equals(oldRef))
                {
                    throw RefdbBackendException.Exists(newRef.RefName);
                }

                refs[newRef.RefName] = newRef;
            }
        }

        private class MockOdbBackend : OdbBackend
        {
            private readonly Dictionary<ObjectId, byte[]> blobs = new Dictionary<ObjectId, byte[]>();
            private readonly Dictionary<ObjectId, ObjectType> types = new Dictionary<ObjectId, ObjectType>();

            protected override OdbBackendOperations SupportedOperations =>
                OdbBackendOperations.Exists |
                OdbBackendOperations.Read |
                OdbBackendOperations.Write;

            public override bool Exists(ObjectId id)
            {
                return blobs.ContainsKey(id);
            }

            public override int ExistsPrefix(string shortSha, out ObjectId found)
            {
                throw new NotImplementedException();
            }

            public override int ForEach(ForEachCallback callback)
            {
                throw new NotImplementedException();
            }

            public override int Read(ObjectId id, out UnmanagedMemoryStream data, out ObjectType objectType)
            {
                data = null;
                objectType = 0;
                byte[] bytes;
                if (!blobs.TryGetValue(id, out bytes))
                {
                    return (int)ReturnCode.GIT_ENOTFOUND;
                }

                objectType = types[id];
                data = Allocate(bytes.Length);
                data.Write(bytes, 0, bytes.Length);
                return (int)ReturnCode.GIT_OK;
            }

            public override int ReadHeader(ObjectId id, out int length, out ObjectType objectType)
            {
                throw new NotImplementedException();
            }

            public override int ReadPrefix(string shortSha, out ObjectId oid, out UnmanagedMemoryStream data, out ObjectType objectType)
            {
                throw new NotImplementedException();
            }

            public override int ReadStream(ObjectId id, out OdbBackendStream stream)
            {
                throw new NotImplementedException();
            }

            public override int Write(ObjectId id, Stream dataStream, long length, ObjectType objectType)
            {
                using (var ms = new MemoryStream())
                {
                    dataStream.CopyTo(ms);
                    blobs[id] = ms.ToArray();
                    types[id] = objectType;
                }

                return (int)ReturnCode.GIT_OK;
            }

            public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
            {
                throw new NotImplementedException();
            }
        }
    }
}
