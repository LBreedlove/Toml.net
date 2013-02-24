using System;
using System.Collections.Generic;
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
        public static Document Create()
        {
            return new Document(string.Empty);
        }

        private Document(string name)
            : base(name)
        {
        }
    }
}
