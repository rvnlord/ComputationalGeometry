using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.Models
{
    public class DdlItem
    {
        public int Index { get; set; }
        public string Text { get; set; }

        public DdlItem(int index, string text)
        {
            Index = index;
            Text = text;
        }
    }
}
