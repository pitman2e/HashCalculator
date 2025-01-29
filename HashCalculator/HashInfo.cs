using System;

namespace HashCalculator;

internal class HashInfo
{
    public string FileName { get; set; }
    public DateTime FileModifyDateTimeUtc { get; set; }
    public DateTime Sha1HashCalcDateTimeUtc { get; set; }
    public string Sha1Hash { get; set; }
}