using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    /// <summary>
    /// The root group for a Toml document.
    /// </summary>
    public class Document : Group
    {
        /// <summary>
        /// Loads a TOML document from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to load.</param>
        /// <returns>A new Document representing the contents of the file.</returns>
        public static Document Create(string filename)
        {
            Document document = new Document();
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Parser.Parse(document, stream);
            }

            return document;
        }

        /// <summary>
        /// Loads a TOML document from the specified stream.
        /// </summary>
        /// <param name="stream">The Stream to load the Document from.</param>
        /// <returns>A new Document representing the contents of the stream.</returns>
        public static Document Create(Stream stream)
        {
            Document document = new Document();
            Parser.Parse(document, stream);

            return document;
        }

        /// <summary>
        /// Initializes a new instance of the Document class.
        /// </summary>
        private Document()
            : base(string.Empty)
        {
        }
    }
}
