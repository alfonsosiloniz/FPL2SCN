using System.Globalization;
using System.Xml;
using CoordinateSharp;

namespace BGLLibrary
{
    public static class Util
    {
        public static int GetListNumberOfBytes(List<byte[]> lHdr)
        {
            int headerSize = 0;

            foreach (byte[] barr in lHdr)
            {
                headerSize += barr.Length;
            }

            return headerSize;
        }

        public static uint GetBeta(int alpha)
        {
            switch (alpha)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 2:
                    return 4;
                case 3:
                    return 5;
                default:
                    return 0;
            }
        }

        public static UInt32 DecomposeUV(byte B)
        {
            int a3, a2, a1, a0,  resto;
            uint b3, b2, b1, b0;

            (a3, resto) = Math.DivRem((int)B, 64);
            (a2, resto) = Math.DivRem(resto, 16);
            (a1, a0) = Math.DivRem(resto, 4);

            b3 = GetBeta(a3);
            b2 = GetBeta(a2);
            b1 = GetBeta(a1);
            b0 = GetBeta(a0);

            UInt32 result = (UInt32)((4096 * b3) + (256 * b2) + (16 * b1) + b0);
            return result;
        }

        public static UInt32 GetQMIDDWORDs(UInt32 U, UInt32 V, Int32 l)
        {
            byte[] _U = BitConverter.GetBytes(U);
            byte[] _V = BitConverter.GetBytes(V);

            uint U0 = DecomposeUV(_U[0]);
            uint U1 = DecomposeUV(_U[1]);
            byte U2 = _U[2];
            byte U3 = _U[3];

            uint V0 = DecomposeUV(_V[0]);
            uint V1 = DecomposeUV(_V[1]);

            byte V2 = _V[2];
            byte V3 = _V[3];

            UInt32 _Ux = (UInt32)((4096 * U3) + (256 * U2) + (16 * U1) + U0);
            int a = 2 << (2 * l);

            UInt32 A = (UInt32)a + (UInt32)(2 * ((V1 * 65536) + V0)) + (UInt32)(U1 * 65536) + (UInt32)U0;
            return A;
        }

        public static (uint u, uint v, uint l) CalcQmidFromCoord(double latitudeDeg, double longitudeDeg, int level)
        {
            int longitudeData = (int)(0.5 + ((180 + longitudeDeg) * (0x2000000 / 15)));
            int latitudeData = (int)(0.5 + ((90 - latitudeDeg) * (0x8000000 / 45)));

            if (longitudeData > 0x30000000)
            {
                longitudeData -= 0x30000000;
            }

            if (longitudeData < 0)
            {
                longitudeData += 0x30000000;
            }

            if (latitudeData > 0x20000000)
            {
                latitudeData -= 0x20000000;
            }

            if (latitudeData < 0)
            {
                latitudeData += 0x20000000;
            }

            int n = 30 - level;
            int u = longitudeData >> n;
            int v = latitudeData >> n;
            int l = level;

            return ((uint)u, (uint)v, (uint)l);
        }

        public static (uint u, uint v, uint l) CalcQmidFromDwords(UInt32 dwordA, UInt32 dwordB)
        {
            var v = 0;
            var u = 0;
            var cnt = 0x1F;
            var workDwordA = dwordA;
            var workDwordB = dwordB;

            while (cnt > 0 && (workDwordB & 0x80000000) == 0)
            {
                workDwordB <<= 2;
                workDwordB += (workDwordA & 0xC0000000) >> 30;

                workDwordA += workDwordA;
                workDwordA += workDwordA;
                cnt--;
            }

            workDwordB &= 0x7FFFFFFF;
            var level = cnt;

            while (cnt >= 0)
            {
                if ((workDwordB & 0x80000000) != 0)
                {
                    v += 1 << cnt;
                }

                if ((workDwordB & 0x40000000) != 0)
                {
                    u += 1 << cnt;
                }

                workDwordB <<= 2;
                workDwordB += (workDwordA & 0xC0000000) >> 30;
                workDwordA += workDwordA;
                workDwordA += workDwordA;
                cnt--;
            }

            return new ((uint)u, (uint)v, (uint)level);
        }

        public static List<double> GetBoundingCoordinates(uint boundingValue)
        {
            var list = new List<double>();
            var shiftValue = 15;
            var work = boundingValue;
            var latitudeData = 0U;
            var longitudeData = 0U;

            while (work < 0x80000000 && shiftValue >= 0)
            {
                shiftValue--;
                work *= 4;
            }

            work &= 0x7FFFFFFF;    // Remove negative flag, if any
            var powerOfTwo = shiftValue;

            while (shiftValue >= 0)
            {
                if ((work & 0x80000000) != 0)
                {
                    latitudeData += (uint)(1 << shiftValue);
                }

                if ((work & 0x40000000) != 0)
                {
                    longitudeData += (uint)(1 << shiftValue);
                }

                work *= 4;
                shiftValue--;
            }

            // factor = 1.0 / (2^i)
            var factor = 1.0 / (1 << powerOfTwo);

            // Calc bounding coordinates
            var minLatitudeDeg = 90.0 - ((latitudeData + 1.0) * factor * 360.0);
            var maxLatitudeDeg = 90.0 - (latitudeData * factor * 360.0);
            var minLongitude = (longitudeData * factor * 480.0) - 180.0;
            var maxLongitude = ((longitudeData + 1.0) * factor * 480.0) - 180.0;

            list.Add(minLatitudeDeg);
            list.Add(maxLatitudeDeg);
            list.Add(minLongitude);
            list.Add(maxLongitude);
            return list;
        }

        public static double GetLongitudeDecimal(byte[] coord)
        {
            double lon = (BitConverter.ToUInt32(coord) * (360.0 / (3 * 0x10000000))) - 180.00;
            return lon;
        }

        public static UInt32 GetLongitudeDWORD(double longitude)
        {
            return (UInt32)((longitude + 180) / (360.0 / (3 * 0x10000000)));
        }

        public static double GetLatitudeDecimal(byte[] coord)
        {
            double lat = 90.0 - (BitConverter.ToUInt32(coord) * (180.0 / (2 * 0x10000000)));
            return lat;
        }

        public static UInt32 GetLatitudeDWORD(double latitude)
        {
            return (UInt32)((90.0 - latitude) / (180.0 / (2 * 0x10000000)));
        }

        public static byte[] ConvertGUIToBytes(string? GUID)
        {
            // The structure of the Array of bits is weird
            // Some of them are UInt128, other UInt32 and others are sequence of individual bytes
            // The problem is to take in account the little endiannes of this sequence
            // So we need to reorder the byte[] accordingly
            // The GUID are 16 bytes
            // The GUID as String is: 1270C61D-B95D-42B1-A223-A0EF40FC640C
            byte[] result = new byte[16];
            string joinedGUID = string.Join(string.Empty, GUID?.Split("-"));
            byte[] tmpString = Convert.FromHexString(joinedGUID);

            // Map the temporary byte array to the different componentes.
            for (int i = 3, j = 0; i >= 0; i--)
            {
                Buffer.BlockCopy(tmpString, i, result, j, 1);
                j++;
            }

            for (int i = 5, j = 4; i >= 4; i--)
            {
                Buffer.BlockCopy(tmpString, i, result, j, 1);
                j++;
            }

            for (int i = 7, j = 6; i >= 6; i--)
            {
                Buffer.BlockCopy(tmpString, i, result, j, 1);
                j++;
            }

            for (int i = 8; i < 16; i++)
            {
                Buffer.BlockCopy(tmpString, i, result, i, 1);
            }

            return result;
        }

        public static string ObjectName(string id, IEnumerable<KeyValuePair<string, string>> kv)
            {
                string code = id.Split(" ")[0];
                foreach (var tuple in from tuple in kv
                                    where "PointsDefinition:" + code == tuple.Key
                                    select tuple)
                {
                    return tuple.Value;
                }

                return string.Empty;
            }

  // https://stackoverflow.com/questions/11492705/how-to-create-an-xml-document-using-xmldocument
        public static void CreateXML(List<Point> points, IEnumerable<KeyValuePair<string, string>> kv, string fileName)
        {
            XmlDocument doc = new ();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", null, null);

            XmlElement? root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement element1 = doc.CreateElement(string.Empty, "FSData", string.Empty);
            element1.SetAttribute("version", "9.0");
            doc.AppendChild(element1);

            foreach (Point point in points)
            {
                var coord = point.C;

                XmlElement element2 = doc.CreateElement(string.Empty, "SceneryObject", string.Empty);
                element2.SetAttribute("groupIndex", "1");
                element2.SetAttribute("lat", coord.Latitude.ToDouble().ToString(CultureInfo.InvariantCulture));
                element2.SetAttribute("lon", coord.Longitude.ToDouble().ToString(CultureInfo.InvariantCulture));
                Util.CalcQmidFromCoord(coord.Latitude.ToDouble(), coord.Longitude.ToDouble(), 11);

                element2.SetAttribute("alt", "0.0");
                element2.SetAttribute("pitch", "0.0");
                element2.SetAttribute("bank", "0.0");
                element2.SetAttribute("heading", "180.0");
                element2.SetAttribute("imageComplexity", "VERY_SPARSE");
                element2.SetAttribute("altitudeIsAgl", "TRUE");
                element2.SetAttribute("snapToGround", "TRUE");
                element2.SetAttribute("snapToNormal", "FALSE");

                XmlElement element3 = doc.CreateElement(string.Empty, "LibraryObject", string.Empty);

                element3.SetAttribute("name", "{" + ObjectName(point.Code, kv) + "}");
                element3.SetAttribute("scale", "1.0");

                element2.AppendChild(element3);

                // Add Coordinate
                element1.AppendChild(element2);
            }

            doc.Save(fileName);
        }

        public struct Point
        {
            public Coordinate C;
            public string Code;
            public string? Heading;
            public string? Altitude;
            public string? GUID;
        }
    }
}