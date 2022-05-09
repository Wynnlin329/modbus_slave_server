using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
namespace modbus_server
{
    public partial class Form1 : Form
    {
        ModbusSlave slave;
        public Form1()
        {
            InitializeComponent();
            //MongoDBConnect();
            StartModbusTcpSlave();
        }
        public void MongoDBConnect()
        {
            string connectionString = "mongodb://wynn:0000@192.168.6.119:27017,192.168.6.120:27017,192.168.6.122:27017/?replicaSet=rs0";
            MongoClient mongoClient = new MongoClient(connectionString);


            IMongoDatabase db = mongoClient.GetDatabase("test");
            var collection = db.GetCollection<BsonDocument>("coil");
            var filter = Builders<BsonDocument>.Filter.Eq("2", "false");
            var doc = collection.Find(filter).FirstOrDefault();
            Console.WriteLine(doc.ToString());
        }
        
        public void StartModbusTcpSlave()
        {
            byte slaveID = 1;
            int port = 502;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;
            TcpListener tcpListener = new TcpListener(addr[1], port);
            tcpListener.Start();
            slave = ModbusTcpSlave.CreateTcp(slaveID, tcpListener);
            slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
            slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
            slave.Listen();
        }

        private void Slave_ModbusSlaveRequestReceived(object? sender, ModbusSlaveRequestEventArgs e)
        {
            int slaveID;
            int functionCode;
            int startAddress;
            int numOfRegister;
            List<bool> writeDataCoil = new List<bool>();
            List<ushort> writeData = new List<ushort>();
            //讀取 mongo data
            //拆解 mongo data 資料
            //資料寫入data store
            functionCode = e.Message.MessageFrame[1];
            startAddress = e.Message.MessageFrame[3];
            numOfRegister = e.Message.MessageFrame[5];
            if(functionCode == 1 || functionCode == 2)
            {
                DataStoreWrite(functionCode, startAddress, numOfRegister, writeDataCoil);
            }
            else
            {
                DataStoreWrite(functionCode, startAddress, numOfRegister, writeData);
            }
        }
        public void LoadDataBase()
        {

        }
        public void DataStoreWrite(int functionCode ,int startAddress,int numOfRegister,List<bool> writeData)
        {
            writeData.Add(true);
            writeData.Add(false);
            writeData.Add(true);
            writeData.Add(false);
            writeData.Add(true);
            switch (functionCode)
            {
                case 1://Read Coil Status
                    try
                    {
                        for (int i = 0; i < numOfRegister; i++)
                        {
                            slave.DataStore.CoilDiscretes[startAddress + i + 1] = writeData[i];
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 2://Read Input Status
                    try
                    {
                        for (int i = 0; i < numOfRegister; i++)
                        {
                            slave.DataStore.InputDiscretes[startAddress + i + 1] = writeData[i];
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                default:
                    break;
            }
        }
        public void DataStoreWrite(int functionCode, int startAddress, int numOfRegister, List<ushort> writeData)
        {
            writeData.Add(1);
            writeData.Add(2);
            writeData.Add(3);
            writeData.Add(4);
            writeData.Add(5);
            switch (functionCode)
            {
                case 3://Read Holding Registers
                    try
                    {
                        for (int i = 0; i < numOfRegister; i++)
                        {
                            slave.DataStore.HoldingRegisters[startAddress + i + 1] = writeData[i];
                        }
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine(e.Message) ;
                    }
                    

                    break;
                case 4://Read Input Registers
                    try
                    {
                        for (int i = 0; i < numOfRegister; i++)
                        {
                            slave.DataStore.InputRegisters[startAddress + i + 1] = writeData[i];
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    
                    break;
                case 5://Force Single Coil

                    break;
                case 6://Force Single Register

                    break;
                case 15://Force Multiple Coils

                    break;
                case 16://Preset Multiple Registers

                    break;
                default:
                    break;
            }
        }

    }
}