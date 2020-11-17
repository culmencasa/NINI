using GongSolutions.Wpf.DragDrop;
using LiteDB;
using MVVMLib;
using NINI.Helper;
using NINI.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shortcut = NINI.Models.Shortcut;

namespace NINI.ViewModels
{
    public class RunViewModel : ViewModelBase, GongSolutions.Wpf.DragDrop.IDropTarget
    {

        private List<Shortcut> _datas;
        public List<Shortcut> Datas
        {
            get
            {
                return _datas;
            }
            set
            {
                Set(ref _datas, value);
            }
        }

        public ICommand LoadCommand { get; set; }
        public ICommand AddCommand { get; set; }

        public RunViewModel()
        {
            AddCommand = new RelayCommand(Add);
            LoadCommand = new RelayCommand(LoadShortcuts);
        }

        void Add()
        {

            using (var db = new LiteDatabase(@".\sc.db"))
            {
                var collection = db.GetCollection<Shortcut>();
                var shortcut = new Shortcut()
                {
                    Command = @"%windir%\system32\notepad.exe",
                    Description = "记事本",
                    Title = "Notepad",
                    ShortcutId = ObjectId.NewObjectId(),
                    Icon = "$/Icons/notepad.jpg"
                };

                collection.Insert(shortcut);


                var storage = db.GetStorage<string>();
                storage.Upload(shortcut.Icon, @"F:\Works\DriveScan1\DriveScan\Resources\luzhi.JPG");
            }

        }



        public void Add(Shortcut shortcut)
        {
            using (var db = new LiteDatabase(@".\sc.db"))
            {
                var collection = db.GetCollection<Shortcut>();
                // 给shortcut加上Id
                shortcut.ShortcutId = ObjectId.NewObjectId();
                collection.Insert(shortcut);


                // 上传图标
                if (!string.IsNullOrEmpty(shortcut.Icon))
                {
                    var storage = db.GetStorage<string>();
                    storage.Upload(shortcut.ShortcutId.ToString(), shortcut.Icon);
                }
                else
                {
                    string key = shortcut.ShortcutId.ToString();
                    string iconKey = "$/Icons/" + key;
                    shortcut.Icon = iconKey;

                    if (File.Exists(shortcut.Command))
                    {
                        var icon = ShellIcon.GetLargeIcon(shortcut.Command);
                        MemoryStream ms = new MemoryStream();
                        icon.Save(ms);

                        var storage = db.GetStorage<string>();
                        storage.Upload(key, iconKey, ms);

                        ms.Dispose();
                    }
                }
            }

        }


        void LoadShortcuts()
        {
            using (var db = new LiteDatabase(@".\sc.db"))
            {
                var collection = db.GetCollection<Shortcut>();
                var list = collection.FindAll()?.ToList();

                if (list != null)
                {
                    foreach (var shortcut in list)
                    {
                        MemoryStream iconStream = new MemoryStream();
                        var fs = db.GetStorage<string>();

                        // method 1
                        //var file = fs.FindById(shortcut.Icon);
                        //file.CopyTo(iconStream);


                        // method 2
                        //fs.Download(shortcut.Icon, @"D:\luzhi.jpg", true);


                        // method 3
                        //var file = fs.FindById(shortcut.Icon);
                        //if (file != null)
                        //{
                        //    using (var fStream = file.OpenRead())
                        //    {
                        //        fStream.CopyTo(iconStream);
                        //        iconStream.Seek(0, SeekOrigin.Begin); 
                        //    }
                        //}

                        // 加载图标
                        if (!string.IsNullOrEmpty(shortcut.Icon))
                        {
                            fs.Download(shortcut.Icon, iconStream);
                            iconStream.Seek(0, SeekOrigin.Begin); // 就差这一句

                            shortcut.IconStream = (ImageSource)new ImageSourceConverter().ConvertFrom(iconStream);
                        }
                        else
                        {
                            if (File.Exists(shortcut.Command))
                            {
                                var icon = ShellIcon.GetLargeIcon(shortcut.Command);
                                shortcut.IconStream = icon.ToImageSource();
                            }
                            else
                            { 
                                //todo: 默认图标
                            }
                        }
                    }
                }

                this.Datas = list;
            }
        }

        void GongSolutions.Wpf.DragDrop.IDropTarget.DragOver(IDropInfo dropInfo)
        {

            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;

            var dataObject = dropInfo.Data as IDataObject; 
            if (dataObject != null && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                dropInfo.Effects = (System.Windows.DragDropEffects)DragDropEffects.Copy;
            }
            else
            {
                dropInfo.Effects = (System.Windows.DragDropEffects)DragDropEffects.Move;
            }

        } 

        void GongSolutions.Wpf.DragDrop.IDropTarget.Drop(IDropInfo dropInfo)
        {
            var dataObject = dropInfo.Data as Shortcut;
            if (dataObject != null)
            {
                //this.HandleDropActionAsync(dropInfo, dataObject.GetFileDropList());
            }
            else
            {
                //GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo); 

                this.HandleDropFile(dropInfo);
            }
        }

        private void HandleDropFile(IDropInfo dropInfo)
        {
            var oleData = dropInfo.Data as System.Windows.IDataObject;
            if (oleData == null)
                return;

            string[] files = (string[])oleData.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (file.EndsWith(".lnk"))
                {
                    string title, command, args, description, iconLocation;
                    int index;
                    LnkHelper.ResolveShortcut(file, out title, out command, out args, out description, out iconLocation, out index);

                    // todo: 把参数加上
                    Shortcut info = new Shortcut()
                    {
                        Title = title,
                        Description = description,
                        Icon = iconLocation,
                        Command = command
                    };

                    Add(info);
                }
                else if (file.EndsWith(".exe"))
                {
                    System.Diagnostics.FileVersionInfo versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(file);
                    Shortcut info = new Shortcut()
                    { 
                        Title = versionInfo.ProductName,
                        Description = versionInfo.FileDescription,
                        Icon = null,
                        Command = file
                    };

                    Add(info);
                }
                else
                {
                    continue;
                }
            }
        }
    }
}
