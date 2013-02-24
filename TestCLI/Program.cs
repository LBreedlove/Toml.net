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
            var doc = Toml.Document.Create("TestData.toml");
            Console.WriteLine(doc.ToString());

            var value = doc.GetValue("servers.alpha.dc");
        }
    }
}
