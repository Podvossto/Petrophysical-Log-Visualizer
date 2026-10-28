using System.IO;
using System.Linq;
using PetrophysicalLogVisualizer.Core;
using Xunit;

namespace PetrophysicalLogVisualizer.Tests
{
    public class FileDuplicatesTests : System.IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "dups_" + System.Guid.NewGuid().ToString("N"));

        public FileDuplicatesTests() { Directory.CreateDirectory(folder); }
        public void Dispose() { Directory.Delete(folder, true); }

        private string Put(string name, string text)
        {
            string path = Path.Combine(folder, name);
            File.WriteAllText(path, text);
            return path;
        }

        [Fact]
        public void A_file_with_the_same_bytes_is_a_copy_of_the_first_one_in_the_list()
        {
            string a = Put("a.las", "same text here");
            string b = Put("b.las", "same text here");
            string c = Put("c.las", "same text here");
            var found = FileDuplicates.Find(new[] { a, b, c });
            Assert.Equal(2, found.Count);
            Assert.Equal(a, found[b]);
            Assert.Equal(a, found[c]);
            Assert.False(found.ContainsKey(a));   // the original is not reported
        }

        [Fact]
        public void The_original_is_the_first_in_the_order_given_not_the_first_on_disk()
        {
            string a = Put("a.las", "same text here");
            string b = Put("b.las", "same text here");
            var found = FileDuplicates.Find(new[] { b, a });
            Assert.Equal(b, found[a]);
        }

        [Fact]
        public void Same_size_but_different_bytes_is_not_a_copy()   // two such files exist in the real data
        {
            string a = Put("a.las", "abcdefgh");
            string b = Put("b.las", "abcdefgX");
            Assert.Empty(FileDuplicates.Find(new[] { a, b }));
        }

        [Fact]
        public void Different_sizes_are_never_compared()
        {
            Assert.Empty(FileDuplicates.Find(new[] { Put("a.las", "abc"), Put("b.las", "abcd") }));
        }

        [Fact]
        public void Two_groups_of_copies_are_kept_apart()
        {
            string a = Put("a.las", "first kind");
            string b = Put("b.las", "other kind");
            string c = Put("c.las", "first kind");
            string d = Put("d.las", "other kind");
            var found = FileDuplicates.Find(new[] { a, b, c, d });
            Assert.Equal(a, found[c]);
            Assert.Equal(b, found[d]);
            Assert.Equal(2, found.Count);
        }

        [Fact]
        public void Empty_files_and_files_that_are_gone_do_not_count_and_do_not_throw()
        {
            string e1 = Put("e1.las", "");
            string e2 = Put("e2.las", "");
            Assert.Empty(FileDuplicates.Find(new[] { e1, e2, Path.Combine(folder, "missing.las") }));
            Assert.Empty(FileDuplicates.Find(new string[0]));
        }
    }
}
