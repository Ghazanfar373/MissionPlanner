using System;
using System.Collections.Generic;
using System.IO;

namespace MissionPlanner.Radio
{
    public class IHex : SortedList<uint, byte[]>
    {
        public delegate void LogEventHandler(string message, int level = 0);

        public delegate void ProgressEventHandler(double completed);

        public bool bankingDetected;

        private readonly SortedList<uint, uint> merge_index;

        private uint upperaddress;

        public IHex()
        {
            merge_index = new SortedList<uint, uint>();
        }

        public event LogEventHandler LogEvent;

        public event ProgressEventHandler ProgressEvent;

        public void load(string fromPath)
        {
            var sr = new StreamReader(fromPath);
            uint loadedSize = 0;

            // discard anything we might previous have loaded
            Clear();
            merge_index.Clear();

            log(string.Format("reading from {0}\n", Path.GetFileName(fromPath)));

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();

                // every line must start with a :
                if (!line.StartsWith(":"))
                    throw new Exception("invalid IntelHex file");

                if (ProgressEvent != null)
                    ProgressEvent(sr.BaseStream.Position / (double)sr.BaseStream.Length);

                // parse the record type and data length, assume ihex8
                // ignore the checksum
                var length = Convert.ToByte(line.Substring(1, 2), 16);
                var address = Convert.ToUInt32(line.Substring(3, 4), 16);
                var rtype = Convert.ToByte(line.Substring(7, 2), 16);

                // handle type zero (data) records
                if (rtype == 0)
                {
                    var b = new byte[length];
                    var hexbytes = line.Substring(9, length * 2);

                    // convert hex bytes
                    for (var i = 0; i < length; i++)
                    {
                        b[i] = Convert.ToByte(hexbytes.Substring(i * 2, 2), 16);
                    }

                    // add for banking address
                    address += upperaddress << 16;

                    log(string.Format("ihex: 0x{0:X}: {1}\n", address, length), 1);
                    loadedSize += length;

                    // and add to the list of ranges
                    insert(address, b);
                }
                else if (rtype == 4 && length == 2 && address == 0)
                {
                    bankingDetected = true;
                    upperaddress = Convert.ToUInt32(line.Substring(9, 4), 16);
                }
            }
            if (Count < 1)
                throw new Exception("no data in IntelHex file");
            log(string.Format("read {0} bytes from {1}\n", loadedSize, fromPath));

            sr.Close();
        }

        private void log(string message, int level = 0)
        {
            if (LogEvent != null)
                LogEvent(message, level);
        }

        private void idx_record(uint start, byte[] data)
        {
            var len = (uint)data.GetLength(0);

            merge_index.Add(start + len, start);
        }
        
        private void idx_remove(uint start, byte[] data)
        {
            var len = (uint)data.GetLength(0);

            merge_index.Remove(start + len);
        }

        private bool idx_find(uint start, out uint other)
        {
            return merge_index.TryGetValue(start, out other);
        }

        public void insert(uint key, byte[] data)
        {
            uint other;
            byte[] mergedata;

            // value of the key that would come after this one
            other = key;
            other += (uint)data.GetLength(0);

            // can we merge with the next block
            if (TryGetValue(other, out mergedata))
            {
                var oldlen = data.GetLength(0);

                // remove the next entry, we are going to merge with it
                Remove(other);

                // remove its index entry as well
                idx_remove(other, mergedata);

                log(string.Format("ihex: merging {0:X}/{1} with next {2:X}/{3}\n",
                    key, data.GetLength(0),
                    other, mergedata.GetLength(0)), 1);

                // resize the data array and append data from the next block
                Array.Resize(ref data, data.GetLength(0) + mergedata.GetLength(0));
                Array.Copy(mergedata, 0, data, oldlen, mergedata.GetLength(0));
            }

            // look up a possible adjacent preceding block in the merge index
            if (idx_find(key, out other))
            {
                mergedata = this[other];
                var oldlen = mergedata.GetLength(0);
                Remove(other);
                idx_remove(other, mergedata);

                log(string.Format("ihex: merging {0:X}/{1} with prev {2:X}/{3}\n",
                    key, data.GetLength(0),
                    other, mergedata.GetLength(0)), 1);

                Array.Resize(ref mergedata, data.GetLength(0) + mergedata.GetLength(0));
                Array.Copy(data, 0, mergedata, oldlen, data.GetLength(0));
                key = other;
                data = mergedata;
            }

            // add the merged block
            Add(key, data);
            idx_record(key, data);
            log(string.Format("ihex: adding {0:X}/{1}\n", key, data.GetLength(0)), 1);
        }
    }
}