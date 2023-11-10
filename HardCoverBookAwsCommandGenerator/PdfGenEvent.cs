using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HardCoverBookAwsCommandGenerator
{
    public class PdfGenEvent
    {
        public int OrderUID { get; set; }
        public int ItemAssetUID { get; set; }
        public string OrderAssetPath { get; set; }

        public static PdfGenEvent GetJsonObject(string jsonString)
        {
            // Deserialize the JSON content into an object
            return JsonConvert.DeserializeObject<PdfGenEvent>(jsonString);
        }

        public static string GetObjectKeyNameFromAssetPath(string assetPath)
        {
            int startingIndex = assetPath.IndexOf("ue2/");
            string fileName = assetPath.Substring(startingIndex + 4, assetPath.Length - (startingIndex + 4));

            return fileName;
        }
    }



}
