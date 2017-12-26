using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//Include mysql client namespace.
using MySql.Data.MySqlClient;
using System.Configuration;
using System.IO.Ports;
using Reader;
using System.Drawing.Printing;
using GridPrintPreviewLib;


namespace CSharpMySqlSample
{
    public partial class frmMySqlSample : Form
    {
        //Read connection string from application settings file
        string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString"];
        MySqlConnection connection;
        MySqlDataAdapter adapter;
        DataTable DTItems;
        private bool m_bDisplayLog = false;
        public static rfidCheckArticles formRFID;
        private Reader.ReaderMethod reader;

        public frmMySqlSample()
        {
            InitializeComponent();
        }

        private void frmMySqlSample_Load(object sender, EventArgs e)
        {
            //Initialize mysql connection
            connection = new MySqlConnection(ConnectionString);
            reader = new Reader.ReaderMethod();
            reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;
            //Get all items in datatable
            DTItems = GetAllItems();

            //Fill grid with items
            dataGridView1.DataSource = DTItems;
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[1].HeaderText = "REFERENCIA";
            dataGridView1.Columns[2].HeaderText = "FECHA";
            dataGridView1.Columns[3].HeaderText = "ARTICULOS";
            string[] ports = SerialPort.GetPortNames();
            if (Properties.Settings.Default.portCOM == "")
            {
                btnSave.Enabled = false;
            }
            else
            {
                comboBox1.Text = Properties.Settings.Default.portCOM;
            }
            foreach (string portAvailable in ports)
            {
                comboBox1.Items.Add(portAvailable);
            }

            DateTime fechaHoy = DateTime.Now;
            DateTime horaMax = Convert.ToDateTime("16:00:00");
            DateTime fechaMax = new DateTime(fechaHoy.Year, fechaHoy.Month, fechaHoy.Day, horaMax.Hour, horaMax.Minute, horaMax.Second);
            for (int h = 0; h < dataGridView1.Rows.Count; h++)
            {
                DateTime fechaPedido = Convert.ToDateTime(dataGridView1[2, h].Value);
                DateTime.Now.ToString("h:mm:ss tt");
                if (DateTime.Compare(fechaPedido,fechaMax) < 0)
                {
                    // Do Today
                    dataGridView1.Rows[h].DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 0);
                }
                else
                {
                    // Dont do today
                    dataGridView1.Rows[h].DefaultCellStyle.BackColor = Color.FromArgb(153, 255, 104);

                }
            }
            dataGridView1.ClearSelection();

        }

        //Get all items from database into datatable
        DataTable GetAllItems()
        {
            try
            {
                //prepare query to get all records from items table
                string query = "SELECT DISTINCT ps_orders.id_order,ps_orders.reference,ps_orders.date_upd,(SELECT SUM(ps_order_detail.product_quantity) FROM ps_order_detail WHERE id_order=ps_orders.id_order) FROM ps_orders INNER JOIN ps_order_detail ON ps_orders.id_order=ps_order_detail.id_order WHERE current_state=2";
                adapter = new MySqlDataAdapter(query, connection);
                DataSet DS = new DataSet();
                //get query results in dataset
                adapter.Fill(DS);
                //prepare adapter to run query
                // Set the UPDATE command and parameters.
                return DS.Tables[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }



        private void btnSave_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                string nIdOrder, portCOM, nRefPedido;
                int selectedrowindex = dataGridView1.SelectedCells[0].RowIndex;

                DataGridViewRow selectedRow = dataGridView1.Rows[selectedrowindex];
                nIdOrder = Convert.ToString(selectedRow.Cells[0].Value); /* Contain ID Order */
                nRefPedido = Convert.ToString(selectedRow.Cells[1].Value);
                rfidCheckArticles m = new rfidCheckArticles(nIdOrder, reader, nRefPedido);
                m.ShowDialog();
            }
            else
            {
                MessageBox.Show("Seleccione sólo un pedido por favor");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                //Delete a row from grid first.
                dataGridView1.Rows.Remove(dataGridView1.SelectedRows[0]);

                //Save records again. This will delete record from database.
                adapter.Update(DTItems);

                //Refresh grid. Get items again from database and show it in grid.
                DTItems = GetAllItems();
                dataGridView1.DataSource = DTItems;
                MessageBox.Show("Selected item deleted successfully...");
            }
            else
            {
                MessageBox.Show("You must select entire row in order to delete it.");
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count >= 1)
            {
                MessageBox.Show("CuLO");
            }
            else
            {
                MessageBox.Show("Seleccione sólo un pedido por favor");
            }
            // Imprimir listado de articulos de 1 o varios pedidos seleccionados para ir a recogerlos al almacen
            /*GridPrintDocument doc = new GridPrintDocument(this.dataGridView1, this.dataGridView1.Font, true);
            doc.DocumentName = "Preview Test";
            doc.DrawCellBox = true;
            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.ClientSize = new Size(400, 300);
            printPreviewDialog.Location = new Point(29, 29);
            printPreviewDialog.Name = "Print Preview Dialog";
            printPreviewDialog.UseAntiAlias = true;
            printPreviewDialog.Document = doc;
            printPreviewDialog.ShowDialog();
            doc.Dispose();
            doc = null;*/
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*btnSave.Enabled = true;*/
            Properties.Settings.Default.portCOM = comboBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openConection(comboBox1.Text);
        }

        private void openConection(string portCOM)
        {
            string strException = string.Empty;
            string strComPort = portCOM;//cmbComPort.Text;
            //Coge el BaudRate en cmbBaudRate en modo numérico(critico)
            Int32 nBaudrate = 115200;//Convert.ToInt32(cmbBaudrate.Text);
            // Comprueba si existe y conecta al puerto COM
            int nRet = reader.OpenCom(strComPort, nBaudrate, out strException);
            if (button3.Text == "CONNECT")
            {
                if (nRet != 0)
                {
                    string strLog = "Connect reader failed, due to: " + strException;
                    this.Text = "Preparador de pedidos - Doctor PRO - " + strLog;
                }
                else
                {
                    string strLog = "Reader connected " + strComPort + "@" + nBaudrate.ToString();
                    button3.Text = "DISCONNECT";
                    this.Text = "hola";
                    btnSave.Enabled = true;
                    this.Text = "Preparador de pedidos - Doctor PRO - " + strLog;

                }
            }
            else
            {
                reader.CloseCom();
                button3.Text = "CONNECT";
                btnSave.Enabled = false;
                this.Text = "Preparador de pedidos - Doctor PRO - Desconectado";
            }
        }
        private void ReceiveData(byte[] btAryReceiveData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);

            }
        }

        private void SendData(byte[] btArySendData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);

            }
        }

        private void AnalyData(Reader.MessageTran msgTran)
        {
            if (msgTran.PacketType != 0xA0)
            {
                return;
            }
            
            
        }

    }
}