using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RfidEncoder
{
    internal static class ComPortHelper
    {
        public static List<ComPortInfo> GetCOMPortsInfo()
        {
            List<ComPortInfo> comPortInfoList = new List<ComPortInfo>();

            //using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
            {
                var collection = searcher.Get();
                foreach (ManagementObject obj in collection)
                {
                    if (obj != null)
                    {
                        object captionObj = obj["Caption"];
                        object desc = obj["Description"];

                        if (captionObj != null)
                        {
                            if (captionObj.ToString().Contains("COM"))
                            {
                                var comPortInfo = new ComPortInfo();
                                comPortInfo.Name = captionObj.ToString();
                                comPortInfo.Description = desc.ToString();

                                comPortInfoList.Add(comPortInfo);
                            }
                        }
                    }
                }
            }
            return comPortInfoList;
        }
    }

    public class ComPortInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return string.Format("{0} – {1}", Name, Description);
        }
    }
}
