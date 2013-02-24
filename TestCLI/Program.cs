using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var testFile = new FileStream("TestData.toml", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var doc = Toml.Parser.Parse(testFile);
                Console.WriteLine(doc.ToString());

                var value = doc.GetValue("servers.alpha.dc");
            }
        }
    }
}
