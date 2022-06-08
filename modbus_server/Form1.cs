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
using System.Timers;

namespace modbus_server
{
    public partial class Form1 : Form
    {
        ModbusTcpConnParam modbusTcpConnParam = new ModbusTcpConnParam();
        MongoDBConnParam mongoDBConnParam = new MongoDBConnParam();
        MongoDBQueryParam mongoDBQueryParam = new MongoDBQueryParam();
        System.Timers.Timer pollingMongoDB = new System.Timers.Timer();
        JObject json = new JObject();
        List<JToken> mongoMappingList = new List<JToken>();
        string mongoConnectionString = "mongodb://wynn:0000@192.168.56.101:27017,192.168.56.102:27017,192.168.56.103:27017/?replicaSet=rs0";
        string logAddress = @"D:\log\";
        string cpuID;
        bool textBoxMessageCloseFlag = false;
        int queryTimeInterval = 1000;
        int errorCount = 0;
        int updateCount = 0;
        public Form1()
        {
            InitializeComponent();
            InitMappingData();
            //cpuID = GetCPUID();
            //if (cpuID == "BFEBFBFF000806C1")//BFEBFBFF000806C1(test)     04:42:1A:CB:96:CA(remote)
            //{
            //    //MongoDBConnect();
            //    InitIPAddress();
            //    StartModbusTcpSlave();
            //}
            //else
            //{
            //    throw new Exception("系統異常");
            //}
            InitIPAddress();
            MongoDBConnect();
            SetPollingMongoDBTimer();
        }
        public void InitMappingData()
        {
            this.json = ExcelHelper.ExcelToJson("mappingTable.xlsx");
            foreach (var mappingList in this.json)
            {
                foreach (var mapping in mappingList.Value)
                {
                    this.mongoMappingList.Add(mapping);
                }
            }
            WriteLog("InitMappingData 資料比數 : " + this.mongoMappingList.Count());
        }
        public void SetPollingMongoDBTimer()
        {
            List<string> writeDataTemp = new List<string>();
            this.pollingMongoDB.Interval = 1000;
            this.pollingMongoDB.AutoReset = false;
            this.pollingMongoDB.Elapsed += new ElapsedEventHandler((x, y) =>
            {
                while (true)
                {
                    //取出MongoDB的資料
                    writeDataTemp = MongoDataCollection(this.mongoMappingList);
                    //將資料寫入Modbus DataStore
                    DataStoreWrite(this.mongoMappingList, writeDataTemp);
                    Thread.Sleep(queryTimeInterval);
                }
            });
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
        public void InitIPAddress()
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
            InitModbusTcpSlave();
            this.pollingMongoDB.Start();
        }

        public void MongoDBConnect()
        {
            try
            {
                this.mongoDBConnParam.connectionString = mongoConnectionString;
                this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);
                WriteLog("連線至Mongo : " );
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "連線至mongo : " + "\r\n");
            }
            catch (Exception e)
            {
                errorCount++;
                WriteLog("MongoDBConnect  Exception : " + e.Message);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDBConnect  Exception : " + e.Message + "\r\n");
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
        }
        
        public void InitModbusTcpSlave()
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
                //List<bool> writeDataCoil = new List<bool>() {true,false,true,false,true};
                //List<string> writeDataTemp = new List<string>();
                //List<ushort> writeDataRegisters = new List<ushort>() { 1,2,3,4,5};

                //for (int i = 0; i < 5; i++)
                //{
                //    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[i + 1] = writeDataCoil[i];
                //    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[i + 1] = writeDataCoil[i];
                //    this.modbusTcpConnParam.slave.DataStore.InputRegisters[i + 1] = writeDataRegisters[i];
                //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[i+ 1] = writeDataRegisters[i];
                //}
                //test---------------------------------------------------------------

            }
            catch (Exception e)
            {
                errorCount++;
                WriteLog("StartModbusTcpSlave : " + e.Message);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "StartModbusTcpSlave : " + e.Message + "\r\n");
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
        }

        private void Slave_ModbusSlaveRequestReceived(object? sender, ModbusSlaveRequestEventArgs e)
        {
            
        }
        //public List<JToken> MongoMappingListCreate(RequestParam requestParam)
        //{

        //    int registerCount = 0;
        //    try
        //    {
        //        var ob = json[requestParam.functionName];
        //        for (int i = 0; i < ob.Count(); i++)
        //        {
        //            mongoMappingList.Add(ob[i]);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        WriteLog("Exception MongoMappingListCreate : " + e.Message);
        //    }
        //    WriteLog("MongoMappingListCreate : " + mongoMappingList.Count());
        //    return mongoMappingList;
        //}

        public List<string> MongoDataCollection(List<JToken> mongoMappingList)
        {
            List<string> mongoDataList = new List<string>();
            List<string> mongoDataListTMP = new List<string>();
            List<string> mongoDataTest = new List<string>();
            try
            {
                for (int i = 0; i < mongoMappingList.Count(); i++)
                {
                    this.mongoDBQueryParam.collection = (string)mongoMappingList[i]["Collection"];
                    this.mongoDBQueryParam.database = (string)mongoMappingList[i]["Database"];
                    this.mongoDBQueryParam.registers = (string)mongoMappingList[i]["Registers"];
                    this.mongoDBQueryParam.functionCodes = (string)mongoMappingList[i]["FunctionCode"];
                    this.mongoDBQueryParam.field = (string)mongoMappingList[i]["Field"];
                    this.mongoDBQueryParam.type = (string)mongoMappingList[i]["Type"];

                    mongoDataListTMP = QueryData(this.mongoDBQueryParam);
                    foreach (var mongoData in mongoDataListTMP)
                    {
                        mongoDataList.Add(mongoData);
                        mongoDataTest.Add(mongoData);//test
                        WriteLog("MongoDataCollection mongoData : " + mongoData);
                    }
                    mongoDataListTMP.Clear();
                }
                WriteLog("MongoDataCollection mongoMappingList : " + mongoMappingList.Count());
                WriteLog("MongoDataCollection mongoDataList : " + mongoDataList.Count());
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "點位數量 : " + mongoMappingList.Count() + " 資料數量 : " + mongoDataList.Count() + "\r\n"); }));
            }
            catch (Exception e)
            {
                WriteLog("Exception MongoDataCollection : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception MongoDataCollection : " + e.Message + "\r\n"); }));
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
            //Test---------------------------------------------------------------------
            List<string> testSample = new List<string>() { "1", "0", "1", "0", "0", "1", "0", "1", "4", "4", "2", "10", "4", "4", "4", "4", "39322", "16025", "4719", "15235", "29884", "15379", "62588", "184", "1", "5", "0", "1", "2", "9", "2", "4", "3", "6", "7", "4", "4", "1", "0", "4", "3", "3", "4", "3", "3", "7", "5", "2", "4", "0", "4", "6", "5", "2", "2", "1", "0", "5", "0", "0", "0", "0", "0" };
            if(mongoDataTest.Count() != mongoDataList.Count())
            {
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "資料筆數有誤" + "\r\n"); }));
                WriteLog("資料筆數有誤");
            }
            else if (!mongoDataList.SequenceEqual(testSample))
            {
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "資料內容有誤" + "\r\n"); }));
                WriteLog("資料內容有誤");
            }
            //---------------------------------------------------------------------
            WriteLog("MongoDataCollection : " + mongoDataList.Count());
            mongoDataTest.Clear();
            return mongoDataList;
        }

        public List<string> QueryData(MongoDBQueryParam mongoDBQueryParam)
        {
            List<string> mongoDataList = new List<string>();
            this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(this.mongoDBQueryParam.database);
            var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);

            var filter = Builders<BsonDocument>.Filter.Empty;

            var doc = collections.Find(filter).ToList().Last();
            //判斷型態
            switch (mongoDBQueryParam.type)
            {
                case "int32":
                    try
                    {
                        //this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(this.mongoDBQueryParam.database);
                        //var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);

                        //var filter = Builders<BsonDocument>.Filter.Empty;

                        //var doc = collections.Find(filter).ToList().Last();
                        Console.WriteLine(doc[mongoDBQueryParam.field].ToString());
                        WriteLog("QueryData : " + doc[mongoDBQueryParam.field].ToString());
                        mongoDataList = Int32ConvertToInt16(doc[mongoDBQueryParam.field]);
                    }
                    catch (Exception e)
                    {
                        WriteLog("Exception QueryData : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                        errorCount++;
                        label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                    }
                    break;
                case "float":
                    try
                    {
                        //this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(this.mongoDBQueryParam.database);
                        //var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);

                        //var filter = Builders<BsonDocument>.Filter.Empty;

                        //var doc = collections.Find(filter).ToList().Last();
                        Console.WriteLine(doc[mongoDBQueryParam.field].ToString());
                        WriteLog("QueryData : " + doc[mongoDBQueryParam.field].ToString());
                        mongoDataList = FloatConvertToInt16(doc[mongoDBQueryParam.field]);
                    }
                    catch (Exception e)
                    {
                        WriteLog("Exception QueryData : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                        errorCount++;
                        label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                    }
                    break;
                default:
                    try
                    {
                        //this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(this.mongoDBQueryParam.database);
                        //var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);

                        //var filter = Builders<BsonDocument>.Filter.Empty;

                        //var doc = collections.Find(filter).ToList().Last();
                        Console.WriteLine(doc[mongoDBQueryParam.field].ToString());
                        WriteLog("QueryData : " + doc[mongoDBQueryParam.field].ToString());
                        mongoDataList.Add(doc[mongoDBQueryParam.field].ToString());
                    }
                    catch (Exception e)
                    {
                        WriteLog("Exception QueryData : " + e.Message);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                        errorCount++;
                        label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                    }
                    break;
            }
            return mongoDataList;
        }

        public void DataStoreWrite(List<JToken> mongoMappingList, object writeData)
        {
            int mongoDataCount = 0;
            for (int i = 0; i < mongoMappingList.Count(); i++)
            {
                switch (mongoMappingList[i]["FunctionCode"].ToString())
                {
                    case "1"://Read Coil Status
                        try
                        {
                            List<string> writeDataCoil = (List<string>)writeData;
                            if(writeDataCoil[mongoDataCount] == "1")
                            {
                                this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[(int)mongoMappingList[i]["Registers"]] = true;
                                
                            }
                            else if(writeDataCoil[mongoDataCount] == "0")
                            {
                                this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[(int)mongoMappingList[i]["Registers"]] = false;
                            }
                            //this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[(int)mongoMappingList[i]["Registers"]] = bool.Parse(writeDataCoil[i]);
                            WriteLog("Read Coil Status : " + this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataCoil[mongoDataCount]);
                            if(textBoxMessageCloseFlag == false)
                            {
                                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status : " + Convert.ToInt16((int)mongoMappingList[i]["Registers"]) + " = " + writeDataCoil[mongoDataCount] + "\r\n"); }));

                            }
                            mongoDataCount++;
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Coil Status exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status exception : " + e.Message + "\r\n"); }));
                            label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    case "2"://Read Input Status
                        try
                        {
                            List<string> writeDataInputDiscreates = (List<string>)writeData;
                            if(writeDataInputDiscreates[mongoDataCount] == "1")
                            {
                                this.modbusTcpConnParam.slave.DataStore.InputDiscretes[(int)mongoMappingList[i]["Registers"]] = true;
                            }
                            else if(writeDataInputDiscreates[mongoDataCount] == "0")
                            {
                                this.modbusTcpConnParam.slave.DataStore.InputDiscretes[(int)mongoMappingList[i]["Registers"]] = false;
                            }
                            //this.modbusTcpConnParam.slave.DataStore.InputDiscretes[(int)mongoMappingList[i]["Registers"]] = bool.Parse(writeDataInputDiscreates[i]);
                            WriteLog("Read Input Status : " + this.modbusTcpConnParam.slave.DataStore.InputDiscretes[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputDiscreates[mongoDataCount]);
                            
                            if (textBoxMessageCloseFlag == false)
                            {
                                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataInputDiscreates[mongoDataCount] + "\r\n"); }));
                            }
                            mongoDataCount++;
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Input Status exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status exception : " + e.Message + "\r\n"); }));
                            label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    case "3"://Read Holding Registers
                        try
                        {
                            int floatLength = 2;
                            List<string> writeDataHoldingRegisters = (List<string>)writeData;
                            switch (mongoMappingList[i]["Type"].ToString())
                            {
                                case "float":
                                case "int32":
                                    for (int address = 0; address < floatLength; address++)
                                    {
                                        this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address] = ushort.Parse(writeDataHoldingRegisters[mongoDataCount]) ;
                                        WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));

                                        }
                                        mongoDataCount++;
                                    }
                                    break;
                                default:
                                    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataHoldingRegisters[mongoDataCount]);
                                    WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                    if (textBoxMessageCloseFlag == false)
                                    {
                                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));

                                    }
                                    mongoDataCount++;
                                    break;
                            }
                            //List<string> writeDataHoldingRegisters = (List<string>)writeData;
                            //this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataHoldingRegisters[i]);
                            //this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[requestParam.startAddress + i + 1] = writeDataHoldingRegisters[i];
                            //WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataHoldingRegisters[i]);
                            //if (textBoxMessageCloseFlag == false)
                            //{
                            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataHoldingRegisters[i] + "\r\n"); }));

                            //}
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Holding Registers exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers exception : " + e.Message + "\r\n"); }));
                            label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    case "4"://Read Input Registers
                        try
                        {
                            int floatLength = 2;
                            List<string> writeDataInputRegisters = (List<string>)writeData;
                            switch (mongoMappingList[i]["Type"].ToString())
                            {
                                case "float":
                                case "int32":
                                    for (int address = 0; address < floatLength; address++)
                                    {
                                        this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"] + address] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                                        WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
                                        }
                                        mongoDataCount++;
                                    }
                                    break;
                                default:
                                    this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                                    WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputRegisters[mongoDataCount]);

                                    if (textBoxMessageCloseFlag == false)
                                    {
                                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
                                    }
                                    mongoDataCount++;
                                    break;
                            }
                            //List<string> writeDataInputRegisters = (List<string>)writeData;
                            //this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                            //WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputRegisters[mongoDataCount]);
                            
                            //if (textBoxMessageCloseFlag == false)
                            //{
                            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
                            //}
                            //mongoDataCount++;
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Input Registers exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers exception : " + e.Message + "\r\n"); }));
                            label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    default:
                        break;
                }
            }
            updateCount++;
            label4.Invoke(new Action(() => { label4.Text = updateCount.ToString(); }));
            textBoxMessageCloseFlag = true;
        }

        public List<string> FloatConvertToInt16(BsonValue floatData)
        {
            List<string> dataList = new List<string>();
            ushort[] uintData = new ushort[2];
            float[] floatDataTmp = new float[1] { Convert.ToSingle(floatData)};
            Buffer.BlockCopy(floatDataTmp, 0, uintData, 0, 4);
            for (int index = 0; index < uintData.Length; index++)
            {
                dataList.Add(uintData[index].ToString());
            }
            return dataList;
        }
        public List<string> Int32ConvertToInt16(BsonValue int32Data)
        {
            List<string> dataList = new List<string>();
            ushort[] uintData = new ushort[2];
            int[] intDataTmp = new int[1] { Convert.ToInt32(int32Data) };
            Buffer.BlockCopy(intDataTmp, 0, uintData, 0, 4);
            for (int index = 0; index < uintData.Length; index++)
            {
                dataList.Add(uintData[index].ToString());
            }
            return dataList;
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