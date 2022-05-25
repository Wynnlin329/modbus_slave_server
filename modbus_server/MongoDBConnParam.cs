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
    public class MongoDBConnParam
    {
        public MongoClient mongoClient { get; set; }
        public IMongoDatabase mongoDataBase { get; set; }
        public string connectionString { get; set; }
    }
}
