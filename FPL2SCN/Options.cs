// <copyright file="Options.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VisualPointsNamespace
{
    using CommandLine;

    public class Options
    {
        [Value(0, Required = true, HelpText = "Input file path + name")]
        public string? InputFilePath { get; set; } = string.Empty;

        [Option('x', "xml", Required = false, HelpText = "Generate the XML file for ulterior compilation. Optional.")]
        public bool CreateXML { get; set; } = false;

        [Option('o', "output", Required = false, HelpText = "Complete filepath for XML file where visualpoints.xml file is to be located. Optional. By default will be included in the correct path under SceneryProject.")]
        public string OutputFilePath { get; set; } = string.Empty;

        [Option('g', "heading", Required = false, HelpText = "Use heading from the KML file")]
        public bool UseHeading { get; set; } = false;

        [Option('a', "alt", Required = false, HelpText = "Use altitude from the KML file. Will be considered MSL or AGL depending on the altitude mode in KML file.")]
        public bool UseAltitude { get; set; } = false;

    }
}