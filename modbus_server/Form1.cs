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
    public partial class Form1 : Form
    {
        ModbusSlave slave;
        MongoClient mongoClient;
        IMongoDatabase mongoDataBase;
        string connectionString = "mongodb://wynn:0000@192.168.6.119:27017,192.168.6.120:27017,192.168.6.122:27017/?replicaSet=rs0";
        string logAddress = @"D:\log\";
        string macAddress;
        public Form1()
        {
            InitializeComponent();
            macAddress = GetMacAddress();
            if (macAddress == "04:42:1A:CB:96:CA")//00:E0:4C:68:3C:F5
            {
                //MongoDBConnect();
                StartModbusTcpSlave();
            }
            else
            {
                throw new Exception("�t�β��`");
            }
        }
        public string GetMacAddress()
        {
            try
            {
                //��������d�w��a�}
                string mac = "";
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        mac = mo["MacAddress"].ToString();
                        break;
                    }
                }
                moc = null;
                mc = null;
                WriteLog("GetMacAddress : " + mac);//����n�R
                return mac;
            }
            catch (Exception e)
            {
                WriteLog("GetMacAddress Excption : " + e.Message);//����n�R
                return "unknow";
            }
        }

        public void MongoDBConnect()
        {
            try
            {
                mongoClient = new MongoClient(connectionString);
                mongoDataBase = mongoClient.GetDatabase("test");
                WriteLog("�s�u��DataBase : " + mongoDataBase );
            }
            catch (Exception e)
            {
                WriteLog("MongoDBConnect  Exception : " + e.Message);
            }
        }
        
        public void StartModbusTcpSlave()
        {
            try
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
                WriteLog("Modbus Slave �w�}��");
            }
            catch (Exception e)
            {

                WriteLog("StartModbusTcpSlave : " + e.Message);
            }
        }

        private void Slave_ModbusSlaveRequestReceived(object? sender, ModbusSlaveRequestEventArgs e)
        {
            int slaveID;
            int functionCode;
            int startAddress;
            int numOfRegister;
            RequestParam requestParam = new RequestParam();
            List<bool> writeDataCoil = new List<bool>();
            List<string> writeDataTemp = new List<string>();
            List<ushort> writeDataRegisters = new List<ushort>();
            requestParam.functionCode = e.Message.MessageFrame[1];
            requestParam.startAddress = e.Message.MessageFrame[3];
            requestParam.numOfRegister = e.Message.MessageFrame[5];



            WriteLog("�i�JEvent : functionCode : "+ requestParam.functionCode + " startAddress : " + requestParam.startAddress + " numOfRegister : " + requestParam.numOfRegister);
            if(requestParam.functionCode == 1 || requestParam.functionCode == 2)
            {
                //Ū�� mongo data
                writeDataTemp = LoadMongoDataCoils();
                //��� mongo data
                writeDataCoil = ProcessingMongoDataCoils(writeDataTemp);
                //�g�J data store
                DataStoreWrite(requestParam, writeDataCoil);
            }
            else
            {
                //Ū�� mongo data
                writeDataTemp = LoadMongoDataRegisters();
                //��� mongo data
                writeDataRegisters = ProcessingMongoDataRegisters(writeDataTemp);
                //�g�J data store
                DataStoreWrite(requestParam, writeDataRegisters);
            }
        }
        public List<string> LoadMongoDataCoils()
        {
            List<string> mongoData = new List<string>();

            var collection = mongoDataBase.GetCollection<BsonDocument>("coil");
            var filter = Builders<BsonDocument>.Filter.Eq("2", "false");
            var doc = collection.Find(filter).FirstOrDefault();
            Console.WriteLine(doc.ToString());

            return mongoData;
        }
        public List<string> LoadMongoDataRegisters()
        {
            List<string> mongoData = new List<string>();

            var collection = mongoDataBase.GetCollection<BsonDocument>("coil");
            var filter = Builders<BsonDocument>.Filter.Eq("2", "false");
            var doc = collection.Find(filter).FirstOrDefault();
            Console.WriteLine(doc.ToString());

            return mongoData;
        }

        public List<bool> ProcessingMongoDataCoils(List<string>mongoData)
        {
            List<bool> mongoDataCoils = new List<bool>();

            return mongoDataCoils;
        }
        public List<ushort> ProcessingMongoDataRegisters(List<string> mongoData)
        {
            List<ushort> mongoDataRegisters = new List<ushort>();

            return mongoDataRegisters;
        }
        public void DataStoreWrite(RequestParam requestParam,List<bool> writeData)
        {
            writeData.Add(true);
            writeData.Add(false);
            writeData.Add(true);
            writeData.Add(false);
            writeData.Add(true);
            switch (requestParam.functionCode)
            {
                case 1://Read Coil Status
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            slave.DataStore.CoilDiscretes[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Coil Status : " + slave.DataStore.CoilDiscretes[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                        }
                        
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Coil Status exception : " + e.Message);
                    }
                    break;
                case 2://Read Input Status
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            slave.DataStore.InputDiscretes[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Input Status : " + slave.DataStore.InputDiscretes[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Input Status exception : " + e.Message);
                    }
                    break;
                default:
                    break;
            }
        }
        public void DataStoreWrite(RequestParam requestParam, List<ushort> writeData)
        {
            writeData.Add(1);
            writeData.Add(2);
            writeData.Add(3);
            writeData.Add(4);
            writeData.Add(5);
            switch (requestParam.functionCode)
            {
                case 3://Read Holding Registers
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Holding Registers : " + slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                        }
                    }
                    catch (Exception e)
                    {

                        WriteLog("Read Holding Registers exception : " + e.Message);
                    }
                    

                    break;
                case 4://Read Input Registers
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            slave.DataStore.InputRegisters[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Input Registers : " + slave.DataStore.InputRegisters[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Input Registers exception : " + e.Message);
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
        public void WriteLog(string logMessage)
        {
            DateTime value = DateTime.Now;
            string timeYMD = value.ToString("yyyy-MM-dd");
            //string timeYMD = value.ToString("yyyy-MM-ddmm");
            string timeHMS = value.ToString("HH:mm:ss");
            try
            {
                StreamWriter sw = new StreamWriter(logAddress + "Log_" + timeYMD + ".txt", true);
                sw.WriteLine("[" + timeHMS + "]" + " : " + logMessage);
                sw.Close();
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }
    }
}