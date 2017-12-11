using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ES6ImportModuleAnalyse
{
    public partial class MainForm : Form
    {
        private OpenFileDialog dia = new OpenFileDialog();
        private string jsFileBasePath = "";
        private string packageJsonPath = "";
        private Dictionary<string, JsFile> DicNameToJsFile;
        public MainForm()
        {
            dia.Filter = "js文件(*.js)|*.js";
            dia.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            dia.Multiselect = false;
            dia.ValidateNames = true;
            dia.CheckFileExists = true;
            dia.DefaultExt = ".js";
            dia.FileName = "main.js";
            InitializeComponent();
            resize();
        }
        private void resize()
        {
            treeView1.Width = (int)(this.Width * 0.5);
            panel1.Width = (int)(this.Width * 0.5) - 20;
            textBox1.Height = (int)(this.Height - 70);
            button1.Location = new System.Drawing.Point((int)(panel1.Width * 0.5 - 20), textBox1.Height + 5);
        }
        private List<string> GetAllFile(string path)
        {
            var result = new List<string>();
            if (path.EndsWith(@"\node_modules"))
            {
                return result;
            }
            result.AddRange(Directory.GetFiles(path, "*.js"));
            result.AddRange(Directory.GetFiles(path, "*.jsx"));
            foreach (var d in Directory.GetDirectories(path))
            {
                result.AddRange(GetAllFile(d));
            }
            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dia.ShowDialog();
            if (!File.Exists(dia.FileName))
            {
                MessageBox.Show("必须选择文件");
                return;
            }
            ReadFileString(dia.FileName);
            FileInfo fileInfo = new FileInfo(dia.FileName);
            jsFileBasePath = fileInfo.DirectoryName + @"\";
            packageJsonPath = jsFileBasePath;
            while (!File.Exists(packageJsonPath + @"\package.json"))
            {
                packageJsonPath += @"..\";
            }
            packageJsonPath = new DirectoryInfo(packageJsonPath).FullName;
            DicNameToJsFile = new Dictionary<string, JsFile>();

            var mainFile = JsFile.GetJsFile(dia.FileName, DicNameToJsFile, fileInfo.Name);
            mainFile.Resolve(DicNameToJsFile);

            var str = mainFile.ToJson(ES6ImportModuleAnalyse.JsFile.JsFileImportStatus.Importing);
            textBox1.Text = str;

            addTreeHead(Common.JsonToDictionary(str));

            var allJsFiles = GetAllFile(packageJsonPath);

            //var package_json = Common.JsonToDictionary(ReadFileString(dia.FileName));
            //var cmd_npm_start_str = package_json.GetString(new string[] { "scripts", "start" });
            //var webpack_config_js_reg = new Regex(@"--config\s+(?<webpackConfig>.+)\s*");
            //var webpack_config_js_group = webpack_config_js_reg.Match(cmd_npm_start_str).Groups;
            //var webpack_config_js_str = webpack_config_js_group["webpackConfig"].ToString();
        }

        private void addTreeHead(IDictionary<string, object> dic)
        {
            treeView1.Nodes.Clear();
            TreeNode node = new TreeNode();
            node.Text = dic.GetString("FileImportStr");
            node.ToolTipText = dic.GetString("ResolveAbsPath");
            node.ForeColor = Color.FromArgb(102, 153, 0);
            treeView1.Nodes.Add(node);
            addTree(Common.JsonToList(dic.Get("Imports", null).ToJsonString()), node);
            treeView1.Nodes[0].ExpandAll();
        }
        private void addTree(IList<IDictionary<string, object>> list, TreeNode PNode)
        {
            if (list == null)
            {
                return;
            }
            foreach (var dic in list)
            {
                TreeNode node = new TreeNode();
                node.Text = dic.GetString("FileImportStr");
                node.ToolTipText = dic.GetString("ResolveAbsPath");
                node.ForeColor = dic.GetString("ImportStatus") == "1" ? Color.FromArgb(212, 73, 80) : Color.FromArgb(102, 153, 0);
                node.Expand();
                PNode.Nodes.Add(node);
                addTree(Common.JsonToList(dic.Get("Imports", null).ToJsonString()), node);
            }
        }

        private string ReadFileString(string path)
        {
            string result = "";
            var file = File.OpenRead(path);
            var sr = new StreamReader(file);
            result = sr.ReadToEnd();
            sr.Close();
            return result;
        }

        private void WebpackAppDependence_Resize(object sender, EventArgs e)
        {
            resize();
        }
    }
    public struct ImportStruct
    {
        private JsFile file;
        public JsFile File
        {
            get
            {
                return file;
            }
        }
        private string filePath;
        public string FilePath
        {
            get
            {
                return filePath;
            }
        }
        private ES6ImportModuleAnalyse.JsFile.JsFileImportStatus fileStatus;
        public ES6ImportModuleAnalyse.JsFile.JsFileImportStatus FileStatus
        {
            get
            {
                return fileStatus;
            }
        }
        public ImportStruct(JsFile f, ES6ImportModuleAnalyse.JsFile.JsFileImportStatus status)
        {
            this.file = f;
            this.filePath = f.ResolveAbsPath;
            this.fileStatus = status;
        }

    }
    public class JsFile
    {
        public List<string> ImportStrings;
        public List<ImportStruct> Imports;
        public string FileRealName;//不带.js
        public string FileExtName;//后缀
        public string FileName;
        public string FileImportStr;//用户输入的
        public string FilePath;//带结尾斜杠的
        public string ResolveAbsPath;
        public bool IsUserFile;
        public JsFileImportStatus ImportStatus;
        public enum JsFileImportStatus
        {
            ImportNone,
            Importing,
            ImportComplete,
        }
        public string ToJson(JsFileImportStatus status, bool recursive = true)
        {
            var dic = Common.NewDictionary();
            dic.Add("ResolveAbsPath", this.ResolveAbsPath.Replace(@"\", @"/"));
            dic.Add("FileImportStr", this.FileImportStr);
            dic.Add("ImportStatus", status);
            if (recursive)
            {
                dic.Add("Imports", this.Imports.Select(a =>
                {
                    return Common.JsonToDictionary(a.File.ToJson(a.FileStatus, a.FileStatus == JsFileImportStatus.ImportNone));
                }).ToList());
            }
            return dic.ToJsonString();
        }
        public static string GetJsFileAbsFullName(string filePath)
        {
            var _filePath = "";
            if (File.Exists(filePath))
            {
                _filePath = filePath;
            }
            else if (File.Exists(filePath + @".js"))
            {
                _filePath = filePath + @".js";
            }
            else if (File.Exists(filePath + @"\index.js"))
            {
                _filePath = filePath + @"\index.js";
            }
            else
            {
                throw new Exception(string.Format("{0}文件不存在!", filePath));
            }
            return _filePath;
        }
        public static JsFile GetJsFile(string filePath, Dictionary<string, JsFile> dic, string importStr)
        {
            var fileInfo = new FileInfo(GetJsFileAbsFullName(filePath));
            if (!dic.ContainsKey(fileInfo.FullName))
            {
                var f = new JsFile();
                f.FileImportStr = importStr;
                f.ResolveAbsPath = fileInfo.FullName;
                f.FileName = fileInfo.Name;
                f.FileRealName = fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf('.'));
                f.FileExtName = fileInfo.Name.Substring(f.FileRealName.Length);
                f.FilePath = fileInfo.DirectoryName + @"\";
                f.IsUserFile = true;
                f.ImportStatus = JsFileImportStatus.ImportNone;
                f.AddToDic(dic);
                var content = f.ReadFileString(f.ResolveAbsPath);
                f.ImportStrings = f.GetImportString(content);
                f.Imports = new List<ImportStruct>();
                if (f.ImportStrings.Count == 0)
                {
                    f.ImportStatus = JsFileImportStatus.ImportComplete;
                }
                return f;
            }
            else
            {
                return dic[fileInfo.FullName];
            }
        }
        public void AddToDic(Dictionary<string, JsFile> dic)
        {
            if (this.FileName == "index.js")
            {
                dic.Add(FilePath, this);
            }
            if (this.FileExtName == ".js")
            {
                dic.Add(this.FilePath + this.FileRealName, this);
            }
            dic.Add(this.FilePath + this.FileName, this);
        }
        public void Resolve(Dictionary<string, JsFile> dic)
        {
            if (this.ImportStatus != JsFileImportStatus.ImportNone)
            {
                this.ImportStatus = JsFileImportStatus.ImportComplete;
                return;
            }
            this.ImportStatus = JsFileImportStatus.Importing;
            foreach (var im in this.ImportStrings)
            {
                //##############
                //var tmpFileFullName = GetJsFileAbsFullName(this.FilePath + im);
                //if (dic.ContainsKey(tmpFileFullName))
                //{
                //    var tmpFile = dic[tmpFileFullName];
                //    if (tmpFile.ImportStatus == JsFileImportStatus.Importing)
                //    {//在前面正在导入中了
                //        tmpFile.ImportStatus = JsFileImportStatus.ImportComplete;
                //        this.Imports.Add(tmpFile);
                //    }
                //    else if (tmpFile.ImportStatus == JsFileImportStatus.ImportComplete)
                //    {//之前已经导入完成了
                //        this.Imports.Add(tmpFile);
                //    }
                //}
                //else
                //{
                //    var tmpFile = GetJsFile(tmpFileFullName, dic);
                //}

                //##############
                try
                {
                    var tmpFile = GetJsFile(this.FilePath + im, dic, im);
                    if (tmpFile.ImportStatus == JsFileImportStatus.ImportNone)
                    {//刚刚拿到的新文件
                        //tmpFile.ImportStatus = JsFileImportStatus.Importing;
                        this.Imports.Add(new ImportStruct(tmpFile, JsFileImportStatus.ImportNone));
                    }
                    else if (tmpFile.ImportStatus == JsFileImportStatus.Importing)
                    {//在前面正在导入中了
                        //tmpFile.ImportStatus = JsFileImportStatus.ImportComplete;
                        this.Imports.Add(new ImportStruct(tmpFile, JsFileImportStatus.Importing));
                    }
                    else if (tmpFile.ImportStatus == JsFileImportStatus.ImportComplete)
                    {//之前已经导入完成了
                        this.Imports.Add(new ImportStruct(tmpFile, JsFileImportStatus.ImportComplete));
                    }
                    tmpFile.Resolve(dic);
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            this.ImportStatus = JsFileImportStatus.ImportComplete;
        }
        private List<string> GetImportString(string content)
        {
            var result = new List<string>();
            var import_regex = new Regex(@"^import [a-zA-Z0-9_,{}\s]+ from '(?<fileName>[^']+)'", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var import_matches = import_regex.Matches(content);
            foreach (Match m in import_matches)
            {
                var import_str = m.Groups["fileName"].ToString();
                if (!Regex.IsMatch(import_str, @"^[a-zA-Z0-9_-]+$"))
                {
                    result.Add(import_str);
                }
            }

            return result;
        }

        private string ReadFileString(string path)
        {
            string result = "";
            var file = File.OpenRead(path);
            var sr = new StreamReader(file);
            result = sr.ReadToEnd();
            sr.Close();
            return result;
        }
    }

}
