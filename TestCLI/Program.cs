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
            var doc = Toml.Document.Create(".\\TestFiles\\TestData.toml");
            Console.WriteLine(doc.ToString());

            var value = doc.GetValue("servers.alpha.dc");
            var multiLine = doc.GetFieldValue<string>("multi_line_test");
            Console.WriteLine("multi_line_test = {0}{1}", System.Environment.NewLine, multiLine);

            int conMax = doc.GetFieldValue<int>("database.connection_max");
            var ports = doc.GetArrayValue<int>("database.ports");
            var portStrings = doc.GetArrayValue<string>("database.ports");

            var clientsDataArray = doc.GetArrayValue<string>("clients.data");

            string bio = doc.GetFieldValue<string>("owner.bio");
            Console.WriteLine("bio: {0}", bio);

            // this type isn't supported, but the TryGetFieldValue shouldn't throw.
            Guid guidResult;
            bool success = doc.TryGetFieldValue<Guid>("owner.bio", out guidResult);
            System.Diagnostics.Debug.Assert(!success);

            TestArray();
            TestParser();
        }

        /// <summary>
        /// testing work-in-progress for pulling out strongly typed arrays.
        /// </summary>
        static void TestArray()
        {
            var arr = new object[] { new[] { (Int32)32, (Int32)63 }, new[] { "hello", "goodbye" } };
 
            var arrayDoc = Toml.Document.Create(".\\TestFiles\\array.toml");
            Type portsType = (arrayDoc.GetValue("clients.ports") as Toml.Array).GetArrayType();
            Type randomType = (arrayDoc.GetValue("clients.random") as Toml.Array).GetArrayType();
            Type dataType = (arrayDoc.GetValue("clients.data") as Toml.Array).GetArrayType();
            Type hostsType = (arrayDoc.GetValue("clients.hosts") as Toml.Array).GetArrayType();
        }

        /// <summary>
        /// was used while testing the new parser
        /// </summary>
        static void TestParser()
        {
            var exampleDoc = Toml.Document.Create(".\\TestFiles\\example.toml");
            Console.WriteLine(exampleDoc);

            var server_alpha_dc = exampleDoc.GetFieldValue<string>("servers.alpha.dc");
            var database_connection_max = exampleDoc.GetFieldValue<int>("database.connection_max");
            string owner_bio = exampleDoc.GetFieldValue<string>("owner.bio");

            // -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= //
            var testDataDoc = Toml.Document.Create(".\\TestFiles\\TestData.toml");
            Console.WriteLine(testDataDoc);

            var dob = testDataDoc.GetFieldValue<DateTime>("dob");
            var multiLineTest = testDataDoc.GetFieldValue<string>("multi_line_test");
            var aNewArray = testDataDoc.GetFieldValue<object>("aNewArray");
            var filePath = testDataDoc.GetFieldValue<string>("base_path");

            var multiArray = testDataDoc.GetArrayValue<object>("database.port_ips");

            // -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-= //
            var hardExampleDoc = Toml.Document.Create(".\\TestFiles\\hard_example.toml");
            Console.WriteLine(hardExampleDoc);
        }
    }
}
