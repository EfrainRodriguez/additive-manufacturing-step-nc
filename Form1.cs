using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace add_stepnc
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowser_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            AdditiveXML addobj = new AdditiveXML();

            addobj.ReadXMLFile(openFileDialog1.FileName);

            lblBrowser.Text = System.IO.Path.GetFileName(openFileDialog1.FileName);

            addobj.WriteSTEPNC(addobj.AdditiveLayers, "AdditiveSTEP-NC");

            lblReady.Text = "Ready!";
        }
    }
}
