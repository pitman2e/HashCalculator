using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CommandLine;
using Newtonsoft.Json;

namespace HashCalculator;

internal class Program
{
    private static void Main(string[] args)
    {
        var cmdOptions = Parser.Default.ParseArguments<CmdOptions>(args);

        cmdOptions.WithParsed(options =>
            {
                PrintDifferent(
                    options.Directory,
                    options.ScanInterval,
                    [".bit_check", "Thumbs.db", ".json", ".driveupload", "hashLog.txt", "desktop.ini", ".xmp"],
                    options.IsVerbose,
                    options.ScanThreshold * 1024 * 1024 * 1024,
                    options.IsNewFolderOnly,
                    options.IsNoConfirm);
            }
        );
    }

    private static void PrintDifferent(
        string scanPath,
        int scanInterval,
        string[] ignoreStrings,
        bool isVerbose,
        long scanThreshold,
        bool IsNewFolderOnly,
        bool isNoConfirm)
    {
        var now = DateTime.Now;
        string msg;
        var logMsgFilePath = scanPath + Path.DirectorySeparatorChar + "hashLog.txt";
        long scannedSize = 0;

        var allPaths = Directory.GetDirectories(scanPath, "*", SearchOption.AllDirectories).ToList();
        allPaths.Add(scanPath);

        var hashFilePaths = (
                from path in allPaths
                select new
                {
                    JsonHashPath = path + Path.DirectorySeparatorChar + "hash.json",
                    IsHashExists = File.Exists(path + Path.DirectorySeparatorChar + "hash.json"),
                    ContainingDirPath = path
                }
            )
            .Where(h => !IsNewFolderOnly || !h.IsHashExists)
            .OrderBy(h => h.IsHashExists ? 1 : 0)
            .ThenBy(h => !h.IsHashExists ? DateTime.MaxValue : new FileInfo(h.JsonHashPath).LastWriteTimeUtc);

        foreach (var hashFilePath in hashFilePaths)
        {
            var isDifferencesFound = false;

            if (new DirectoryInfo(hashFilePath.ContainingDirPath).Attributes.HasFlag(FileAttributes.Hidden)) continue;

            Dictionary<string, HashInfo> orgHashInfos = null;

            if (File.Exists(hashFilePath.JsonHashPath))
            {
                var hashJsonText = File.ReadAllText(hashFilePath.JsonHashPath);
                var orgHashInfosAsList = JsonConvert.DeserializeObject<List<HashInfo>>(hashJsonText);
                orgHashInfos = orgHashInfosAsList.ToDictionary(x => x.FileName);
            }

            var newHashInfos = new List<HashInfo>();

            var allFilesinDirectory = Directory
                .GetFiles(hashFilePath.ContainingDirPath, "*.*", SearchOption.TopDirectoryOnly).ToList();

            foreach (var filePath in allFilesinDirectory)
            {
                var fileInfo = new FileInfo(filePath);

                if (IsIgnoredFile(fileInfo, ignoreStrings)) continue;

                var newHashInfo = new HashInfo();
                newHashInfos.Add(newHashInfo);
                newHashInfo.FileName = fileInfo.Name;
                newHashInfo.FileModifyDateTimeUtc = fileInfo.LastWriteTimeUtc;

                if (orgHashInfos == null)
                {
                    if (isVerbose) Console.WriteLine($"[{now:yyyyMMddHHmmss}] Hashing (New): {filePath}");

                    newHashInfo.Sha1Hash = SHA1Hash(fileInfo.FullName);
                    newHashInfo.Sha1HashCalcDateTimeUtc = now.ToUniversalTime();
                    scannedSize += fileInfo.Length;
                }
                else
                {
                    orgHashInfos.Remove(fileInfo.Name,
                        out var orgHashInfo); //New function in dotnet Core 2 (Retrieve and Remove)

                    if (orgHashInfo != null)
                    {
                        if (Math.Abs(
                                (orgHashInfo.FileModifyDateTimeUtc - newHashInfo.FileModifyDateTimeUtc).Seconds) >
                            1)
                        {
                            msg =
                                $"[{now:yyyyMMddHHmmss}] {fileInfo.FullName} : Different Write Time - {fileInfo.LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss.fff} (Expected {orgHashInfo.FileModifyDateTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff")})";
                            Console.WriteLine(msg);
                            File.AppendAllText(logMsgFilePath, msg + Environment.NewLine);
                            isDifferencesFound = true;
                        }

                        var daysAfterLastScan = (now.ToUniversalTime() - orgHashInfo.Sha1HashCalcDateTimeUtc).Days;
                        if (daysAfterLastScan > scanInterval)
                        {
                            if (isVerbose) Console.WriteLine($"[{now:yyyyMMddHHmmss}] Hashing (Verify): {filePath}");

                            newHashInfo.Sha1Hash = SHA1Hash(fileInfo.FullName);
                            newHashInfo.Sha1HashCalcDateTimeUtc = now.ToUniversalTime();
                            scannedSize += fileInfo.Length;

                            if (orgHashInfo.Sha1Hash != newHashInfo.Sha1Hash)
                            {
                                msg =
                                    $"[{now:yyyyMMddHHmmss}] {fileInfo.FullName} : Different Sha1 Hash - {newHashInfo.Sha1Hash} (Exptected {orgHashInfo.Sha1Hash})";
                                Console.WriteLine(msg);
                                File.AppendAllText(logMsgFilePath, msg + Environment.NewLine);
                                isDifferencesFound = true;
                            }
                        }
                        else
                        {
                            newHashInfo.Sha1Hash = orgHashInfo.Sha1Hash;
                            newHashInfo.Sha1HashCalcDateTimeUtc = orgHashInfo.Sha1HashCalcDateTimeUtc;
                        }
                    }
                    else
                    {
                        if (isVerbose) Console.WriteLine($"[{now:yyyyMMddHHmmss}] Hashing (Addition): {filePath}");

                        newHashInfo.Sha1Hash = SHA1Hash(fileInfo.FullName);
                        newHashInfo.Sha1HashCalcDateTimeUtc = now.ToUniversalTime();
                        scannedSize += fileInfo.Length;
                    }
                }
            }

            if (orgHashInfos != null)
                foreach (var hashInfo_of_MissingFile in orgHashInfos.Values)
                {
                    if (IsIgnoredFile(hashInfo_of_MissingFile.FileName, ignoreStrings)) continue;

                    msg =
                        $"[{now:yyyyMMddHHmmss}] Missing file : {hashFilePath.ContainingDirPath + Path.DirectorySeparatorChar + hashInfo_of_MissingFile.FileName}";
                    Console.WriteLine(msg);
                    File.AppendAllText(logMsgFilePath, msg + Environment.NewLine);
                    var missingFileJoshHashText = JsonConvert.SerializeObject(orgHashInfos);
                    File.WriteAllText(
                        hashFilePath.ContainingDirPath + Path.DirectorySeparatorChar +
                        $"hash_missing_{now:yyyyMMddHHmmss}.json", missingFileJoshHashText);
                }

            string newHashFilePath;
            if (isDifferencesFound)
                //If found error, do not overwrite original hash file
                newHashFilePath = hashFilePath.ContainingDirPath + Path.DirectorySeparatorChar +
                                  $"hash_error_{now:yyyyMMddHHmmss}.json";
            else
                //Overwrite original file
                newHashFilePath = hashFilePath.JsonHashPath;

            var newJoshHashText = JsonConvert.SerializeObject(newHashInfos);
            File.WriteAllText(newHashFilePath, newJoshHashText);

            if (scanThreshold > 0)
                if (scannedSize > scanThreshold)
                {
                    Console.WriteLine("Scanned file size exceeding threshold, operation aborted.");
                    if (!isNoConfirm) Console.ReadKey();

                    break;
                }
        }
    }

    private static bool IsIgnoredFile(string fullFileName, string[] ignoredKeywords)
    {
        foreach (var ignoreString in ignoredKeywords)
            if (fullFileName.Contains(ignoreString, StringComparison.InvariantCultureIgnoreCase))
                return true;

        return false;
    }

    private static bool IsIgnoredFile(FileInfo fileInfo, string[] ignoredKeywords)
    {
        if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden)) return true;

        return IsIgnoredFile(fileInfo.FullName, ignoredKeywords);
    }

    private static string SHA1Hash(string filePath)
    {
        StringBuilder formatted;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var bs = new BufferedStream(fs);
        using var sha1 = SHA1.Create();

        var hash = sha1.ComputeHash(bs);
        formatted = new StringBuilder(2 * hash.Length);
        foreach (var b in hash) formatted.AppendFormat("{0:x2}", b);

        return formatted.ToString();
    }
}