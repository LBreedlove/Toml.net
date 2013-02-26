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

            int conMax = doc.GetFieldValue<int>("database.connection_max");

            string bio = doc.GetFieldValue<string>("owner.bio");
            Console.WriteLine("bio: {0}", bio);

            // this type isn't supported, but the TryGetFieldValue shouldn't throw.
            Guid guidResult;
            bool success = doc.TryGetFieldValue<Guid>("owner.bio", out guidResult);
            System.Diagnostics.Debug.Assert(!success);
        }
    }
}
