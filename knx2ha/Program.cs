using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace knx2ha
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string yamlFilePath = null;
            string knxprojFilePath = null;
            bool selectAllDevices = false;

            // Debug-Code: Wenn keine Befehlszeilenargumente angegeben sind, wähle eine KNXPROJ-Datei im lokalen Verzeichnis
            if (args.Length == 0)
            {
                string[] knxprojFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.knxproj");

                if (knxprojFiles.Length == 0)
                {
                    Console.WriteLine("Keine KNXPROJ-Datei gefunden.");
                    return;
                }

                knxprojFilePath = knxprojFiles[0];
            }
            else
            {
                //knxprojFilePath = args[0];
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-in" && i < args.Length - 1)
                    {
                        knxprojFilePath = args[i + 1];
                    }
                    else if (args[i] == "-out" && i < args.Length - 1)
                    {
                        yamlFilePath = args[i + 1];
                    }
                    else if (args[i] == "-all")
                    {
                        selectAllDevices = true;
                    }
                }
            }
            // Verwendeter YAML-Dateipfad
            yamlFilePath = "output.yaml";

            if (knxprojFilePath == null || yamlFilePath == null)
            {
                Console.WriteLine("Usage: -in <input_file> -out <output_file> [-all]");
                return;
            }



            // Extrahiere die KNXPROJ-Datei
            string extractedFolderPath = ExtractKnxProjFile(knxprojFilePath);

            // Lese die Gruppenadressen aus der KNX-Master-XML-Datei
            string masterXmlFilePath = Path.Combine(extractedFolderPath, "knx_master.xml");
            List<DatapointType> datapointTypes = ExtractDatapointTypesFromMasterXml(masterXmlFilePath);

            // Extrahiere die Gruppenadressen aus den XML-Dateien
            List<Device> devices = ExtractGroupAddressesFromXmlFiles(extractedFolderPath);

            // Verknüpfe die Gruppenadressen mit den Datapoint-Typen
            AssignDatapointTypesToGroupAddresses(datapointTypes.ToList(), devices);

            // Erstelle die YAML-Konfigurationsdatei
            if (selectAllDevices)
                GenerateYamlConfig(devices, yamlFilePath);
            else
                GenerateYamlConfig(devices.Where(d => d.Type != DeviceType.Unknown).ToList(), yamlFilePath);

            Console.WriteLine("YAML-Konfiguration wurde erfolgreich erstellt.");
            Console.ReadLine();
        }

        static string ExtractKnxProjFile(string knxprojFilePath)
        {
            string extractedFolderPath = Path.Combine(Path.GetDirectoryName(knxprojFilePath), "Extracted");

            try
            {
                if (Directory.Exists(extractedFolderPath))
                {
                    Directory.Delete(extractedFolderPath, true);
                }

                ZipFile.ExtractToDirectory(knxprojFilePath, extractedFolderPath);

                return extractedFolderPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Extrahieren der KNXPROJ-Datei: " + ex.Message);
                return null;
            }
        }

        static List<DatapointType> ExtractDatapointTypesFromMasterXml(string masterXmlFilePath)
        {
            List<DatapointType> datapointTypes = new List<DatapointType>();

            XmlDocument masterXmlDoc = new XmlDocument();
            masterXmlDoc.Load(masterXmlFilePath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(masterXmlDoc.NameTable);
            nsmgr.AddNamespace("knx", "http://knx.org/xml/project/20");

            XmlNodeList datapointTypeNodes = masterXmlDoc.SelectNodes("//knx:DatapointTypes/knx:DatapointType", nsmgr);

            foreach (XmlNode datapointTypeNode in datapointTypeNodes)
            {
                string id = datapointTypeNode.Attributes["Id"].Value;
                string number = datapointTypeNode.Attributes["Number"].Value;
                string name = datapointTypeNode.Attributes["Name"].Value;
                string text = datapointTypeNode.Attributes["Text"].Value;
                string sizeInBit = datapointTypeNode.Attributes["SizeInBit"].Value;


                DatapointType datapointType = new DatapointType(id, number, name, text, sizeInBit);

                ExtractDatapointSubtypes(datapointTypeNode, datapointType, nsmgr);

                datapointTypes.Add(datapointType);
            }

            return datapointTypes;
        }

        static void ExtractDatapointSubtypes(XmlNode datapointTypeNode, DatapointType datapointType, XmlNamespaceManager nsmgr)
        {
            XmlNodeList subtypeNodes = datapointTypeNode.SelectNodes("knx:DatapointSubtypes/knx:DatapointSubtype", nsmgr);

            foreach (XmlNode subtypeNode in subtypeNodes)
            {
                string subtypeId = subtypeNode.Attributes["Id"].Value;
                string subtypeNumber = subtypeNode.Attributes["Number"].Value;
                string subtypeName = subtypeNode.Attributes["Name"].Value;
                string subtypeText = subtypeNode.Attributes["Text"].Value;

                DatapointSubtype subtype = new DatapointSubtype(subtypeId, subtypeNumber, subtypeName, subtypeText);
                datapointType.Subtypes.Add(subtype);
            }
        }

        static List<Device> ExtractGroupAddressesFromXmlFiles(string extractedFolderPath)
        {
            List<Device> devices = new List<Device>();

            string[] xmlFiles = Directory.GetFiles(extractedFolderPath, "*.xml", SearchOption.AllDirectories);

            foreach (string xmlFile in xmlFiles)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFile);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("knx", "http://knx.org/xml/project/20");

                XmlNodeList groupAddressNodes = xmlDoc.SelectNodes("//knx:GroupAddress", nsmgr);

                foreach (XmlNode groupAddressNode in groupAddressNodes)
                {
                    string id = groupAddressNode.Attributes["Id"].Value;
                    string name = groupAddressNode.Attributes["Name"].Value;
                    string address = groupAddressNode.Attributes["Address"].Value;
                    string description = groupAddressNode.Attributes["Description"]?.Value;
                    string datapointType = groupAddressNode.Attributes["DatapointType"]?.Value;
                    string deviceName = GetDeviceNameFromGroupName(name);

                    Device device = devices.FirstOrDefault(d => d.Name == deviceName);

                    DeviceType deviceType;
                    string additionalInfo;
                    ParseDescription(description, out deviceType, out additionalInfo);

                    if (device == null || deviceType == DeviceType.BinarySensor)
                    {
                        device = new Device(deviceName, name);
                        devices.Add(device);
                    }

                    if (description != null && device.Type == DeviceType.Unknown)
                    {
                        device.Type = deviceType;
                    }
                    if (datapointType != null && deviceType != DeviceType.Unknown)
                    {
                        GroupAddress groupAddress = new GroupAddress(id, name, address, datapointType, additionalInfo);
                        device.GroupAddresses.Add(groupAddress);
                    }
                }
            }

            return devices;
        }

        private static string GetDeviceNameFromGroupName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return "";

            int indexOfSpace = groupName.IndexOf(' ');
            if (indexOfSpace != -1)
                return groupName.Substring(0, indexOfSpace);

            return groupName;
        }
        public static void ParseDescription(string description, out DeviceType deviceType, out string additionalInfo)
        {
            deviceType = DeviceType.Unknown;
            additionalInfo = "";
            if (string.IsNullOrEmpty(description))
            { deviceType = DeviceType.Unknown; return; }
                

            Regex regex = new Regex(@"#(.*?)!");
            Match match = regex.Match(description);
            if (match.Success)
            {
                string deviceTypeStr = match.Groups[1].Value;
                if (Enum.TryParse(deviceTypeStr, out DeviceType parsedDeviceType))
                {
                    deviceType = parsedDeviceType;
                }
            }

            regex = new Regex(@"\[(.*?)\]");
            MatchCollection matches = regex.Matches(description);
            foreach (Match infoMatch in matches)
            {
                additionalInfo = infoMatch.Groups[1].Value;
            }
        }


        static void AssignDatapointTypesToGroupAddresses(List<DatapointType> datapointTypes, List<Device> devices)
        {
            foreach (Device device in devices)
            {
                foreach (GroupAddress groupAddress in device.GroupAddresses)
                {
                    string datapointTypeId = groupAddress.DatapointTypeId;
                    if (!string.IsNullOrEmpty(datapointTypeId))
                    {
                        string pattern = Regex.Escape(datapointTypeId);
                        DatapointType datapointType = datapointTypes.FirstOrDefault(d => d.Subtypes.Any(s => Regex.IsMatch(s.Id, pattern)));
                        if (datapointType != null)
                        {
                            DatapointSubtype datapointSubtype = datapointType.Subtypes.FirstOrDefault(s => Regex.IsMatch(s.Id, pattern));

                            if (datapointSubtype != null)
                            {
                                groupAddress.DatapointType = new DatapointType()
                                {
                                    Id = datapointType.Id,
                                    Name = datapointType.Name,
                                    Subtypes = new List<DatapointSubtype> { CloneDatapointSubtype(datapointSubtype) }
                                };
                            }
                        }
                    }
                }
            }
        }
        private static DatapointSubtype CloneDatapointSubtype(DatapointSubtype subtype)
        {
            return new DatapointSubtype()
            {
                Id = subtype.Id,
                Name = subtype.Name,
                Number = subtype.Number,
                Text = subtype.Text
            };
            // Füge hier weitere Eigenschaften hinzu, die kopiert werden sollen
        }


        static void GenerateYamlConfig(List<Device> devices, string yamlFilePath)
        {
            StringBuilder sb = new StringBuilder();
            var lastType = DeviceType.Unknown;
            var lastDeviceName = "";
            Dictionary<string, bool> generatedValues = new Dictionary<string, bool>();
            // Schreibe YAML-Konfiguration für jedes Gerät und dessen Gruppenadressen
            foreach (Device device in devices.Where(d => d.GroupAddresses.Count() > 0).OrderBy(d => d.Type))
            {
                sb.AppendLine($"# Gerät: {device.LongName}");

                switch (device.Type)
                {
                    case DeviceType.BinarySensor:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"binary_sensor:");
                        }
                        if (lastDeviceName != device.LongName)
                        {
                            lastDeviceName = device.LongName;
                            sb.AppendLine($"  - name: {device.LongName}");
                        }
                        break;
                    case DeviceType.Button:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"button:");
                        }

                        break;
                    case DeviceType.Cover:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"cover:");
                        }
                        if (lastDeviceName != device.LongName)
                        {
                            lastDeviceName = device.LongName;
                            sb.AppendLine($"  - name: {device.LongName}");
                       
                            sb.AppendLine($"    device_class: blind");
                        }
                        break;
                    case DeviceType.Light:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"light:");
                        }
                        if (lastDeviceName != device.LongName)
                        {
                            lastDeviceName = device.LongName;
                            sb.AppendLine($"  - name: {device.LongName}");
                           

                        }
                        break;
                    case DeviceType.Scene:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"scene:");
                        }

                        break;
                    case DeviceType.Sensor:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"sensor:");
                        }
                        if (lastDeviceName != device.LongName)
                        {
                            lastDeviceName = device.LongName;
                            sb.AppendLine($"  - name: {device.LongName}");
                        }
                        break;
                    case DeviceType.Switch:
                        if (lastType != device.Type)
                        {
                            lastType = device.Type;
                            sb.AppendLine($"switch:");
                        }

                        break;
                    default:
                        break;
                }

                // Unterschiedliche Variablen je nach DeviceType
                foreach (GroupAddress groupAddress in device.GroupAddresses.OrderBy(d => d.DPT))
                {
                    switch (device.Type)
                    {
                        case DeviceType.BinarySensor:
                            sb.AppendLine($"    state_address: {groupAddress.Address}");
                            break;
                        case DeviceType.Button:
                            break;
                        case DeviceType.Cover:
                            switch (groupAddress.DPT)
                            {
                                case "1.008":
                                    if (!generatedValues.ContainsKey("1.008_" + device.Id))
                                    {
                                        generatedValues["1.008_" + device.Id] = true;
                                        sb.AppendLine($"    move_long_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    move_long_address: {groupAddress.Address}");
                                    break;
                                case "1.007":
                                    if (!generatedValues.ContainsKey("1.007_" + device.Id))
                                    {
                                        generatedValues["1.007_" + device.Id] = true;
                                        sb.AppendLine($"    move_short_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    move_short_address: {groupAddress.Address}");
                                    break;
                                case "5.001":
                                    if (!generatedValues.ContainsKey("5.001_" + groupAddress.AdditionalInfo + "_" + device.Id))
                                    {
                                        generatedValues["5.001_" + groupAddress.AdditionalInfo + "_" + device.Id] = true;
                                        if(groupAddress.AdditionalInfo == "height")
                                        sb.AppendLine($"    position_state_address: {groupAddress.Address}");
                                        else if (groupAddress.AdditionalInfo == "angle")
                                            sb.AppendLine($"    angle_state_address: {groupAddress.Address}");
                                    }
                                    else
                                    {
                                        if (groupAddress.AdditionalInfo == "height")
                                            sb.AppendLine($"    position_address: {groupAddress.Address}");
                                        else if (groupAddress.AdditionalInfo == "angle")
                                            sb.AppendLine($"    angle_address: {groupAddress.Address}");
                                    }
                                    break;
                            }
                            break;
                        case DeviceType.Light:
                            switch (groupAddress.DPT)
                            {
                                case "1.001":
                                    if (!generatedValues.ContainsKey("1.001_" + device.Id))
                                    {
                                        generatedValues["1.001_" + device.Id] = true;
                                        sb.AppendLine($"    address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    state_address: {groupAddress.Address}");
                                    break;
                                case "5.001":
                                    if (!generatedValues.ContainsKey("5.001_" + device.Id))
                                    {
                                        generatedValues["5.001_" + device.Id] = true;
                                        sb.AppendLine($"    brightness_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    brightness_state_address: {groupAddress.Address}");
                                    break;
                                case "232.600":
                                    if (!generatedValues.ContainsKey("232.600_" + device.Id))
                                    {
                                        generatedValues["232.600_" + device.Id] = true;
                                        sb.AppendLine($"    color_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    color_state_address: {groupAddress.Address}");
                                    break;
                                case "251.600":
                                    if (!generatedValues.ContainsKey("251.600_" + device.Id))
                                    {
                                        generatedValues["251.600_" + device.Id] = true;
                                        sb.AppendLine($"    rgbw_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    rgbw_state_address: {groupAddress.Address}");
                                    break;
                                case "5.003":
                                    if (!generatedValues.ContainsKey("5.003_" + device.Id))
                                    {
                                        generatedValues["5.003_" + device.Id] = true;
                                        sb.AppendLine($"    hue_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    hue_state_address: {groupAddress.Address}");
                                    break;
                                case "7.600":
                                    if (!generatedValues.ContainsKey("7.600_" + device.Id))
                                    {
                                        generatedValues["7.600_" + device.Id] = true;
                                        sb.AppendLine($"    color_temperature_address: {groupAddress.Address}");
                                    }
                                    else
                                        sb.AppendLine($"    color_temperature_state_address: {groupAddress.Address}");
                                    break;
                            }
                            break;
                        case DeviceType.Scene:
                            break;
                        case DeviceType.Sensor:
                            sb.AppendLine($"    state_address: {groupAddress.Address}");
                            sb.AppendLine($"    type: {DPT.Values[groupAddress.DPT]}");
                            break;
                        case DeviceType.Switch:
                            break;
                        default:
                            break;
                    }
                    
                }
                sb.AppendLine();
            }

            // Speichere die generierte YAML-Konfiguration in die Datei
            File.WriteAllText(yamlFilePath, sb.ToString());
        }




    }
}