using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toml
{
    /*
    public class Document : Group
    {
        #region Private Members

        /// <summary>
        /// The Groups contained in this document.
        /// </summary>
        private Dictionary<string, Group> _groups;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Toml Document class.
        /// </summary>
        /// <param name="key">The key that uniquely identifies the document.</param>
        internal Document(string name)
        {
            this.Name = name;
            _groups = new Dictionary<string, Group>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the KeyValuePairs that define the groups.
        /// </summary>
        public IEnumerable<KeyValuePair<string, Group>> Groups
        {
            get
            {
                return _groups.Select
                (
                    (g) =>
                    {
                        return new KeyValuePair<string, Group>(g.Key, g.Value);
                    }
                );
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to add the specified group to the Document.
        /// </summary>
        /// <param name="group">The Group to add to the Document.</param>
        public Group CreateGroup(string key)
        {
            IEnumerable<string> keyParts = key.Split(Group.Separators, StringSplitOptions.RemoveEmptyEntries);

            Group rootGroup = _groups[keyParts.First()];
            if (rootGroup == null)
            {
                rootGroup = new Group(keyParts.First());
                _groups.Add(keyParts.First(), rootGroup);
                return rootGroup.CreateGroup(keyParts.Skip(1));
            }

            return rootGroup.CreateGroup(keyParts.Skip(1));
        }

        /// <summary>
        /// Attempts to retrieve the group with the specified key.
        /// </summary>
        /// <param name="key">The key of the group to retrieve.</param>
        /// <param name="result">Contains the located Group on output.</param>
        /// <returns>true if the Group is found, otherwise false.</returns>
        public bool TryGetGroup(string key, out Group result)
        {
            result = null;

            IEnumerable<string> keyParts = key.Split(Group.Separators, StringSplitOptions.RemoveEmptyEntries);
            Group rootGroup = _groups[keyParts.First()];
            if (rootGroup == null)
            {
                return false;
            }

            return rootGroup.TryGetGroup(keyParts.Skip(1), out result);
        }

        /// <summary>
        /// Indicates whether or not the specified group exists in the Document.
        /// </summary>
        /// <param name="groupKey">The key of the Group to search for.</param>
        /// <returns>true if a Group with the specified key is found, otherwise false.</returns>
        public bool Exists(string key)
        {
            IEnumerable<string> keyParts = key.Split(Group.Separators, StringSplitOptions.RemoveEmptyEntries);
            Group rootGroup = _groups[keyParts.First()];
            if (rootGroup == null)
            {
                return false;
            }

            return rootGroup.GroupExists(keyParts.Skip(1));
        }

        #endregion
    }
    */
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
