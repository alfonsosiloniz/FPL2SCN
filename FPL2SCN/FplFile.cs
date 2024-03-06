// <copyright file="FplFile.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VisualPointsNamespace
{
    using System.Xml;
    using BGLLibrary;
    using CoordinateSharp;
    using static BGLLibrary.Util;

    public class FplFile : XmlFile
    {
        public FplFile(XmlDocument doc)
        {
            Doc = doc;
        }

        public static new bool IsType(XmlDocument? doc)
        {
            // Check the type of XML file
            // Test if this is a KML
            XmlNodeList? nodes = doc?.DocumentElement?.SelectNodes("*/ATCWaypoint");

            if (nodes is not null && nodes.Count > 0)
            {
                return true;
            }

            return false;
        }

        public override List<Point> GetPoints(IEnumerable<KeyValuePair<string, string>> pointsDefinition)
        {
            XmlNodeList? nodes = Doc?.DocumentElement?.SelectNodes("*/ATCWaypoint");

            if (nodes is not null)
            {
                foreach (XmlNode node in nodes)
                {
                    if (node is not null)
                    {
                        // Get coordinates from the node
                        // id,  <ATCWaypointType>User</ATCWaypointType> <WorldPosition>N36° 36' 43.99",W5° 35' 33.82",+000000,00</WorldPosition>  <SpeedMaxFP>-1</SpeedMaxFP> <Descr>A1</Descr>
                        var id = node.Attributes?["id"]?.Value;

                        // Get Children
                        var children = node.ChildNodes;
                        string? type = string.Empty;
                        string? position = string.Empty;
                        string? description;

                        // Identify if this is one waypoint to be referenced
                        if (id != null && ScnUtil.HasReferenceCode(id, pointsDefinition))
                        {
                            // Fill the point with the adecquate values according to the PLN format
                            foreach (XmlNode? child in children)
                            {
                                if (child is not null && child.ChildNodes is not null && child.ChildNodes.Count != 0 && child.ChildNodes[0] is not null)
                                {
                                    switch (child.Name)
                                    {
                                        case "ATCWaypointType":
                                            type = child.ChildNodes[0] !.Value;
                                            break;
                                        case "WorldPosition":
                                            position = child.ChildNodes[0] !.Value;
                                            break;
                                        case "Descr":
                                            description = child.ChildNodes[0] !.Value;
                                            break;
                                    }
                                }
                            }

                            // We only convert to scenery objects those that are User points
                            if (type == "User" && position is not null && position != string.Empty)
                            {
                                // Convert the coordinates to an standard form
                                string[] clearPosition = position.Split(",");
                                Coordinate c = Coordinate.Parse(clearPosition[0] + " " + clearPosition[1], new DateTime(2023, 2, 24, 10, 10, 0));
                                Point p = new ();
                                p.C = c;
                                p.Code = id;
                                p.GUID = ObjectName(id, pointsDefinition);
                                p.isAgl = true;
                                p.Altitude = "0";
                                p.Heading = "180";

                                Points.Add(p);
                            }
                        }
                        else
                        {
                            logger.Debug("Skipped point {0}", id);
                        }
                    }
                }
            }

            return Points;
        }
    }
}