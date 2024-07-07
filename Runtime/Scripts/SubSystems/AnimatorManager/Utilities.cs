using System.Collections.Generic;

namespace NAK.AASEmulator.Runtime.SubSystems
{
    public static class Utilities
    {
        public static void ByteToBoolArrayNoAlloc(IReadOnlyList<byte> bytes, ref bool[] bools)
        {
            // if (bools.Length * 8 != bytes.Count)
            // {
            //     Debug.LogError("ByteToBoolArrayNoAlloc: bools.Length * 8 != bytes.Count");
            //     return;
            // }
            
            for (int i = 0; i < bools.Length; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;
                bools[i] = (bytes[byteIndex] & (1 << bitIndex)) != 0;
            }
        }
    }
}