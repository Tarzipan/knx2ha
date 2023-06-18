using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace knx2ha
{
    public class DatapointSubtype
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }

        public DatapointSubtype(string id, string number, string name, string text)
        {
            Id = id;
            Number = number;
            Name = name;
            Text = text;
        }

        public DatapointSubtype()
        {
        }
    }
}
