using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    internal class Item
    {
        public enum ItemType
        {
            String,
            Int,
            Float,
            DateTime,
            Bool
        }

        public Item(string key, ItemType type, bool isArray)
        {
        }
    }
}
