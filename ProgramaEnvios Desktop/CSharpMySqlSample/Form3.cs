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
    public partial class Form3 : Form
    {
        string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString2"];
        MySqlConnection connection;
        MySqlDataAdapter adapter;
        DataTable DTItems;
        String listOfQuery="";
        Boolean anotaCambios = false;
        public Form3()
        {
            InitializeComponent();
        }

        public Form3(string numOrder)
        {
            InitializeComponent();
            label1.Text = "PEDIDO: " + numOrder.ToString();
            //Initialize mysql connection
            connection = new MySqlConnection(ConnectionString);
            //Get all items in datatable
            if (OpenConnectionMySQL())
            {
                DTItems = GetAllItems(numOrder);
                //Fill grid with items
                dataGridView1.DataSource = DTItems;
                dataGridView1.Columns[0].HeaderText = "REFERENCIA";
                dataGridView1.Columns[1].HeaderText = "CANTIDAD";
                //dataGridView1.Columns[2].HeaderText = "DESCRIPCION";
                CloseConnectionMySQL();
            }
            else
            {
                MessageBox.Show("Mierda! No conecto a la BBDD");
            }
            anotaCambios = true;
        }

        //Get all items from database into datatable
        DataTable GetAllItems(String numOrder)
        {
            try
            {
                //prepare query to get all records from items table
                string query = "SELECT referencia, cantidad FROM proxPedidosResumen WHERE idPedidoImport="+numOrder;
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


        private void button2_Click(object sender, EventArgs e)
        {
            string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString2"];
            // Update all 
            if (listOfQuery == "")
            {
                MessageBox.Show("No has hecho ningún cambio");
            }
            else
            {
                if (OpenConnectionMySQL())
                {
                    MySqlCommand cmd = new MySqlCommand(listOfQuery, connection);
                    cmd.ExecuteNonQuery();
                    CloseConnectionMySQL();
                    MessageBox.Show("Pedido actualizado");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ha habido un error al intentar conectarse a la BBDD");
                }
            }
            
        }

        private bool OpenConnectionMySQL()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        MessageBox.Show("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        MessageBox.Show("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }
        private bool CloseConnectionMySQL()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cambioContenidoCell(object sender, DataGridViewCellEventArgs e)
        {
            if (anotaCambios == true){
                listOfQuery = listOfQuery + "UPDATE proxPedidosResumen SET cantidad=" + dataGridView1[1, dataGridView1.CurrentCell.RowIndex].Value.ToString() + " WHERE referencia=" + dataGridView1[0, dataGridView1.CurrentCell.RowIndex].Value.ToString() + ";";
            }
       }
    }
}
