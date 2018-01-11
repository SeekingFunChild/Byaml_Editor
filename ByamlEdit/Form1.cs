using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ByamlEdit
{
    public partial class ByamlEdit : Form
    {
        Byaml byaml;

        public ByamlEdit()
        {
            InitializeComponent();
        }

        private void ByamlEdit_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ByamlEdit_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Trim() != "")
                {
                    byaml = new Byaml(s[i]);
                    byaml.Show(this);
                    this.Text = "ByamlEdit" + "(" +s[i]+ ")";
                    break;
                }
            }
        }
    }
}
