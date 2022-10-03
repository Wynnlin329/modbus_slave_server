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
using System.Text.RegularExpressions;

namespace modbus_server
{
    public partial class Form1 : Form
    {
        ModbusTcpConnParam modbusTcpConnParam = new ModbusTcpConnParam();
        MongoDBConnParam mongoDBConnParam = new MongoDBConnParam();
        MongoDBQueryParam mongoDBQueryParam = new MongoDBQueryParam();
        System.Timers.Timer pollingMongoDB = new System.Timers.Timer();
        System.Timers.Timer pollingClient = new System.Timers.Timer();
        System.Timers.Timer pollingMongoDBConnect = new System.Timers.Timer();
        System.Timers.Timer updateStatus = new System.Timers.Timer();
        System.Timers.Timer scadaHeartbeat = new System.Timers.Timer();
        JObject json = new JObject();
        JObject jsonStatus = new JObject();
        List<JToken> mongoMappingList = new List<JToken>();
        List<JToken> statusList = new List<JToken>();

        //之後改成偵測init數量來動態新增list的數量
        List<string> statusUpdateList = new List<string>() { "0", "0", "0", "0" };// statusUpdateList[0] : mongodbStatus 0斷線 1連線 , statusUpdateList[1] : clientStatus 0斷線 1連線 ,statusUpdateList[2] : modbus server mode  0近端 1遠端 ,statusUpdateList[3] : pExecuteStatus
        List<string> passIpAddress = new List<string>() {"192.168.101.203","192.168.56.1","192.168.111.58"};
        
        //string mongoConnectionString = "mongodb://wynn:0000@192.168.56.101:27017,192.168.56.102:27017,192.168.56.103:27017/?replicaSet=rs0&serverSelectionTimeoutMS=5000";
        string mongoConnectionString = string.Empty;
        string logAddress = @"D:\log\";
        string mongoDBStatus = "unConnect";
        string clientStatus = "unConnect";
        string previousMongoDBStatus = string.Empty;
        string previousClientStatus = string.Empty;
        string cpuID;
        bool textBoxMessageCloseFlag = false;
        bool textBoxMessageStatusCloseFlag = false;
        bool clientDetectFlag = false;
        bool updateStatusFlag = false;
        bool timerProcessFlag = false;
        bool scadaHeartbeatFlag = false;
        int pMax = 100;
        int pMin = -100;
        int pExecuteStatus = 0;
        int previousPExecuteStatus = 3;
        int modbusServerMode = 0;
        int previousModbusServerMode = 2;
        int scadaMode = 0;
        int previousScadaMode = 2;
        int queryTimeInterval = 10;
        int errorCount = 0;
        int updateCount = 0;
        int scadaHeartbeatTime = 0;
        int previousScadaHeartbeatCount = 0;
        int clientNo = 0;
        int previousClientNo = 0;
        int previousPValue = 0;

        enum Mode : int
        {
            Local = 0,
            Remote = 1
        }
        enum Status : int
        {
            unSuccess = 0,
            success = 1
        }
        public Form1()
        {
            InitializeComponent();
            cpuID = GetCPUID();
            if (cpuID == "BFEBFBFF000906EA")//BFEBFBFF000806C1(test)   BFEBFBFF000906EA(Modbus Server)   04:42:1A:CB:96:CA(remote)
            {
                InitMappingData();
                InitIPAddress();
                InitRadioButton();
                SetPollingClient();
                SetPollingMongoDBDataCollection();
                SetPollingMongoDBConnect();
                SetPollingScadaConnect();
                SetUpdateStatusProcess();
            }
            else
            {
                throw new Exception("系統異常");
            }
            //InitMappingData();
            //InitIPAddress();
            //InitRadioButton();
            //SetPollingClient();
            //SetPollingMongoDBDataCollection();
            //SetPollingMongoDBConnect();
            //SetPollingScadaConnect();
            //SetUpdateStatusProcess();
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
        public void InitMappingData()
        {
            this.json = ExcelHelper.ExcelToJson("mappingTable.xlsx");
            foreach (var mappingList in this.json)
            {
                if (mappingList.Key == "InitStatus")
                {
                    foreach (var status in mappingList.Value)
                    {
                        this.statusList.Add(status);
                    }
                }
                else
                {
                    foreach (var mapping in mappingList.Value)
                    {
                        this.mongoMappingList.Add(mapping);
                    }
                }
            }
            //-------------------------------------------------------------
            //for (int i = 0; i < this.mongoMappingList.Count(); i++)
            //{
            //    WriteLog("InitMappingData mappingTable資料內容 : " + this.mongoMappingList[i]);
            //    textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitMappingData mappingTable資料內容 : " + this.mongoMappingList[i] + " \r\n");
            //}
            //for (int i = 0; i < this.statusList.Count(); i++)
            //{
            //    WriteLog("InitMappingData statusList內容 : " + this.statusList[i]);
            //    textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitMappingData statusList內容 : " + this.statusList[i] + " \r\n");
            //}
            //-------------------------------------------------------------
            WriteLog("InitMappingData mappingTable資料筆數 : " + this.mongoMappingList.Count());
            WriteLog("InitMappingData statusList : " + this.statusList.Count());
        }
        public void InitIPAddress()
        {
            int count = 0;
            this.modbusTcpConnParam.port = 502;
            this.modbusTcpConnParam.slaveID = 1;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;
            for (int i = 0; i < addr.Count(); i++)
            {
                if (addr[i].AddressFamily.ToString() == "InterNetwork")
                {
                    comboBox1.Items.Insert(count, addr[i]);
                    count++;
                }
            }
        }
        private void InitRadioButton()
        {

            if (radioButtonNo.Checked == true)
            {
                textboxSecondary.Enabled = false;
                textboxSecondaryPort.Enabled = false;
                textboxArbiter.Enabled = false;
                textboxArbiterPort.Enabled = false;
                textboxReplicaSet.Enabled = false;
            }
            else if (radioButtonYes.Checked == true)
            {

            }
        }
        public void SetPollingClient()
        {
            List<string> writeDataTemp = new List<string>();
            this.pollingClient.Interval = 1000;
            this.pollingClient.AutoReset = false;
            this.pollingClient.Elapsed += new ElapsedEventHandler((x, y) =>
            {
                MongoDBQueryParam tcpControlQueryParam = new MongoDBQueryParam();
                tcpControlQueryParam.database = "AFC";
                tcpControlQueryParam.collection = "tcp_control";
                tcpControlQueryParam.field = "control";
                while (true)
                {
                    ClientDetect();
                    if(this.mongoDBStatus == "connected")
                    {
                        EmsControlMode(tcpControlQueryParam);
                    }
                    Thread.Sleep(1000);
                }
            });
        }
        public void SetPollingMongoDBDataCollection()
        {
            List<string> writeDataTemp = new List<string>();
            this.pollingMongoDB.Interval = 1000;
            this.pollingMongoDB.AutoReset = false;
            this.pollingMongoDB.Elapsed += new ElapsedEventHandler((x, y) =>
            {
                while (true)
                {
                    if (mongoDBStatus == "connected")
                    {
                        //取出MongoDB的資料
                        writeDataTemp = MongoDataCollection(this.mongoMappingList);
                        //將資料寫入Modbus DataStore
                        if (writeDataTemp[0] != "mongoStatusUnconnect")
                        {
                            DataStoreWrite(this.mongoMappingList, writeDataTemp);
                        }
                        textBoxMessageCloseFlag = true;
                    }
                    Thread.Sleep(queryTimeInterval);
                }
            });
        }
        public void SetPollingMongoDBConnect()
        {
            this.pollingMongoDBConnect.Interval = 1000;
            this.pollingMongoDBConnect.AutoReset = false;
            this.pollingMongoDBConnect.Elapsed += new ElapsedEventHandler((x, y) =>
            {

                while (true)
                {
                    MongoDBHeartBeat();
                    Thread.Sleep(1000);
                }
            });
        }
        public void SetPollingScadaConnect()
        {
            /***
             * 每秒累加scadaHeartbeatTime，若超過6秒沒被歸0，將modbusServerMode改為近端模式
             * 
             ***/
            this.scadaHeartbeat.Interval = 1000;
            this.scadaHeartbeat.Enabled = false;
            this.scadaHeartbeat.AutoReset = true;
            this.scadaHeartbeat.Elapsed += new ElapsedEventHandler((x, y) =>
            {
                this.scadaHeartbeatTime++;
                ScadaHeartBeatDetect(this.scadaHeartbeatTime);
                //RemoteIPScan();
            });
        }
        public void SetUpdateStatusProcess()
        {
            this.updateStatus.Interval = 1000;
            this.updateStatus.AutoReset = false;
            this.updateStatus.Elapsed += new ElapsedEventHandler((x, y) =>
            {
                while (true)
                {
                    StatusChangeProcess();
                    Thread.Sleep(1000);
                }
            });
        }
        private void ProcessStart_Click(object sender, EventArgs e)
        {
            bool configReport;
            configReport = ConfigCheacker();
            if (configReport == true)
            {
                this.queryTimeInterval = Convert.ToInt32(textboxIntervalTime.Text) * 1000;
                InitMongoDBConnectString();
                
                //textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "this.modbusTcpConnParam.tcpListener : " + this.modbusTcpConnParam.tcpListener + "\r\n");//debug
                if (this.modbusTcpConnParam.tcpListener != null)
                {
                    WriteLog("this.modbusTcpConnParam.tcpListener : " + this.modbusTcpConnParam.tcpListener);//debug
                    this.mongoDBStatus = "unConnect";
                    this.modbusTcpConnParam.slave.Dispose();
                    this.modbusTcpConnParam.tcpListener.Stop();
                    //this.pollingMongoDBConnect.Stop();
                    //this.pollingClient.Stop();
                    //this.updateStatus.Stop();
                    //this.pollingMongoDB.Stop();
                    this.modbusTcpConnParam.ipAddress = (IPAddress)comboBox1.SelectedItem;
                    InitModbusTcpSlave();
                }
                else
                {
                    WriteLog("this.modbusTcpConnParam.tcpListener : null " + this.modbusTcpConnParam.tcpListener);//debug
                    this.modbusTcpConnParam.ipAddress = (IPAddress)comboBox1.SelectedItem;
                    InitModbusTcpSlave();
                    InitStatus();
                }
                comboBox1.Enabled = false;
                textboxDBUser.Enabled = false;
                textboxPassword.Enabled = false;
                textboxPrimary.Enabled = false;
                textboxPrimaryPort.Enabled = false;
                textboxSecondary.Enabled = false;
                textboxSecondaryPort.Enabled = false;
                textboxArbiter.Enabled = false;
                textboxArbiterPort.Enabled = false;
                textboxIntervalTime.Enabled = false;
                radioButtonYes.Enabled = false;
                radioButtonNo.Enabled = false;
                button2.Enabled = true;
                button1.Enabled = false;
                this.updateStatusFlag = true;
                WriteLog("讀取時間間隔 : " + textboxIntervalTime.Text + "秒");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "讀取時間間隔 : " + textboxIntervalTime.Text + "秒 \r\n");
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            radioButtonYes.Enabled = true;
            radioButtonNo.Enabled = true;
            button2.Enabled = false;
            button1.Enabled = true;
            comboBox1.Enabled = true;
            textboxDBUser.Enabled = true;
            textboxPassword.Enabled = true;
            textboxPrimary.Enabled = true;
            textboxPrimaryPort.Enabled = true;
            textboxIntervalTime.Enabled = true;

            if (radioButtonYes.Checked == true)
            {
                textboxSecondary.Enabled = true;
                textboxSecondaryPort.Enabled = true;
                textboxArbiter.Enabled = true;
                textboxArbiterPort.Enabled = true;
                textboxReplicaSet.Enabled = true;
            }
        }
        public bool ConfigCheacker()
        {
            bool checkReport = true;
            if (comboBox1.SelectedItem == null)
            {
                checkReport = false;
                MessageBox.Show("請選擇Modbus Server IP");
            }
            else if (string.IsNullOrEmpty(textboxPrimary.Text))
            {
                checkReport = false;
                MessageBox.Show("請輸入EMS IP");
            }
            else if (string.IsNullOrEmpty(textboxPrimaryPort.Text))
            {
                checkReport = false;
                MessageBox.Show("請輸入Port");
            }
            else if (Convert.ToDecimal(textboxIntervalTime.Text.Trim()) < 1)
            {
                checkReport = false;
                MessageBox.Show("最低秒數為1秒");
            }
            else if (string.IsNullOrEmpty(textboxSecondary.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("請輸入EMS IP");
            }
            else if (string.IsNullOrEmpty(textboxSecondaryPort.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("請輸入Port");
            }
            else if (string.IsNullOrEmpty(textboxArbiter.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("請輸入EMS IP");
            }
            else if (string.IsNullOrEmpty(textboxArbiterPort.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("請輸入Port");
            }
            else if (string.IsNullOrEmpty(textboxReplicaSet.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("請輸入ReplicaSetName");
            }
            return checkReport;
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
                this.modbusTcpConnParam.slave.DataStore.DataStoreWrittenTo += DataStore_DataStoreWrittenTo;
                this.modbusTcpConnParam.slave.Listen();
                WriteLog("Modbus Server 已開啟");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Modbus Server 已開啟 \r\n");



                //test-------------------------------------------------------------- -
                //List<bool> writeDataCoil = new List<bool>() { true, false, true, false, true };
                //List<string> writeDataTemp = new List<string>();
                //List<ushort> writeDataRegisters = new List<ushort>() { 1, 2, 3, 4, 5 };

                //for (int i = 0; i < 5; i++)
                //{
                //    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[i + 1] = writeDataCoil[i];
                //    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[i + 1] = writeDataCoil[i];
                //    this.modbusTcpConnParam.slave.DataStore.InputRegisters[i + 1] = writeDataRegisters[i];
                //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[i + 1] = writeDataRegisters[i];
                //}
                //test-------------------------------------------------------------- -

            }
            catch (Exception e)
            {
                errorCount++;
                WriteLog("InitModbusTcpSlave : " + e.Message);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitModbusTcpSlave : " + e.Message + "\r\n");
                //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
        }
        public void InitStatus()
        {
            this.timerProcessFlag = true;
            //foreach (var status in this.statusList)
            //{
            //    switch (status["Status"].ToString())
            //    {
            //        //可在此新增需要監聽的連線狀態
            //        case "mongoDB_connect":
            //            this.pollingMongoDBConnect.Start();
            //            break;
            //        case "client_connect":
            //            this.pollingClient.Start();
            //            break;

            //    }
            //}

            this.pollingMongoDBConnect.Start();
            this.pollingClient.Start();
            //this.scadaHeartbeat.Start();//need open
            this.updateStatus.Start();
            this.pollingMongoDB.Start();
            WriteLog("InitStatus ");//debug
        }
        public void InitMongoDBConnectString()
        {
            if (radioButtonNo.Checked == true)
            {
                string DBuser = textboxDBUser.Text.Trim();
                string DBpassword = textboxPassword.Text.Trim();
                string mongoDBPrimary = textboxPrimary.Text.Trim();
                string mongoDBPrimaryPort = textboxPrimaryPort.Text.Trim();
                this.mongoConnectionString = string.Format("mongodb://{0}:{1}@{2}:{3}/admin?serverSelectionTimeoutMS=5000", DBuser, DBpassword, mongoDBPrimary, mongoDBPrimaryPort);
                //this.mongoConnectionString = string.Format("mongodb://{0}:{1}/admin?serverSelectionTimeoutMS=5000", mongoDBPrimary, mongoDBPrimaryPort);
            }
            else if (radioButtonYes.Checked == true)
            {
                string DBuser = textboxDBUser.Text.Trim();
                string DBpassword = textboxPassword.Text.Trim();
                string mongoDBPrimary = textboxPrimary.Text.Trim();
                string mongoDBSecondary = textboxSecondary.Text.Trim();
                string mongoDBArbiter = textboxArbiter.Text.Trim();
                string mongoDBPrimaryPort = textboxPrimaryPort.Text.Trim();
                string mongoDBSecondaryPort = textboxSecondaryPort.Text.Trim();
                string mongoDBArbiterPort = textboxArbiterPort.Text.Trim();
                string replicaSetName = textboxReplicaSet.Text.Trim();
                //this.mongoConnectionString = "mongodb://wynn:0000@192.168.56.101:27017,192.168.56.102:27017,192.168.56.103:27017/?replicaSet=rs0&serverSelectionTimeoutMS=5000";
                this.mongoConnectionString = string.Format("mongodb://{0}:{1}@{2}:{3},{4}:{5},{6}:{7}/?replicaSet={8}&serverSelectionTimeoutMS=5000"
                                                          , DBuser, DBpassword, mongoDBPrimary, mongoDBPrimaryPort, mongoDBSecondary, mongoDBSecondaryPort, mongoDBArbiter, mongoDBArbiterPort, replicaSetName);
            }
        }
        public void MongoDBHeartBeat()
        {
            try
            {
                this.mongoDBConnParam.connectionString = mongoConnectionString;
                this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);
                IMongoDatabase db = this.mongoDBConnParam.mongoClient.GetDatabase("AFC");
                var collections = db.GetCollection<BsonDocument>("program");
                var filter = Builders<BsonDocument>.Filter.Eq("ID", "modbusData");
                var updater = Builders<BsonDocument>.Update.Set("count", 0);
                collections.UpdateOne(filter, updater);
                mongoDBStatus = "connected";
            }
            catch (Exception e)
            {
                errorCount++;
                WriteLog("MongoDBConnect  Exception : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線失敗 " + "\r\n"); }));
                //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                mongoDBStatus = "unConnect";
            }


        }
        public void MongoDBConnect()
        {
            if (mongoDBStatus == "unConnect")
            {
                try
                {
                    this.mongoDBConnParam.connectionString = mongoConnectionString;
                    this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);
                    IMongoDatabase db = this.mongoDBConnParam.mongoClient.GetDatabase("AFC");
                    var collections = db.GetCollection<BsonDocument>("program");
                    var filter = Builders<BsonDocument>.Filter.Eq("ID", "modbusData");
                    var updater = Builders<BsonDocument>.Update.Set("count", 0);
                    collections.UpdateOne(filter, updater);



                    //var collections = db.GetCollection<BsonDocument>("system.users");
                    //var filter = Builders<BsonDocument>.Filter.Empty;
                    //var doc  = collections.Find(filter).ToList();
                    //var conn = db.ListCollectionNames();

                    WriteLog("MongoDB連線成功");
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線成功 " + "\r\n"); }));
                    mongoDBStatus = "connected";
                }
                catch (Exception e)
                {
                    errorCount++;
                    WriteLog("MongoDBConnect  Exception : " + e.Message);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線失敗 " + "\r\n"); }));
                    //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                }
            }
        }
        public void ClientDetect()
        {
            clientNo = ((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters.Count();
            if (this.clientNo != this.previousClientNo && this.clientNo != 0)
            {
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client 連線數量 : " + this.clientNo.ToString() + "\r\n"); }));
                //WriteLog("Client 連線數量 : " + this.clientNo.ToString());
                this.previousClientNo = this.clientNo;
                this.clientDetectFlag = false;
                this.clientStatus = "connected";
            }
            else if (this.clientNo == 0 && this.clientDetectFlag == false)
            {
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client 連線數量 : " + this.clientNo.ToString() + "\r\n"); }));
                //WriteLog("Client 連線數量 : " + this.clientNo.ToString());
                this.previousClientNo = this.clientNo;
                this.clientDetectFlag = true;
                this.clientStatus = "unConnect";
            }
        }
        public void EmsControlMode(MongoDBQueryParam tcpControlQueryParam)
        {
            var queryData = Query(tcpControlQueryParam);
            if(queryData[tcpControlQueryParam.field] == 1)
            {
                this.modbusServerMode = (int)Mode.Remote;
            }
            else
            {
                this.modbusServerMode = (int)Mode.Local;
            }
        }
        public void ScadaHeartBeatDetect(int scadaHeartbeatTime)
        {
            if (scadaHeartbeatTime >= 6 && this.scadaHeartbeatFlag == false)
            {
                this.modbusServerMode = (int)Mode.Local;
                //this.scadaMode = (int)Mode.Local;
                this.previousScadaMode = 2;//主要重置先前的狀態
                WriteLog("Scada斷線 : modbusServerMode = " + (int)Mode.Local);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Scada斷線，ModbusServer切換為近端模式" + "\r\n"); }));

                try
                {
                    //停止模式
                    //寫入site_control
                    MongoDBQueryParam mongoDBQueryParam = new MongoDBQueryParam();
                    mongoDBQueryParam.database = "AFC";
                    mongoDBQueryParam.collection = "site_control";
                    var queryData = Query(mongoDBQueryParam);
                    var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);
                    var doc = SiteControlCollection(0, queryData, 0);//need open
                    collections.InsertOneAsync(doc);//need open


                    //寫入SOE
                    MongoDBQueryParam eventQueryParam = new MongoDBQueryParam();
                    eventQueryParam.database = "AFC";
                    eventQueryParam.collection = "SOE";
                    var eventCollections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(eventQueryParam.collection);
                    string events = "SCADA斷線，發送停止模式指令(實功:" + 0 + " kW)";
                    var eventDoc = SOECollection(events);
                    eventCollections.InsertOneAsync(eventDoc);
                    this.scadaHeartbeatFlag = true;

                    //執行狀況為1
                    this.pExecuteStatus = (int)Status.success;

                    //WriteLog("PValue Status : success " + doc.ToString());//need open
                    WriteLog("PValue Status : success " + this.pExecuteStatus + ": " + 0);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : success " + 0 + "\r\n"); }));
                    //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "這是輸入EMS的指令 " + "\r\n"); }));
                    //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + doc.ToString() + "\r\n"); }));//need open
                }
                catch (Exception e)
                {
                    //寫入失敗 執行狀況為0
                    this.pExecuteStatus = (int)Status.unSuccess;
                    WriteLog("PValue Status : unSuccess : 寫入EMS失敗 " + this.pExecuteStatus);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess : 寫入EMS失敗 " + "\r\n"); }));
                }
            }
        }
        public void RemoteIPScan()
        {
            try
            {
                for (int i = 0; i < ((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters.Count(); i++)
                {
                    if (((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters[i].Client.Connected == true)
                    {
                        string matchString = @"((0|1\d?\d?|2(\d?|[0-4]\d|5[0-5])|[3-9]\d?)\.){3}(0|1\d?\d?|2(\d?|[0-4]\d|5[0-5])|[3-9]\d?)";
                        string remoteEndPoint = (((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters[i].Client.RemoteEndPoint).ToString();
                        Regex regex = new Regex(matchString);
                        string remoteIp = regex.Match(remoteEndPoint).ToString();

                        if (!this.passIpAddress.Contains(remoteIp))
                        {
                            ((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters[i].Client.Disconnect(false);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "已斷開IP: " + remoteIp + "\r\n"); }));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteLog("RemoteIPScan Exception : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "RemoteIPScan Exception : " + e.Message + "\r\n"); }));
            }
        }
        private void DataStore_DataStoreWrittenTo(object? sender, Modbus.Data.DataStoreEventArgs e)
        {
            /***
             * 
             * 判斷要寫入哪個點位
             *      寫入scada mode點位
             *          判斷哪種模式
             *              0近端
             *              1遠端
             *          scadaHeartbeatTime 歸0
             *      寫入實虛功點位且 modbus mode為遠端模式
             *          判斷scada mode狀態是否為1且判斷是否介於最大最小值
             *              是  寫入EMS，判斷是否寫入成功
             *                  是  執行狀況點位為1
             *                  否  執行狀況點位為0
             *              否  寫入LOG，執行狀況為2
             *      寫入實虛功點位且 modbus mode為近端模式
             *          執行狀況為2
             *          
             *          
             *      
             *          檢查是否有超過最大最小值
             *              是  執行狀況點位改為0(不執行)
             *              否  判斷scada mode狀態是否為1
             *                      是 寫入EMS,判斷是否寫成功
             *                           成功 執行狀況點位為1
             *                           失敗 執行狀況點位為0
             *                      否 寫入Log,寫完執行狀況點位為2
             ***/

            int pValue = 0;
            BsonDocument doc = new BsonDocument();
            BsonDocument eventDoc = new BsonDocument(); 
            MongoDBQueryParam eventQueryParam = new MongoDBQueryParam();
            eventQueryParam.database = "AFC";
            eventQueryParam.collection = "SOE";
            var eventCollections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(eventQueryParam.collection);

            MongoDBQueryParam mongoDBQueryParam = new MongoDBQueryParam();
            mongoDBQueryParam.database = "AFC";
            mongoDBQueryParam.collection = "site_control";
            var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);

            if (e.StartAddress == 2)
            {
                if (e.Data.B[0] == 0 && e.Data.B[0] != this.previousScadaMode)
                {
                    this.scadaMode = (int)Mode.Local;
                    WriteLog("ScadaMode : 近端模式 " + this.scadaMode);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ScadaMode : 近端模式 " + this.scadaMode + "\r\n"); }));
                }
                else if (e.Data.B[0] == 1 && e.Data.B[0] != this.previousScadaMode)
                {
                    this.scadaMode = (int)Mode.Remote;
                    WriteLog("ScadaMode : 遠端模式 " + this.scadaMode);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ScadaMode : 遠端模式 " + this.scadaMode + "\r\n"); }));
                }
                this.previousScadaMode = this.scadaMode;
                this.scadaHeartbeatTime = 0;
                this.scadaHeartbeatFlag = false;
            }
            else if (e.StartAddress == 5 && this.modbusServerMode == (int)Mode.Remote)
            {
                int mode = 6;
                string events = string.Empty;
                ushort signedParam;
                pValue = ArrayToInt32(e.Data.B.ToArray());
                if (this.scadaMode == (int)Mode.Remote)
                {
                    signedParam = this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[7];
                    if( pValue > 0 && signedParam == 0 || pValue < 0 && signedParam == 65535 || pValue == 0 )
                    {
                        if(pValue != this.previousPValue)
                        {
                            try
                            {
                                //寫入site_control
                                var queryData = Query(mongoDBQueryParam);
                                doc = SiteControlCollection(pValue, queryData, mode);//need open
                                collections.InsertOneAsync(doc);//need open

                                //if (pValue == 0)
                                //{
                                //    Thread.Sleep(2000);
                                //    mode = 0;
                                //    doc = SiteControlCollection(pValue, queryData, mode);//need open
                                //    collections.InsertOneAsync(doc);//need open
                                //}

                                //寫入SOE
                                events = "由SCADA遠端指令控制(實功:" + pValue + " kW)";
                                eventDoc = SOECollection(events);
                                eventCollections.InsertOneAsync(eventDoc);


                                //執行狀況為1
                                this.pExecuteStatus = (int)Status.success;
                                this.previousPValue = pValue;
                                //WriteLog("PValue Status : success " + doc.ToString());//need open
                                WriteLog("PValue Status : success " + this.pExecuteStatus + ": " + pValue);
                                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : success " + pValue + "\r\n"); }));
                                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "這是輸入EMS的指令 " + "\r\n"); }));
                                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + doc.ToString() + "\r\n"); }));//need open
                            }
                            catch (Exception ee)
                            {
                                //寫入失敗 執行狀況為0
                                this.pExecuteStatus = (int)Status.unSuccess;
                                WriteLog("PValue Status : unSuccess : 寫入EMS失敗 " + this.pExecuteStatus);
                                WriteLog("PValue Status : unSuccess : " + doc);//need open
                                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess : 寫入EMS失敗 " + "\r\n"); }));
                            }
                        }
                        else
                        {
                            //寫入失敗 執行狀況為0
                            this.pExecuteStatus = (int)Status.unSuccess;
                            //寫入SOE
                            events = "SCADA寫入重複數值(實功:" + pValue + " kW)";
                            eventDoc = SOECollection(events);
                            eventCollections.InsertOneAsync(eventDoc);

                            WriteLog("PValue Status : unSuccess : 寫入重複數值 " + pValue);
                            WriteLog("PValue Status : unSuccess : " + doc);//need open
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess : 寫入重複數值 " + pValue + "\r\n"); }));
                        }
                    }
                    else
                    {
                        //寫入失敗 執行狀況為0
                        this.pExecuteStatus = (int)Status.unSuccess;
                        WriteLog("PValue Status : unSuccess : 寫入EMS失敗 符號輸入錯誤 " + this.pExecuteStatus + ": " + pValue);
                        WriteLog("PValue Status : unSuccess : " + doc);//need open
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess : 寫入EMS失敗 符號輸入錯誤 " + "\r\n"); }));
                    }
                }
                else if (this.scadaMode == (int)Mode.Local)
                {
                    //scada mode 為近端模式
                    this.pExecuteStatus = (int)Status.unSuccess;
                    WriteLog("PValue Status : unSuccess : scada mode為近端模式 " + this.pExecuteStatus);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : .unSuccess : scada mode為近端模式 " + "\r\n"); }));
                }
                //else if (pValue > this.pMax || pValue < this.pMin)
                //{
                //    //功率值超過上下限
                //    this.pExecuteStatus = (int)Status.noExecute;
                //    WriteLog("PValue Status : noExecute : 功率值超過上下限 " + this.pExecuteStatus);
                //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : noExecute : 功率值超過上下限 " + "\r\n"); }));
                //}
            }
            else if (e.StartAddress == 5 && this.modbusServerMode == (int)Mode.Local)
            {
                pValue = ArrayToInt32(e.Data.B.ToArray());
                //doc = SiteControlCollection(pValue);
                //modbusServerMode 為近端模式
                //寫入LOG
                //執行狀況為2
                this.pExecuteStatus = (int)Status.unSuccess;
                WriteLog("PValue Status : unSuccess : modbusServerMode 為近端模式 " + pValue);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess : modbusServerMode 為近端模式 " + +pValue+"\r\n"); }));
            }
        }
        public BsonDocument SOECollection(string events)
        {
            BsonDocument doc = new BsonDocument
                {
                    {"ID","site_control"},
                    {"time",DateTime.Now.AddHours(8)},
                    {"event",events},
                    {"type","systemMode" }
                };
            return doc;
        }
        public BsonDocument SiteControlCollection(int pValue, BsonDocument queryData,int mode)
        {
            BsonDocument doc = new BsonDocument {
                {"ID", "site_control"},
                {"mode" , mode},
                {"time",DateTime.Now.AddHours(8)},
                {"scheduleFlag",0 },
                {"soc_max" , queryData["soc_max"]},
                {"soc_min" , queryData["soc_min"]},
                {"System_p_max" , queryData["System_p_max"]},
                {"System_p_min" , queryData["System_p_min"]},
                {"System_q_max" , queryData["System_q_max"]},
                {"System_q_min" , queryData["System_q_min"]},
                {"PQ_p_ref", pValue },
                {"PQ_q_ref", 0 },
            };
            return doc;
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
        public void StatusChangeProcess()
        {
            // 狀態點位的更改都由這邊改
            // statusUpdateList[0] : mongodbStatus
            // statusUpdateList[1] : clientStatus
            // statusUpdateList[2] : modbusServerMode
            // statusUpdateList[3] : pStatus
            if (this.mongoDBStatus != this.previousMongoDBStatus && this.mongoDBStatus == "connected")
            {
                WriteLog("StatusChangeProcess mongodbStatus : " + this.mongoDBStatus + "  " + this.previousMongoDBStatus);//debug
                this.statusUpdateList[0] = "1";
                this.updateStatusFlag = true;
                this.previousMongoDBStatus = this.mongoDBStatus;
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線成功 " + "\r\n"); }));
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB 連線狀態 : " + this.statusUpdateList[0] + "\r\n"); }));
                WriteLog("MongoDB連線成功");
                WriteLog("MongoDB 連線狀態為 : " + this.statusUpdateList[0]);
            }
            else if (this.mongoDBStatus != this.previousMongoDBStatus && this.mongoDBStatus == "unConnect")
            {
                this.statusUpdateList[0] = "0";
                this.updateStatusFlag = true;
                this.previousMongoDBStatus = this.mongoDBStatus;
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB 連線狀態 : " + this.statusUpdateList[0] + "\r\n"); }));
                WriteLog("MongoDB 連線狀態為 : " + this.statusUpdateList[0]);
            }
            
            if(this.clientStatus != this.previousClientStatus && this.clientStatus == "connected")
            {
                this.statusUpdateList[1] = "1";
                this.updateStatusFlag = true;
                this.previousClientStatus = this.clientStatus;
                WriteLog("Client  連線狀態為 : " + this.statusUpdateList[1]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client  連線狀態 : " + this.statusUpdateList[1] + "\r\n"); }));
            }
            else if(this.clientStatus != this.previousClientStatus && this.clientStatus == "unConnect")
            {
                WriteLog("StatusChangeProcess clientStatus : " + this.clientStatus + "  " + this.previousClientStatus);//debug
                this.statusUpdateList[1] = "0";
                this.updateStatusFlag = true;
                this.previousClientStatus = this.clientStatus;
                WriteLog("Client  連線狀態為 : " + this.statusUpdateList[1]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client  連線狀態 : " + this.statusUpdateList[1] + "\r\n"); }));
            }
            
            if(this.modbusServerMode == (int)Mode.Local && this.modbusServerMode != this.previousModbusServerMode)
            {
                WriteLog("StatusChangeProcess modbusServerMode : " + this.modbusServerMode + "  " + this.previousModbusServerMode);//debug
                this.statusUpdateList[2] = "0";
                this.updateStatusFlag = true;
                this.previousModbusServerMode = this.modbusServerMode;
                WriteLog("ModbusServer Mode : 近端模式 " + this.statusUpdateList[2]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ModbusServer Mode : 近端模式" + "\r\n"); }));
            }
            else if(this.modbusServerMode == (int)Mode.Remote && this.modbusServerMode != this.previousModbusServerMode)
            {
                WriteLog("StatusChangeProcess modbusServerMode : " + this.modbusServerMode + "  " + this.previousModbusServerMode);//debug
                this.statusUpdateList[2] = "1";
                this.updateStatusFlag = true;
                this.previousModbusServerMode = this.modbusServerMode;
                WriteLog("ModbusServer Mode : 遠端模式 " + this.statusUpdateList[2]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ModbusServer Mode : 遠端模式" + "\r\n"); }));
            }

            if (this.pExecuteStatus == (int)Status.success && this.pExecuteStatus != this.previousPExecuteStatus)
            {
                WriteLog("StatusChangeProcess pStatus : " + this.pExecuteStatus + "  " + this.previousPExecuteStatus);//debug
                this.statusUpdateList[3] = "1";
                this.updateStatusFlag = true;
                this.previousPExecuteStatus = this.pExecuteStatus;
                //WriteLog("PValue Status : success " + this.statusUpdateList[3]);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : success " + "\r\n"); }));
            }
            else if (this.pExecuteStatus == (int)Status.unSuccess && this.pExecuteStatus != this.previousPExecuteStatus)
            {
                WriteLog("StatusChangeProcess pStatus : " + this.pExecuteStatus + "  " + this.previousPExecuteStatus);//debug
                this.statusUpdateList[3] = "0";
                this.updateStatusFlag = true;
                this.previousPExecuteStatus = this.pExecuteStatus;
                //WriteLog("PValue Status : unSuccess " + this.statusUpdateList[3]);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess " + "\r\n"); }));
            }
            //else if (this.pExecuteStatus == (int)Status.noExecute && this.pExecuteStatus != this.previousPExecuteStatus)
            //{
            //    this.statusUpdateList[3] = "2";
            //    this.updateStatusFlag = true;
            //    this.previousPExecuteStatus = this.pExecuteStatus;
            //    //WriteLog("PValue Status : noExecute " + this.statusUpdateList[3]);
            //    //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : noExecute " + "\r\n"); }));
            //}

            if(this.updateStatusFlag == true)
            {
                DataStoreWrite(this.statusList, this.statusUpdateList);
                this.updateStatusFlag = false;
                this.textBoxMessageStatusCloseFlag = true;
            }
            //偵測mongodbStatus 字串
                //跟之前狀態相同  跳過
                //跟之前狀態不同  更新list  打開更新flag
            //偵測clientStatus 字串
                //跟之前狀態相同  跳過
                //跟之前狀態不同  更新list  打開更新flag
            //偵測更新flag
                //true   修改modbus點位  flag改為false
                //false  跳過
        }

        public List<string> MongoDataCollection(List<JToken> mongoMappingList)
        {
            string previousCollection = string.Empty;
            string previousField = string.Empty;
            string previousId = string.Empty;
            BsonDocument document = new BsonDocument();
            List<string> mongoDataList = new List<string>();
            List<string> mongoDataListTMP = new List<string>();
            List<string> mongoDataTest = new List<string>();
            try
            {
                for (int i = 0; i < mongoMappingList.Count(); i++)
                {
                    if(this.mongoDBStatus != "unConnect")
                    {
                        this.mongoDBQueryParam.collection = (string)mongoMappingList[i]["Collection"];
                        this.mongoDBQueryParam.database = (string)mongoMappingList[i]["Database"];
                        this.mongoDBQueryParam.registers = (string)mongoMappingList[i]["Registers"];
                        this.mongoDBQueryParam.functionCodes = (string)mongoMappingList[i]["FunctionCode"];
                        this.mongoDBQueryParam.field = (string)mongoMappingList[i]["Field"];
                        this.mongoDBQueryParam.type = (string)mongoMappingList[i]["Type"];
                        this.mongoDBQueryParam.array = (bool)mongoMappingList[i]["Array"];
                        this.mongoDBQueryParam.arrayLevel = (int)mongoMappingList[i]["ArrayLevel"];
                        this.mongoDBQueryParam.arrayNum = (int)mongoMappingList[i]["ArrayNumber"];
                        this.mongoDBQueryParam.id = (string)mongoMappingList[i]["ID"];




                        if (this.mongoDBQueryParam.collection != previousCollection && this.mongoDBQueryParam.field != previousField || this.mongoDBQueryParam.id != previousId)
                        {
                            document = Query(this.mongoDBQueryParam);
                            previousField = this.mongoDBQueryParam.field;
                            previousCollection = this.mongoDBQueryParam.collection;
                            previousId = this.mongoDBQueryParam.id;
                        }

                        if (document.ElementCount != 0)
                        {
                            mongoDataListTMP = QueryDataExtrating(this.mongoDBQueryParam, document);
                        }
                        else
                        {
                            mongoDataListTMP.Add("mongoStatusUnconnect");
                            WriteLog("MongoDataCollection : " + this.mongoDBQueryParam.collection + " 尚未產生資料 ");
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDataCollection : " + this.mongoDBQueryParam.collection + " 尚未產生資料 " + "\r\n"); }));
                        }
                        if (mongoDataListTMP[0] != "mongoStatusUnconnect")
                        {
                            foreach (var mongoData in mongoDataListTMP)
                            {
                                mongoDataList.Add(mongoData);
                                //mongoDataTest.Add(mongoData);//test
                                //WriteLog("MongoDataCollection mongoData : " + mongoData);
                            }
                            mongoDataListTMP.Clear();
                        }
                        else
                        {
                            mongoDataList.Clear();
                            mongoDataList.Add("mongoStatusUnconnect");
                            break;
                        }
                    }
                    else
                    {
                        mongoDataList.Clear();
                        mongoDataList.Add("mongoStatusUnconnect");
                        break;
                    }
                }
                //WriteLog("MongoDataCollection mongoMappingList : " + mongoMappingList.Count());
                //WriteLog("MongoDataCollection mongoDataList : " + mongoDataList.Count());
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "點位數量 : " + mongoMappingList.Count() + " 資料數量 : " + mongoDataList.Count() + "\r\n"); }));
            }
            catch (Exception e)
            {
                WriteLog("Exception MongoDataCollection : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception MongoDataCollection : " + e.Message + "\r\n"); }));
                errorCount++;
                //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
            //Test---------------------------------------------------------------------
            //List<string> testSample = new List<string>() { "1", "0", "1", "0", "0", "1", "0", "1", "4", "4", "2", "10", "4", "4", "4", "4", "39322", "16025", "4719", "15235", "29884", "15379", "62588", "184", "1", "5", "0", "1", "2", "9", "2", "4", "3", "6", "7", "4", "4", "1", "0", "4", "3", "3", "4", "3", "3", "7", "5", "2", "4", "0", "4", "6", "5", "2", "2", "1", "0", "5", "0", "0", "0", "0", "0" };
            //if(mongoDataTest.Count() != mongoDataList.Count())
            //{
            //    errorCount++;
            //    label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "資料筆數有誤" + "\r\n"); }));
            //    WriteLog("資料筆數有誤");
            //}
            //else if (!mongoDataList.SequenceEqual(testSample))
            //{
            //    errorCount++;
            //    label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "資料內容有誤" + "\r\n"); }));
            //    WriteLog("資料內容有誤");
            //}
            //---------------------------------------------------------------------
            //WriteLog("MongoDataCollection : " + mongoDataList.Count());
            mongoDataTest.Clear();
            return mongoDataList ;
        }

        public BsonDocument Query(MongoDBQueryParam mongoDBQueryParam)
        {
            BsonDocument doc = new BsonDocument();
            try
            {
                this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(mongoDBQueryParam.database);
                var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);
                var filter = Builders<BsonDocument>.Filter;
                var sort = Builders<BsonDocument>.Sort;

                if (mongoDBQueryParam.collection.ToString() == "env" || mongoDBQueryParam.collection.ToString() == "rio")
                {
                    //filter出特定ID
                    doc = collections.Find(filter.Eq("ID", mongoDBQueryParam.id)).Sort(sort.Descending("_id")).Limit(1).ToList().Last();//倒序
                }
                else 
                {
                    doc = collections.Find(filter.Empty).Sort(sort.Descending("_id")).Limit(1).ToList().Last();//倒序
                }
                //WriteLog("Query : " + doc);
            }
            catch (Exception e)
            {
                WriteLog("Exception Query : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception Query : " + e.Message + "\r\n"); }));
                errorCount++;
                //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
            return doc;
        }

        public List<string> QueryDataExtrating(MongoDBQueryParam mongoDBQueryParam,BsonDocument doc)
        {
            List<string> mongoDataList = new List<string>();
            BsonValue mongoValue = null;
            try
            {
                if (mongoDBQueryParam.arrayLevel > 0 && mongoDBQueryParam.array == true)
                {
                    //另外想多層怎麼解
                }
                else if (mongoDBQueryParam.array == true)
                {
                    mongoValue = doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum];
                }
                else if (mongoDBQueryParam.array == false && mongoDBQueryParam.field == "time")
                {
                    //加入時間戳處理Function
                    string time = doc[mongoDBQueryParam.field].ToString();
                    mongoValue = (BsonValue)DateTimeToTimestamp(time);
                }
                else if (mongoDBQueryParam.array == false && mongoDBQueryParam.field == "expireAt")
                {
                    //加入時間戳處理Function
                    string time = doc[mongoDBQueryParam.field].ToString();
                    mongoValue = (BsonValue)DateTimeToTimestamp(time);
                }
                else if (mongoDBQueryParam.array == false)
                {
                    mongoValue = doc[mongoDBQueryParam.field];
                }
                if(mongoValue == BsonNull.Value)
                {
                    mongoValue = 0;
                }   


                switch (mongoDBQueryParam.type)
                {
                    case "int32":
                    case "uint32":
                        try
                        {
                            //WriteLog("QueryData : " + mongoValue.ToString());
                            mongoDataList = Int32ConvertToInt16(mongoValue);
                        }
                        catch (Exception e)
                        {
                            WriteLog("Exception uint32 QueryData : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                            errorCount++;
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    case "float":
                        try
                        {
                            //WriteLog("QueryData : " + mongoValue.ToString());
                            mongoDataList = FloatConvertToInt16(mongoValue);
                        }
                        catch (Exception e)
                        {
                            WriteLog("Exception float QueryData : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                            errorCount++;
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    default:
                        try
                        {
                            //WriteLog("QueryData : " + mongoValue.ToString());
                            mongoDataList.Add(mongoValue.ToString());
                        }
                        catch (Exception e)
                        {
                            WriteLog("Exception default QueryData : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                            errorCount++;
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                this.mongoDBStatus = "unConnect";
                mongoDataList.Add("mongoStatusUnconnect");
                WriteLog("Exception QueryData : " + e.Message);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                errorCount++;
                //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
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
                            WriteLog("Read Coil Status : " + this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[(int)mongoMappingList[i]["Registers"]-1] + " = " + writeDataCoil[mongoDataCount]);
                            if(textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                            {
                                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status : " + (Convert.ToInt16((int)mongoMappingList[i]["Registers"])-1) + " = " + writeDataCoil[mongoDataCount] + "\r\n"); }));
                            }
                            mongoDataCount++;
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Coil Status exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Coil Status exception : " + e.Message + "\r\n"); }));
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
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
                            //WriteLog("Read Input Status : " + this.modbusTcpConnParam.slave.DataStore.InputDiscretes[(int)mongoMappingList[i]["Registers"]-1] + " = " + writeDataInputDiscreates[mongoDataCount]);
                            
                            if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                            {
                                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status : " + (Convert.ToInt16(mongoMappingList[i]["Registers"])-1) + " = " + writeDataInputDiscreates[mongoDataCount] + "\r\n"); }));
                            }
                            mongoDataCount++;
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Input Status exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Status exception : " + e.Message + "\r\n"); }));
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    case "3"://Read Holding Registers
                        try
                        {
                            int floatLength = 2;
                            int longLegnth = 4;
                            List<string> writeDataHoldingRegisters = (List<string>)writeData;
                            switch (mongoMappingList[i]["Type"].ToString())
                            {
                                case "float":
                                case "int32":
                                case "uint32":
                                    for (int address = 0; address < floatLength; address++)
                                    {
                                        this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address] = ushort.Parse(writeDataHoldingRegisters[mongoDataCount]) ;
                                        //WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address -1 ] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + (Convert.ToInt16(mongoMappingList[i]["Registers"])+ address -1) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));
                                        }
                                        mongoDataCount++;
                                    }
                                    break;
                                default:
                                    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataHoldingRegisters[mongoDataCount]);
                                    //WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] -1 ] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                    if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                    {
                                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + (Convert.ToInt16(mongoMappingList[i]["Registers"]) -1) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));
                                    }
                                    mongoDataCount++;
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            WriteLog("Read Holding Registers exception : " + e.Message);
                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers exception : " + e.Message + "\r\n"); }));
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    case "4"://Read Input Registers
                        try
                        {
                            int floatLength = 2;
                            int longLegnth = 4;
                            List<string> writeDataInputRegisters = (List<string>)writeData;
                            switch (mongoMappingList[i]["Type"].ToString())
                            {
                                case "float":
                                case "int32":
                                case "uint32":
                                    for (int address = 0; address < floatLength; address++)
                                    {
                                        this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"] + address] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                                        //WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]-1] + " = " + writeDataInputRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + (Convert.ToInt16(mongoMappingList[i]["Registers"]) + address -1) +  " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
                                        }
                                        mongoDataCount++;
                                    }
                                    break;
                                default:
                                    this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                                    //WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]-1] + " = " + writeDataInputRegisters[mongoDataCount]);
                                    
                                    if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                    {
                                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + (Convert.ToInt16(mongoMappingList[i]["Registers"])-1) + " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
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
                            //label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                        }
                        break;
                    default:
                        break;
                }
            }
            updateCount++;
            //label4.Invoke(new Action(() => { label4.Text = updateCount.ToString(); }));
        }
        public void DataStoreWrite(int startAddress , int value)
        {
            //if(this.modbusTcpConnParam.slave == null )
            //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[startAddress + 1] = (ushort)value;
        }
        public List<string> FloatConvertToInt16(BsonValue floatData)
        {
            List<string> dataList = new List<string>();
            try
            {
                ushort[] uintData = new ushort[2];
                float[] floatDataTmp = new float[1] { Convert.ToSingle(floatData) };
                Buffer.BlockCopy(floatDataTmp, 0, uintData, 0, 4);
                for (int index = 0; index < uintData.Length; index++)
                {
                    dataList.Add(uintData[index].ToString());
                }
            }
            catch (Exception e)
            {
                WriteLog("FloatConvertToInt16 exception : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "FloatConvertToInt16 exception : " + e.Message + "\r\n"); }));
            }
            
            return dataList;
        }
        public List<string> Int32ConvertToInt16(BsonValue int32Data)
        {
            List<string> dataList = new List<string>();
            try
            {
                ushort[] uintData = new ushort[2];
                int[] intDataTmp = new int[1] { Convert.ToInt32(int32Data) };
                Buffer.BlockCopy(intDataTmp, 0, uintData, 0, 4);
                for (int index = 0; index < uintData.Length; index++)
                {
                    dataList.Add(uintData[index].ToString());
                }
            }
            catch (Exception e)
            {
                WriteLog("Int32ConvertToInt16 exception : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Int32ConvertToInt16 exception : " + e.Message + "\r\n"); }));
            }
           
            return dataList;
        }
        public int ArrayToInt32(ushort[] arrayData)
        {
            int data = 0;
            try
            {
                short[] array = new short[1];
                Buffer.BlockCopy(arrayData.ToArray(), 0, array, 0, 2);
                data = array[0];
            }
            catch (Exception e)
            {
                WriteLog("ArrayToInt32 exception : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ArrayToInt32 exception : " + e.Message + "\r\n"); }));
            }
            return data;
        }
        public List<string> Int64ConvertToInt16(BsonValue int64Data)
        {
            List<string> dataList = new List<string>();
            ushort[] uintData = new ushort[4];
            ulong[] intDataTmp = new ulong[1] { Convert.ToUInt64(int64Data) };
            Buffer.BlockCopy(intDataTmp, 0, uintData, 0, 8);
            for (int index = 0; index < uintData.Length; index++)
            {
                dataList.Add(uintData[index].ToString());
            }
            return dataList;
        }
        public List<string> ASCII2HexByte(string str)
        {
            List<string> result = new List<string>();

            for (int i = 0; i < str.Length; i++)
            {
                byte tmp = (byte)str[i];
                result.Add(tmp.ToString());
            }
            return result;
        }
        public long DateTimeToTimestamp(string time)
        {
            long timestamp = 0;
            try
            {
                DateTimeOffset ddd = DateTimeOffset.Parse(time);
                timestamp = ddd.ToUnixTimeSeconds(); // 相差秒數
            }
            catch (Exception e)
            {
                WriteLog("ArrayToInt32 exception : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ArrayToInt32 exception : " + e.Message + "\r\n"); }));
            }
            return timestamp;
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

        private void radioButtonNo_CheckedChanged(object sender, EventArgs e)
        {
            textboxSecondary.Enabled = false;
            textboxSecondaryPort.Enabled = false;
            textboxArbiter.Enabled = false;
            textboxArbiterPort.Enabled = false;
            textboxReplicaSet.Enabled = false;
        }

        private void radioButtonYes_CheckedChanged(object sender, EventArgs e)
        {
            textboxSecondary.Enabled = true;
            textboxSecondaryPort.Enabled = true;
            textboxArbiter.Enabled = true;
            textboxArbiterPort.Enabled = true;
            textboxReplicaSet.Enabled = true;
        }
    }
}