using MVVMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINI.Models
{
    public class ToDo : ObservableObject
    {
        public ToDo()
        {
            //Guid = System.Guid.NewGuid().ToString("N");
            ShowWatermarks = false;
        }


        private string _guid;
        public string Guid
        {
            get
            {
                return _guid;
            }
            set
            {
            }
        }


        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                Set(ref _title, value);
                if (string.IsNullOrEmpty(_title))
                {
                    ShowWatermarks = true;
                }
                else
                {
                    ShowWatermarks = false;
                }
            }
        }

        private bool _isDone;
        public bool IsDone
        {
            get
            {
                return _isDone;
            }
            set
            {
                Set(ref _isDone, value);
            }
        }

        private bool _showWatermarks;
        public bool ShowWatermarks 
        { 
            get => _showWatermarks; 
            set 
            {
                Set(ref _showWatermarks, value);
            } 
        }


    }
}
