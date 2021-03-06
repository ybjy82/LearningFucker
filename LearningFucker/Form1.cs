﻿using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LearningFucker
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public Form1()        {
            
            InitializeComponent();
            layoutLogin.Dock = DockStyle.Fill;
            layoutTask.Dock = DockStyle.Fill;
            Worker = new Worker();


            Worker.TaskRefresed += new Action<Worker>(s => {
                if(InvokeRequired)
                {
                    this.Invoke(Worker.TaskRefresed, s);
                    return;
                }
                gridControl1.RefreshDataSource();
                this.barTodyIntegral.Caption = Worker.UserStatistics.TodayIntegral.ToString();
                this.barWeekIntegral.Caption = Worker.UserStatistics.WeekIntegral.ToString();
                this.barSummaryIntegral.Caption = Worker.UserStatistics.SumIntegral.ToString();
                this.barIntegralRank.Caption = Worker.UserStatistics.IntegralRanking;
            });
            Worker.OnSaying = new Action<object, string>((sender, Text) =>
            {
                if (InvokeRequired)
                {
                    this.Invoke(Worker.OnSaying, sender, Text);
                    return;
                }
                barStatusText.Caption = Text;
            });

            Worker.OnReportingError = new Action<object, string>((sender, Text) =>
            {
                if(InvokeRequired)
                {
                    this.Invoke(Worker.OnReportingError, sender, Text);
                    return;
                }
                barListItem1.Strings.Add(Text);
                barListItem1.Caption = "错误: " + barListItem1.Strings.Count;
            });
            Config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if(Config.GetSection("UserCredential") == null )
            {
                Config.Sections.Add("UserCredential", new UserSection());
            }
            ReadPassword();
            var image = imageCollection1.Images["learn32.png"];
            this.Icon = Icon.FromHandle(((Bitmap)image).GetHicon());
        }

        private const string KEY = "jcflRWUJqAs=";
        private const string IV = "hBoIpG2rhqE=";
        private bool running = false;


        public Worker Worker { get; set; }
        Configuration Config { get; set; }

        private void BarBtnLogin_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            layoutLogin.Visible = true;
            layoutTask.Visible = false;
            Worker.StopWork();
            
        }

        private async void SimpleButton11_Click(object sender, EventArgs e)
        {
            dxError.ClearErrors();
            var userId = textEdit11.Text;
            var password = textEdit2.Text;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {

            }
            else
            {
                userId = userId.Trim();
                password = password.Trim();
                var login = await Worker.Login(userId, password);
                if (login)
                {
                    if (checkEdit1.Checked)
                        SavePassword(userId, password);

                    layoutLogin.Visible = false;
                    layoutTask.Visible = true;

                    await Worker.Init();

                    this.barTodyIntegral.Caption = Worker.UserStatistics.TodayIntegral.ToString();
                    this.barWeekIntegral.Caption = Worker.UserStatistics.WeekIntegral.ToString();
                    this.barSummaryIntegral.Caption = Worker.UserStatistics.SumIntegral.ToString();
                    this.barIntegralRank.Caption = Worker.UserStatistics.IntegralRanking;

                    //var taskList = new List<int>();
                    //taskList.Add(14);
                    //Worker.StartWork(taskList, false);
                }
                else
                {
                    dxError.SetError(textEdit11, "用户名密码不正确");
                    barStatusText.Caption = "用户名密码不正确, 登陆失败!";
                }
            }
        }

        private void SavePassword(string userId, string password)
        {
            SymmetricAlgorithm sa = DES.Create();
            sa.Key = Convert.FromBase64String(KEY);
            sa.IV = Convert.FromBase64String(IV);
            byte[] content = Encoding.UTF8.GetBytes(password);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, sa.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(content, 0, content.Length);
            cs.FlushFinalBlock();
            var cryPassword = Convert.ToBase64String(ms.ToArray());

            if(Config.AppSettings.Settings.AllKeys.Contains("UserId"))
            {
                Config.AppSettings.Settings["UserId"].Value = userId;
            }
            else
                Config.AppSettings.Settings.Add("UserId", userId);


            if (Config.AppSettings.Settings.AllKeys.Contains("Password"))
            {
                Config.AppSettings.Settings["Password"].Value = cryPassword;
            }
            else
                Config.AppSettings.Settings.Add("Password", cryPassword);

            var userSection = Config.GetSection("UserCredential") as UserSection;
            if (userSection.Users.Contain(userId))
                userSection.Users.GetUser(userId).Password = cryPassword;
            else
                userSection.Users.Add(userId, cryPassword);

            Config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            ConfigurationManager.RefreshSection("UserCredential");
            
            
        }

        private void ReadPassword()
        {
            if (Config.AppSettings.Settings["UserId"] == null)
            {
                textEdit11.Text = "";
            }
            else
                textEdit11.Text = Config.AppSettings.Settings["UserId"].Value;

            if (Config.AppSettings.Settings["Password"] == null)
            {
                textEdit2.Text = "";
            }
            else
            {
                SymmetricAlgorithm sa = DES.Create();
                sa.Key = Convert.FromBase64String(KEY);
                sa.IV = Convert.FromBase64String(IV);
                var cryPassword = Config.AppSettings.Settings["Password"].Value;

                byte[] content = Convert.FromBase64String(cryPassword);
                var ms = new MemoryStream();
                var cs = new CryptoStream(ms, sa.CreateDecryptor(), CryptoStreamMode.Write);
                
                cs.Write(content, 0, content.Length);
                cs.FlushFinalBlock();
                textEdit2.Text = Encoding.UTF8.GetString(ms.ToArray());
            }

            var userSection = Config.GetSection("UserCredential") as UserSection;
            foreach (var item in userSection.Users.AllKeys)
            {
                textEdit11.Properties.Items.Add(item);
            } 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.bindingSource1.DataSource = Worker.TaskList;

            repositoryItemImageComboBox1.Items.Add("", LearningFucker.Handler.TaskStatus.Initial, 8);
            repositoryItemImageComboBox1.Items.Add("", LearningFucker.Handler.TaskStatus.Completed, 3);
            repositoryItemImageComboBox1.Items.Add("", LearningFucker.Handler.TaskStatus.Stopped, 2);
            repositoryItemImageComboBox1.Items.Add("", LearningFucker.Handler.TaskStatus.Stopping, 2);
            repositoryItemImageComboBox1.Items.Add("", LearningFucker.Handler.TaskStatus.Working, 6);

            this.progressBar.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;

            Worker.WorkStarted += Worker_WorkStarted;
            Worker.WorkStopped += Worker_WorkStopped;
        }

        private void Worker_WorkStopped(object sender, EventArgs e)
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action(() => Worker_WorkStopped(sender, e)));
                return;
            }

            this.progressBar.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            barButtonItem3.Enabled = true;
            barButtonItem4.Enabled = false;
            running = false;
            gridView1.RefreshData();
            Worker.Refresh();
        }

        private void Worker_WorkStarted(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Worker_WorkStarted(sender, e)));
                return;
            }

            this.progressBar.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;
            barButtonItem3.Enabled = false;
            barButtonItem4.Enabled = true;
            running = true;
            gridView1.RefreshData();
            Worker.Refresh();
        }

        private void GridView1_CustomRowCellEditForEditing(object sender, CustomRowCellEditEventArgs e)
        {
            if (e.Column.FieldName != "IsSelect") return;
            var grid = sender as GridView;
            var row = grid.GetRow(e.RowHandle) as LearningFucker.Models.Task;
#if !DEBUG
            if (running)
            {
                e.RepositoryItem.ReadOnly = true;
            }
            else
            {
                if (row.LimitIntegral <= row.Integral || row.UncompeletedItemCount == 0)
                {
                    e.RepositoryItem.ReadOnly = true;
                }
                else
                {
                    e.RepositoryItem.ReadOnly = false;
                }
            }
#endif
        }

        private void BarButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Worker.Refresh();
        }

        private void BarButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            
            var selectedTask = Worker.TaskList.Where(s => s.IsSelect);
            gridView1.PostEditor();
            
            if(selectedTask.Count() == 0)
            {
                XtraMessageBox.Show("请选择要刷分的任务!", "请注意!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {             
                
                List<int> tasks = new List<int>();
                foreach (var item in selectedTask)
                {
                    tasks.Add(item.TaskType);
                }

                Worker.StartWork(tasks, false);
            }
        }







        private void barButtonItem4_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Worker.StopWork();
        }

        private void textEdit11_Properties_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                textEdit2.Focus();
        }

        private void textEdit2_Properties_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                SimpleButton11_Click(sender, new EventArgs());
        }

        private void textEdit11_Properties_SelectedValueChanged(object sender, EventArgs e)
        {
            var userSection = Config.GetSection("UserCredential") as UserSection;
            var User = userSection.Users.GetUser(textEdit11.SelectedItem.ToString());
            if (User == null)
                return;

            SymmetricAlgorithm sa = DES.Create();
            sa.Key = Convert.FromBase64String(KEY);
            sa.IV = Convert.FromBase64String(IV);
            var cryPassword = User.Password;

            byte[] content = Convert.FromBase64String(cryPassword);
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, sa.CreateDecryptor(), CryptoStreamMode.Write);

            cs.Write(content, 0, content.Length);
            cs.FlushFinalBlock();
            textEdit2.Text = Encoding.UTF8.GetString(ms.ToArray());
        }
    }

    

    
}
