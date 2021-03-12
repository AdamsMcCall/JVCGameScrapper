using Newtonsoft.Json;
using System;
using System.IO;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            var scrapper = new JVCScrapper(2112, 4);

            scrapper.GetGrades();
            var json = JsonConvert.SerializeObject(scrapper.gameInfos, Formatting.Indented);
            File.WriteAllText(@".\output.json", json);
        }
    }
}
