using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace CSharpMySqlSample
{
    public partial class Form2 : Form
    {
        string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString2"];
        MySqlConnection connection;
        MySqlDataAdapter adapter;
        DataTable DTItems;
        public Form2()
        {
            InitializeComponent();

        }
        private void Form2_Load(object sender, EventArgs e)
        {
            //Initialize mysql connection
            connection = new MySqlConnection(ConnectionString);
            //Get all items in datatable
            DTItems = GetAllItems();

            //Fill grid with items
            dataGridView1.DataSource = DTItems;
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[1].HeaderText = "FECHA";
            dataGridView1.Columns[2].HeaderText = "PROVEEDOR";
            dataGridView1.Columns[3].HeaderText = "TRACKING";

        }

        //Get all items from database into datatable
        DataTable GetAllItems()
        {
            try
            {
                //prepare query to get all records from items table
                string query = "SELECT idPedidoImport, fechaPedido,idProveedor,tracking FROM proxPedidos WHERE recibido='Si' AND disponible='Si'";
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
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 1)
            {
                string nIdOrder;
                int selectedrowindex = dataGridView1.SelectedCells[0].RowIndex;

                DataGridViewRow selectedRow = dataGridView1.Rows[selectedrowindex];
                nIdOrder = Convert.ToString(selectedRow.Cells[0].Value); /* Contain ID Order */
                Form3 m = new Form3(nIdOrder);
                m.ShowDialog();
            }
            else
            {
                MessageBox.Show("Seleccione sólo un pedido por favor");
            }
        }
    }
}
