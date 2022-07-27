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
        System.Timers.Timer pollingClient = new System.Timers.Timer();
        System.Timers.Timer pollingMongoDBConnect = new System.Timers.Timer();
        System.Timers.Timer updateStatus = new System.Timers.Timer();
        JObject json = new JObject();
        JObject jsonStatus = new JObject();
        List<JToken> mongoMappingList = new List<JToken>();
        List<JToken> statusList = new List<JToken>();
        List<string> statusUpdateList = new List<string>() { "0", "0" };// statusUpdateList[0] : mongodbStatus, statusUpdateList[1] : clientStatus
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
        int queryTimeInterval = 1000;
        int errorCount = 0;
        int updateCount = 0;
        int clientNo = 0;
        int previousClientNo = 0;
        
        
        public Form1()
        {
            InitializeComponent();
            //cpuID = GetCPUID();
            //if (cpuID == "BFEBFBFF000806C1")//BFEBFBFF000806C1(test)   BFEBFBFF000906EA(Modbus Server)   04:42:1A:CB:96:CA(remote)
            //{
            //    InitMappingData();
            //    InitIPAddress();
            //    InitTextbox();
            //    SetPollingClient();
            //    SetPollingMongoDBTimer();
            //    SetPollingMongoDBConnect();
            //    SetUpdateStatusProcess();
            //}
            //else
            //{
            //    throw new Exception("系統異常");
            //}
            InitMappingData();
            InitIPAddress();
            InitTextbox();
            SetPollingClient();
            SetPollingMongoDBDataCollection();
            SetPollingMongoDBConnect();
            SetUpdateStatusProcess();
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
                if(mappingList.Key == "InitStatus")
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
            for (int i = 0; i < this.mongoMappingList.Count(); i++)
            {
                WriteLog("InitMappingData mappingTable資料內容 : " + this.mongoMappingList[i]);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitMappingData mappingTable資料內容 : " + this.mongoMappingList[i]+" \r\n");
            }
            for (int i = 0; i < this.statusList.Count(); i++)
            {
                WriteLog("InitMappingData statusList內容 : " + this.statusList[i]);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitMappingData statusList內容 : " + this.statusList[i] + " \r\n");
            }
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
        private void InitTextbox()
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
                while (true)
                {
                    ClientDetect();
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
                    MongoDBConnect();
                    Thread.Sleep(5000);
                }
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
                    Thread.Sleep(3000);
                }
            });
        }
        private void ProcessStart_Click(object sender, EventArgs e)
        {
            bool configReport;
            configReport = ConfigCheacker();
            if (configReport == true)
            {
                InitMongoDBConnectString();
                if (this.modbusTcpConnParam.tcpListener != null)
                {
                    mongoDBStatus = "unConnect";
                    this.modbusTcpConnParam.slave.Dispose();
                    this.modbusTcpConnParam.tcpListener.Stop();
                    //this.pollingMongoDBConnect.Stop();
                    //this.pollingMongoDBConnect.Stop();
                    //this.pollingClient.Stop();
                    //this.updateStatus.Stop();
                    //this.pollingMongoDB.Stop();
                    this.modbusTcpConnParam.ipAddress = (IPAddress)comboBox1.SelectedItem;
                    InitModbusTcpSlave();
                }
                else
                {
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
                radioButtonYes.Enabled = false;
                radioButtonNo.Enabled = false;
                button2.Enabled = true;
                button1.Enabled = false;
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
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }
        }

        private void Slave_ModbusSlaveRequestReceived(object? sender, ModbusSlaveRequestEventArgs e)
        {

        }
        public void InitStatus()
        {
            this.timerProcessFlag = true;
            foreach (var status in this.statusList)
            {
                switch (status["Status"].ToString())
                {
                    //可在此新增需要監聽的連線狀態
                    case "mongoDB_connect":
                        this.pollingMongoDBConnect.Start();
                        break;
                    case "client_connect":
                        this.pollingClient.Start();
                        break;
                }
            }
            //this.pollingMongoDBConnect.Start();
            //this.pollingClient.Start();


            this.updateStatus.Start();
            this.pollingMongoDB.Start();
        }
        public void InitMongoDBConnectString()
        {
            if (radioButtonNo.Checked == true)
            {
                string DBuser = textboxDBUser.Text.Trim();
                string DBpassword = textboxPassword.Text.Trim();
                string mongoDBPrimary = textboxPrimary.Text.Trim();
                string mongoDBPrimaryPort = textboxPrimaryPort.Text.Trim();
                //this.mongoConnectionString = string.Format("mongodb://{0}:{1}@{2}:{3}/admin?serverSelectionTimeoutMS=5000", DBuser, DBpassword, mongoDBPrimary, mongoDBPrimaryPort);
                this.mongoConnectionString = string.Format("mongodb://{0}:{1}/admin?serverSelectionTimeoutMS=5000",mongoDBPrimary, mongoDBPrimaryPort);
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
        public void MongoDBConnect()
        {
            //try
            //{
            //    this.mongoDBConnParam.connectionString = mongoConnectionString;
            //    this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);

            //    IMongoDatabase db = this.mongoDBConnParam.mongoClient.GetDatabase("admin");
            //    var conn = db.ListCollectionNames();
            //    WriteLog("連線至Mongo");
            //    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[6 + 1] = true;
            //    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[6 + 1] = true;
            //    this.modbusTcpConnParam.slave.DataStore.InputRegisters[6 + 1] = 1;
            //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[6 + 1] = 1;
            //    if (mongoDBStatus == "unConnect")
            //    {
            //        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB 連線狀態點位為 : 1" + "\r\n"); }));
            //        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線成功 " + "\r\n"); }));
            //    }
            //    //textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線成功 " + "\r\n");
            //    mongoDBStatus = "connected";
            //}
            //catch (Exception e)
            //{
            //    errorCount++;
            //    WriteLog("MongoDBConnect  Exception : " + e.Message);
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB 連線狀態點位為 : 0" + "\r\n"); }));
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線失敗 " + "\r\n"); }));
            //    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[6 + 1] = false;
            //    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[6 + 1] = false;
            //    this.modbusTcpConnParam.slave.DataStore.InputRegisters[6 + 1] = 0;
            //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[6 + 1] = 0;
            //    //textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線失敗 : " + "\r\n");
            //    label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            //    mongoDBStatus = "unConnect";
            //}

            if (mongoDBStatus == "unConnect")
            {
                try
                {
                    this.mongoDBConnParam.connectionString = mongoConnectionString;
                    this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);
                    IMongoDatabase db = this.mongoDBConnParam.mongoClient.GetDatabase("admin");
                    //var collections = db.GetCollection<BsonDocument>("system.users");
                    //var filter = Builders<BsonDocument>.Filter.Empty;
                    //var doc  = collections.Find(filter).ToList();
                    var conn = db.ListCollectionNames();

                    WriteLog("連線至Mongo");
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線成功 " + "\r\n"); }));
                    mongoDBStatus = "connected";
                }
                catch (Exception e)
                {
                    errorCount++;
                    WriteLog("MongoDBConnect  Exception : " + e.Message);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB連線失敗 " + "\r\n"); }));
                    label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                }
            }
        }
        public void ClientDetect()
        {
            
            clientNo = ((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters.Count();
            if(this.clientNo != this.previousClientNo && this.clientNo != 0 )
            {
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client 連線數量 : " + this.clientNo.ToString() + "\r\n"); }));
                WriteLog("Client 連線數量 : " + this.clientNo.ToString());
                this.previousClientNo = this.clientNo;
                this.clientDetectFlag = false;
                this.clientStatus = "connected";
            }
            else if(this.clientNo == 0 && this.clientDetectFlag == false)
            {
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client 連線數量 : " + this.clientNo.ToString() + "\r\n"); }));
                WriteLog("Client 連線數量 : " + this.clientNo.ToString());
                this.previousClientNo = this.clientNo;
                this.clientDetectFlag = true;
                this.clientStatus = "unConnect";
            }
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
            // statusUpdateList[0] : mongodbStatus
            // statusUpdateList[1] : clientStatus
            if (this.mongoDBStatus != this.previousMongoDBStatus && this.mongoDBStatus == "connected")
            {
                this.statusUpdateList[0] = "1";
                this.updateStatusFlag = true;
                this.previousMongoDBStatus = this.mongoDBStatus;
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB 連線狀態 : " + this.statusUpdateList[0] + "\r\n"); }));
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
                this.statusUpdateList[1] = "0";
                this.updateStatusFlag = true;
                this.previousClientStatus = this.clientStatus;
                WriteLog("Client  連線狀態為 : " + this.statusUpdateList[1]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client  連線狀態 : " + this.statusUpdateList[1] + "\r\n"); }));
            }
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
                        //this.mongoDBQueryParam.array = (bool)mongoMappingList[i]["Array"];
                        //this.mongoDBQueryParam.arrayLevel = (int)mongoMappingList[i]["ArrayLevel"];
                        //this.mongoDBQueryParam.arrayNum = (int)mongoMappingList[i]["ArrayNumber"];

                        /***
                         * 判斷是不是list
                         *    是  進入QueryDataArray
                         *    否  進入QueryData
                         * 
                         * ***/
                        










                        /***---------------優化部分-----------------
                         * 判斷是不是list
                         *      是  
                         *          判斷 欄位 跟 collection 是不是跟之前一樣
                         *              是  沿用之前的doc
                         *              否  進入QueryDataArray 
                         *              
                         *      否
                         *          判斷 collection 是不是跟之前一樣
                         *              是  沿用之前的doc
                         *              否  進入QueryData
                         * 
                         * 
                         ***/











                        mongoDataListTMP = QueryData(this.mongoDBQueryParam);
                        if(mongoDataListTMP[0] != "mongoStatusUnconnect")
                        {
                            foreach (var mongoData in mongoDataListTMP)
                            {
                                mongoDataList.Add(mongoData);
                                mongoDataTest.Add(mongoData);//test
                                WriteLog("MongoDataCollection mongoData : " + mongoData);
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
                WriteLog("MongoDataCollection mongoMappingList : " + mongoMappingList.Count());
                WriteLog("MongoDataCollection mongoDataList : " + mongoDataList.Count());
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "點位數量 : " + mongoMappingList.Count() + " 資料數量 : " + mongoDataList.Count() + "\r\n"); }));
            }
            catch (Exception e)
            {
                WriteLog("Exception MongoDataCollection : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception MongoDataCollection : " + e.Message + "\r\n"); }));
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
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
            WriteLog("MongoDataCollection : " + mongoDataList.Count());
            mongoDataTest.Clear();
            return mongoDataList ;
        }
        public List<string> QueryDataArray(MongoDBQueryParam mongoDBQueryParam)
        {
            List<string> mongoDataList = new List<string>();
            this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(mongoDBQueryParam.database);
            var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);
            var filter = Builders<BsonDocument>.Filter.Empty;
            //-----------------------------------------------
            var filtera = Builders<BsonDocument>.Filter;
            var sort = Builders<BsonDocument>.Sort;
            try
            {
                var doc = collections.Find(filtera.Empty)//過濾
                                 .Sort(sort.Descending("_id")).Limit(1).ToList().Last();//倒序
                //var docTMP = collections.Find(filter).ToList();
                //docTMP.Sort();
                //var doc = docTMP.Last(); //還是要sort取出最新一筆
                switch (mongoDBQueryParam.type)
                {
                    case "int32":
                    case "uint32":
                        try
                        {
                            if(mongoDBQueryParam.arrayLevel > 0)
                            {
                                WriteLog("QueryData : " + doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayLevel][mongoDBQueryParam.arrayNum].ToString());
                                mongoDataList = Int32ConvertToInt16(doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayLevel][mongoDBQueryParam.arrayNum]);
                            }
                            else
                            {
                                WriteLog("QueryData : " + doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum].ToString());
                                mongoDataList = Int32ConvertToInt16(doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum]);
                            }
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
                            if (mongoDBQueryParam.arrayLevel > 0)
                            {
                                WriteLog("QueryData : " + doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayLevel][mongoDBQueryParam.arrayNum].ToString());
                                mongoDataList = FloatConvertToInt16(doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayLevel][mongoDBQueryParam.arrayNum]);
                            }
                            else
                            {
                                WriteLog("QueryData : " + doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum].ToString());
                                mongoDataList = FloatConvertToInt16(doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum]);
                            }
                            
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
                            if (mongoDBQueryParam.arrayLevel > 0)
                            {
                                WriteLog("QueryData : " + doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayLevel][mongoDBQueryParam.arrayNum].ToString());
                                mongoDataList.Add(doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayLevel][mongoDBQueryParam.arrayNum].ToString());
                            }
                            else
                            {
                                WriteLog("QueryData : " + doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum].ToString());
                                mongoDataList.Add(doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum].ToString());
                            }
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
            }
            catch (Exception e)
            {
                this.mongoDBStatus = "unConnect";
                mongoDataList.Add("mongoStatusUnconnect");
                WriteLog("Exception QueryData : " + e.Message);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            }


            return mongoDataList;
        }
        public List<string> QueryData(MongoDBQueryParam mongoDBQueryParam)
        {
            List<string> mongoDataList = new List<string>();
            this.mongoDBConnParam.mongoDataBase = this.mongoDBConnParam.mongoClient.GetDatabase(mongoDBQueryParam.database);
            var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>(mongoDBQueryParam.collection);
            var filter = Builders<BsonDocument>.Filter.Empty;
            //-----------------------------------------------
            var filtera = Builders<BsonDocument>.Filter;
            var sort = Builders<BsonDocument>.Sort;
            
            //-----------------------------------------------

            try
            {
                var doc = collections.Find(filtera.Empty)//過濾
                                 .Sort(sort.Descending("_id")).Limit(1).ToList().Last();//倒序
                //var docTMP = collections.Find(filter).ToList();
                //docTMP.Sort();
                //var doc = docTMP.Last(); //還是要sort取出最新一筆
                switch (mongoDBQueryParam.type)
                {
                    case "int32":
                    case "uint32":
                        try
                        {
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
            }
            catch (Exception e)
            {
                this.mongoDBStatus = "unConnect";
                mongoDataList.Add("mongoStatusUnconnect");
                WriteLog("Exception QueryData : " + e.Message);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception QueryData : " + e.Message + "\r\n"); }));
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
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
                            if(textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
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
                            
                            if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
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
                                case "uint32":
                                    for (int address = 0; address < floatLength; address++)
                                    {
                                        this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address] = ushort.Parse(writeDataHoldingRegisters[mongoDataCount]) ;
                                        WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));
                                        }
                                        mongoDataCount++;
                                    }
                                    break;
                                default:
                                    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataHoldingRegisters[mongoDataCount]);
                                    WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                    if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                    {
                                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));
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
                                case "uint32":
                                    for (int address = 0; address < floatLength; address++)
                                    {
                                        this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"] + address] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                                        WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + Convert.ToInt16(mongoMappingList[i]["Registers"]) + " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
                                        }
                                        mongoDataCount++;
                                    }
                                    break;
                                default:
                                    this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] = ushort.Parse(writeDataInputRegisters[mongoDataCount]);
                                    WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputRegisters[mongoDataCount]);

                                    if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
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