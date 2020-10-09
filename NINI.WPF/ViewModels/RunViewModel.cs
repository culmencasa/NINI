using LiteDB;
using MVVMLib;
using NINI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINI.ViewModels
{
    public class RunViewModel : ViewModelBase
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

        void LoadShortcuts()
        {
            using (var db = new LiteDatabase(@".\sc.db"))
            {
                var collection = db.GetCollection<Shortcut>();
                var list = collection.FindAll();

                var shortcut = list.First();

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


                fs.Download(shortcut.Icon, iconStream);
                iconStream.Seek(0, SeekOrigin.Begin); // 就差这一句

                shortcut.IconStream = (ImageSource)new ImageSourceConverter().ConvertFrom(iconStream);

                this.Datas = list.ToList();
            }
        }
    }
}
