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
    using Microsoft.Extensions.Configuration;
    using Serilog;

    internal sealed class FPL2SCN
    {
        private static bool HasReferenceCode(string id,  IEnumerable<KeyValuePair<string, string>> kv)
        {
            string code = id.Split(" ")[0];
            IEnumerable<string> keys = kv.Select(x => x.Key); // To get the keys.
            if (keys.Contains("PointsDefinition:" + code))
            {
                return true;
            }

            return false;
        }

        private static string ObjectName(string id, IEnumerable<KeyValuePair<string, string>> kv)
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
        private static void CreateXML(List<Point> points, string fileName)
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

                // TODO: UNCOMMENT element3.SetAttribute("name", "{" + objectReference[point.Code] + "}");
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

            string outputFileName = Directory.GetCurrentDirectory() + "/SceneryProject/visualpoints/PackageSources/Scenery/visualpoints/visualpoints/visualpoints.xml";

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("fpl2scn.config", optional: true, reloadOnChange: true)
                .Build();

            IEnumerable<KeyValuePair<string, string>> pointsDefinition = configuration.GetSection("PointsDefinition").AsEnumerable();
            logger.Information("FPL2SCN Convert a MSFS2020 Fligh Plan to a Scenery Package");

            var t = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    logger.Debug($"Input file: {options.InputFilePath}");

                    if (!string.IsNullOrWhiteSpace(options.OutputFilePath))
                    {
                        if (!Directory.Exists(options.OutputFilePath))
                        {
                            Directory.CreateDirectory(options.OutputFilePath);
                        }

                        logger.Debug($"Output file: {options.OutputFilePath}");
                        outputFileName = options.OutputFilePath + "/visualpoints.xml";
                    }

                    var fileName = options.InputFilePath;

                    if (!File.Exists(fileName))
                    {
                        logger.Error("ERROR: Input File " + fileName + "does not exists. ");
                        System.Environment.Exit(-1);
                    }

                    List<Point> points = [];
                    XmlDocument doc = new ();

                    // Load the XML file
                    doc.Load(fileName);

                    // Create a BGL Object
                    Bgl bgl = new (Directory.GetCurrentDirectory() + "/MSFS_Package/aquilon-visualpoints/Scenery/visualpoints/visualpoints/visualpoints.bgl");

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
                        if (id != null && HasReferenceCode(id, pointsDefinition))
                        {
                            // Fill the point with the adecquate values according to the PLN format
                            foreach (XmlNode child in children)
                            {
                                if (child.ChildNodes != null && child.ChildNodes.Count != 0 && child.ChildNodes[0].Value != null)
                                {
                                    switch (child.Name)
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
                            if (type == "User" && position != string.Empty)
                            {
                                // Convert the coordinates to an standard form
                                string[] clearPosition = position.Split(",");
                                Coordinate c = Coordinate.Parse(clearPosition[0] + " " + clearPosition[1], new DateTime(2023, 2, 24, 10, 10, 0));
                                Point p;
                                p.C = c;
                                p.Code = 0;
                                points.Add(p);

                                LibraryObject lObj = new LibraryObject(ObjectName(id, pointsDefinition), c.Longitude.ToDouble(), c.Latitude.ToDouble());
                                logger.Debug("Added point {0} Lon {1} Lat {2}", id, c.Longitude.ToDouble(), c.Latitude.ToDouble());
                                bgl.AddLibraryObject(lObj);
                                numberOfPoints++;
                            }
                        }
                        else
                        {
                                logger.Debug("Skipped point {0}", id);
                            }
                    }

                    logger.Information("Parsed Flightplan. Found {0} User points", numberOfPoints);

                    // With -x we can choose to create the XML file to use the MSFS compiler
                    // We can use that to validate the generated BGL object is the same as
                    // the generated by the MSFS compiler
                    if (options.CreateXML)
                    {
                        CreateXML(points, outputFileName);
                    }

                    // Create the BGL file in the MSFS scenery Package
                    logger.Information("Generating BGL File");
                    bgl.BuildBGLFile();

                    logger.Information("Done");
                });

            logger.Information("Press any key to exit.....");
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        }

        private struct Point
        {
            public Coordinate C;
            public int Code;
        }
    }
}