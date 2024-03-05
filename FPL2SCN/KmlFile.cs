// <copyright file="KmlFile.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VisualPointsNamespace
{
    using System.Xml;
    using System.Xml.XPath;
    using BGLLibrary;
    using CoordinateSharp;
    using static BGLLibrary.Util;

    public class KmlFile : XmlFile
    {
        public KmlFile(XmlDocument doc)
        {
            Doc = doc;
        }

        public static new bool IsType(XmlDocument? doc)
        {
            // Check the type of XML file
            // Test if this is a KML
            // XPathDocument document = new XPathDocument(fileName);
            XPathNavigator? navigator = doc?.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(doc?.NameTable);
            manager.AddNamespace("kml", "http://www.opengis.net/kml/2.2");
            XmlNodeList? nodes = doc?.SelectNodes("/kml:kml/kml:Document/kml:Placemark", manager);

            if (nodes != null && nodes.Count > 0)
            {
                return true;
            }

            return false;
        }

        public override List<Point> GetPoints(IEnumerable<KeyValuePair<string, string>> pointsDefinition)
        {
            XPathNavigator? navigator = Doc?.CreateNavigator();
            XmlNamespaceManager? manager = new XmlNamespaceManager(Doc?.NameTable);
            manager.AddNamespace("kml", "http://www.opengis.net/kml/2.2");
            XmlNodeList? nodes = Doc?.SelectNodes("/kml:kml/kml:Document/kml:Placemark", manager);

            if (nodes != null)
            {
                foreach (XmlNode xmlnode in nodes)
                {
                    if (xmlnode is not null)
                    {
                        XmlNode? name = xmlnode.SelectNodes("./kml:name", manager)?[0];
                        XmlNode? coordinates = xmlnode.SelectNodes("./*/kml:coordinates", manager)?[0];
                        XmlNode? altitude = xmlnode.SelectNodes("./*/kml:altitude", manager)?[0];
                        XmlNode? heading = xmlnode.SelectNodes("./*/kml:heading", manager)?[0];
                        string? pointName = name?.InnerText;
                        string? coords = coordinates?.InnerText;

                        if (pointName != null && ScnUtil.HasReferenceCode(pointName, pointsDefinition) && coords is not null)
                        {
                            string[] clearPosition = coords.Trim().Split(",");
                            Coordinate c = Coordinate.Parse(clearPosition[1] + " " + clearPosition[0], new DateTime(2023, 2, 24, 10, 10, 0));
                            Point p = new ()
                            {
                                C = c,
                                Code = pointName,
                                Altitude = altitude?.InnerText,
                                Heading = heading?.InnerText,
                                GUID = Util.ObjectName(pointName, pointsDefinition),
                            };
                            Points.Add(p);
                        }
                        else
                        {
                            logger.Debug("Skipped point {0}", pointName);
                        }
                    }
                }
            }

            return Points;
        }
    }
}