using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCLI
{
    [Serializable]
    class TestConfig
    {
        private TestConfig()
        {
        }

        public TestConfig(string ip, int port, bool enabled)
        {
            this.IP = ip;
            this.Port = port;
            this.IsEnabled = enabled;
        }

        public string IP { get; private set; }
        public int Port { get; private set; }
        public bool IsEnabled { get; private set; }
        public int[] BackupPorts { get; private set; }

        public string[][] IPNames { get; set; }

        public TestConfig[] ComplexArrayNoSerialize { get; set; }

        public TestConfig Alternative { get; private set; }
        public void SetAlternative(TestConfig alt)
        {
            this.Alternative = alt;
        }

        public void SetBackupPorts(int[] ports)
        {
            this.BackupPorts = ports;
        }
    }
}
