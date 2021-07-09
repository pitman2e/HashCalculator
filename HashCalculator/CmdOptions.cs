using System.Collections.Generic;
using CommandLine;

namespace HashCalculator
{
    public class CmdOptions
    {
        [Option('d', "directory", Required = true, HelpText = "Root directory to scan")]
        public string Directory { get; set; }
        
        [Option('i', "interval", Required = true, HelpText = "Days before rescan")]
        public int ScanInterval { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Print verbose message")]
        public bool IsVerbose { get; set; }      
    }
}