using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace MemRead {
    static class ByteArrayRocks {
        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate, byte[] mask) {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++) {
                if (!IsMatch(self, i, candidate, mask))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate, byte[] mask) {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++) {
                if (mask[i] == 0x00) {
                    if (array[position + i] != candidate[i]) {
                        return false;
                    }
                } else if (mask[i] != 0xFF) {
                    if ((array[position + i] & mask[i]) != (array[position + i] & candidate[i])) {
                        return false;
                    }
                }
                 
            }
            return true;
        }

        static void Test(this byte[] self, byte[] candidate, byte[] mask, int i, List<int> list) {
            if (IsMatch(self, i, candidate, mask)) {
                list.Add(i);
            }
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate) {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }
    }
}
