using MVVMLib;
using NINI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINI.ViewModels
{
    public class ToDoListViewModel 
    {

        public ObservableCollection<ToDo> ToDoes
        {
            get;
            set;
        } = new ObservableCollection<ToDo>();

        public ToDoListViewModel()
        {
            ToDoes.Add(new ToDo() { Title = "Monday", IsDone = false });
            ToDoes.Add(new ToDo() { Title = "Tuesday", IsDone = true });
            ToDoes.Add(new ToDo() { Title = "Wednesday", IsDone = false });

            DeleteCommand = new RelayCommand<ToDo>(DeleteAction, (o)=> { return o != null; });
        }

        public ICommand DeleteCommand { get; set; }

        public void DeleteAction(ToDo item)
        {
            if (ToDoes.Contains(item))
            {
                ToDoes.Remove(item);
            }
        }
    }


}
