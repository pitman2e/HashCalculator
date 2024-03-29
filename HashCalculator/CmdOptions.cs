using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CommandLine;

namespace HashCalculator
{
    public class CmdOptions
    {
        [Option('d', "directory", Required = true, HelpText = "Root directory to scan")]
        public string Directory { get; set; }
        
        [Option('i', "interval", Default = 365, Required = false, HelpText = "Days before rescan")]
        public int ScanInterval { get; set; }

        [Option('t', "threshold", Default = 20, Required = false, HelpText = "Scan threshold (Gigabyte)")]
        public long ScanThreshold { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Print verbose message")]
        public bool IsVerbose { get; set; }     

        [Option('n', "new", Required = false, HelpText = "Only Hash folder without hash file")]
        public bool IsNewFolderOnly { get; set; } 

        [Option("noconfirm", Required = false, HelpText = "Whether to wait for confirm if for messages")]
        public bool IsNoConfirm { get; set; }
    }
}