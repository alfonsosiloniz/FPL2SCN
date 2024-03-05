namespace VisualPointsNamespace
{
    using System.Xml;
    using static BGLLibrary.Util;

    internal interface IXmlFile
    {
        public static abstract bool IsType(XmlDocument doc);

        public abstract List<Point> GetPoints(IEnumerable<KeyValuePair<string, string>> pointsDefinition);
    }
}