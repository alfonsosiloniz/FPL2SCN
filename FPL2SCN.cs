using System.IO.Enumeration;
using System.Runtime.InteropServices;
using CoordinateSharp;
using System.Xml;
using System.Security.Permissions;
using CommandLine;
using System.Globalization;

namespace VisualPointsNamespace {

internal sealed class Program
{
/* 
    C0 AHS_Banderas_Europa {1270C61D-B95D-42B1-A223-A0EF40FC640C}
    C1 Vegetacion Elm1_20m {17FCC022-BDF4-4C47-A5B5-0E43EA9BBDCB}
    C2 Vegetacion ASpen 11m {71DC5285-56DF-4328-919C-CD0D63CC0CCF}
    C3 towerfirespot01 {F9CF3024-4430-4CA6-ADAB-2B3D69217CC1}
    C4 Antena pequeña checkered {C545A270-E2EC-11D2-9C84-00105A0CE62A}
    C5 Aerogenerador {DF297AF7-A5A1-4F79-AC8D-FD892E7FF308}
    C6 Torre Agua {567C15BF-E002-4DE9-A38C-B68C55135A8A}
    C7 Antena Torre aeropuerto {CC3E07B2-9539-4A9C-A4C2-98A080518F04}
*/
    static string[] objectReference = [
        "1270C61D-B95D-42B1-A223-A0EF40FC640C",
        "17FCC022-BDF4-4C47-A5B5-0E43EA9BBDCB",
        "71DC5285-56DF-4328-919C-CD0D63CC0CCF",
        "F9CF3024-4430-4CA6-ADAB-2B3D69217CC1",
        "C545A270-E2EC-11D2-9C84-00105A0CE62A",
        "DF297AF7-A5A1-4F79-AC8D-FD892E7FF308",
        "567C15BF-E002-4DE9-A38C-B68C55135A8A",
        "CC3E07B2-9539-4A9C-A4C2-98A080518F04"
    ];
    struct  Point {
        public Coordinate c;
        public int code;
    }

    private static bool isReferenceCode(string code) 
    {

        switch (code.ToLower()) {
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
    static void createXML(List<Point> points, string fileName) {

        XmlDocument doc = new( );
        XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration( "1.0", null , null );
        
        if (doc.DocumentElement == null) return;
        XmlElement root = doc.DocumentElement;
        doc.InsertBefore( xmlDeclaration, root );

        XmlElement element1 = doc.CreateElement( string.Empty, "FSData", string.Empty );
        element1.SetAttribute("version","9.0");
        doc.AppendChild( element1 );

        foreach (Point point in points) {
            var coord = point.c;

            // Elements into FSData for each Coordinate
            //<SceneryObject groupIndex="1" lat="36.612221" lon="-5.592728" alt="0.00000000000000" pitch="0.000000" bank="0.000000" heading="-179.999995" imageComplexity="VERY_SPARSE" altitudeIsAgl="TRUE" snapToGround="TRUE" snapToNormal="FALSE">
            //	<LibraryObject name="{567C15BF-E002-4DE9-A38C-B68C55135A8A}" scale="1.000000"/>
            //</SceneryObject>

            XmlElement element2 = doc.CreateElement( string.Empty, "SceneryObject", string.Empty);
            element2.SetAttribute("groupIndex","1");
            element2.SetAttribute("lat",coord.Latitude.ToDouble().ToString(CultureInfo.InvariantCulture));
            element2.SetAttribute("lon",coord.Longitude.ToDouble().ToString(CultureInfo.InvariantCulture));
            element2.SetAttribute("alt","0.0");
            element2.SetAttribute("pitch","0.0");
            element2.SetAttribute("bank","0.0");
            element2.SetAttribute("heading","180.0");
            element2.SetAttribute("imageComplexity","VERY_SPARSE");
            element2.SetAttribute("altitudeIsAgl","TRUE");
            element2.SetAttribute("snapToGround","TRUE");
            element2.SetAttribute("snapToNormal","FALSE");

            XmlElement element3 = doc.CreateElement (String.Empty, "LibraryObject", string.Empty);

            element3.SetAttribute("name","{"+objectReference[point.code] +"}");
            element3.SetAttribute("scale","1.0");

            element2.AppendChild(element3);


            // Add Coordinate
            element1.AppendChild(element2);
        }
        doc.Save( fileName);

    }

    private static void Main(string[] args)
    {
        string  OutputFileName = Directory.GetCurrentDirectory() + "/SceneryProject/visualpoints/PackageSources/Scenery/visualpoints/visualpoints/visualpoints.xml";

        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(options =>
            {
                Console.WriteLine($"Input file: {options.InputFilePath}");

                if (!string.IsNullOrWhiteSpace(options.OutputFilePath))
                {
                    if (!Directory.Exists(options.OutputFilePath)) Directory.CreateDirectory(options.OutputFilePath);
                    Console.WriteLine($"Output file: {options.OutputFilePath}");
                    OutputFileName = options.OutputFilePath + "/visualpoints.xml";
                }

                var fileName = options.InputFilePath;
                
                if (!File.Exists(fileName)) { Console.WriteLine("ERROR: Input File "+ fileName +"does not exists. ");  System.Environment.Exit(-1); }
                
                List<Point> points = [];


                XmlDocument doc = new();

                doc.Load(fileName);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("*/ATCWaypoint");

                foreach (XmlNode node in nodes) {
                    // Get coordinates from the node
                    // id,  <ATCWaypointType>User</ATCWaypointType> <WorldPosition>N36° 36' 43.99",W5° 35' 33.82",+000000,00</WorldPosition>  <SpeedMaxFP>-1</SpeedMaxFP> <Descr>A1</Descr>
                    var id = node.Attributes["id"].Value;
                    // Get Children
                    var children = node.ChildNodes;
                    string type="";
                    string position="";
                    string description;

                    // Identify if this is one waypoint to be referenced
                    var codes = id.Split(" ");
                    if (codes.Length > 0 && isReferenceCode(codes[0])) {

                        foreach (XmlNode child in children) {
                            if (child.ChildNodes != null && child.ChildNodes.Count!= 0 && child.ChildNodes[0].Value != null) {
                                switch(child.Name) {
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


                        if ( type=="User" && position!="") {
                            string[] clearPosition = position.Split(",");
                            Coordinate c = Coordinate.Parse(clearPosition[0] + " " + clearPosition[1], new DateTime(2023,2,24,10,10,0));
                            Point p;
                            p.c = c;
                            p.code = (int)codes[0][1]-48;
                            points.Add(p);
                        }

                    }

                }

                createXML(points,OutputFileName);

            });


    }

}
}