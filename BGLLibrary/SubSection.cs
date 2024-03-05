// <copyright file="Bgl.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BGLLibrary
{
    using Newtonsoft.Json.Linq;
    using Serilog;
    using Serilog.Core;

    public class SubSection
    {
        public UInt32 QMID_A { get; set; } = 0;
        public UInt32 QMID_9 = 0;

        public UInt32 FileOffset { get; set; } = 0;
        public UInt32 Size = 0;
        public const UInt32 LENGTH = 16;
        private List<LibraryObject> LibraryObjects { get; set; } = new ();

        public SubSection(UInt32 QMID)
        {
            QMID_A = QMID;

            // TODO: Put this in an Util because it is twice in the document
            (uint u, uint v, uint l) = Util.CalcQmidFromDwords (QMID_A, 0);
            uint deltaLevel = l - 9;
            uint uprima = u >> (int)deltaLevel;
            uint vprima = v >> (int)deltaLevel;
            QMID_9 = Util.GetQMIDDWORDs(uprima, vprima, 9);

        }

        public void AddLibraryObject(LibraryObject lObject)
        {
            LibraryObjects.Add(lObject);

            // Recompute Size
            Size = 0;
            foreach (LibraryObject lObj in LibraryObjects)
            {
                Size += (UInt32)lObj.GetBytes().Length;
            }
        }

        internal byte[] GetHeaderBytes()
        {
            List<byte[]> lHdr = new ();

            byte[] tmpArr = new byte[5000];

            lHdr.Add(BitConverter.GetBytes(QMID_A));
            lHdr.Add(BitConverter.GetBytes((UInt32)LibraryObjects.Count));
            lHdr.Add(BitConverter.GetBytes((UInt32)FileOffset));
            lHdr.Add(BitConverter.GetBytes(Size));

            int headerSize = Util.GetListNumberOfBytes(lHdr);

            byte[] buffer = new byte[headerSize];

            int offset = 0;
            foreach (byte[] barr in lHdr)
            {
                Buffer.BlockCopy(barr, 0, tmpArr, offset, barr.Length);
                offset += barr.Length;
            }

            // Copy in the Buffer of Bytes
            Buffer.BlockCopy(tmpArr, 0, buffer, 0, headerSize);

            return buffer;
        }

        internal List<LibraryObject> GetLibraryObjects()
        {
            return LibraryObjects;
        }
    }

}
