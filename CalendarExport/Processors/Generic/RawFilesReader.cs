using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CalendarExport.Processors
{
    public class RawFilesReader : IEnumerable<Stream>
    {
        private readonly IEnumerable<string> Paths;
        private readonly bool ReadFully;
        private readonly bool DeleteSource;

        public RawFilesReader(IEnumerable<string> paths, bool readFully = true, bool deleteSource = false)
        {
            this.Paths = paths;
            this.ReadFully = readFully;
            this.DeleteSource = deleteSource;
        }

        public IEnumerator<Stream> GetEnumerator()
        {
            foreach (var path in this.Paths)
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Stream toReturn = fs;

                    if (this.ReadFully)
                    {
                        MemoryStream ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        toReturn = ms;
                    }

                    yield return toReturn;
                }

                if (this.DeleteSource)
                {
                    File.Delete(path);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
