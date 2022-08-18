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
        System.Timers.Timer scadaHeartbeat = new System.Timers.Timer();
        JObject json = new JObject();
        JObject jsonStatus = new JObject();
        List<JToken> mongoMappingList = new List<JToken>();
        List<JToken> statusList = new List<JToken>();

        //����令����init�ƶq�ӰʺA�s�Wlist���ƶq
        List<string> statusUpdateList = new List<string>() { "0", "0", "0", "0" };// statusUpdateList[0] : mongodbStatus 0�_�u 1�s�u , statusUpdateList[1] : clientStatus 0�_�u 1�s�u ,statusUpdateList[2] : modbus server mode  0��� 1���� ,statusUpdateList[3] : pExecuteStatus
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

        enum Mode : int
        {
            Local = 0,
            Remote = 1
        }
        enum Status : int
        {
            unSuccess = 0,
            success = 1,
            noExecute = 2
        }
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
            //    throw new Exception("�t�β��`");
            //}
            InitMappingData();
            InitIPAddress();
            InitRadioButton();
            SetPollingClient();
            SetPollingMongoDBDataCollection();
            SetPollingMongoDBConnect();
            SetPollingScadaConnect();
            SetUpdateStatusProcess();
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
                //WriteLog("GetMacAddress : " + mac);//����n�R
                return mac;
            }
            catch (Exception e)
            {
                //WriteLog("GetMacAddress Excption : " + e.Message);//����n�R
                return "unknow";
            }
        }
        public string GetCPUID()
        {
            try
            {
                //���CPUID
                string mac = "";
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    mac = mo["ProcessorId"].ToString();
                }
                moc = null;
                mc = null;
                //WriteLog("GetCPUID : " + mac);//����n�R
                return mac;
            }
            catch (Exception e)
            {
                //WriteLog("GetCPUID Excption : " + e.Message);//����n�R
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
            for (int i = 0; i < this.mongoMappingList.Count(); i++)
            {
                WriteLog("InitMappingData mappingTable��Ƥ��e : " + this.mongoMappingList[i]);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitMappingData mappingTable��Ƥ��e : " + this.mongoMappingList[i] + " \r\n");
            }
            for (int i = 0; i < this.statusList.Count(); i++)
            {
                WriteLog("InitMappingData statusList���e : " + this.statusList[i]);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "InitMappingData statusList���e : " + this.statusList[i] + " \r\n");
            }
            //-------------------------------------------------------------
            WriteLog("InitMappingData mappingTable��Ƶ��� : " + this.mongoMappingList.Count());
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
            if (radioButtonLocal.Checked == true)
            {
                this.modbusServerMode = (int)Mode.Local;
            }
            else if (radioButtonRemote.Checked == true)
            {
                this.modbusServerMode = (int)Mode.Remote;
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
                        //���XMongoDB�����
                        writeDataTemp = MongoDataCollection(this.mongoMappingList);
                        //�N��Ƽg�JModbus DataStore
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
        public void SetPollingScadaConnect()
        {
            /***
             * �C��֥[scadaHeartbeatTime�A�Y�W�L6��S�Q�k0�A�NmodbusServerMode�אּ��ݼҦ�
             * 
             ***/
            this.scadaHeartbeat.Interval = 1000;
            this.scadaHeartbeat.Enabled = false;
            this.scadaHeartbeat.AutoReset = true;
            this.scadaHeartbeat.Elapsed += new ElapsedEventHandler((x, y) =>
            {
                this.scadaHeartbeatTime++;
                ScadaHeartBeatDetect(this.scadaHeartbeatTime);
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
                if (this.modbusTcpConnParam.tcpListener != null)
                {
                    mongoDBStatus = "unConnect";
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
                WriteLog("Ū���ɶ����j : " + textboxIntervalTime.Text + "��");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Ū���ɶ����j : " + textboxIntervalTime.Text + "�� \r\n");
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
                MessageBox.Show("�п��Modbus Server IP");
            }
            else if (string.IsNullOrEmpty(textboxPrimary.Text))
            {
                checkReport = false;
                MessageBox.Show("�п�JEMS IP");
            }
            else if (string.IsNullOrEmpty(textboxPrimaryPort.Text))
            {
                checkReport = false;
                MessageBox.Show("�п�JPort");
            }
            else if (Convert.ToDecimal(textboxIntervalTime.Text.Trim()) < 1)
            {
                checkReport = false;
                MessageBox.Show("�̧C��Ƭ�1��");
            }
            else if (string.IsNullOrEmpty(textboxSecondary.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("�п�JEMS IP");
            }
            else if (string.IsNullOrEmpty(textboxSecondaryPort.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("�п�JPort");
            }
            else if (string.IsNullOrEmpty(textboxArbiter.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("�п�JEMS IP");
            }
            else if (string.IsNullOrEmpty(textboxArbiterPort.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("�п�JPort");
            }
            else if (string.IsNullOrEmpty(textboxReplicaSet.Text) && radioButtonYes.Checked == true)
            {
                checkReport = false;
                MessageBox.Show("�п�JReplicaSetName");
            }
            return checkReport;
        }
        public void InitModbusTcpSlave()
        {
            try
            {
                this.modbusTcpConnParam.tcpListener = new TcpListener(this.modbusTcpConnParam.ipAddress, this.modbusTcpConnParam.port);
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "�s�uIP : " + this.modbusTcpConnParam.ipAddress + "\r\n");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Port : " + this.modbusTcpConnParam.port + "\r\n");
                this.modbusTcpConnParam.tcpListener.Start();
                this.modbusTcpConnParam.slave = ModbusTcpSlave.CreateTcp(this.modbusTcpConnParam.slaveID, this.modbusTcpConnParam.tcpListener);
                this.modbusTcpConnParam.slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                this.modbusTcpConnParam.slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
                this.modbusTcpConnParam.slave.DataStore.DataStoreWrittenTo += DataStore_DataStoreWrittenTo;
                this.modbusTcpConnParam.slave.Listen();
                WriteLog("Modbus Server �w�}��");
                textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Modbus Server �w�}�� \r\n");



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
        public void InitStatus()
        {
            this.timerProcessFlag = true;
            //foreach (var status in this.statusList)
            //{
            //    switch (status["Status"].ToString())
            //    {
            //        //�i�b���s�W�ݭn��ť���s�u���A
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
            this.scadaHeartbeat.Start();
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
                this.mongoConnectionString = string.Format("mongodb://{0}:{1}/admin?serverSelectionTimeoutMS=5000", mongoDBPrimary, mongoDBPrimaryPort);
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
        
        public void MongoDBConnect()
        {
            //try
            //{
            //    this.mongoDBConnParam.connectionString = mongoConnectionString;
            //    this.mongoDBConnParam.mongoClient = new MongoClient(this.mongoDBConnParam.connectionString);

            //    IMongoDatabase db = this.mongoDBConnParam.mongoClient.GetDatabase("admin");
            //    var conn = db.ListCollectionNames();
            //    WriteLog("�s�u��Mongo");
            //    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[6 + 1] = true;
            //    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[6 + 1] = true;
            //    this.modbusTcpConnParam.slave.DataStore.InputRegisters[6 + 1] = 1;
            //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[6 + 1] = 1;
            //    if (mongoDBStatus == "unConnect")
            //    {
            //        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB �s�u���A�I�쬰 : 1" + "\r\n"); }));
            //        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB�s�u���\ " + "\r\n"); }));
            //    }
            //    //textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB�s�u���\ " + "\r\n");
            //    mongoDBStatus = "connected";
            //}
            //catch (Exception e)
            //{
            //    errorCount++;
            //    WriteLog("MongoDBConnect  Exception : " + e.Message);
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB �s�u���A�I�쬰 : 0" + "\r\n"); }));
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB�s�u���� " + "\r\n"); }));
            //    this.modbusTcpConnParam.slave.DataStore.CoilDiscretes[6 + 1] = false;
            //    this.modbusTcpConnParam.slave.DataStore.InputDiscretes[6 + 1] = false;
            //    this.modbusTcpConnParam.slave.DataStore.InputRegisters[6 + 1] = 0;
            //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[6 + 1] = 0;
            //    //textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB�s�u���� : " + "\r\n");
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

                    WriteLog("�s�u��Mongo");
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB�s�u���\ " + "\r\n"); }));
                    mongoDBStatus = "connected";
                }
                catch (Exception e)
                {
                    errorCount++;
                    WriteLog("MongoDBConnect  Exception : " + e.Message);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB�s�u���� " + "\r\n"); }));
                    label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
                }
            }
        }
        public void ClientDetect()
        {
            clientNo = ((Modbus.Device.ModbusTcpSlave)modbusTcpConnParam.slave).Masters.Count();
            if (this.clientNo != this.previousClientNo && this.clientNo != 0)
            {
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client �s�u�ƶq : " + this.clientNo.ToString() + "\r\n"); }));
                WriteLog("Client �s�u�ƶq : " + this.clientNo.ToString());
                this.previousClientNo = this.clientNo;
                this.clientDetectFlag = false;
                this.clientStatus = "connected";
            }
            else if (this.clientNo == 0 && this.clientDetectFlag == false)
            {
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client �s�u�ƶq : " + this.clientNo.ToString() + "\r\n"); }));
                WriteLog("Client �s�u�ƶq : " + this.clientNo.ToString());
                this.previousClientNo = this.clientNo;
                this.clientDetectFlag = true;
                this.clientStatus = "unConnect";
            }
        }
        public void ScadaHeartBeatDetect(int scadaHeartbeatTime)
        {
            if (scadaHeartbeatTime >= 6 && this.scadaHeartbeatFlag == false)
            {
                this.modbusServerMode = (int)Mode.Local;
                //this.scadaMode = (int)Mode.Local;
                this.previousScadaMode = 2;//�D�n���m���e�����A
                WriteLog("Scada�_�u : modbusServerMode = " + (int)Mode.Local);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Scada�_�u�AModbusServer��������ݼҦ�" + "\r\n"); }));
                textBox1.Invoke(new Action(() => { radioButtonLocal.Checked = true; ; }));
                this.scadaHeartbeatFlag = true;
            }






            //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ScadaHeartBeatDetect" + "\r\n"); }));
            //if (this.scadaHeartbeatCount != this.previousScadaHeartbeatCount)
            //{
            //    this.scadaHeartbeatFlag = false;
            //}
            //else if (this.scadaHeartbeatCount == this.previousScadaHeartbeatCount && this.scadaHeartbeatFlag == false)
            //{
            //    this.modbusServerMode = (int)Mode.Local;
            //    //this.scadaMode = (int)Mode.Local;
            //    this.previousScadaMode = 2;//�D�n���m���e�����A
            //    WriteLog("Scada�_�u : modbusServerMode = " + (int)Mode.Local);
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Scada�_�u�AModbusServer��������ݼҦ�" + "\r\n"); }));
            //    textBox1.Invoke(new Action(() => { radioButtonLocal.Checked = true; ; }));
            //    this.scadaHeartbeatFlag = true;
            //}

            //this.previousScadaHeartbeatCount = this.scadaHeartbeatCount;
        }
        private void DataStore_DataStoreWrittenTo(object? sender, Modbus.Data.DataStoreEventArgs e)
        {
            /***
             * �P�_�n�g�J�����I��
             *      �g�Jscada mode�I��
             *          �P�_���ؼҦ�
             *              0���
             *              1����
             *          scadaHeartbeatTime �k0
             *      �g�J���\�I��B modbus mode�����ݼҦ�
             *          �P�_scada mode���A�O�_��1�B�P�_�O�_����̤j�̤p��
             *              �O  �g�JEMS�A�P�_�O�_�g�J���\
             *                  �O  ���檬�p�I�쬰1
             *                  �_  ���檬�p�I�쬰0
             *              �_  �g�JLOG�A���檬�p��2
             *      �g�J���\�I��B modbus mode����ݼҦ�
             *          ���檬�p��2
             *          
             *          
             *      
             *          �ˬd�O�_���W�L�̤j�̤p��
             *              �O  ���檬�p�I��אּ0(������)
             *              �_  �P�_scada mode���A�O�_��1
             *                      �O �g�JEMS,�P�_�O�_�g���\
             *                           ���\ ���檬�p�I�쬰1
             *                           ���� ���檬�p�I�쬰0
             *                      �_ �g�JLog,�g�����檬�p�I�쬰2
             ***/

            int pValue = 0;
            BsonDocument doc;
            if (e.StartAddress == 2)
            {
                if (e.Data.B[0] == 0 && e.Data.B[0] != this.previousScadaMode)
                {
                    this.scadaMode = (int)Mode.Local;
                    WriteLog("ScadaMode : ��ݼҦ� " + this.scadaMode);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ScadaMode : ��ݼҦ� " + this.scadaMode + "\r\n"); }));
                }
                else if (e.Data.B[0] == 1 && e.Data.B[0] != this.previousScadaMode)
                {
                    this.scadaMode = (int)Mode.Remote;
                    WriteLog("ScadaMode : ���ݼҦ� " + this.scadaMode);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ScadaMode : ���ݼҦ� " + this.scadaMode + "\r\n"); }));
                }
                this.previousScadaMode = this.scadaMode;
                this.scadaHeartbeatTime = 0;
                this.scadaHeartbeatFlag = false;
            }
            else if (e.StartAddress == 5 && radioButtonRemote.Checked == true)
            {
                pValue = ArrayToInt32(e.Data.B.ToArray());
                if (this.scadaMode == (int)Mode.Remote && pValue <= this.pMax && pValue >= this.pMin)
                {
                    try
                    {
                        //�g�Jmongo 
                        var collections = this.mongoDBConnParam.mongoDataBase.GetCollection<BsonDocument>("site_control");
                        doc = SiteControlCollection(pValue);
                        //collections.InsertOneAsync(doc);
                        //���檬�p��1
                        this.pExecuteStatus = (int)Status.success;
                        WriteLog("PValue Status : success " + doc.ToString());
                        WriteLog("PValue Status : success " + this.pExecuteStatus);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : success " + "\r\n"); }));
                    }
                    catch (Exception ee)
                    {
                        //�g�J���� ���檬�p��0
                        this.pExecuteStatus = (int)Status.unSuccess;
                        WriteLog("PValue Status : unSuccess : �g�JEMS���� " + this.pExecuteStatus);
                        textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess : �g�JEMS���� " + "\r\n"); }));
                    }
                }
                else if (this.scadaMode == (int)Mode.Local)
                {
                    //scada mode ����ݼҦ�
                    this.pExecuteStatus = (int)Status.noExecute;
                    WriteLog("PValue Status : noExecute : scada mode����ݼҦ� " + this.pExecuteStatus);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : noExecute : scada mode����ݼҦ� " + "\r\n"); }));
                }
                else if (pValue > this.pMax || pValue < this.pMin)
                {
                    //�\�v�ȶW�L�W�U��
                    this.pExecuteStatus = (int)Status.noExecute;
                    WriteLog("PValue Status : noExecute : �\�v�ȶW�L�W�U�� " + this.pExecuteStatus);
                    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : noExecute : �\�v�ȶW�L�W�U�� " + "\r\n"); }));
                }
            }
            else if (e.StartAddress == 5 && radioButtonLocal.Checked == true)
            {
                pValue = ArrayToInt32(e.Data.B.ToArray());
                doc = SiteControlCollection(pValue);
                //modbusServerMode ����ݼҦ�
                //�g�JLOG
                //���檬�p��2
                this.pExecuteStatus = (int)Status.noExecute;
                WriteLog("PValue Status : noExecute " + doc.ToString());
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : noExecute " + "\r\n"); }));
            }
        }
        public BsonDocument SiteControlCollection(int pValue)
        {
           
            BsonDocument doc = new BsonDocument {
                {"ID","schedule" },
                {"time",DateTime.Now},
                {"start",DateTime.Now },
                {"end",DateTime.Now },
                {"right","USER"},
                {"eventOrder",1},
                {"soc_max" , pValue},
                {"soc_min" , 15},
                {"System_p_max" , 1800},
                {"System_p_min" , -1800},
                {"System_q_max" , 300},
                {"System_q_min" , -300},
                {"mode" , 11},
                {"f1_line_set" , 59.5},
                {"f2_line_set" , 59.75},
                {"f3_line_set" , 59.98},
                {"f4_line_set" , 60.02},
                {"f5_line_set" , 60.25},
                {"f6_line_set" , 60.5},
                {"p1_line_set" , 100},
                {"p2_line_set" , 48},
                {"p3_line_set" , 0},
                {"p4_line_set" , 0},
                {"p5_line_set" , -48},
                {"p6_line_set" , -100},
                {"FP_line_p_base" , 1800},
                {"FP_type" , 0},
                {"FP_soc_goal_percent" , 50},
                {"FP_main_f_set" , 60},
                {"Vq_v1_set" , 95},
                {"Vq_v2_set" , 98},
                {"Vq_v3_set" , 103},
                {"Vq_v4_set" , 105},
                {"Vq_v5_set" , 102},
                {"Vq_v6_set" , 97},
                {"Vq_v7_set" , 100},
                {"Vq_q1_set" , 100},
                {"Vq_q2_set" , 80},
                {"Vq_q3_set" , -80},
                {"Vq_q4_set" , -100},
                {"Vq_q5_set" , -80},
                {"Vq_q6_set" , 80},
                {"Vq_q7_set" , 100},
                {"Vq_q_base" , 300},
                {"Vq_v_base" , 11.4},
                {"show", 1 }
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
            // ���A�I�쪺��ﳣ�ѳo���
            // statusUpdateList[0] : mongodbStatus
            // statusUpdateList[1] : clientStatus
            // statusUpdateList[2] : modbusServerMode
            // statusUpdateList[3] : pStatus
            if (this.mongoDBStatus != this.previousMongoDBStatus && this.mongoDBStatus == "connected")
            {
                this.statusUpdateList[0] = "1";
                this.updateStatusFlag = true;
                this.previousMongoDBStatus = this.mongoDBStatus;
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB �s�u���A : " + this.statusUpdateList[0] + "\r\n"); }));
                WriteLog("MongoDB �s�u���A�� : " + this.statusUpdateList[0]);
            }
            else if (this.mongoDBStatus != this.previousMongoDBStatus && this.mongoDBStatus == "unConnect")
            {
                this.statusUpdateList[0] = "0";
                this.updateStatusFlag = true;
                this.previousMongoDBStatus = this.mongoDBStatus;
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "MongoDB �s�u���A : " + this.statusUpdateList[0] + "\r\n"); }));
                WriteLog("MongoDB �s�u���A�� : " + this.statusUpdateList[0]);
            }
            
            if(this.clientStatus != this.previousClientStatus && this.clientStatus == "connected")
            {
                this.statusUpdateList[1] = "1";
                this.updateStatusFlag = true;
                this.previousClientStatus = this.clientStatus;
                WriteLog("Client  �s�u���A�� : " + this.statusUpdateList[1]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client  �s�u���A : " + this.statusUpdateList[1] + "\r\n"); }));
            }
            else if(this.clientStatus != this.previousClientStatus && this.clientStatus == "unConnect")
            {
                this.statusUpdateList[1] = "0";
                this.updateStatusFlag = true;
                this.previousClientStatus = this.clientStatus;
                WriteLog("Client  �s�u���A�� : " + this.statusUpdateList[1]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Client  �s�u���A : " + this.statusUpdateList[1] + "\r\n"); }));
            }
            
            if(this.modbusServerMode == (int)Mode.Local && this.modbusServerMode != this.previousModbusServerMode)
            {
                this.statusUpdateList[2] = "0";
                this.updateStatusFlag = true;
                this.previousModbusServerMode = this.modbusServerMode;
                WriteLog("ModbusServer Mode : ��ݼҦ� " + this.statusUpdateList[2]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ModbusServer Mode : ��ݼҦ�" + "\r\n"); }));
            }
            else if(this.modbusServerMode == (int)Mode.Remote && this.modbusServerMode != this.previousModbusServerMode)
            {
                this.statusUpdateList[2] = "1";
                this.updateStatusFlag = true;
                this.previousModbusServerMode = this.modbusServerMode;
                WriteLog("ModbusServer Mode : ���ݼҦ� " + this.statusUpdateList[2]);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "ModbusServer Mode : ���ݼҦ�" + "\r\n"); }));
            }

            if (this.pExecuteStatus == (int)Status.success && this.pExecuteStatus != this.previousPExecuteStatus)
            {
                this.statusUpdateList[3] = "1";
                this.updateStatusFlag = true;
                this.previousPExecuteStatus = this.pExecuteStatus;
                //WriteLog("PValue Status : success " + this.statusUpdateList[3]);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : success " + "\r\n"); }));
            }
            else if (this.pExecuteStatus == (int)Status.unSuccess && this.pExecuteStatus != this.previousPExecuteStatus)
            {
                this.statusUpdateList[3] = "0";
                this.updateStatusFlag = true;
                this.previousPExecuteStatus = this.pExecuteStatus;
                //WriteLog("PValue Status : unSuccess " + this.statusUpdateList[3]);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : unSuccess " + "\r\n"); }));
            }
            else if (this.pExecuteStatus == (int)Status.noExecute && this.pExecuteStatus != this.previousPExecuteStatus)
            {
                this.statusUpdateList[3] = "2";
                this.updateStatusFlag = true;
                this.previousPExecuteStatus = this.pExecuteStatus;
                //WriteLog("PValue Status : noExecute " + this.statusUpdateList[3]);
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "PValue Status : noExecute " + "\r\n"); }));
            }

            if(this.updateStatusFlag == true)
            {
                DataStoreWrite(this.statusList, this.statusUpdateList);
                this.updateStatusFlag = false;
                this.textBoxMessageStatusCloseFlag = true;
            }
            //����mongodbStatus �r��
                //�򤧫e���A�ۦP  ���L
                //�򤧫e���A���P  ��slist  ���}��sflag
            //����clientStatus �r��
                //�򤧫e���A�ۦP  ���L
                //�򤧫e���A���P  ��slist  ���}��sflag
            //������sflag
                //true   �ק�modbus�I��  flag�אּfalse
                //false  ���L
        }

        public List<string> MongoDataCollection(List<JToken> mongoMappingList)
        {
            string previousCollection = string.Empty;
            string previousField = string.Empty;
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

                        
                        

                        if (this.mongoDBQueryParam.collection != previousCollection && this.mongoDBQueryParam.field != previousField)
                        {
                            document = Query(this.mongoDBQueryParam);
                            previousField = this.mongoDBQueryParam.field;
                            previousCollection = this.mongoDBQueryParam.collection;
                        }
                        mongoDataListTMP = QueryDataExtrating(this.mongoDBQueryParam, document);

                        if (mongoDataListTMP[0] != "mongoStatusUnconnect")
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
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "�I��ƶq : " + mongoMappingList.Count() + " ��Ƽƶq : " + mongoDataList.Count() + "\r\n"); }));
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
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "��Ƶ��Ʀ��~" + "\r\n"); }));
            //    WriteLog("��Ƶ��Ʀ��~");
            //}
            //else if (!mongoDataList.SequenceEqual(testSample))
            //{
            //    errorCount++;
            //    label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
            //    textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "��Ƥ��e���~" + "\r\n"); }));
            //    WriteLog("��Ƥ��e���~");
            //}
            //---------------------------------------------------------------------
            WriteLog("MongoDataCollection : " + mongoDataList.Count());
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
                doc = collections.Find(filter.Empty).Sort(sort.Descending("_id")).Limit(1).ToList().Last();//�˧�
                WriteLog("Query : " + doc);
            }
            catch (Exception e)
            {
                WriteLog("Exception Query : " + e.Message);
                textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Exception Query : " + e.Message + "\r\n"); }));
                errorCount++;
                label3.Invoke(new Action(() => { label3.Text = errorCount.ToString(); }));
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
                    //�t�~�Q�h�h����
                }
                else if (mongoDBQueryParam.array == true)
                {
                    mongoValue = doc[mongoDBQueryParam.field][mongoDBQueryParam.arrayNum];
                }
                else if (mongoDBQueryParam.array == false && mongoDBQueryParam.field == "time")
                {
                    //�[�J�ɶ��W�B�zFunction
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
                            WriteLog("QueryData : " + mongoValue.ToString());
                            mongoDataList = Int32ConvertToInt16(mongoValue);
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
                            WriteLog("QueryData : " + mongoValue.ToString());
                            mongoDataList = FloatConvertToInt16(mongoValue);
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
                            WriteLog("QueryData : " + mongoValue.ToString());
                            mongoDataList.Add(mongoValue.ToString());
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
                                        WriteLog("Read Holding Registers : " + this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[(int)mongoMappingList[i]["Registers"] + address] + " = " + writeDataHoldingRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Holding Registers : " + (Convert.ToInt16(mongoMappingList[i]["Registers"])+ address) + " = " + writeDataHoldingRegisters[mongoDataCount] + "\r\n"); }));
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
                                        WriteLog("Read Input Registers : " + this.modbusTcpConnParam.slave.DataStore.InputRegisters[(int)mongoMappingList[i]["Registers"]] + " = " + writeDataInputRegisters[mongoDataCount]);
                                        if (textBoxMessageCloseFlag == false || textBoxMessageStatusCloseFlag == false)
                                        {
                                            textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Read Input Registers : " + (Convert.ToInt16(mongoMappingList[i]["Registers"]) + address) +  " = " + writeDataInputRegisters[mongoDataCount] + "\r\n"); }));
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
        public void DataStoreWrite(int startAddress , int value)
        {
            //if(this.modbusTcpConnParam.slave == null )
            //    this.modbusTcpConnParam.slave.DataStore.HoldingRegisters[startAddress + 1] = (ushort)value;
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

        public int ArrayToInt32(ushort[] arrayData)
        {
            int data = 0;
            short[] array = new short[1];
            Buffer.BlockCopy(arrayData.ToArray(), 0, array, 0, 2);
            data = array[0];
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
            DateTimeOffset ddd = DateTimeOffset.Parse(time);
            timestamp = ddd.ToUnixTimeSeconds(); // �ۮt���
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

        private void radioButtonLocal_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButtonLocal.Checked == true)
            {
                this.modbusServerMode = (int)Mode.Local;
                //WriteLog("Modbus Server Mode : ��ݼҦ�");
                //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Modbus Server Mode : ��ݼҦ�" + "\r\n"); }));
            }
        }

        private void radioButtonRemote_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonRemote.Checked == true)
            {
                this.modbusServerMode = (int)Mode.Remote;
            }
            //this.modbusServerMode = 1;
            //DataStoreWrite(startAddress, modeValue);
            //WriteLog("Modbus Server Mode : ���ݼҦ�");
            //textBox1.Invoke(new Action(() => { textBox1.AppendText(DateTime.Now.ToString("T") + "   " + "Modbus Server Mode : ���ݼҦ�" + "\r\n"); }));

        }

    }
}