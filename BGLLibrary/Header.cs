// <copyright file="Header.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BGLLibrary
{
    using Newtonsoft.Json.Linq;
    using Serilog;
    using Serilog.Core;

    public class Header
    {
        public const UInt32 LENGTH = 56;

        // Magic Number 01
        private UInt32 magicNumber1 = 0x19920201;

        // Header Size always 0x38
        private UInt32 headerSize = 0x38;
        private UInt32 dWLowDateTime = 0x00;
        private UInt32 dWHighDateTime = 0x00;
        private UInt32 magicNumber2 = 0x08051803;

        internal List<byte[]> LHdr { get; set; } = new ();

        private UInt32 NumberOfSections { get; set; } = 0;

        public UInt32[] QMIDs = new UInt32[8];

        public UInt32 GetNumberOfSections()
        {
            return NumberOfSections;
        }

        public void SetNumberOfSections(UInt32 NumberOfSections)
        {
            this.NumberOfSections = NumberOfSections;
        }

        public byte[] GetBytes()
        {
            byte[] tmpArr = new byte[5000];

            LHdr.Add(BitConverter.GetBytes(magicNumber1));
            LHdr.Add(BitConverter.GetBytes(this.headerSize));
            LHdr.Add(BitConverter.GetBytes(dWLowDateTime));
            LHdr.Add(BitConverter.GetBytes(dWHighDateTime));
            LHdr.Add(BitConverter.GetBytes(magicNumber2));
            LHdr.Add(BitConverter.GetBytes(NumberOfSections));

            foreach (UInt32 QMID in QMIDs)
            {
                LHdr.Add(BitConverter.GetBytes(QMID));
            }

            int headerSize = Util.GetListNumberOfBytes(LHdr);

            byte[] buffer = new byte[headerSize];

            int offset = 0;
            foreach (byte[] barr in LHdr)
            {
                Buffer.BlockCopy(barr, 0, tmpArr, offset, barr.Length);
                offset += barr.Length;
            }

            // Copy in the Buffer of Bytes
            Buffer.BlockCopy(tmpArr, 0, buffer, 0, headerSize);
            return buffer;
        }
    }
}
