using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace HashCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            PrintDifferent(@"/home/usrn/F/My Pictures/我的東西/2003/2003-07-17 參觀文化館/", 360, new string[] { ".bit_check", "Thumbs.db", ".json", ".driveupload" });
            //ConvertBitCheck_To_HashJson(@"F:\My Pictures");
        }

        private static void PrintDifferent(string scanPath, int scanInterval, string[] ignoreStrings)
        {
            var now = DateTime.Now;
            string msg;
            string logMsgFilePath = scanPath + Path.DirectorySeparatorChar + "hashLog.txt";

            var allPaths = Directory.GetDirectories(scanPath, "*", SearchOption.AllDirectories).ToList();
            allPaths.Add(scanPath);
            var hashFilePaths = from path in allPaths select path + Path.DirectorySeparatorChar + "hash.json";

            foreach (string hashFilePath in hashFilePaths)
            {
                var directoryPath = Path.GetDirectoryName(hashFilePath);
                var isDifferencesFound = false;

                if (new DirectoryInfo(directoryPath).Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }

                var isOrgHashFileExist = File.Exists(hashFilePath);
                List<HashInfo> orgHashInfos = null;

                if (isOrgHashFileExist)
                {
                    var hashJsonText = File.ReadAllText(hashFilePath);
                    orgHashInfos = JsonConvert.DeserializeObject<List<HashInfo>>(hashJsonText);
                }
                List<HashInfo> newHashInfos = new List<HashInfo>();

                var allFilesinDirectory = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly).ToList();

                foreach (var filePath in allFilesinDirectory)
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    if (IsIgnoredFile(fileInfo, ignoreStrings))
                    {
                        continue;
                    }

                    HashInfo newHashInfo = new HashInfo();
                    newHashInfos.Add(newHashInfo);
                    newHashInfo.FileName = fileInfo.Name;
                    newHashInfo.FileModifyDateTimeUtc = fileInfo.LastWriteTimeUtc;

                    if (orgHashInfos != null)
                    {
                        var orgHashInfo = orgHashInfos.FirstOrDefault(x => filePath.EndsWith(x.FileName));

                        if (orgHashInfo != null)
                        {
                            orgHashInfos.Remove(orgHashInfo); //O(n), should use Hashmap

                            if (Math.Abs((orgHashInfo.FileModifyDateTimeUtc - newHashInfo.FileModifyDateTimeUtc).Seconds) > 1)
                            {
                                msg = $"[{now.ToString("yyyyMMddHHmmss")}] {fileInfo.FullName} : Different Write Time - {fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff")} (Expected {orgHashInfo.FileModifyDateTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff")})";
                                Console.WriteLine(msg);
                                File.AppendAllText(logMsgFilePath, msg + Environment.NewLine);
                                isDifferencesFound = true;
                            }

                            if ((now.ToUniversalTime() - orgHashInfo.Sha1HashCalcDateTimeUtc).Days > scanInterval)
                            {
                                newHashInfo.Sha1Hash = SHA1Hash(fileInfo.FullName);
                                newHashInfo.Sha1HashCalcDateTimeUtc = now.ToUniversalTime();

                                if (orgHashInfo.Sha1Hash != newHashInfo.Sha1Hash)
                                {
                                    msg = $"[{now.ToString("yyyyMMddHHmmss")}] {fileInfo.FullName} : Different Sha1 Hash - {newHashInfo.Sha1Hash} (Exptected {orgHashInfo.Sha1Hash})";
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
                            newHashInfo.Sha1Hash = SHA1Hash(fileInfo.FullName);
                            newHashInfo.Sha1HashCalcDateTimeUtc = now.ToUniversalTime();
                        }
                    }
                }

                if (orgHashInfos != null && orgHashInfos.Count > 0)
                {
                    foreach (var hashInfo_of_MissingFile in orgHashInfos)
                    {
                        if (IsIgnoredFile(hashInfo_of_MissingFile.FileName, ignoreStrings))
                        {
                            continue;
                        }

                        msg = $"[{now.ToString("yyyyMMddHHmmss")}] Missing file : {directoryPath + Path.DirectorySeparatorChar + hashInfo_of_MissingFile.FileName}";
                        Console.WriteLine(msg);
                        File.AppendAllText(logMsgFilePath, msg + Environment.NewLine);
                        var missingFileJoshHashText = JsonConvert.SerializeObject(orgHashInfos);
                        File.WriteAllText(directoryPath + Path.DirectorySeparatorChar + $"hash_missing_{now.ToString("yyyyMMddHHmmss")}.json", missingFileJoshHashText);
                    }
                }

                string newHashFilePath;
                if (isDifferencesFound)
                {
                    //If found error, do not overwrite original hash file
                    newHashFilePath = directoryPath + Path.DirectorySeparatorChar + $"hash_error_{now.ToString("yyyyMMddHHmmss")}.json";
                }
                else
                {
                    //Overwrite original file
                    newHashFilePath = hashFilePath;
                }

                var newJoshHashText = JsonConvert.SerializeObject(newHashInfos);
                File.WriteAllText(newHashFilePath, newJoshHashText);
            }
        }

        static bool IsIgnoredFile(string fullFileName, string[] ignoredKeywords) 
        {
            foreach (var ignoreString in ignoredKeywords)
            {
                if (fullFileName.Contains(ignoreString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsIgnoredFile(FileInfo fileInfo, string[] ignoredKeywords) 
        {
            if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
            {
                return true;
            }

            return IsIgnoredFile(fileInfo.FullName, ignoredKeywords);
        }

        static string SHA1Hash(string filePath)
        {
            StringBuilder formatted;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (SHA1Managed sha1 = new SHA1Managed())
                    {
                        byte[] hash = sha1.ComputeHash(bs);
                        formatted = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash)
                        {
                            formatted.AppendFormat("{0:x2}", b);
                        }
                    }
                }
            }

            return formatted.ToString();
        }

        private static void ConvertBitCheck_To_HashJson(string directory)
        {
            string[] filePaths = Directory.GetFiles(directory, ".bit_check", SearchOption.AllDirectories);

            foreach (string filePath in filePaths)
            {
                string jsonString = File.ReadAllText(filePath);
                var fileInfos = new List<HashInfo>();
                JObject jobject = (JObject)JsonConvert.DeserializeObject(jsonString); // parse as array  
                var childrenTokens = jobject.Children();
                var scanDateTime = DateTime.MinValue;
                foreach (JProperty token in childrenTokens)
                {
                    var hashInfo = new HashInfo();
                    hashInfo.FileName = token.Name;
                    hashInfo.FileModifyDateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(token.Value[0])).DateTime;
                    scanDateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(token.Value[1])).DateTime; ;
                    hashInfo.Sha1Hash = token.Value[2].ToString();
                    fileInfos.Add(hashInfo);
                }

                var newJsonString = JsonConvert.SerializeObject(fileInfos);
                var hashJoshFilePath = filePath.Replace(".bit_check", "hash.json");
                File.WriteAllText(hashJoshFilePath, newJsonString);
                if (scanDateTime != DateTime.MinValue)
                {
                    new FileInfo(hashJoshFilePath).LastWriteTimeUtc = scanDateTime;
                }
                else
                {
                    new FileInfo(hashJoshFilePath).LastWriteTimeUtc = DateTime.UtcNow;
                }
            }
        }

        private static void Convert_HashJson_From_v1_to_v2(string directory)
        {
            string[] filePaths = Directory.GetFiles(directory, "hash.json", SearchOption.AllDirectories);
            foreach(var filePath in filePaths)
            {
                var hashString = File.ReadAllText(filePath);
                var hashInfos = JsonConvert.DeserializeObject<List<HashInfo>>(hashString);
                var lastScannedDateTimeUtc = new FileInfo(filePath).LastWriteTimeUtc;
                foreach(var hashInfo in hashInfos)
                {
                    hashInfo.Sha1HashCalcDateTimeUtc = lastScannedDateTimeUtc;
                }
                File.WriteAllText(filePath, JsonConvert.SerializeObject(hashInfos));
            }
        }
    }
}
