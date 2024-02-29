namespace VisualPointsNamespace {

    using CommandLine;

    public class Options
    {
        [Value(0, Required = true, HelpText = "Input file path + name")]
        public string InputFilePath {get; set;}

        [Option('o', "output", Required = false, HelpText = "Output file path.")]
        public string OutputFilePath { get; set; }

        [Option('x', "xml", Required = false, HelpText = "Output file path.")]
        public bool CreateXML { get; set; } = false;
    }
}