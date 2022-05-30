using System;
using System.Management;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace modbus_server
{
    public partial class Form1 : Form
    {
        ModbusTcpConnParam modbusTcpConnParam = new ModbusTcpConnParam();
        MongoDBConnParam mongoDBConnParam = new MongoDBConnParam();
        JObject json = new JObject();
        string logAddress = @"D:\log\";
        string cpuID;
        public Form1()
        {
            InitializeComponent();
            InitMappingData();
            //cpuID = GetCPUID();
            //if (cpuID == "BFEBFBFF000806C1")//BFEBFBFF000806C1(test)     04:42:1A:CB:96:CA(remote)
            //{
            //    //MongoDBConnect();
            //    StartModbusTcpSlave();
            //}
            //else
            //{
            //    throw new Exception("系統異常");
            //}
            IPAddressInit();
        }
        public void InitMappingData()
        {
            this.json = ExcelHelper.ExcelToJson(@"D:\mappingTable.xlsx");
        }
        public string GetCPUID()
        {
            try
            {
                //獲取CPUID
                string mac = "";
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    mac = mo["ProcessorId"].ToString();
                }
                moc = null;
                mc = null;
                //WriteLog("GetCPUID : " + mac);//之後要刪
                return mac;
            }
            catch (Exception e)
            {
                //WriteLog("GetCPUID Excption : " + e.Message);//之後要刪
                return "unknow";
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
                //WriteLog("GetMacAddress : " + mac);//之後要刪
                return mac;
            }
            catch (Exception e)
            {
                //WriteLog("GetMacAddress Excption : " + e.Message);//之後要刪
                return "unknow";
            }
        }
        public void IPAddressInit()
        {
            this.modbusTcpConnParam.port = 502;
            this.modbusTcpConnParam.slaveID = 1;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;
            for (int i = 0; i < addr.Count(); i++)
            {
                comboBox1.Items.Insert(i, addr[i]);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.modbusTcpConnParam.tcpListener != null)
            {
                this.modbusTcpConnParam.slave.Dispose();
                this.modbusTcpConnParam.tcpListener.Stop();
            }
            this.modbusTcpConnParam.ipAddress = (IPAddress)comboBox1.SelectedItem;
            StartModbusTcpSlave();
        }

        public void MongoDBConnect()
        {
            try
            {
                this.mongoDBConnParam.connectionString = "mongodb://wynn:0000@192.168.56.101:27017,192.168.56.102:27017,192.168.56.103:27017/?replicaSet=rs0";
                this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);
                this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase("test");
                WriteLog("連線至DataBase : " + this.mongoDBConnParam.mongoDataBase );
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "連線至DataBase : " + this.mongoDBConnParam.mongoDataBase + "\r\n");
            }
            catch (Exception e)
            {
                WriteLog("MongoDBConnect  Exception : " + e.Message);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDBConnect  Exception : " + e.Message + "\r\n");
            }
        }
        
        public void StartModbusTcpSlave()
        {
            try
            {
                this.modbusTcpConnParam.tcpListener = new TcpListener(this.modbusTcpConnParam.ipAddress, this.modbusTcpConnParam.port);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "連線IP : " + this.modbusTcpConnParam.ipAddress + "\r\n");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Port : " + this.modbusTcpConnParam.port + "\r\n");
                this.modbusTcpConnParam.tcpListener.Start();
                this.modbusTcpConnParam.slave = ModbusTcpSlave.CreateTcp(this.modbusTcpConnParam.slaveID, this.modbusTcpConnParam.tcpListener);
                this.modbusTcpConnParam.slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                this.modbusTcpConnParam.slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
                this.modbusTcpConnParam.slave.Listen();
                WriteLog("Modbus Slave 已開啟");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Modbus Slave 已開啟 \r\n");

                //test---------------------------------------------------------------
                List<bool> writeDataCoil = new List<bool>() {true,false,true,false,true};
                List<string> writeDataTemp = new List<string>();
                List<ushort> writeDataRegisters = new List<ushort>() { 1,2,3,4,5};

                for (int i = 0; i < 5; i++)
                {
                    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[i + 1] = writeDataCoil[i];
                    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[i + 1] = writeDataCoil[i];
                    this.modbusTcpConnParam.slave.DataStore.InputRegisters[i + 1] = writeDataRegisters[i];
                    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[i+ 1] = writeDataRegisters[i];
                }
                //test---------------------------------------------------------------

            }
            catch (Exception e)
            {

                WriteLog("StartModbusTcpSlave : " + e.Message);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "StartModbusTcpSlave : " + e.Message + "\r\n");
            }
        }

        private void Slave_ModbusSlaveRequestReceived(object? sender, ModbusSlaveRequestEventArgs e)
        {
            RequestParam requestParam = new RequestParam()
            {
                functionCode = e.Message.MessageFrame[1],
                startAddress = e.Message.MessageFrame[3],
                numOfRegister = e.Message.MessageFrame[5]
            };
            List<JToken> jsonList = new List<JToken>();
            List<bool> writeDataCoil = new List<bool>();
            List<string> writeDataTemp = new List<string>();
            List<ushort> writeDataRegisters = new List<ushort>();

            WriteLog("進入Event : functionCode : "+ requestParam.functionCode + " startAddress : " + requestParam.startAddress + " numOfRegister : " + requestParam.numOfRegister);

            switch (requestParam.functionCode)
            {
                case 1:
                    requestParam.functionName = "Coil";
                    break;
                case 2:
                    requestParam.functionName = "InputStatus";
                    break;
                case 3:
                    requestParam.functionName = "HoldingRegister";
                    break;
                case 4:
                    requestParam.functionName = "InputRegister";
                    break;
            }
            if (requestParam.functionCode == 1 || requestParam.functionCode == 2)
            {
                //讀取 mongo data
                writeDataTemp = LoadMongoDataCoils(requestParam);
                //拆解 mongo data
                writeDataCoil = ProcessingMongoDataCoils(writeDataTemp);
                //寫入 data store
                //DataStoreWrite(requestParam, writeDataCoil);
                //TestDataStoreWrite(requestParam, writeDataCoil);
            }
            else
            {
                //讀取 mongo data
                writeDataTemp = LoadMongoDataRegisters(requestParam);
                //拆解 mongo data
                writeDataRegisters = ProcessingMongoDataRegisters(writeDataTemp);
                //寫入 data store
                //DataStoreWrite(requestParam, writeDataRegisters);
                //TestDataStoreWrite(requestParam, writeDataRegisters);
            }
            
        }
        public List<string> LoadMongoDataCoils(RequestParam requestParam)
        {
            List<string> mongoData = new List<string>();

            //var collection = mongoDataBase.GetCollection<BsonDocument>("coil");
            //var filter = Builders<BsonDocument>.Filter.Eq("2", "false");
            //var doc = collection.Find(filter).FirstOrDefault();
            //Console.WriteLine(doc.ToString());

            return mongoData;
        }
        public List<string> LoadMongoDataRegisters(RequestParam requestParam)
        {
            List<string> mongoData = new List<string>();

            //var collection = mongoDataBase.GetCollection<BsonDocument>("coil");
            //var filter = Builders<BsonDocument>.Filter.Eq("2", "false");
            //var doc = collection.Find(filter).FirstOrDefault();
            //Console.WriteLine(doc.ToString());

            return mongoData;
        }

        public List<bool> ProcessingMongoDataCoils(List<string>mongoData)
        {
            List<bool> mongoDataCoils = new List<bool>() { true,false,true,false,true};

            

            return mongoDataCoils;
        }
        public List<ushort> ProcessingMongoDataRegisters(List<string> mongoData)
        {
            List<ushort> mongoDataRegisters = new List<ushort>() { 1,2,3,4,5};


            

            return mongoDataRegisters;
        }
        public void DataStoreWrite(RequestParam requestParam,List<bool> writeData)
        {
            //Test
            //----------------------------------------------------------
            bool count = true;
            for (ushort i = 0; i < requestParam.numOfRegister; i++)
            {
                writeData.Add(count);
                count = (!count);
            }
            //----------------------------------------------------------

            switch (requestParam.functionCode)
            {
                case 1://Read Coil Status
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Coil Status : " + this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T")+ "   " + "Read Coil Status : " + Convert.ToInt16(requestParam.startAddress + i)  + " = " + writeData[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Coil Status exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status exception : " + e.Message + "\r\n"); }));
                    }
                    break;
                case 2://Read Input Status
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.InputDiscretes[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Input Status : " + this.modbusTcpConnParam.slave.DataStore.InputDiscretes[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status : " + Convert.ToInt16(requestParam.startAddress + i)  + " = " + writeData[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Input Status exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status exception : " + e.Message + "\r\n"); }));
                    }
                    break;
                default:
                    break;
            }
        }
        public void DataStoreWrite(RequestParam requestParam, List<ushort> writeData)
        {
            //Test
            //----------------------------------------------------------
            for (ushort i = 0; i < requestParam.numOfRegister; i++)
            {
                writeData.Add((ushort)Convert.ToInt16(i + 1));
            }
            //----------------------------------------------------------

            switch (requestParam.functionCode)
            {
                case 3://Read Holding Registers
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(requestParam.startAddress +i ) + " = " + writeData[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Holding Registers exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers exception : " + e.Message + "\r\n"); }));
                    }
                    break;
                case 4://Read Input Registers
                    try
                    {
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.InputRegisters[requestParam.startAddress + i + 1] = writeData[i];
                            WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[requestParam.startAddress + i + 1] + " = " + writeData[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + Convert.ToInt16(requestParam.startAddress + i)  + " = " + writeData[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Input Registers exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers exception : " + e.Message + "\r\n"); }));
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
        public void TestDataStoreWrite(RequestParam requestParam, object writeData)
        {
            switch (requestParam.functionCode)
            {
                case 1://Read Coil Status
                    try
                    {
                        List<bool> writeDataCoil = (List<bool>)writeData;
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[requestParam.startAddress + i + 1] = writeDataCoil[i];
                            WriteLog("Read Coil Status : " + this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[requestParam.startAddress + i + 1] + " = " + writeDataCoil[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status : " + Convert.ToInt16(requestParam.startAddress + i) + " = " + writeDataCoil[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Coil Status exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status exception : " + e.Message + "\r\n"); }));
                    }
                    break;
                case 2://Read Input Status
                    try
                    {
                        List<bool> writeDataInputDiscreates = (List<bool>)writeData;
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.InputDiscretes[requestParam.startAddress + i + 1] = writeDataInputDiscreates[i];
                            WriteLog("Read Input Status : " + this.modbusTcpConnParam.slave.DataStore.InputDiscretes[requestParam.startAddress + i + 1] + " = " + writeDataInputDiscreates[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status : " + Convert.ToInt16(requestParam.startAddress + i) + " = " + writeDataInputDiscreates[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Input Status exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status exception : " + e.Message + "\r\n"); }));
                    }
                    break;
                case 3://Read Holding Registers
                    try
                    {
                        List<ushort> writeDataHoldingRegisters = (List<ushort>)writeData;
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] = writeDataHoldingRegisters[i];
                            WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] + " = " + writeDataHoldingRegisters[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(requestParam.startAddress + i) + " = " + writeDataHoldingRegisters[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Holding Registers exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers exception : " + e.Message + "\r\n"); }));
                    }
                    break;
                case 4://Read Input Registers
                    try
                    {
                        List<ushort> writeDataInputRegisters = (List<ushort>)writeData;
                        for (int i = 0; i < requestParam.numOfRegister; i++)
                        {
                            this.modbusTcpConnParam.slave.DataStore.InputRegisters[requestParam.startAddress + i + 1] = writeDataInputRegisters[i];
                            WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[requestParam.startAddress + i + 1] + " = " + writeDataInputRegisters[i]);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + Convert.ToInt16(requestParam.startAddress + i) + " = " + writeDataInputRegisters[i] + "\r\n"); }));
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("Read Input Registers exception : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers exception : " + e.Message + "\r\n"); }));
                    }
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
                if (!Directory.Exists(logAddress))
                {
                    Directory.CreateDirectory(logAddress);
                }
                StreamWriter sw = new StreamWriter(logAddress + "Log_" + timeYMD + ".txt", true);
                sw.WriteLine("[" + timeHMS + "]" + " : " + logMessage);
                sw.Close();
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }

        private void linkLabel1_MouseClick(object sender, MouseEventArgs e)
        {
            FormAbout formAboutPop = new FormAbout();
            formAboutPop.Show();
        }
        
    }
}