using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace knx2ha
{
    public class DatapointType
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string SizeInBit { get; set; }
        public List<DatapointSubtype> Subtypes { get; set; }



        public DatapointType(string id, string number, string name, string text, string sizeInBit)
        {
            Id = id;
            Number = number;
            Name = name;
            Text = text;
            SizeInBit = sizeInBit;
            Subtypes = new List<DatapointSubtype>();
        }

        public DatapointType()
        {
        }
    }
}
