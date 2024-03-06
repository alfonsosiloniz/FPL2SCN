// <copyright file="XmlFile.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VisualPointsNamespace
{
    using System.Xml;
    using Serilog;
    using Serilog.Core;
    using static BGLLibrary.Util;

    public abstract class XmlFile : IXmlFile
    {
        protected Logger logger = new LoggerConfiguration()
                            .WriteTo.Console()
                            .WriteTo.Debug()
                            .MinimumLevel.Debug()
                            .CreateLogger();

        protected bool UseAltitude = false;
        protected bool UseHeading = false;

        public List<Point> Points { get; set; } = new ();

        protected XmlDocument? Doc { get; set; } = null;

        public static bool IsType(XmlDocument doc)
        {
            return false;
        }

        public virtual List<Point> GetPoints(IEnumerable<KeyValuePair<string, string>> pointsDefinition)
        {
            return Points;
        }

        void IXmlFile.UseAltitude(bool useAltitude)
        {
            UseAltitude = useAltitude;
        }

        void IXmlFile.UseHeading(bool useHeading)
        {
            UseHeading = useHeading;
        }
    }
}