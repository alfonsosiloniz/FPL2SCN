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
#pragma warning disable SA1401 // Fields should be private
        protected Logger logger = new LoggerConfiguration()
#pragma warning restore SA1401 // Fields should be private
                            .WriteTo.Console()
                            .WriteTo.Debug()
                            .MinimumLevel.Debug()
                            .CreateLogger();

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
    }
}