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

        [Option('o', "output", Required = false, HelpText = "Output file path.")]
        public string OutputFilePath { get; set; } = string.Empty;

        [Option('x', "xml", Required = false, HelpText = "Output file path.")]
        public bool CreateXML { get; set; } = false;
    }
}