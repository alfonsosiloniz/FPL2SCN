using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
namespace BGLLibrary;


public class Header {
    
    List<byte[]> LHdr = new();

    public const UInt32 LENGTH = 56;
    // Magic Number 01
    private UInt32 MagicNumber1 = 0x19920201;

    // Header Size always 0x38
    private UInt32 HeaderSize = 0x38;
    private UInt32 DWLowDateTime = 0x00;
    private UInt32 DWHighDateTime = 0x00;
    private UInt32 MagicNumber2 = 0x08051803;

    private UInt32 NumberOfSections = 0;

    public UInt32[] QMIDs = new UInt32[8];

    public UInt32 GetNumberOfSections() { return this.NumberOfSections;}
    public void SetNumberOfSections(UInt32 NumberOfSections) {
        this.NumberOfSections = NumberOfSections;
    }
    
    public byte[] GetBytes() {
        byte[] tmpArr = new byte[5000];

        LHdr.Add(BitConverter.GetBytes(this.MagicNumber1));
        LHdr.Add(BitConverter.GetBytes(this.HeaderSize));
        LHdr.Add(BitConverter.GetBytes(this.DWLowDateTime));
        LHdr.Add(BitConverter.GetBytes(this.DWHighDateTime));
        LHdr.Add(BitConverter.GetBytes(this.MagicNumber2));
        LHdr.Add(BitConverter.GetBytes(this.NumberOfSections));

        foreach (UInt32 QMID in QMIDs) {
            LHdr.Add(BitConverter.GetBytes(QMID));
        }

        int headerSize = Util.GetListNumberOfBytes(LHdr);
        

        byte[] buffer = new byte[headerSize];

        int offset = 0;
        foreach (byte[] barr in LHdr) {
            Buffer.BlockCopy(barr,0,tmpArr,offset,barr.Length);                
            offset += barr.Length;
        }
        // Copy in the Buffer of Bytes
        Buffer.BlockCopy(tmpArr,0,buffer,0,headerSize);
        return buffer;
    }


}

public class SceneryObjectSection {

    public const UInt32 LENGTH = 20;

    List<SubSection> SubSections = new();

    UInt32 Type = 0x25;
    UInt32 SubSectionSize = 0x01;
    UInt32 NumbeOfSubsections = 0;
    public UInt32 SubsectionFileOffset = 0;
    UInt32 TotalSizeSubsections = 0;

    public void AddSubSection(SubSection SubSec) {
        this.SubSections.Add(SubSec);
    }

    public byte[] GetHeaderBytes() {

        byte[] tmpArr = new byte[5000];
    
        List<byte[]> LHdr = new();

        LHdr.Add(BitConverter.GetBytes(this.Type));
        LHdr.Add(BitConverter.GetBytes(this.SubSectionSize));
        LHdr.Add(BitConverter.GetBytes((UInt32)this.SubSections.Count));
        LHdr.Add(BitConverter.GetBytes(this.SubsectionFileOffset));
        LHdr.Add(BitConverter.GetBytes((UInt32)(this.SubSections.Count * 16)));

        int headerSize = Util.GetListNumberOfBytes(LHdr);
        

        byte[] buffer = new byte[headerSize];
        
        int offset = 0;
        foreach (byte[] barr in LHdr) {
            Buffer.BlockCopy(barr,0,tmpArr,offset,barr.Length);                
            offset += barr.Length;
        }
        // Copy in the Buffer of Bytes
        Buffer.BlockCopy(tmpArr,0,buffer,0,headerSize);

        return buffer;
    }

    public  List<SubSection> GetSubsections()
    {
        return this.SubSections;
    }
}

public class SubSection
{
    public UInt32 QMID_A {get; set;}= 0;
    public UInt32 QMID_9 = 0;

    public UInt32 FileOffset {get; set;}= 0;
    public UInt32 Size = 0;
    public const UInt32 LENGTH = 16;
    private List<LibraryObject> LibraryObjects = [];

    public SubSection(UInt32 QMID)
    {
        this.QMID_A = QMID;

        // TODO: Put this in an Util because it is twice in the document
        (uint u, uint v, uint l) = Util.CalcQmidFromDwords (this.QMID_A,0);
        uint deltaLevel = l - 9;
        uint uprima = u >> (int)deltaLevel;
        uint vprima = v >> (int)deltaLevel;
        this.QMID_9 = Util.GetQMIDDWORDs(uprima, vprima, 9);

    }

    internal byte[] GetHeaderBytes()
    {
        List<byte[]> LHdr = new();

        byte[] tmpArr = new byte[5000];

        LHdr.Add(BitConverter.GetBytes(this.QMID_A));
        LHdr.Add(BitConverter.GetBytes((UInt32)this.LibraryObjects.Count));
        LHdr.Add(BitConverter.GetBytes((UInt32)this.FileOffset));
        LHdr.Add(BitConverter.GetBytes(this.Size));


        int headerSize = Util.GetListNumberOfBytes(LHdr);

        byte[] buffer = new byte[headerSize];
        
        int offset = 0;
        foreach (byte[] barr in LHdr) {
            Buffer.BlockCopy(barr,0,tmpArr,offset,barr.Length);                
            offset += barr.Length;
        }
        // Copy in the Buffer of Bytes
        Buffer.BlockCopy(tmpArr,0,buffer,0,headerSize);

        return buffer;
    }

    public void AddLibraryObject(LibraryObject lObject)
    {
        this.LibraryObjects.Add(lObject);
        // Recompute Size
        this.Size = 0;
        foreach (LibraryObject lObj in LibraryObjects) {
            this.Size += (UInt32)lObj.GetBytes().Length;
        }
    }

    internal List<LibraryObject> GetLibraryObjects()
    {
        return this.LibraryObjects;

        
    }
}

public class LibraryObject {
    UInt16 RecType = 0x0B;
    UInt16 RecSize {get; set;} = 0;
    public UInt32 Longitude  = 0;
    public UInt32 Latitude  = 0;

    public double lon=0;
    public double lat = 0;
    UInt32 Altitude = 0;
    UInt16 Properties = 0x01;
    UInt16 pitch = 0;
    UInt16 bank = 0;
    UInt16 heading = 0x8000;
    UInt16 imageComplexity = 0;
    UInt16 unk = 0;

    public LibraryObject(string name, double lon, double lat)
    {
        Latitude  =  Util.getLatitudeDWORD(lat);
        Longitude =  Util.getLongitudeDWORD(lon);
        this.lon = lon;
        this.lat = lat;
        this.Name = Util.ConvertGUIToBytes(name);
    }

    public byte[] instanceId {get; set; }= new byte[16];

    public byte[] Name {get; set;}= new byte[16];

    public float Scale {get; set;}= 1.0f;

    public byte[] GetBytes() {
        List<byte[]> LHdr = new();

        byte[] tmpArr = new byte[5000];

        LHdr.Add(BitConverter.GetBytes(this.RecType));
        LHdr.Add(BitConverter.GetBytes(this.RecSize));
        LHdr.Add(BitConverter.GetBytes(this.Longitude));
        LHdr.Add(BitConverter.GetBytes(this.Latitude));
        LHdr.Add(BitConverter.GetBytes(this.Altitude));
        LHdr.Add(BitConverter.GetBytes(this.Properties));
        LHdr.Add(BitConverter.GetBytes(this.pitch));
        LHdr.Add(BitConverter.GetBytes(this.bank));
        LHdr.Add(BitConverter.GetBytes(this.heading));
        LHdr.Add(BitConverter.GetBytes(this.imageComplexity));
        LHdr.Add(BitConverter.GetBytes(this.unk));
        LHdr.Add(this.instanceId);
        LHdr.Add(this.Name);
        LHdr.Add(BitConverter.GetBytes(this.Scale));


        int headerSize = Util.GetListNumberOfBytes(LHdr);
        this.RecSize = (UInt16)headerSize;

        // Readd the Size after computing
        LHdr[1] =BitConverter.GetBytes(this.RecSize);

        byte[] buffer = new byte[headerSize];
        
        int offset = 0;
        foreach (byte[] barr in LHdr) {
            Buffer.BlockCopy(barr,0,tmpArr,offset,barr.Length);                
            offset += barr.Length;
        }
        // Copy in the Buffer of Bytes
        Buffer.BlockCopy(tmpArr,0,buffer,0,headerSize);


        return buffer;
    }

}



public class Bgl
{
    
    Logger logger; 
    string filename;

    public Bgl(string filename) {
        // Create a First Section in this case
        this.Sections.Add(new SceneryObjectSection());
        this.Header.SetNumberOfSections(1); 

        this.logger = new LoggerConfiguration()
                                // add console as logging target
                                .WriteTo.Console()
                                // add debug output as logging target
                                .WriteTo.Debug()
                                // set minimum level to log
                                .MinimumLevel.Debug()
                                .CreateLogger();

        this.filename = filename;

    }
    private Header Header = new();
    private List<SceneryObjectSection> Sections = new();

    private List<LibraryObject> LibraryObjects = new();

    public void AddLibraryObject(LibraryObject Object) {
        this.LibraryObjects.Add(Object);
        
        // We compute the QMID to group this object in Subsection
        (uint u, uint v, uint l) = Util.CalcQmidFromCoord(Object.lat, Object.lon, 11);
        UInt32 QMID = Util.GetQMIDDWORDs(u,v,(int)l);
        // Look for a subsection with this QMID
        IEnumerable<SubSection> result = [];
        result = this.Sections.First().GetSubsections().Where( o => o.QMID_A == QMID);
        if (!result.Any()) {
            // There is no SubSection with this QMID. Create it
            SubSection ssec = new(QMID);
            ssec.AddLibraryObject(Object);
            this.Sections.First().AddSubSection(ssec);
            
        } else {
            // Add this Object to the SubSection
            result.First().AddLibraryObject(Object);
        }
    }

    public void BuildBGLFile() {

        logger.Information("### BUILDING BGL FILE ###");

        // Compute FileOffsets of the different elements
        UInt32 FileOffset = 0;
        FileOffset += Header.LENGTH;
        foreach (SceneryObjectSection sec in this.Sections) {
            // Only One Section in this file. After the Section it starts with SubSections. 
            // First Subsection then has this File Offset
            // This FileOffset is the one that appears in 
            FileOffset += SceneryObjectSection.LENGTH;
            
            // This section has subsections. We need to set the offset for first subsection.
            sec.SubsectionFileOffset = FileOffset;
        }

        // Now we position the SubSections in the File
        UInt32 QMID_0 = 0;
        foreach (SceneryObjectSection sec in this.Sections) {
            // First subsection starts after all the SubSections headers (I think)
            // The first Object Offset is the number of Subsections * 16 bytes
            FileOffset += (UInt32) sec.GetSubsections().Count * 16;

            List<UInt32> RecomputedQMIDs = [];

            // Let's group the Objects by QMID_9
            var GroupedSubsectionList = sec.GetSubsections()
            .OrderBy(u => u.QMID_A)
            .ToList();

            foreach (SubSection ssec2 in GroupedSubsectionList) {
                ssec2.FileOffset = FileOffset;
                // SubSections are concatenated. FileOffset for SubSections take that in account
                FileOffset += ssec2.Size;

                // Get QMID Zoom Level 9. We recompute the QMID based on the Boundary coordinates of the SubSection QMID
                // TODO We need to remove the first calculation but provide the need information to the second algorithm
                (uint u, uint v, uint l) = Util.CalcQmidFromDwords (ssec2.QMID_A,0);
                uint ltmp = l;
                uint utmp = u; uint vtmp = v;
                List<double> list = Util.GetBoundingCoordinates (ssec2.QMID_A);
                (u,v,l) = Util.CalcQmidFromCoord(list[1], list[3],9);
                QMID_0 = Util.GetQMIDDWORDs(u,v,(int)l);

                // TEST OTHER ALGORITHM < THIS IS THE GOOD ONE
                uint deltaLevel = ltmp - 9;
                uint uprima = utmp >> (int)deltaLevel;
                uint vprima = vtmp >> (int)deltaLevel;
                QMID_0 = Util.GetQMIDDWORDs(uprima, vprima, 9);
                ( u,  v,  l) = Util.CalcQmidFromDwords (QMID_0,0);

                if (!RecomputedQMIDs.Contains(QMID_0)) {
                    RecomputedQMIDs.Add(QMID_0);
                }
            }    

            int i=0;
            foreach (UInt32 QMID in RecomputedQMIDs) {
                this.Header.QMIDs[i] = QMID;
                i++;
            }

        }

        // TODO: Compute Header QMIDS algorithm
        // TODO: We need to add what happens if there more than 8 QMIDs in the header



        // Now we have all FileOffsets defined. We can build up the BGL File
        var bw = new BinaryWriter(File.Open(this.filename,FileMode.Truncate));

        logger.Information("### Writing  BINARY File ###");
        bw.Write(this.Header.GetBytes());
        foreach (SceneryObjectSection sec2 in this.Sections) {
            bw.Write(sec2.GetHeaderBytes());

            // Let's group the Objects by QMID_9
            var GroupedSubsectionList = sec2.GetSubsections()
            .OrderBy(u => u.QMID_A)
            .ToList();

            // SubSection headers
            foreach (SubSection ssec2 in GroupedSubsectionList) {
                bw.Write(ssec2.GetHeaderBytes());

            }

            foreach (SubSection ssec2 in GroupedSubsectionList) {
                foreach (LibraryObject lObj in ssec2.GetLibraryObjects()) {
                    bw.Write(lObj.GetBytes());
                }
            }
        }

        long fileSize = bw.BaseStream.Length;
        bw.Close();

        // TODO: This could be in a separate class to generate the Package independent of the BGL
        logger.Information("### Generating layout.json ###");
        // We need to update the manifest.json file with the size of the BGL
        // https://www.newtonsoft.com/json/help/html/modifyjson.htm
        var jsonFile = Directory.GetCurrentDirectory()+"/MSFS_Package/aquilon-visualpoints/layout.json";
        string json = File.ReadAllText(jsonFile);
        JObject layout = JObject.Parse(json);
        foreach (JObject content in (JArray)layout["content"]) {
            if (content["path"].ToString().Contains("visualpoints.bgl")) {
                content["size"] = fileSize;
                content["date"] = System.DateTime.Now.ToFileTime();
            }
        }
        File.WriteAllText(jsonFile, layout.ToString());
    }
}
