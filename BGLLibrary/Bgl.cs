namespace BGLLibrary
{
    using Newtonsoft.Json.Linq;
    using Serilog;
    using Serilog.Core;
    using static BGLLibrary.Util;

    public class Bgl
    {
        private Logger logger;
        private string filename;

        private List<QMID> SubSectionsQMIDs { get; set; } = new ();

        public Bgl(string filename)
        {
            // Create a First Section in this case
            Sections.Add(new SceneryObjectSection());
            Header.SetNumberOfSections(1);

            logger = new LoggerConfiguration()
                                    .WriteTo.Console()
                                    .WriteTo.Debug()
                                    .MinimumLevel.Information()
                                    .CreateLogger();

            this.filename = filename;
        }

        private Header Header { get; set; } = new ();

        private List<SceneryObjectSection> Sections { get; set; } = new ();

        private List<LibraryObject> LibraryObjects { get; set; } = new ();

        public void AddLibraryObject(LibraryObject obj)
        {
            LibraryObjects.Add(obj);

            // We compute the QMID to group this object in Subsection
            (uint u, uint v, uint l) = Util.CalcQmidFromCoord(obj.Lat, obj.Lon, 11);
            SubSectionsQMIDs.Add(new Bgl.QMID(u, v, l));

            UInt32 QMID = Util.GetQMIDDWORDs(u, v, (int)l);

            // Look for a subsection with this QMID
            IEnumerable<SubSection> result = [];
            result = Sections.First().GetSubsections().Where(o => o.QMID_A == QMID);

            if (!result.Any())
            {
                // There is no SubSection with this QMID. Create it
                SubSection ssec = new (QMID);
                ssec.AddLibraryObject(obj);
                Sections.First().AddSubSection(ssec);
            }
            else
            {
                // Add this Object to the SubSection
                result.First().AddLibraryObject(obj);
            }
        }

        public void BuildBGLFile()
        {
            logger.Information("### BUILDING BGL FILE ###");

            // Compute FileOffsets of the different elements
            UInt32 fileOffset = 0;
            fileOffset += Header.LENGTH;
            foreach (SceneryObjectSection sec in Sections)
            {
                // Only One Section in this file. After the Section it starts with SubSections.
                // First Subsection then has this File Offset
                // This FileOffset is the one that appears in
                fileOffset += SceneryObjectSection.LENGTH;

                // This section has subsections. We need to set the offset for first subsection.
                sec.SubsectionFileOffset = fileOffset;
            }

            // Now we position the SubSections in the File
            UInt32 QMID_0 = 0;
            foreach (SceneryObjectSection sec in Sections)
            {
                // First subsection starts after all the SubSections headers (I think)
                // The first Object Offset is the number of Subsections * 16 bytes
                fileOffset += (UInt32)sec.GetSubsections().Count * 16;

                List<UInt32> recomputedQMIDs = new ();

                // Let's group the Objects by QMID_9
                var groupedSubsectionList = sec.GetSubsections()
                    .OrderBy(u => u.QMID_A)
                    .ToList();

                foreach (SubSection ssec2 in groupedSubsectionList)
                {
                    ssec2.FileOffset = fileOffset;

                    // SubSections are concatenated. FileOffset for SubSections take that in account
                    fileOffset += ssec2.Size;

                    // Get QMID Zoom Level 9. We recompute the QMID based on the Boundary coordinates of the SubSection QMID
                    // TODO We need to remove the first calculation but provide the need information to the second algorithm
                    (uint u, uint v, uint l) = Util.CalcQmidFromDwords(ssec2.QMID_A, 0);
                    uint ltmp = l;
                    uint utmp = u;
                    uint vtmp = v;

                    List<double> list = Util.GetBoundingCoordinates(ssec2.QMID_A);
                    (u, v, l) = Util.CalcQmidFromCoord(list[1], list[3], 9);
                    QMID_0 = Util.GetQMIDDWORDs(u, v, (int)l);

                    // TEST OTHER ALGORITHM < THIS IS THE GOOD ONE
                    // TODO Clean this code
                    uint deltaLevel = ltmp - 9;
                    uint uprima = utmp >> (int)deltaLevel;
                    uint vprima = vtmp >> (int)deltaLevel;
                    QMID_0 = Util.GetQMIDDWORDs(uprima, vprima, 9);
                    (u, v, l) = Util.CalcQmidFromDwords(QMID_0, 0);

                    if (!recomputedQMIDs.Contains(QMID_0))
                    {
                        recomputedQMIDs.Add(QMID_0);
                    }
                }

                // First part of Header QMID Algorithm. Number QMIDs < 9
                if (recomputedQMIDs.Count < 9)
                {
                    int i = 0;
                    foreach (UInt32 QMID in recomputedQMIDs)
                    {
                        if (i < 8)
                        {
                            Header.QMIDs[i] = QMID;
                        }

                        i++;
                    }
                }
                else
                {
                    // More than 8 header QMIDs. We need to find the minimum that
                    // cover all the scenery area
                    List<QMID> headerQMIDs = new ();
                    headerQMIDs.Add(new QMID(0, 0, 0));

                    bool entryAdded = false;
                    int iteration = 0;

                    do
                    {
                        List<QMID> tmpQMIDs = new (headerQMIDs);

                        entryAdded = false;
                        logger.Debug("Iteration: {0} // Count: {1}, {2}",  iteration++, headerQMIDs.Count(), tmpQMIDs.Count());

                        foreach (QMID qmid in headerQMIDs)
                        {
                            logger.Debug("{0}", qmid);
                        }

                        logger.Debug("**********************");

                        // foreach (QMID qmid in tmpQMIDs)
                        int tmpIndex = 0;
                        foreach ((QMID qmid, int index) in tmpQMIDs.Select((item, index) => (item, index)))
                        {
                            logger.Debug("Index{0} QMID: {1}", index, qmid);
                            if (qmid.L < 9)
                            {
                                // compute subQmids
                                // uprima = { u*2 , u*2 + 1 }
                                // vprima = { v*2 , v*2 + 1}
                                var match = new List<QMID>();
                                for (int i = 0; i < 2; i++)
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        QMID evalQMID = new ((uint)((qmid.U * 2) + i), (uint)((qmid.V * 2) + j), qmid.L + 1);

                                        // subQmid Match
                                        var OrderedSubSectionsQMIDs = SubSectionsQMIDs.OrderBy(u => u.QMIDWORD).ToList();

                                        if (LocalMatchQMID(evalQMID, SubSectionsQMIDs))
                                        {
                                            match.Add(evalQMID);
                                            logger.Debug("subqmid MATCHED {0}", evalQMID);
                                        }
                                        else
                                        {
                                            logger.Debug("subqmid NOT MATCHED {0}", evalQMID);
                                        }
                                    }
                                }

                                if (match.Count == 1)
                                {
                                    // Only one match. We replace this element in the header List
                                    logger.Debug("REPLACED QMID {0} with QMID {1} in index {2}", qmid, match[0], tmpIndex);
                                    headerQMIDs[tmpIndex] = match[0];
                                    entryAdded = true;
                                }
                                else
                                {
                                    if (match.Count == 0)
                                    {
                                        logger.Debug("QMID MOT FOUND {0}", qmid);
                                        break;
                                    }
                                    else
                                    {
                                        if (headerQMIDs.Count() + match.Count() < 10)
                                        {
                                            logger.Debug("Removing original element at index {0}", tmpIndex);
                                            headerQMIDs.RemoveAt(tmpIndex);
                                            logger.Debug("Inserting the following list in index {0}", tmpIndex);
                                            int z = tmpIndex;
                                            tmpIndex--;
                                            foreach (QMID m in match)
                                            {
                                                logger.Debug("{0} at index {1}", m, z);
                                                headerQMIDs.Insert(z++, m);
                                                tmpIndex++;
                                            }

                                            entryAdded = true;
                                        }
                                        else
                                        {
                                            logger.Debug("More than 8 QMIDs: Found  {0} ",  match.Count());
                                        }
                                    }
                                }
                            }

                            tmpIndex++;
                        }
                    }
                    while (entryAdded && iteration < 50);

                    logger.Debug("FINAL QMID HEADER LIST");
                    int w = 0;
                    foreach (QMID qmid in headerQMIDs)
                    {
                        Header.QMIDs[w++] = Util.GetQMIDDWORDs(qmid.U, qmid.V, (int)qmid.L);
                        logger.Debug("QMID: {0}", qmid);
                    }
                }
            }

            // Now we have all FileOffsets defined. We can build up the BGL File
            var bw = new BinaryWriter(File.Open(filename, FileMode.Truncate));

            logger.Information("### Writing  BINARY File ###");
            bw.Write(Header.GetBytes());
            foreach (SceneryObjectSection sec2 in Sections)
            {
                bw.Write(sec2.GetHeaderBytes());

                // Let's group the Objects by QMID_9
                var GroupedSubsectionList = sec2.GetSubsections()
                .OrderBy(u => u.QMID_A)
                .ToList();

                // SubSection headers
                foreach (SubSection ssec2 in GroupedSubsectionList)
                {
                    bw.Write(ssec2.GetHeaderBytes());
                }

                foreach (SubSection ssec2 in GroupedSubsectionList)
                {
                    foreach (LibraryObject lObj in ssec2.GetLibraryObjects())
                    {
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
            var jsonFile = Directory.GetCurrentDirectory() + "/MSFS_Package/aquilon-visualpoints/layout.json";
            string json = File.ReadAllText(jsonFile);
            JObject layout = JObject.Parse(json);
            foreach (JObject content in (JArray)layout["content"])
            {
                if (content["path"].ToString().Contains("visualpoints.bgl"))
                {
                    content["size"] = fileSize;
                    content["date"] = System.DateTime.Now.ToFileTime();
                }
            }

            File.WriteAllText(jsonFile, layout.ToString());
        }

        private static bool LocalMatchQMID(QMID subqmid, List<QMID>? existingQmids)
        {
            if (existingQmids is null)
            {
                return false;
            }

            // Complex evaluation of QMIDs
            foreach (QMID qmid in existingQmids)
            {
                int deltaLevel = (int)qmid.L - (int)subqmid.L;
                if (deltaLevel < 0)
                {
                    deltaLevel = -deltaLevel;
                    uint U1 = subqmid.U >> deltaLevel;
                    uint V1 = subqmid.V >> deltaLevel;

                    if (U1 == qmid.U && V1 == qmid.V)
                    {
                        return true;
                    }
                }
                else
                {
                    uint U1 = qmid.U >> deltaLevel;
                    uint V1 = qmid.V >> deltaLevel;

                    if (U1 == subqmid.U && V1 == subqmid.V)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int AddObjects(List<Util.Point> points)
        {
            int numberOfPoints = 0;

            foreach (Point point in points)
            {
                LibraryObject lObj = new (point.GUID, point.C.Longitude.ToDouble(), point.C.Latitude.ToDouble());
                logger.Information("Added point {0} Lon {1} Lat {2}", point.Code, point.C.Longitude.ToDouble(), point.C.Latitude.ToDouble());
                AddLibraryObject(lObj);
                numberOfPoints++;
            }
            return numberOfPoints;
        }

        private struct QMID
        {
            public uint U;
            public uint V;
            public uint L;

            public QMID(uint u, uint v, uint l)
            {
                U = u;
                V = v;
                L = l;

                QMIDWORD = Util.GetQMIDDWORDs(U, V, (int)L);
            }

            public UInt32 QMIDWORD = 0;

            public override string ToString()
            {
                return "(" + U.ToString() + "," + V.ToString() + "," + L.ToString() + ")";
            }
        }
    }
}
