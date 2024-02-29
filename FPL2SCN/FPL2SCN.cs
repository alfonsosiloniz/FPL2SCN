// <copyright file="FPL2SCN.cs" company="Aquilon Services">
// Copyright (c) Aquilon Services. MIT License.
// </copyright>

namespace VisualPointsNamespace
{
    using System.Globalization;
    using System.Xml;
    using BGLLibrary;
    using CommandLine;
    using CoordinateSharp;
    using Serilog;

    internal sealed class FPL2SCN
    {
    /*  TODO: Take this from a configuration file so anybody can change them

        C0 AHS_Banderas_Europa {1270C61D-B95D-42B1-A223-A0EF40FC640C}
        C1 Vegetacion Elm1_20m {17FCC022-BDF4-4C47-A5B5-0E43EA9BBDCB}
        C2 Vegetacion ASpen 11m {71DC5285-56DF-4328-919C-CD0D63CC0CCF}
        C3 towerfirespot01 {F9CF3024-4430-4CA6-ADAB-2B3D69217CC1}
        C4 Antena pequeña checkered {C545A270-E2EC-11D2-9C84-00105A0CE62A}
        C5 Aerogenerador {DF297AF7-A5A1-4F79-AC8D-FD892E7FF308}
        C6 Torre Agua {567C15BF-E002-4DE9-A38C-B68C55135A8A}
        C7 Antena Torre aeropuerto {CC3E07B2-9539-4A9C-A4C2-98A080518F04}
    */
        static string[] objectReference =
        [
            "1270C61D-B95D-42B1-A223-A0EF40FC640C",
            "17FCC022-BDF4-4C47-A5B5-0E43EA9BBDCB",
            "71DC5285-56DF-4328-919C-CD0D63CC0CCF",
            "F9CF3024-4430-4CA6-ADAB-2B3D69217CC1",
            "C545A270-E2EC-11D2-9C84-00105A0CE62A",
            "DF297AF7-A5A1-4F79-AC8D-FD892E7FF308",
            "567C15BF-E002-4DE9-A38C-B68C55135A8A",
            "CC3E07B2-9539-4A9C-A4C2-98A080518F04"
        ];

        private static bool isReferenceCode(string code)
        {
            switch (code.ToLower())
            {
                case "c0":
                case "c1":
                case "c2":
                case "c3":
                case "c4":
                case "c5":
                case "c6":
                case "c7":
                    return true;
                default:
                    return false;
            }
        }

        // https://stackoverflow.com/questions/11492705/how-to-create-an-xml-document-using-xmldocument
        static void createXML(List<Point> points, string fileName)
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

                // Elements into FSData for each Coordinate
                //<SceneryObject groupIndex="1" lat="36.612221" lon="-5.592728" alt="0.00000000000000" pitch="0.000000" bank="0.000000" heading="-179.999995" imageComplexity="VERY_SPARSE" altitudeIsAgl="TRUE" snapToGround="TRUE" snapToNormal="FALSE">
                //	<LibraryObject name="{567C15BF-E002-4DE9-A38C-B68C55135A8A}" scale="1.000000"/>
                //</SceneryObject>

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

                XmlElement element3 = doc.CreateElement (String.Empty, "LibraryObject", string.Empty);

                element3.SetAttribute("name", "{" + objectReference[point.Code] + "}");
                element3.SetAttribute("scale", "1.0");

                element2.AppendChild(element3);

                // Add Coordinate
                element1.AppendChild(element2);
            }
            doc.Save(fileName);

        }

        private static void Main(string[] args)
        {
            using var logger = new LoggerConfiguration()
                                    .WriteTo.Console()
                                    .WriteTo.Debug()
                                    .MinimumLevel.Debug()
                                    .CreateLogger();

            string OutputFileName = Directory.GetCurrentDirectory() + "/SceneryProject/visualpoints/PackageSources/Scenery/visualpoints/visualpoints/visualpoints.xml";

            logger.Information("FPL2SCN Convert a MSFS2020 Fligh Plan to a Scenery Package");

            var t = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    logger.Debug($"Input file: {options.InputFilePath}");

                    if (!string.IsNullOrWhiteSpace(options.OutputFilePath))
                    {
                        if (!Directory.Exists(options.OutputFilePath)) Directory.CreateDirectory(options.OutputFilePath);
                        logger.Debug($"Output file: {options.OutputFilePath}");
                        OutputFileName = options.OutputFilePath + "/visualpoints.xml";
                    }

                    var fileName = options.InputFilePath;

                    if (!File.Exists(fileName)) {
                        logger.Error("ERROR: Input File " + fileName + "does not exists. ");
                        System.Environment.Exit(-1);
                    }

                    List<Point> points =[];
                    XmlDocument doc = new ();

                    // Load the XML file
                    doc.Load(fileName);

                    // Create a BGL Object
                    Bgl Bgl = new (Directory.GetCurrentDirectory()+"/MSFS_Package/aquilon-visualpoints/Scenery/visualpoints/visualpoints/visualpoints.bgl");

                    XmlNodeList nodes = doc.DocumentElement.SelectNodes("*/ATCWaypoint");
                    var numberOfPoints = 0;
                    foreach (XmlNode node in nodes)
                    {
                        // Get coordinates from the node
                        // id,  <ATCWaypointType>User</ATCWaypointType> <WorldPosition>N36° 36' 43.99",W5° 35' 33.82",+000000,00</WorldPosition>  <SpeedMaxFP>-1</SpeedMaxFP> <Descr>A1</Descr>
                        var id = node.Attributes["id"].Value;

                        // Get Children
                        var children = node.ChildNodes;
                        string type = string.Empty;
                        string position = string.Empty;
                        string description;

                        // Identify if this is one waypoint to be referenced
                        var codes = id.Split(" ");
                        if (codes.Length > 0 && isReferenceCode(codes[0]))
                        {

                            // Fill the point with the adecquate values according to the PLN format
                            foreach (XmlNode child in children)
                            {
                                if (child.ChildNodes != null && child.ChildNodes.Count!= 0 && child.ChildNodes[0].Value != null)
                                {
                                    switch(child.Name)
                                    {
                                        case "ATCWaypointType":
                                            type = child.ChildNodes[0].Value;
                                            break;
                                        case "WorldPosition":
                                            position = child.ChildNodes[0].Value;
                                            break;
                                        case "Descr":
                                            description = child.ChildNodes[0].Value;
                                            break;
                                    }
                                }
                            }


                            // We only convert to scenery objects those that are User points
                            if ( type=="User" && position!=string.Empty)
                            {
                                // Convert the coordinates to an standard form
                                string[] clearPosition = position.Split(",");
                                Coordinate c = Coordinate.Parse(clearPosition[0] + " " + clearPosition[1], new DateTime(2023, 2, 24, 10, 10, 0));
                                Point p;
                                p.C = c;
                                p.Code = (int)codes[0][1]-48;
                                points.Add(p);
                                LibraryObject lObj = new LibraryObject((string)objectReference[p.Code], c.Longitude.ToDouble(), c.Latitude.ToDouble());
                                logger.Debug("Added point {0} Lon {1} Lat {2}", id, c.Longitude.ToDouble(), c.Latitude.ToDouble());
                                Bgl.AddLibraryObject(lObj);
                                numberOfPoints++;
                            }

                        }
                        else
                        {
                                logger.Debug("Skipped point {0}", id);
                            }

                    }
                    logger.Information("Parsed Flightplan. Found {0} User points", numberOfPoints );


                    // With -x we can choose to create the XML file to use the MSFS compiler
                    // We can use that to validate the generated BGL object is the same as
                    // the generated by the MSFS compiler

                    if (options.CreateXML)
                        createXML(points, OutputFileName);

                    // Create the BGL file in the MSFS scenery Package
                    logger.Information("Generating BGL File");
                    Bgl.BuildBGLFile();

                    logger.Information("Done");

                });

            logger.Information("Press any key to exit.....");
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        }

        struct Point
        {
            public Coordinate C;
            public int Code;
        }

    }
}