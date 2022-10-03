namespace modbus_server
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textboxDBUser = new System.Windows.Forms.TextBox();
            this.textboxPassword = new System.Windows.Forms.TextBox();
            this.textboxPrimary = new System.Windows.Forms.TextBox();
            this.textboxSecondary = new System.Windows.Forms.TextBox();
            this.textboxArbiter = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.textboxPrimaryPort = new System.Windows.Forms.TextBox();
            this.textboxSecondaryPort = new System.Windows.Forms.TextBox();
            this.textboxArbiterPort = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.textboxReplicaSet = new System.Windows.Forms.TextBox();
            this.radioButtonYes = new System.Windows.Forms.RadioButton();
            this.radioButtonNo = new System.Windows.Forms.RadioButton();
            this.textboxIntervalTime = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(12, 11);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(476, 420);
            this.textBox1.TabIndex = 0;
            // 
            // comboBox1
            // 
            this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(102, 458);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(160, 27);
            this.comboBox1.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button1.Location = new System.Drawing.Point(310, 545);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 36);
            this.button1.TabIndex = 2;
            this.button1.Text = "開始";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ProcessStart_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoEllipsis = true;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 458);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 19);
            this.label1.TabIndex = 3;
            this.label1.Text = "選擇IP位址";
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(418, 605);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(57, 19);
            this.linkLabel1.TabIndex = 4;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "About..";
            this.linkLabel1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.linkLabel1_MouseClick);
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label6.AutoEllipsis = true;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(34, 506);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 19);
            this.label6.TabIndex = 9;
            this.label6.Text = "EMS IP : ";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label7.AutoEllipsis = true;
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(30, 554);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(72, 19);
            this.label7.TabIndex = 13;
            this.label7.Text = "DBUser : ";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label8.AutoEllipsis = true;
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(14, 596);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(88, 19);
            this.label8.TabIndex = 14;
            this.label8.Text = "Password : ";
            // 
            // textboxDBUser
            // 
            this.textboxDBUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textboxDBUser.Location = new System.Drawing.Point(102, 551);
            this.textboxDBUser.Name = "textboxDBUser";
            this.textboxDBUser.Size = new System.Drawing.Size(160, 27);
            this.textboxDBUser.TabIndex = 15;
            this.textboxDBUser.Text = "root";
            // 
            // textboxPassword
            // 
            this.textboxPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textboxPassword.Location = new System.Drawing.Point(102, 593);
            this.textboxPassword.Name = "textboxPassword";
            this.textboxPassword.Size = new System.Drawing.Size(160, 27);
            this.textboxPassword.TabIndex = 16;
            this.textboxPassword.Text = "pc152";
            this.textboxPassword.UseSystemPasswordChar = true;
            // 
            // textboxPrimary
            // 
            this.textboxPrimary.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textboxPrimary.Location = new System.Drawing.Point(102, 503);
            this.textboxPrimary.Name = "textboxPrimary";
            this.textboxPrimary.Size = new System.Drawing.Size(160, 27);
            this.textboxPrimary.TabIndex = 17;
            this.textboxPrimary.Text = "192.168.101.201";
            // 
            // textboxSecondary
            // 
            this.textboxSecondary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textboxSecondary.Location = new System.Drawing.Point(112, 233);
            this.textboxSecondary.Name = "textboxSecondary";
            this.textboxSecondary.Size = new System.Drawing.Size(160, 27);
            this.textboxSecondary.TabIndex = 18;
            this.textboxSecondary.Visible = false;
            // 
            // textboxArbiter
            // 
            this.textboxArbiter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textboxArbiter.Location = new System.Drawing.Point(112, 260);
            this.textboxArbiter.Name = "textboxArbiter";
            this.textboxArbiter.Size = new System.Drawing.Size(160, 27);
            this.textboxArbiter.TabIndex = 19;
            this.textboxArbiter.Visible = false;
            // 
            // label9
            // 
            this.label9.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label9.AutoEllipsis = true;
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(44, 233);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(68, 19);
            this.label9.TabIndex = 20;
            this.label9.Text = "EMS IP : ";
            this.label9.Visible = false;
            // 
            // label10
            // 
            this.label10.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label10.AutoEllipsis = true;
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(44, 263);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(68, 19);
            this.label10.TabIndex = 21;
            this.label10.Text = "EMS IP : ";
            this.label10.Visible = false;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label11.AutoEllipsis = true;
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(285, 506);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(38, 19);
            this.label11.TabIndex = 22;
            this.label11.Text = "Port";
            // 
            // label12
            // 
            this.label12.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label12.AutoEllipsis = true;
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(278, 233);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(38, 19);
            this.label12.TabIndex = 23;
            this.label12.Text = "Port";
            this.label12.Visible = false;
            // 
            // label13
            // 
            this.label13.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label13.AutoEllipsis = true;
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(278, 266);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(38, 19);
            this.label13.TabIndex = 24;
            this.label13.Text = "Port";
            this.label13.Visible = false;
            // 
            // textboxPrimaryPort
            // 
            this.textboxPrimaryPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textboxPrimaryPort.Location = new System.Drawing.Point(329, 503);
            this.textboxPrimaryPort.Name = "textboxPrimaryPort";
            this.textboxPrimaryPort.Size = new System.Drawing.Size(71, 27);
            this.textboxPrimaryPort.TabIndex = 25;
            this.textboxPrimaryPort.Text = "27017";
            // 
            // textboxSecondaryPort
            // 
            this.textboxSecondaryPort.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textboxSecondaryPort.Location = new System.Drawing.Point(322, 230);
            this.textboxSecondaryPort.Name = "textboxSecondaryPort";
            this.textboxSecondaryPort.Size = new System.Drawing.Size(71, 27);
            this.textboxSecondaryPort.TabIndex = 26;
            this.textboxSecondaryPort.Visible = false;
            // 
            // textboxArbiterPort
            // 
            this.textboxArbiterPort.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textboxArbiterPort.Location = new System.Drawing.Point(322, 263);
            this.textboxArbiterPort.Name = "textboxArbiterPort";
            this.textboxArbiterPort.Size = new System.Drawing.Size(71, 27);
            this.textboxArbiterPort.TabIndex = 27;
            this.textboxArbiterPort.Visible = false;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(310, 588);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(90, 36);
            this.button2.TabIndex = 28;
            this.button2.Text = "變更參數";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label16
            // 
            this.label16.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label16.AutoEllipsis = true;
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(278, 319);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(134, 19);
            this.label16.TabIndex = 32;
            this.label16.Text = "ReplicaSetName : ";
            this.label16.Visible = false;
            // 
            // textboxReplicaSet
            // 
            this.textboxReplicaSet.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textboxReplicaSet.Location = new System.Drawing.Point(297, 341);
            this.textboxReplicaSet.Name = "textboxReplicaSet";
            this.textboxReplicaSet.Size = new System.Drawing.Size(98, 27);
            this.textboxReplicaSet.TabIndex = 31;
            this.textboxReplicaSet.Visible = false;
            // 
            // radioButtonYes
            // 
            this.radioButtonYes.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.radioButtonYes.AutoSize = true;
            this.radioButtonYes.Location = new System.Drawing.Point(6, 22);
            this.radioButtonYes.Name = "radioButtonYes";
            this.radioButtonYes.Size = new System.Drawing.Size(54, 23);
            this.radioButtonYes.TabIndex = 34;
            this.radioButtonYes.Text = "Yes";
            this.radioButtonYes.UseVisualStyleBackColor = true;
            this.radioButtonYes.CheckedChanged += new System.EventHandler(this.radioButtonYes_CheckedChanged);
            // 
            // radioButtonNo
            // 
            this.radioButtonNo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.radioButtonNo.AutoSize = true;
            this.radioButtonNo.Checked = true;
            this.radioButtonNo.Location = new System.Drawing.Point(96, 20);
            this.radioButtonNo.Name = "radioButtonNo";
            this.radioButtonNo.Size = new System.Drawing.Size(51, 23);
            this.radioButtonNo.TabIndex = 35;
            this.radioButtonNo.TabStop = true;
            this.radioButtonNo.Text = "No";
            this.radioButtonNo.UseVisualStyleBackColor = true;
            this.radioButtonNo.CheckedChanged += new System.EventHandler(this.radioButtonNo_CheckedChanged);
            // 
            // textboxIntervalTime
            // 
            this.textboxIntervalTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textboxIntervalTime.Location = new System.Drawing.Point(392, 463);
            this.textboxIntervalTime.Name = "textboxIntervalTime";
            this.textboxIntervalTime.Size = new System.Drawing.Size(71, 27);
            this.textboxIntervalTime.TabIndex = 37;
            this.textboxIntervalTime.Text = "10";
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label18.AutoEllipsis = true;
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(285, 466);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(110, 19);
            this.label18.TabIndex = 36;
            this.label18.Text = "讀取時間間隔 : ";
            // 
            // label19
            // 
            this.label19.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label19.AutoEllipsis = true;
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(469, 466);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(24, 19);
            this.label19.TabIndex = 38;
            this.label19.Text = "秒";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.groupBox2.Controls.Add(this.radioButton1);
            this.groupBox2.Controls.Add(this.radioButton2);
            this.groupBox2.Controls.Add(this.radioButtonYes);
            this.groupBox2.Controls.Add(this.radioButtonNo);
            this.groupBox2.Location = new System.Drawing.Point(44, 359);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(181, 49);
            this.groupBox2.TabIndex = 43;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Replication";
            this.groupBox2.Visible = false;
            // 
            // radioButton1
            // 
            this.radioButton1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(96, -28);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(66, 23);
            this.radioButton1.TabIndex = 41;
            this.radioButton1.Text = "Local";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(6, -28);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(84, 23);
            this.radioButton2.TabIndex = 40;
            this.radioButton2.Text = "Remote";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(500, 646);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.textboxIntervalTime);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.textboxReplicaSet);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textboxArbiterPort);
            this.Controls.Add(this.textboxSecondaryPort);
            this.Controls.Add(this.textboxPrimaryPort);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.textboxArbiter);
            this.Controls.Add(this.textboxSecondary);
            this.Controls.Add(this.textboxPrimary);
            this.Controls.Add(this.textboxPassword);
            this.Controls.Add(this.textboxDBUser);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.textBox1);
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Modbus Server";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextBox textBox1;
        private ComboBox comboBox1;
        private Button button1;
        private Label label1;
        private LinkLabel linkLabel1;
        private Label label6;
        private Label label7;
        private Label label8;
        private TextBox textboxDBUser;
        private TextBox textboxPassword;
        private TextBox textboxPrimary;
        private TextBox textboxSecondary;
        private TextBox textboxArbiter;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label label12;
        private Label label13;
        private TextBox textboxPrimaryPort;
        private TextBox textboxSecondaryPort;
        private TextBox textboxArbiterPort;
        private Button button2;
        private Label label16;
        private TextBox textboxReplicaSet;
        private RadioButton radioButtonYes;
        private RadioButton radioButtonNo;
        private TextBox textboxIntervalTime;
        private Label label18;
        private Label label19;
        private GroupBox groupBox2;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
    }
}