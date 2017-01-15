using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    public class Group
    {
        #region Public Static Fields

        /// <summary>
        /// The string used to separate groups, from their parent groups.
        /// </summary>
        public static readonly string Separator = ".";

        /// <summary>
        /// The Separator array used by String.Split.
        /// </summary>
        public static readonly string[] Separators = { Separator };

        #endregion

        #region Private Members

        /// <summary>
        /// The children of this group.
        /// </summary>
        private Dictionary<string, Group> _children = new Dictionary<string, Group>();

        /// <summary>
        /// The values of this group.
        /// </summary>
        private Dictionary<string, Entry> _items = new Dictionary<string, Entry>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Group class.
        /// </summary>
        /// <param name="key">The key of the new Group.</param>
        internal Group(string key)
        {
            this.Key = key;
        }

        /// <summary>
        /// Initializes a new instance of the Group class.
        /// </summary>
        /// <param name="parent">The owner of the new Group.</param>
        /// <param name="key">The key of the new Group.</param>
        internal Group(Group parent, string key)
        {
            this.Parent = parent;
            this.Key = key;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the key that identifies this Group.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the full key of the Group, using the keys
        /// of the parent groups to create the key
        /// </summary>
        public string FullKey
        {
            get
            {
                if (this.Parent != null)
                {
                    string parentKey = this.Parent.FullKey;
                    if (!string.IsNullOrEmpty(parentKey))
                    {
                        return this.Parent.FullKey + Group.Separator + this.Key;
                    }
                }

                return this.Key;
            }
        }

        /// <summary>
        /// Gets the parent of this Group, if any.
        /// </summary>
        public Group Parent { get; private set; }

        /// <summary>
        /// Gets the groups owned by this group.
        /// </summary>
        public IEnumerable<Group> Children
        {
            get { return _children.Values; }
        }

        /// <summary>
        /// Gets the Items owned by the group.
        /// </summary>
        public IEnumerable<KeyValuePair<string, Entry>> Items
        {
            get
            {
                return _items.Select(entry => new KeyValuePair<string, Entry>(entry.Key, entry.Value));
            }
        }

        /// <summary>
        /// Gets all the items in the tree, from this node down.
        /// </summary>
        public IEnumerable<KeyValuePair<string, Entry>> AllItems
        {
            get
            {
                return (this.Items).Concat(this._children.SelectMany((item) => item.Value.AllItems));
            }
        }

        /// <summary>
        /// Gets the Groups that comprise this item, starting from
        /// the current group, working towards the root of the tree.
        /// </summary>
        public IEnumerable<Group> DescendingGroups
        {
            get
            {
                var current = this;
                while (current != null)
                {
                    yield return current;
                    current = current.Parent;
                }

                yield break;
            }
        }

        /// <summary>
        /// Gets the Groups that comprise this item, starting from
        /// the root of the tree, to the current group.
        /// </summary>
        public IEnumerable<Group> AscendingGroups
        {
            get
            {
                return this.DescendingGroups.Reverse();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a string representing the values and subgroups in the Group.
        /// </summary>
        /// <returns>A string representing the Group's contents.</returns>
        public override string ToString()
        {
            string value = string.Empty;
            if (!string.IsNullOrEmpty(this.Key))
            {
                value += "[" + this.FullKey + "]" + System.Environment.NewLine;
            }

            foreach (var item in this.Items)
            {
                value += item.ToString() + System.Environment.NewLine;
            }

            foreach (var group in this.Children)
            {
                value += group.ToString();
                value += System.Environment.NewLine;
            }

            return value;
        }

        /// <summary>
        /// Attempts to retrieve the group with the specified key.
        /// </summary>
        /// <param name="key">The key of the group to search for.</param>
        /// <param name="group">The located group.</param>
        /// <returns></returns>
        public bool TryGetGroup(IEnumerable<string> keyParts, out Group group)
        {
            if (keyParts.Count() == 0)
            {
                group = this;
                return true;
            }
            Group child;
            if(_children.TryGetValue(keyParts.First(), out child))
            {
                return child.TryGetGroup(keyParts.Skip(1), out group);
            }
            group = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the group with the specified key.
        /// </summary>
        /// <param name="key">The key of the group to search for.</param>
        /// <param name="group">The located group.</param>
        /// <returns></returns>
        public Group GetGroup(IEnumerable<string> keyParts)
        {
            if (keyParts.Count() == 0)
            {
                return this;
            }

            Group child = _children[keyParts.First()];
            return child.GetGroup(keyParts.Skip(1));
        }

        /// <summary>
        /// Attempts to create the group comprised of the specified key.
        /// </summary>
        public Group CreateGroup(string key)
        {
            var keyParts = key.Split(Group.Separators, StringSplitOptions.None);
            return CreateGroup(keyParts);
        }

        /// <summary>
        /// Attempts to create the group comprised of the specified key parts.
        /// </summary>
        /// <param name="keyParts">The key parts that define the group key.</param>
        /// <returns>The new key group.</returns>
        protected Group CreateGroup(IEnumerable<string> keyParts)
        {
            if (keyParts.Count() == 0)
            {
                return this;
            }

            string firstKeyPart = keyParts.First();

            Group child = null;
            if (!_children.TryGetValue(firstKeyPart, out child))
            {
                // create the missing child group
                child = new Group(this, firstKeyPart);
                _children.Add(firstKeyPart, child);
            }

            return child.CreateGroup(keyParts.Skip(1));
        }

        /// <summary>
        /// Indicates whether or not all the keys in the path exist.
        /// </summary>
        /// <param name="keyParts">The key parts to search for.</param>
        /// <returns>true if all the key parts are found, otherwise false.</returns>
        public bool GroupExists(IEnumerable<string> keyParts)
        {
            if (keyParts.Count() == 0)
            {
                return true;
            }

            Group child = _children[keyParts.First()] as Group;
            if (child == null)
            {
                return false;
            }

            return child.GroupExists(keyParts.Skip(1));
        }

        /// <summary>
        /// Attempts to add the specified item under this Item.
        /// </summary>
        /// <param name="group">The child item to add.</param>
        public void AddValue(Entry entry)
        {
            IEnumerable<string> keyParts = entry.FullName.Split(Group.Separators, StringSplitOptions.None);
            if (keyParts.Count() == 1)
            {
                _items.Add(keyParts.Last(), entry);
                return;
            }

            Group group = CreateGroup(keyParts.Take(keyParts.Count() - 1));
            group.AddValueDirect(entry);
        }

        /// <summary>
        /// Adds the value directly under this node.
        /// </summary>
        /// <param name="entry">The value to add.</param>
        private void AddValueDirect(Entry entry)
        {
            var pathParts = entry.FullName.Split(Group.Separators, StringSplitOptions.None);
            var parentPath = string.Concat(pathParts.Take(pathParts.Length - 1).Select(s => s + "."));
            
            // remove the extra "." at the end
            parentPath = parentPath.TrimEnd('.');

            // verify the parent path == our path
            if (!parentPath.Equals(this.FullKey))
            {
                throw new InvalidOperationException("Cannot add a value to a group it doesn't belong to");
            }

            _items.Add(entry.Name, entry);
            return;
        }

        /// <summary>
        /// Attempts to add a child with the specified key/value.
        /// </summary>
        /// <typeparam name="T">The type of the new child to add.</typeparam>
        /// <param name="key">The key of the new item.</param>
        /// <param name="value">The value of the new key to add.</param>
        public void AddValue<T>(string key, T value)
        {
            if (value is Entry)
            {
                AddValue(value as Entry);
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or Empty", "Key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value", "Value cannot be null");
            }

            var entry = Parser.ParseEntry(string.Format("{0} = {1}", key, value.ToString())).First();

            AddValue(key, entry);
            return;
        }

        /// <summary>
        /// Attempts to find the child with the specified key.
        /// </summary>
        /// <param name="key">The key of the item to search for.</param>
        /// <returns>true if the Item is found, otherwise false.</returns>
        public bool TryGetValue(string key, out string result)
        {
            var keyParts = key.Split(Group.Separators, StringSplitOptions.None);
            if (keyParts.Count() == 1)
            {
                Entry entry = null;
                bool found = _items.TryGetValue(keyParts.Last(), out entry);

                result = null;
                if (found)
                {
                    result = entry.SourceText;
                }

                return found;
            }

            Group group = null;
            if (this.TryGetGroup(keyParts.Take(keyParts.Count() - 1), out group))
            {
                return group.TryGetValue(keyParts.Last(), out result);
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Attempts to find the child with the specified key.
        /// </summary>
        /// <param name="key">The key of the item to search for.</param>
        /// <returns>The item Entry.</returns>
        public Entry GetValue(string key)
        {
            var keyParts = key.Split(Group.Separators, StringSplitOptions.None);
            if (keyParts.Count() == 1)
            {
                return _items[key];
            }

            Group group = GetGroup(keyParts.Take(keyParts.Count() - 1));
            return group.GetValue(keyParts.Last());
        }

        /// <summary>
        /// Attempts to find the child with the specified key.
        /// </summary>
        /// <param name="key">The key of the item to search for.</param>
        /// <returns>The item Entry text.</returns>
        public string GetValueString(string key)
        {
            return GetValue(key).SourceText;
        }

        #endregion
    }
}
