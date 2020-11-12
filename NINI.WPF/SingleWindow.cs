using System;
using System.Collections.Generic;
using System.Text;
using System.Windows; 

namespace NINI
{
    public partial class SingleWindow : Window, ISingleWindow
    {
        #region ISingleWindow实现

        public bool ForceClose { get; set; }

        public virtual void HideOnClose()
        {
            Hide();
        }

        #endregion

        public SingleWindow()
        {
            this.Closing += SingleWindow_Closing;
        }

        private void SingleWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ForceClose)
            {
                e.Cancel = true;
                HideOnClose();
            }
        }


    }
}
