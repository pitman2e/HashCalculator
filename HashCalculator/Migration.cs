using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HashCalculator
{
    public class Migration
    {
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
            foreach (var filePath in filePaths)
            {
                var hashString = File.ReadAllText(filePath);
                var hashInfos = JsonConvert.DeserializeObject<List<HashInfo>>(hashString);
                var lastScannedDateTimeUtc = new FileInfo(filePath).LastWriteTimeUtc;
                foreach (var hashInfo in hashInfos)
                {
                    hashInfo.Sha1HashCalcDateTimeUtc = lastScannedDateTimeUtc;
                }
                File.WriteAllText(filePath, JsonConvert.SerializeObject(hashInfos));
            }
        }
    }
}
