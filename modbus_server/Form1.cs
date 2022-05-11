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
        string logAddress = @"D:\log\";
        string MacAddress;
        public Form1()
        {
            InitializeComponent();
            MacAddress = GetMacAddress();
            if (MacAddress == "04:42:1A:CB:96:CA")//00:E0:4C:68:3C:F5
            {
                //MongoDBConnect();
                StartModbusTcpSlave();
            }
            else
            {
                throw new Exception("系統異常");
            }
        }
        public string GetMacAddress()
        {
            try
            {
                //獲取網路卡硬體地址
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
                WriteLog("GetMacAddress : " + mac);
                return mac;
            }
            catch (Exception e)
            {
                WriteLog("GetMacAddress Excption : " + e.Message);
                return "unknow";
            }
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
                WriteLog("Modbus Slave 已開啟");
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
            List<bool> writeDataCoil = new List<bool>();
            List<ushort> writeData = new List<ushort>();
            //讀取 mongo data
            //拆解 mongo data 資料
            //資料寫入data store
            functionCode = e.Message.MessageFrame[1];
            startAddress = e.Message.MessageFrame[3];
            numOfRegister = e.Message.MessageFrame[5];
            WriteLog("進入Event : functionCode : "+ functionCode + " startAddress : " + startAddress + " numOfRegister : " + numOfRegister);
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
                            WriteLog("Read Coil Status : " + slave.DataStore.CoilDiscretes[startAddress + i + 1] + " = " + writeData[i]);
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
                        for (int i = 0; i < numOfRegister; i++)
                        {
                            slave.DataStore.InputDiscretes[startAddress + i + 1] = writeData[i];
                            WriteLog("Read Input Status : " + slave.DataStore.InputDiscretes[startAddress + i + 1] + " = " + writeData[i]);
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
                            WriteLog("Read Holding Registers : " + slave.DataStore.HoldingRegisters[startAddress + i + 1] + " = " + writeData[i]);
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
                        for (int i = 0; i < numOfRegister; i++)
                        {
                            slave.DataStore.InputRegisters[startAddress + i + 1] = writeData[i];
                            WriteLog("Read Input Registers : " + slave.DataStore.InputRegisters[startAddress + i + 1] + " = " + writeData[i]);
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