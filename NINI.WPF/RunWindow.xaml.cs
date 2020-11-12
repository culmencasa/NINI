using LiteDB;
using NINI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NINI
{
    /// <summary>
    /// RunWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RunWindow : SingleWindow
    {
        public RunWindow()
        {
            InitializeComponent();
        }

        public bool ForceClose { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            //using (var db = new LiteDatabase(@".\MyData.db"))
            //{
            //    // Get a collection (or create, if doesn't exist)
            //    var col = db.GetCollection<Customer>("customers");

            //    // Create your new customer instance
            //    var customer = new Customer
            //    {
            //        Name = "John Doe",
            //        Phones = new List<string> { "8000-0000", "9000-0000" },
            //        IsActive = true
            //    };

            //    // Insert new customer document (Id will be auto-incremented)
            //    col.Insert(customer);

            //    // Update a document inside a collection
            //    customer.Name = "Jane Doe";

            //    col.Update(customer);

            //    // Index document using document Name property
            //    col.EnsureIndex(x => x.Name);

            //    // Use LINQ to query documents (filter, sort, transform)
            //    var results = col.Query()
            //        .Where(x => x.Name.StartsWith("J"))
            //        .OrderBy(x => x.Name)
            //        .Select(x => new { x.Name, NameUpper = x.Name.ToUpper() })
            //        .Limit(10)
            //        .ToList();

            //    // Let's create an index in phone numbers (using expression). It's a multikey index
            //    col.EnsureIndex(x => x.Phones);

            //    // and now we can query phones
            //    var r = col.FindOne(x => x.Phones.Contains("8888-5555"));
            //}
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            //using (var db = new LiteDatabase(@".\MyData.db"))
            //{
            //    // Get file storage with Int Id
            //    var storage = db.GetStorage<int>();

            //    // Upload a file from file system to database
            //    storage.Upload(123, @"F:\Works\DriveScan1\DriveScan\Resources\luzhi.JPG");

            //    // And download later
            //    storage.Download(123, @"D:\Temp\copy-of-picture-01.jpg", true);
            //}

        }

    }
}
