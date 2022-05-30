using System;
using System.Management;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace modbus_server
{
    public class ModbusTcpConnParam
    {
        public ModbusSlave slave { get; set; }
        public byte slaveID { get; set; }
        public TcpListener tcpListener { get; set; }
        public IPAddress ipAddress { get; set; }
        public int port { get; set; }
        
    }
}
