using Newtonsoft.Json;
using NGramm.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NGramm.LanguageDetectionTool
{
    public class CodeTokenizerService
    {
        private readonly string toolPath = Path.Combine(Path.GetTempPath(), "CodeTokenizatorTool");
        public List<CategorizedTokens> Tokenize(string filePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = $@"{toolPath}/tokenizer.exe",
                Arguments = $"\"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return JsonConvert.DeserializeObject<List<CategorizedTokens>>(output);
        }
    }
}
