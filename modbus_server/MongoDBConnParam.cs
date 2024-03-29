﻿using System;
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

    public class MongoDBQueryParam
    {
        public string time { get; set; }
        public string collection { get; set; }
        public string database { get; set; }
        public string registers { get; set; }
        public string functionCodes { get; set; }
        public string field { get; set; }
        public string type { get; set; }
        public bool array { get; set; }
        public string arrayLevel { get; set; }
        public string arrayNum { get; set; }
        public string id { get; set; }
    }
}
