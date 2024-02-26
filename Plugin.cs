using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Navigation;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer;
using WAS_解压;

namespace QuickLook.Plugin.HelloWorld
{
    public class Plugin : IViewer
    {
        private string _imagePath;
        private ImagePanel _ip;
        private MetaProvider _meta;
        public int Priority => 0;
        public int Direction;
        public int Frame;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && new[] { ".tcp",".was" }.Any(path.ToLower().EndsWith);
        }

        public void Prepare(string path, ContextObject context)
        {
            _imagePath = ExtractFile(path);
            _meta = new MetaProvider(_imagePath);
            var size = _meta.GetSize();
            if (!size.IsEmpty)
                context.SetPreferredSizeFit(size, 0.8);
            else
                context.PreferredSize = new Size(800, 600);
            //context.Theme = (Themes)SettingHelper.Get("LastTheme", 1, "QuickLook.Plugin.ImageViewer");
        }

        public void View(string path, ContextObject context)
        {
            _imagePath = ExtractFile(path);
            _ip = new ImagePanel();
            _ip.ContextObject = context;
            _ip.Meta = _meta;
            _ip.Theme = context.Theme;

            var size = _meta.GetSize();
            context.ViewerContent = _ip;
            context.Title = size.IsEmpty
                ? $"{Path.GetFileName(path)}"
                : $"方向数：{this.Direction} 单方向帧数：{this.Frame}";

            _ip.ImageUriSource = FilePathToFileUrl(_imagePath);
        }

        public void Cleanup()
        {
            _ip?.Dispose();
            _ip = null;
        }

        private string ExtractFile(string path)
        {
            List<string> nopath = new List<string>();
            string destinationPath = null;
            try
            {
                WAS mwas = new WAS(path);
                this.Direction = mwas.Direction;
                this.Frame = mwas.Frame; 
                //LogInformation("new WAS:" + path);
                System.Drawing.Bitmap mbitmap = mwas.PutPNG();
                //string curAssemblyFolder = Path.Combine(Path.GetDirectoryName(path), "tmp");
                string fileName = Path.GetFileNameWithoutExtension(path);
                string curAssemblyFolder = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                curAssemblyFolder = Path.GetDirectoryName(curAssemblyFolder);
                //LogInformation("curAssemblyFolder" + curAssemblyFolder);
                destinationPath = Path.GetFullPath(Path.Combine(curAssemblyFolder, fileName+".png"));
                //LogInformation("destinationPath" + destinationPath);
                if (mbitmap != null)
                {
                    mbitmap.Save(destinationPath);
                    mwas.Close();
                    mbitmap.Dispose();
                }
            }
            catch(Exception e)
            {
                LogError(e.ToString());
            }
            


            return destinationPath;
        }

        public Uri FilePathToFileUrl(string filePath)
        {
            var uri = new StringBuilder();
            foreach (var v in filePath)
                if (v >= 'a' && v <= 'z' || v >= 'A' && v <= 'Z' || v >= '0' && v <= '9' ||
                    v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                    v > '\x80')
                    uri.Append(v);
                else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
                    uri.Append('/');
                else
                    uri.Append($"%{(int)v:X2}");
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");

            try
            {
                return new Uri(uri.ToString());
            }
            catch
            {
                return new Uri(filePath);
            }
        }

        static void LogInformation(string message)
        {
            // 将运行时信息写入文件
            using (StreamWriter writer = new StreamWriter("quicklook_tcp_log.txt", true))
            {
                writer.WriteLine($"信息: {DateTime.Now} - {message}");
            }
        }

        static void LogError(string message)
        {
            // 将错误信息写入文件
            using (StreamWriter writer = new StreamWriter("quicklook_tcp_log.txt", true))
            {
                writer.WriteLine($"错误: {DateTime.Now} - {message}");
            }
        }
    }
}