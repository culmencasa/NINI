using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINI.Controls
{
    public class CrossButton : Button
    {
        static CrossButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CrossButton), new FrameworkPropertyMetadata(typeof(CrossButton)));
        }

    }
}
