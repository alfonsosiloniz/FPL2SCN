using System.Runtime.CompilerServices;

namespace BGLLibrary;
public static class Util {
    public static int GetListNumberOfBytes(List<byte[]> lHdr)
    {
        int  headerSize = 0;
        foreach (byte[] barr in lHdr) {
            headerSize+= barr.Length;
        }

        return headerSize;
    }


    public static uint GetBeta(int alpha) {
        switch (alpha) {
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
    public static UInt32 decomposeUV(byte B) {
        
        int a3, a2, a1 ,a0,  resto;
        uint b3, b2, b1 ,b0;

        (a3, resto) = Math.DivRem((int)B, 64);
        (a2, resto) = Math.DivRem(resto, 16 );
        (a1, a0) = Math.DivRem(resto, 4 );

        b3 = GetBeta(a3);
        b2 = GetBeta(a2);
        b1 = GetBeta(a1);
        b0 = GetBeta(a0);

        UInt32 result = (UInt32) (4096*b3 + 256*b2 + 16*b1  + b0);
        return result;

    }

    public static UInt32 GetQMIDDWORDs (UInt32 U, UInt32 V, Int32 l) {
        //TODO: What happens with U2 & U3
        byte[] _U = BitConverter.GetBytes(U);
        byte[] _V = BitConverter.GetBytes(V);

        uint U0 = decomposeUV(_U[0]);
        uint U1 = decomposeUV(_U[1]);
        byte U2 = _U[2];
        byte U3 = _U[3];

        uint V0 = decomposeUV(_V[0]);
        uint V1 = decomposeUV(_V[1]);

        byte V2 = _V[2];
        byte V3 = _V[3];

        UInt32 _Ux = (UInt32) (4096*U3 + 256*U2 + 16*U1 + U0);
        int a = 2 << (2*l);

        UInt32 A = (UInt32)a + (UInt32) (2 * (V1 * 65536 + V0)) + (UInt32)(U1 * 65536) + (UInt32)U0;
        return A;


    }
     public static (uint, uint, uint) CalcQmidFromCoord (double LatitudeDeg, double LongitudeDeg, int level) {
        int LongitudeData = (int)(0.5 + (180 + LongitudeDeg) * (0x2000000 / 15));
        int LatitudeData = (int)(0.5 + (90 - LatitudeDeg) * (0x8000000 / 45));

        if (LongitudeData > 0x30000000)
            LongitudeData -= 0x30000000;

        if (LongitudeData < 0 )
            LongitudeData += 0x30000000;

        if (LatitudeData > 0x20000000) 
            LatitudeData -= 0x20000000;
        if (LatitudeData < 0)
            LatitudeData += 0x20000000;

        int n = 30 - level;
        int u = LongitudeData >> n;
        int v = LatitudeData >> n;
        int l = level;

        return ((uint)u,(uint)v,(uint)l);
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
                v += (1 << cnt);
            }

            if ((workDwordB & 0x40000000) != 0)
            {
                u += (1 << cnt);
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
        var latitudeData = (uint)0;
        var longitudeData = (uint)0;

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


    public static double getLongitudeDecimal (byte[] Coord) {
        double Lon = (BitConverter.ToUInt32((Coord))  * (360.0 / (3 * 0x10000000))) - 180.00;
        return Lon;
    }

    public static UInt32 getLongitudeDWORD (double longitude) {
        return (UInt32)((longitude + 180) / (360.0 / (3 * 0x10000000)));
    }

    public static double getLatitudeDecimal (byte[] Coord) {
        double Lat = 90.0 - BitConverter.ToUInt32((Coord)) * (180.0 / (2 * 0x10000000));
        return Lat;
    }

    public static UInt32 getLatitudeDWORD (double latitude) {
        return (UInt32)((90.0 - latitude) / (180.0 / (2 * 0x10000000)));
    }


    public static byte[] ConvertGUIToBytes (string? GUID) {
        // The structure of the Array of bits is weird
        // Some of them are UInt128, other UInt32 and others are sequence of individual bytes
        // The problem is to take in account the little endiannes of this sequence
        // So we need to reorder the byte[] accordingly
        // The GUID are 16 bytes
        // The GUID as String is: 1270C61D-B95D-42B1-A223-A0EF40FC640C
        
        byte[] result = new byte[16];
        string JoinedGUID = string.Join(string.Empty, GUID.Split("-"));
        byte[] tmpString = Convert.FromHexString(JoinedGUID);

        // Map the temporary byte array to the different componentes.
        for (int i=3,j=0; i>=0; i--) {
            Buffer.BlockCopy(tmpString,i,result,j,1);    
            j++;
        }
        for (int i=5,j=4; i>=4; i--) {
            Buffer.BlockCopy(tmpString,i,result,j,1);    
            j++;
        }
        for (int i=7,j=6; i>=6; i--) {
            Buffer.BlockCopy(tmpString,i,result,j,1);    
            j++;
        }
        for (int i=8; i<16; i++)
            Buffer.BlockCopy(tmpString,i,result,i,1);

        return result;
    }

}