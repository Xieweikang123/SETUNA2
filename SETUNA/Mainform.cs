﻿namespace SETUNA
{
    using com.clearunit;
    using SETUNA.Main;
    using SETUNA.Main.KeyItems;
    using SETUNA.Main.Option;
    using SETUNA.Main.Other;
    using SETUNA.Main.Style;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Resources;
    using System.Runtime.CompilerServices;
    using System.Windows.Forms;
    using System.Xml.Serialization;

    public sealed class Mainform : Form, IScrapKeyPressEventListener, IScrapAddedListener, IScrapRemovedListener, IScrapStyleListener, IScrapMenuListener, ISingletonForm
    {
        private Queue<ScrapSource> _imgpool;
        private bool _iscapture = false;
        private bool _isoption = false;
        private bool _isstart = false;
        private static CaptureForm cap_form;
        private IContainer components;
        public Queue<ScrapBase> dustbox;
        private ClickCapture frmClickCapture;
        private SplashForm frmSplash;
        public KeyItemBook keyBook;
        public SetunaOption optSetuna;
        public ScrapBook scrapBook;
        private NotifyIcon setunaIcon;
        private SETUNA.Main.ContextStyleMenuStrip setunaIconMenu;
        private SETUNA.Main.ContextStyleMenuStrip subMenu;
        private ToolStripMenuItem testToolStripMenuItem;
        private Timer timPool;
        private Button button4;
        private Button btnCapture;
        private ToolTip toolTip1;

        public Mainform()
        {
            this.InitializeComponent();
            this.scrapBook = new ScrapBook(this);
            this.scrapBook.addKeyPressListener(this);
            this.scrapBook.addScrapAddedListener(this);
            this.scrapBook.addScrapRemovedListener(this);
            this.optSetuna = new SetunaOption();
            this.dustbox = new Queue<ScrapBase>();
            this.scrapBook.DustBox = this.dustbox;
            this.scrapBook.DustBoxCapacity = 5;
            this.keyBook = this.optSetuna.GetKeyItemBook();
            this._imgpool = new Queue<ScrapSource>();
            this.SetSubMenu();
            this.Width = 300;
            this.Height = 100;
        }

        public void AddImageList(ScrapSource src)
        {
            this._imgpool.Enqueue(src);
            this.timPool.Start();
        }
        /// <summary>
        /// 开始截取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCapture_Click(object sender, EventArgs e)
        {
            this.StartCapture();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GC.Collect();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Option();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.optSetuna = SetunaOption.GetDefaultOption();
            int num = 0;
            num++;
        }

        private void button7_Click(object sender, EventArgs e)
        {
        }

        private void CloseSetuna()
        {
            base.Close();
        }

        void ISingletonForm.DetectExternalStartup(string version, string[] args)
        {
            base.Invoke(new ExternalStartupDelegate(this.ExternalStartup), new object[] { version, args });
        }

        private void CommandCutRect(Rectangle rect, string fname)
        {
            using (Bitmap bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb))
            {
                Point location = new Point(rect.X, rect.Y);
                CaptureForm.CopyFromScreen(bitmap, location);
                if (fname == "")
                {
                    this.AddImageList(new ScrapSourceImage(bitmap, location));
                }
            }
        }

        public void CommandRun(string[] args)
        {
            Console.WriteLine("-命令行参数--------------------");
            int num = 0;
            Rectangle rect = new Rectangle(0, 0, 0, 0);
            string fname = "";
            foreach (string str4 in args)
            {
                try
                {
                    string path = str4;
                    string str = "";
                    if (str4.Length > 3)
                    {
                        str = path.Substring(0, 3);
                        if ((str.Substring(0, 1) == "/") && (str.Substring(2, 1) == ":"))
                        {
                            path = str4.Substring(str.Length, path.Length - str.Length);
                        }
                        else
                        {
                            str = "";
                        }
                    }
                    if (str.Length > 0)
                    {
                        if (str == "/R:")
                        {
                            string[] strArray = path.Split(new char[] { ',' });
                            if (strArray.Length == 4)
                            {
                                rect = new Rectangle
                                {
                                    X = int.Parse(strArray[0]),
                                    Y = int.Parse(strArray[1]),
                                    Width = int.Parse(strArray[2]),
                                    Height = int.Parse(strArray[3])
                                };
                                Console.WriteLine("[位置]" + rect.ToString());
                                continue;
                            }
                        }
                        if (str == "/P:")
                        {
                            fname = path;
                        }
                        if (str == "/C:")
                        {
                            if (path.ToUpper() == "OPTION")
                            {
                                num = 1;
                                continue;
                            }
                            if (path.ToUpper() == "CAPTURE")
                            {
                                num = 2;
                                continue;
                            }
                            if (path.ToUpper() == "SUBMENU")
                            {
                                num = 3;
                                continue;
                            }
                        }
                    }
                    this.AddImageList(new ScrapSourcePath(path));
                    Console.WriteLine(path);
                }
                catch
                {
                    Console.WriteLine("[Error]" + str4);
                }
            }
            Console.WriteLine("---------------------------------------");
            if ((rect.Width >= 10) && (rect.Height >= 10))
            {
                this.CommandCutRect(rect, fname);
            }
            else if ((num != 0) && this.IsStart)
            {
                switch (num)
                {
                    case 1:
                        if (this.IsOption)
                        {
                            break;
                        }
                        this.Option();
                        return;

                    case 2:
                        if (!this.IsCapture)
                        {
                            this.StartCapture();
                        }
                        break;

                    default:
                        return;
                }
            }
        }

        public void CreateScrapFromImage(Image image, Point location)
        {
            if (image != null)
            {
                using (Bitmap bitmap = (Bitmap)image.Clone())
                {
                    if (location == Point.Empty)
                    {
                        location = Cursor.Position;
                    }
                    int x = location.X;
                    int y = location.Y;
                    this.scrapBook.AddScrap((Bitmap)bitmap.Clone(), x, y, bitmap.Width, bitmap.Height);
                }
            }
        }

        private void CreateScrapFromsource(ScrapSource src)
        {
            this.CreateScrapFromImage(src.GetImage(), src.GetPosition());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void EndCapture(CaptureForm cform)
        {
            try
            {
                Console.WriteLine("Mainform EndCapture Start---");
                if (cform.DialogResult == DialogResult.OK)
                {
                    using (Bitmap bitmap = cform.ClipBitmap)
                    {
                        if (bitmap != null)
                        {
                            this.scrapBook.AddScrap(bitmap, cform.ClipStart.X, cform.ClipStart.Y, cform.ClipSize.Width, cform.ClipSize.Height);
                        }
                    }
                }
                cform.Hide();
                Cursor.Clip = Rectangle.Empty;
                Console.WriteLine("Mainform EndCapture End---");
            }
            catch (Exception exception)
            {
                Console.WriteLine("MainForm EndCapture Exception:" + exception.Message);
            }
            finally
            {
                this.IsCapture = false;
                if (this.frmClickCapture != null)
                {
                    this.frmClickCapture.Restart();
                }
            }
        }

        private void ExternalStartup(string version, string[] args)
        {
            if (Application.ProductVersion != version)
            {
                MessageBox.Show("SETUNA已经运行在不同的版本。", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            else if (args.Length > 0)
            {
                this.CommandRun(args);
            }
            else if (this.optSetuna.Setuna.DupType == SetunaOption.SetunaOptionData.OpeningType.Capture)
            {
                this.StartCapture();
            }
        }

        private void frmClickCapture_ClickCaptureEvent(object sender, EventArgs e)
        {
            this.StartCapture();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Mainform));
            this.timPool = new System.Windows.Forms.Timer(this.components);
            this.setunaIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.setunaIconMenu = new SETUNA.Main.ContextStyleMenuStrip(this.components);
            this.subMenu = new SETUNA.Main.ContextStyleMenuStrip(this.components);
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.button4 = new System.Windows.Forms.Button();
            this.btnCapture = new System.Windows.Forms.Button();
            this.subMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // timPool
            // 
            this.timPool.Tick += new System.EventHandler(this.timPool_Tick);
            // 
            // setunaIcon
            // 
            this.setunaIcon.ContextMenuStrip = this.setunaIconMenu;
            this.setunaIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("setunaIcon.Icon")));
            this.setunaIcon.Text = "SETUNA2";
            this.setunaIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.setunaIcon_MouseClick);
            // 
            // setunaIconMenu
            // 
            this.setunaIconMenu.ImageScalingSize = new System.Drawing.Size(36, 36);
            this.setunaIconMenu.Name = "setunaIconMenu";
            this.setunaIconMenu.Scrap = null;
            this.setunaIconMenu.Size = new System.Drawing.Size(61, 4);
            this.setunaIconMenu.Opening += new System.ComponentModel.CancelEventHandler(this.setunaIconMenu_Opening);
            // 
            // subMenu
            // 
            this.subMenu.ImageScalingSize = new System.Drawing.Size(36, 36);
            this.subMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testToolStripMenuItem});
            this.subMenu.Name = "subMenu";
            this.subMenu.Scrap = null;
            this.subMenu.Size = new System.Drawing.Size(107, 28);
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(106, 24);
            this.testToolStripMenuItem.Text = "test";
            // 
            // toolTip1
            // 
            this.toolTip1.IsBalloon = true;
            this.toolTip1.ShowAlways = true;
            this.toolTip1.StripAmpersands = true;
            this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTip1.ToolTipTitle = "asfdadsf";
            // 
            // button4
            // 
            this.button4.Dock = System.Windows.Forms.DockStyle.Right;
            this.button4.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.button4.ForeColor = System.Drawing.Color.Gray;
            this.button4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button4.Location = new System.Drawing.Point(378, 0);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(154, 153);
            this.button4.TabIndex = 1;
            this.button4.Text = "选项";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // btnCapture
            // 
            this.btnCapture.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCapture.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.btnCapture.ForeColor = System.Drawing.Color.Gray;
            this.btnCapture.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnCapture.Location = new System.Drawing.Point(0, 0);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(378, 153);
            this.btnCapture.TabIndex = 0;
            this.btnCapture.Text = "截取";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // Mainform
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(532, 153);
            this.ContextMenuStrip = this.setunaIconMenu;
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.button4);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(550, 200);
            this.Name = "Mainform";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SETUNA2";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Mainform_FormClosing);
            this.Load += new System.EventHandler(this.Mainform_Load);
            this.Shown += new System.EventHandler(this.Mainform_Shown);
            this.subMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void LoadOption()
        {
            string configFile = SetunaOption.ConfigFile;
            try
            {
                if (!File.Exists(configFile))
                {
                    this.optSetuna = SetunaOption.GetDefaultOption();
                }
                else
                {
                    Type[] allType = SetunaOption.GetAllType();
                    XmlSerializer serializer = new XmlSerializer(typeof(SetunaOption), allType);
                    FileStream stream = new FileStream(configFile, FileMode.Open);
                    this.optSetuna = (SetunaOption)serializer.Deserialize(stream);
                    stream.Close();
                }
            }
            catch
            {
                this.optSetuna = SetunaOption.GetDefaultOption();
                MessageBox.Show("无法读取配置文件。\n使用默认设置。", "SETUNA2", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            finally
            {
                this.OptionApply();
            }
        }

        private void Mainform_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.optSetuna.UnregistHotKey();
        }

        private void Mainform_Load(object sender, EventArgs e)
        {
            base.Visible = false;
            this.LoadOption();
            this.OptionApply();
            this.SaveOption();
            this.InitData();
            if (this.optSetuna.Setuna.ShowSplashWindow)
            {
                this.frmSplash = new SplashForm();
                base.AddOwnedForm(this.frmSplash);
                this.frmSplash.Show(this);
                this.frmSplash.SplashTimer.Start();
            }
            this.timPool.Start();
            cap_form = new CaptureForm(this.optSetuna.Setuna);
            this.IsStart = true;
        }

        public void InitData()
        {
            CacheManager.Init(info =>
            {
                this.scrapBook.AddScrap(info, true);
            });

            DPIUtils.Init(this);
        }

        private void Mainform_Shown(object sender, EventArgs e)
        {
        }

        private void miCapture_Click(object sender, EventArgs e)
        {
            this.StartCapture();
        }

        private void miOption_Click(object sender, EventArgs e)
        {
            this.Option();
        }

        private void miSetunaClose_Click(object sender, EventArgs e)
        {
            this.CloseSetuna();
        }

        public void OnActiveScrapInList(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            if (item.Tag != null)
            {
                ((ScrapBase)item.Tag).Activate();
            }
        }

        public void Option()
        {
            if (!this.IsCapture)
            {
                this.IsOption = true;
                SetunaOption opt = (SetunaOption)this.optSetuna.Clone();
                List<ScrapBase> list = new List<ScrapBase>();
                try
                {
                    foreach (ScrapBase base2 in this.scrapBook)
                    {
                        if (base2.Visible && base2.TopMost)
                        {
                            list.Add(base2);
                        }
                    }
                    foreach (ScrapBase base3 in list)
                    {
                        base3.TopMost = false;
                    }
                    base.TopMost = false;
                    this.optSetuna.UnregistHotKey();
                    if (this.frmClickCapture != null)
                    {
                        this.frmClickCapture.Stop();
                    }
                    OptionForm form = new OptionForm(opt)
                    {
                        StartPosition = FormStartPosition.CenterScreen,
                    };
                    form.ShowDialog();
                    if (form.DialogResult == DialogResult.OK)
                    {
                        this.optSetuna = form.Option;
                        this.OptionApply();
                    }
                    if (!this.optSetuna.RegistHotKey(base.Handle))
                    {
                        this.optSetuna.ScrapHotKeyEnable = false;
                        new HotkeyMsg { HotKey = (Keys)this.optSetuna.ScrapHotKey }.ShowDialog();
                    }
                    if (form.DialogResult == DialogResult.OK)
                    {
                        this.SaveOption();
                    }
                }
                finally
                {
                    base.TopMost = true;
                    foreach (ScrapBase base4 in list)
                    {
                        base4.TopMost = true;
                    }
                    this.IsOption = false;
                }
            }
        }

        private void OptionApply()
        {
            try
            {
                this.keyBook = this.optSetuna.GetKeyItemBook();
                if (this.optSetuna.Setuna.DustBoxEnable)
                {
                    this.scrapBook.DustBoxCapacity = (short)this.optSetuna.Setuna.DustBoxCapacity;
                }
                else
                {
                    this.scrapBook.DustBoxCapacity = 0;
                }
                if (!this.optSetuna.RegistHotKey(base.Handle))
                {
                    this.optSetuna.ScrapHotKeyEnable = false;
                    new HotkeyMsg { HotKey = (Keys)this.optSetuna.ScrapHotKey }.ShowDialog();
                }
                if (this.optSetuna.Setuna.AppType == SetunaOption.SetunaOptionData.ApplicationType.ApplicationMode)
                {
                    base.ShowInTaskbar = true;
                    this.setunaIcon.Visible = false;
                    base.MinimizeBox = true;
                    base.Visible = true;
                }
                else
                {
                    this.setunaIcon.Visible = true;
                    base.ShowInTaskbar = false;
                    base.MinimizeBox = false;
                    base.WindowState = FormWindowState.Normal;
                    base.Visible = this.optSetuna.Setuna.ShowMainWindow;
                }
                this.subMenu.Items.Clear();
                foreach (int num in this.optSetuna.Scrap.SubMenuStyles)
                {
                    if (num >= 0)
                    {
                        foreach (CStyle style in this.optSetuna.Styles)
                        {
                            if (style.StyleID == num)
                            {
                                this.subMenu.Items.Add(style.GetToolStrip(this.scrapBook));
                            }
                        }
                    }
                    else
                    {
                        CStyle preStyle = CPreStyles.GetPreStyle(num);
                        if (preStyle != null)
                        {
                            this.subMenu.Items.Add(preStyle.GetToolStrip(this.scrapBook));
                        }
                    }
                }
                if (this.optSetuna.Setuna.ClickCapture)
                {
                    if (this.frmClickCapture == null)
                    {
                        this.frmClickCapture = new ClickCapture(this.optSetuna.Setuna.ClickCaptureValue);
                        this.frmClickCapture.ClickCaptureEvent += new ClickCapture.ClipCaptureDelegate(this.frmClickCapture_ClickCaptureEvent);
                        this.frmClickCapture.Show();
                    }
                    else
                    {
                        this.frmClickCapture.ClickFlags = this.optSetuna.Setuna.ClickCaptureValue;
                        this.frmClickCapture.Restart();
                    }
                }
                else if (this.frmClickCapture != null)
                {
                    this.frmClickCapture.Close();
                    this.frmClickCapture.Dispose();
                    this.frmClickCapture = null;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Mainform OptionApply Exception:" + exception.Message);
            }
        }

        public void RestoreScrap(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            List<ScrapBase> list = new List<ScrapBase>();
            while (this.dustbox.Count > 0)
            {
                ScrapBase base2 = this.dustbox.Dequeue();
                if (!base2.Equals(item.Tag))
                {
                    list.Add(base2);
                }
                else
                {
                    this.scrapBook.AddScrap(base2);
                    base2.Show();
                }
            }
            this.dustbox.Clear();
            foreach (ScrapBase base3 in list)
            {
                this.dustbox.Enqueue(base3);
            }
            new ScrapEventArgs();
        }

        private void SaveOption()
        {
            string configFile = SetunaOption.ConfigFile;
            System.Type[] allType = SetunaOption.GetAllType();
            try
            {
                XmlSerializer serializer = new XmlSerializer(this.optSetuna.GetType(), allType);
                FileStream stream = new FileStream(configFile, FileMode.Create);
                serializer.Serialize((Stream)stream, this.optSetuna);
                stream.Close();
            }
            catch
            {
                MessageBox.Show("无法保存配置文件。", "SETUNA2", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        public void ScrapActivated(object sender, ScrapEventArgs e)
        {
            bool inactiveAlphaChange = this.optSetuna.Scrap.InactiveAlphaChange;
            bool inactiveLineChange = this.optSetuna.Scrap.InactiveLineChange;
        }

        public void ScrapAdded(object sender, ScrapEventArgs e)
        {
        }

        public void ScrapCreated(object sender, ScrapEventArgs e)
        {
            CStyle style = this.optSetuna.FindStyle(this.optSetuna.Scrap.CreateStyleID);
            if (style != null)
            {
                e.scrap.Initialized = false;
                style.Apply(ref e.scrap);
            }
            else
            {
                e.scrap.Initialized = true;
            }
        }

        public void ScrapInactived(object sender, ScrapEventArgs e)
        {
            if (this.optSetuna.Scrap.InactiveAlphaChange)
            {
                e.scrap.InactiveOpacity = 1.0 - (((double)this.optSetuna.Scrap.InactiveAlphaValue) / 100.0);
            }
            else
            {
                e.scrap.InactiveOpacity = e.scrap.ActiveOpacity;
            }
            bool inactiveLineChange = this.optSetuna.Scrap.InactiveLineChange;
        }

        public void ScrapInactiveMouseOut(object sender, ScrapEventArgs e)
        {
            if (this.optSetuna.Scrap.InactiveAlphaChange)
            {
                e.scrap.InactiveOpacity = 1.0 - (((double)this.optSetuna.Scrap.InactiveAlphaValue) / 100.0);
            }
            else
            {
                e.scrap.InactiveOpacity = e.scrap.ActiveOpacity;
            }
            bool inactiveLineChange = this.optSetuna.Scrap.InactiveLineChange;
        }

        public void ScrapInactiveMouseOver(object sender, ScrapEventArgs e)
        {
            if (this.optSetuna.Scrap.MouseOverAlphaChange)
            {
                e.scrap.RollOverOpacity = 1.0 - (((double)this.optSetuna.Scrap.MouseOverAlphaValue) / 100.0);
            }
            else
            {
                e.scrap.RollOverOpacity = e.scrap.ActiveOpacity;
            }
            bool mouseOverLineChange = this.optSetuna.Scrap.MouseOverLineChange;
        }

        public void ScrapKeyPress(object sender, ScrapKeyPressEventArgs e)
        {
            KeyItem item = this.keyBook.FindKeyItem(e.key);
            if (item != null)
            {
                ScrapBase scrap = (ScrapBase)sender;
                item.ParentStyle.Apply(ref scrap);
            }
        }

        public void ScrapMenuOpening(object sender, ScrapMenuArgs e)
        {
            this.subMenu.Scrap = e.scrap;
            this.subMenu.Show(e.scrap, e.scrap.PointToClient(Cursor.Position));
        }

        public void ScrapRemoved(object sender, ScrapEventArgs e)
        {
        }

        private void SetSubMenu()
        {
            this.setunaIconMenu.Scrap = this.scrapBook.GetDummyScrap();
            this.setunaIconMenu.Items.Clear();
            this.setunaIconMenu.Items.Add(new CCleanCacheStyle().GetToolStrip(this.scrapBook));
            this.setunaIconMenu.Items.Add(new CScrapListStyle().GetToolStrip(this.scrapBook));
            this.setunaIconMenu.Items.Add(new CDustBoxStyle().GetToolStrip(this.scrapBook));
            this.setunaIconMenu.Items.Add(new CDustEraseStyle().GetToolStrip());
            this.setunaIconMenu.Items.Add(new ToolStripSeparator());
            this.setunaIconMenu.Items.Add(new CCaptureStyle().GetToolStrip());
            this.setunaIconMenu.Items.Add(new ToolStripSeparator());
            this.setunaIconMenu.Items.Add(new CShowVersionStyle().GetToolStrip());
            this.setunaIconMenu.Items.Add(new COptionStyle().GetToolStrip());
            this.setunaIconMenu.Items.Add(new ToolStripSeparator());
            this.setunaIconMenu.Items.Add(new CShutDownStyle().GetToolStrip());
        }

        private void setunaIcon_MouseClick(object sender, MouseEventArgs e)
        {
            base.Activate();
        }

        private void setunaIconMenu_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = false;
        }

        public void StartCapture()
        {
            if ((!this.IsCapture && (cap_form != null)) && !this.IsOption)
            {
                try
                {
                    if (this.frmClickCapture != null)
                    {
                        this.frmClickCapture.Stop();
                    }
                    this.IsCapture = true;
                    Console.WriteLine(string.Concat(new object[] { "9 - ", DateTime.Now.ToString(), " ", DateTime.Now.Millisecond }));
                    cap_form.OnCaptureClose = new CaptureForm.CaptureClosedDelegate(this.EndCapture);
                    cap_form.ShowCapture(this.optSetuna.Setuna);
                    Console.WriteLine(string.Concat(new object[] { "16 - ", DateTime.Now.ToString(), " ", DateTime.Now.Millisecond }));
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Mainform StartCapture Exception:" + exception.Message);
                    this.IsCapture = false;
                    if (cap_form != null)
                    {
                        cap_form.DialogResult = DialogResult.Cancel;
                    }
                    this.EndCapture(cap_form);
                }
            }
        }

        private void timPool_Tick(object sender, EventArgs e)
        {
            if (((this._imgpool.Count == 0) || this.IsCapture) || (this.IsOption || !this.IsStart))
            {
                this.timPool.Stop();
            }
            else
            {
                using (ScrapSource source = this._imgpool.Dequeue())
                {
                    this.CreateScrapFromsource(source);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if ((m.Msg == 0x312) && (((int)m.WParam) == 1))
            {
                this.StartCapture();
            }
        }

        public bool IsCapture
        {
            get
            {
                return  this._iscapture;
            }
            set
            {
                this._iscapture = value;
                if (!value && (this._imgpool.Count > 0))
                {
                    this.timPool.Start();
                }
            }
        }

        public bool IsOption
        {
            get
            {
                return
                this._isoption;
            }
            set
            {
                this._isoption = value;
                if (!value && (this._imgpool.Count > 0))
                {
                    this.timPool.Start();
                }
            }
        }

        public bool IsStart
        {
            get
            {
                return
                this._isstart;
            }
            set
            {
                this._isstart = value;
                if (value && (this._imgpool.Count > 0))
                {
                    this.timPool.Start();
                }
            }
        }

        private delegate void ExternalStartupDelegate(string version, string[] args);

  
    }
}

