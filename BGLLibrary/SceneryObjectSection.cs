// <copyright file="SceneryObjectSection.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BGLLibrary
{
    public class SceneryObjectSection
    {

        public const UInt32 LENGTH = 20;

        List<SubSection> SubSections = new ();

        UInt32 Type = 0x25;
        UInt32 SubSectionSize = 0x01;
        public UInt32 SubsectionFileOffset = 0;

        public void AddSubSection(SubSection SubSec)
        {
            SubSections.Add(SubSec);
        }

        public byte[] GetHeaderBytes()
        {
            byte[] tmpArr = new byte[5000];
            List<byte[]> lHdr = new ();

            lHdr.Add(BitConverter.GetBytes(Type));
            lHdr.Add(BitConverter.GetBytes(SubSectionSize));
            lHdr.Add(BitConverter.GetBytes((UInt32)SubSections.Count));
            lHdr.Add(BitConverter.GetBytes(SubsectionFileOffset));
            lHdr.Add(BitConverter.GetBytes((UInt32)(SubSections.Count * 16)));

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

        public List<SubSection> GetSubsections()
        {
            return SubSections;
        }
    }

}
