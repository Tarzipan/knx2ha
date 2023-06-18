using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace knx2ha
{
    public class GroupAddress
    {
        public string Id { get; }
        public string Name { get; }
        public string Address
        { get; set;
        }
        public string DeviceName { get; set; }
        public string DatapointTypeId { get; }
        public DatapointType DatapointType { get; set; }
        public string AdditionalInfo { get; set; }

        public string DPT
        {
            get { 
                if(DatapointType != null && DatapointType.Name != "" && DatapointType.Subtypes.First() != null)
                return GenerateCombinedVariable(DatapointType.Name, DatapointType.Subtypes.First().Number);
                return "-";
            }
        }

        public GroupAddress(string id, string name, string addressCode, string datapointTypeId, string additionInfo)
        {
            Id = id;
            Name = name;
            Address = ConvertAddress(addressCode);
            DatapointTypeId = datapointTypeId;
            DatapointType = null;
            AdditionalInfo = additionInfo;
            DeviceName = GetDeviceNameFromGroupName(name);
        }

        private string ConvertAddress(string addressCode)
        {
            int hauptgruppe = (Convert.ToInt32(addressCode) & 0x7800) >> 11;
            int mittelgruppe = (Convert.ToInt32(addressCode) & 0x700) >> 8;
            int untergruppe = Convert.ToInt32(addressCode) & 0xFF;

            return $"{hauptgruppe}/{mittelgruppe}/{untergruppe}";
        }

        private string GenerateCombinedVariable(string variable1, string variable2)
        {
            string[] parts1 = variable1.Split('.');
            string[] parts2 = variable2.Split('.');

            if (parts1.Length != 2 || parts2.Length < 1 || parts2.Length > 2)
            {
                throw new ArgumentException("Ungültiges Variablenformat. Erwartet wird 'x.xxx', 'xx.xxx' oder 'xxx.xxx'.");
            }

            string prefix = parts1[0];
            string suffix = parts2.Length == 1 ? parts2[0].PadLeft(3, '0') : parts2[1];

            return $"{prefix}.{suffix}";
        }

        private string GetDeviceNameFromGroupName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return "";

            int indexOfSpace = groupName.IndexOf(' ');
            if (indexOfSpace != -1)
                return groupName.Substring(0, indexOfSpace);

            return groupName;
        }
    }


}
