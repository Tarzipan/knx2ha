using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace knx2ha
{
    public class Device
    {
        
        public string Id { get; set; }
        public string LongName { get; set; }
        public string Name { get; set; }
        public List<GroupAddress> GroupAddresses { get; set; }
        public DeviceType Type { get; set; }

        public Device(string name, string longName)
        {
            Name = name;
            GroupAddresses = new List<GroupAddress>();
            LongName = longName;
            Id = Guid.NewGuid().ToString();
        }
    }
}
