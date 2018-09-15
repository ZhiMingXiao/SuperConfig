﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace exporter
{
    public partial class Form1 : Form
    {
        public Form1(string[] args)
        {
            Console.WriteLine("初始化路径...");
            LoadPaths();

            Console.WriteLine("初始化缓存...");
            List<string> argslist = new List<string>(args);
            int labelindex = 0;
            foreach (string arg in argslist)
                if (arg.StartsWith("label-"))
                    labelindex = int.Parse(arg.Substring(6));
            Cache.Init(labellist[labelindex], argslist.Contains("divfloder") ? labelNames[labelindex].Split(':')[0] : "", argslist.Contains("cache"));

            if (argslist.Contains("nowindow"))
            {
                Console.WriteLine("启动导表程序...");
                if (Export())
                {
                    Console.WriteLine("Complete");
                    Environment.Exit(0);
                }
            }
            else
            {
                InitializeComponent();
                cacheTog.Checked = Cache.enable;
                foreach (string ln in labelNames)
                    labelSelect.Items.Add(ln);
                labelSelect.SelectedIndex = 0;
            }
        }

        string[] paths = new string[4];
        List<List<string>> labellist = new List<List<string>>();
        List<string> labelNames = new List<string>();
        string pathConfigFile = "pathconfig";
        void LoadPaths()
        {
            pathConfigFile = new FileInfo(Application.ExecutablePath).Directory.FullName + Path.DirectorySeparatorChar + pathConfigFile;
            if (File.Exists(pathConfigFile))
                paths = File.ReadAllLines(pathConfigFile);

            string labelcfg = new FileInfo(Application.ExecutablePath).Directory.FullName + Path.DirectorySeparatorChar + "labels";
            string[] arr;
            if (File.Exists(labelcfg))
            {
                arr = File.ReadAllLines(labelcfg);
            }
            else
            {
                arr = new string[] { "default" };
                File.WriteAllLines(labelcfg, arr);
            }

            for (int i = 0; i < arr.Length; i++)
            {
                labelNames.Add(arr[i]);
                string[] ls = arr[i].Split(':');
                if (ls.Length == 2)
                    labellist.Add(new List<string>(ls[1].Split(',')));
                else
                    labellist.Add(new List<string>());
            }
        }

        bool Export()
        {
            try
            {
                DateTime start = DateTime.Now;
                List<string> readfiles;
                CustomWorkbook.Init(paths[0], out readfiles);
                if (readfiles.Count < 10)
                    readfiles.ForEach(Console.WriteLine);
                Console.WriteLine("读入" + readfiles.Count + "张表," + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "秒");

                start = DateTime.Now;
                if (!CheckError(Exporter.ReadDataXlsx())) return false;
                Console.WriteLine("读取xlsx, " + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "秒");

                start = DateTime.Now;
                if (!CheckError(Exporter.ReadFormulaXlsx(Exporter.DealWithFormulaSheetLua))) return false;
                Console.WriteLine("lua公式, " + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "秒");

                start = DateTime.Now;
                if (!CheckError(Exporter.ExportLua(paths[1]))) return false;
                Console.WriteLine("导出lua文件," + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "秒");

                start = DateTime.Now;
                if (!CheckError(Exporter.ReadFormulaXlsx(Exporter.DealWithFormulaSheetGo))) return false;
                Console.WriteLine("go公式," + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "秒");

                start = DateTime.Now;
                if (!CheckError(Exporter.ExportGo(paths[2], paths[3]))) return false;
                Console.WriteLine("导出go文件," + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "秒");

                Cache.SaveCache();
                Console.WriteLine("存储缓存");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return false;
        }

        List<Label> labels = new List<Label>();
        FolderBrowserDialog folderBrowserDialog;

        private void Form1_Load(object sender, EventArgs e)
        {
            labels.AddRange(new Label[] { label1, label2, label3, label4 });

            for (int index = 0; index < paths.Length; index++)
            {
                if (Directory.Exists(paths[index]))
                    labels[index].Text = paths[index];
                else
                    paths[index] = string.Empty;
            }

            folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择路径";
            folderBrowserDialog.ShowNewFolderButton = true;
        }

        private void SelectDir(int index)
        {
            Label label = labels[index];
            if (Directory.Exists(label.Text))
                folderBrowserDialog.SelectedPath = label.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string dir = folderBrowserDialog.SelectedPath + Path.DirectorySeparatorChar;
                label.Text = paths[index] = dir;
                File.WriteAllLines(pathConfigFile, paths);
            }
        }

        bool CheckError(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error);
                Console.WriteLine(error);
                return false;
            }
            return true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int index = 0; index < paths.Length; index++)
            {
                if (string.IsNullOrEmpty(paths[index]))
                {
                    MessageBox.Show("请设置好路径先!");
                    return;
                }
            }

            Cache.Init(labellist[labelSelect.SelectedIndex], divfolder.Checked ? labelNames[labelSelect.SelectedIndex].Split(':')[0] : "", cacheTog.Checked);
            DateTime start = DateTime.Now;
            if (Export())
            {
                Cache.SaveCache();
                MessageBox.Show("导出完成" + (DateTime.Now - start).TotalSeconds);
            }
        }

        private void label_Click(object sender, EventArgs e) { SelectDir(labels.IndexOf((Label)sender)); }
    }
}
