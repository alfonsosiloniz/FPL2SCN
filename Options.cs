namespace VisualPointsNamespace {

    using CommandLine;

    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input file path + name.")]
        public string InputFilePath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output file path.")]
        public string OutputFilePath { get; set; }

    }
}