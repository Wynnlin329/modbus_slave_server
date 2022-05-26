using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace modbus_server
{
    public class RequestParam
    {
        public int slaveID { get; set; }
        public int functionCode { get; set; }
        public int startAddress { get; set; }
        public int numOfRegister { get; set; }
    }
    
}
