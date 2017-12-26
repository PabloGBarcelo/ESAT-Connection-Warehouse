using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CSharpMySqlSample
{
    public partial class Informe : Form
    {
        public Informe()
        {
            InitializeComponent();
        }

        private void Informe_Load(object sender, EventArgs e)
        {

            this.reportViewer1.RefreshReport();
        }
    }
}
