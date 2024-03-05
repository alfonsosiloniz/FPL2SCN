namespace BGLLibrary
{
    public class LibraryObject
    {
        public double Lon { get; set; } = 0;

        public double Lat { get; set; } = 0;

        public UInt32 Longitude { get; set; } = 0;

        public UInt32 Latitude { get; set; } = 0;

        public byte[] InstanceId { get; set; } = new byte[16];

        public byte[] Name { get; set; } = new byte[16];

        public float Scale { get; set; } = 1.0f;

        private UInt16 RecSize { get; set; } = 0;

        private UInt16 recType = 0x0B;
        private UInt32 altitude = 0;
        private UInt16 properties = 0x01;
        private UInt16 pitch = 0;
        private UInt16 bank = 0;
        private UInt16 heading = 0x8000;
        private UInt16 imageComplexity = 0;
        private UInt16 unk = 0;

        public LibraryObject(string name, double lon, double lat)
        {
            Latitude = Util.GetLatitudeDWORD(lat);
            Longitude = Util.GetLongitudeDWORD(lon);
            Lon = lon;
            Lat = lat;
            Name = Util.ConvertGUIToBytes(name);
        }

        public byte[] GetBytes()
        {
            List<byte[]> lHdr = new ();

            byte[] tmpArr = new byte[5000];

            lHdr.Add(BitConverter.GetBytes(recType));
            lHdr.Add(BitConverter.GetBytes(RecSize));
            lHdr.Add(BitConverter.GetBytes(Longitude));
            lHdr.Add(BitConverter.GetBytes(Latitude));
            lHdr.Add(BitConverter.GetBytes(altitude));
            lHdr.Add(BitConverter.GetBytes(properties));
            lHdr.Add(BitConverter.GetBytes(pitch));
            lHdr.Add(BitConverter.GetBytes(bank));
            lHdr.Add(BitConverter.GetBytes(heading));
            lHdr.Add(BitConverter.GetBytes(imageComplexity));
            lHdr.Add(BitConverter.GetBytes(unk));
            lHdr.Add(InstanceId);
            lHdr.Add(Name);
            lHdr.Add(BitConverter.GetBytes(Scale));

            int headerSize = Util.GetListNumberOfBytes(lHdr);
            RecSize = (UInt16)headerSize;

            // Readd the Size after computing
            lHdr[1] = BitConverter.GetBytes(RecSize);

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
    }
}