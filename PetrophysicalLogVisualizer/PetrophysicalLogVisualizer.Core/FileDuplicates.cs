using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace PetrophysicalLogVisualizer.Core
{
    /// <summary>Finds files whose bytes are exactly the same as an earlier file in the list (D-24). Nothing is skipped or deleted: the caller only gets to say so in the reports.</summary>
    public static class FileDuplicates
    {
        /// <summary>
        /// copy path -> path of the first file in `paths` with the same bytes. The first of each group is the original and is not in the result.
        /// Only files of the same size are hashed, so most files are never read here. Empty files are ignored (they fail to load anyway).
        /// A file that cannot be read is left out; the loader will report it.
        /// </summary>
        public static Dictionary<string, string> Find(string[] paths)
        {
            var copies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bySize = new Dictionary<long, List<string>>();
            foreach (string path in paths)
            {
                long size;
                try { size = new FileInfo(path).Length; }
                catch (Exception) { continue; }
                if (size == 0) continue;
                List<string> group;
                if (!bySize.TryGetValue(size, out group)) { group = new List<string>(); bySize[size] = group; }
                group.Add(path);
            }

            using (SHA256 sha = SHA256.Create())
            {
                foreach (List<string> group in bySize.Values)
                {
                    if (group.Count < 2) continue;
                    var first = new Dictionary<string, string>();   // hash -> first path with it (group keeps the order of `paths`)
                    foreach (string path in group)
                    {
                        string hash;
                        try
                        {
                            using (FileStream stream = File.OpenRead(path)) hash = Convert.ToBase64String(sha.ComputeHash(stream));
                        }
                        catch (Exception) { continue; }
                        string original;
                        if (first.TryGetValue(hash, out original)) copies[path] = original;
                        else first[hash] = path;
                    }
                }
            }
            return copies;
        }
    }
}
