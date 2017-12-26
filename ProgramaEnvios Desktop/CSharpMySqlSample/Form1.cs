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
    public partial class Form1 : Form
    {
        string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString"];
        string ConnectionString2 = ConfigurationSettings.AppSettings["ConnectionString2"];
        MySqlConnection connection;
        MySqlConnection connection2;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form2 m = new Form2();
            m.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frmMySqlSample m = new frmMySqlSample();
            m.ShowDialog();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Esta opción puede llevar bastante tiempo, todos los stocks se verán modificados.", "ATENCION", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
              
                button7.Text = "Actualizando...";
                button7.Enabled = false;
                // Update all the number of items available in Prestashop.
                // Cogemos todas las referencias con RFID para actualizarlas online
                string getAllReferencesToUpdate = "SELECT reference FROM ps_product WHERE rfid!='No'";
                string ordersPaidTextSQL = "";
                List<String> referenciaConRFID = new List<String>();
                List<String> referenciaIdProduct = new List<String>();
                List<String> stockConEPC = new List<String>();
                List<String> stockEPC = new List<String>();
                List<String> refDeEPC = new List<String>();
                List<String> stockVendidoOnline = new List<String>();
                connection = new MySqlConnection(ConnectionString);
                connection2 = new MySqlConnection(ConnectionString2);
                string listaReferencias = "";
                // Cogemos todas las ferencias con RFID
                if (OpenConnectionMySQLPS())
                {
                    using (MySqlCommand command = new MySqlCommand(getAllReferencesToUpdate, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                referenciaConRFID.Add(reader.GetString(0));
                            }
                        }
                    }
                    CloseConnectionMySQLPS();
                }
                else
                {
                    MessageBox.Show("Error en la conexion");
                }
                int x = 0;
                foreach (string reference in referenciaConRFID)
                {
                    if (x == 0){

                        listaReferencias = listaReferencias + " AND referencia='" + reference + "'";
                        x++;
                    }
                    else
                    {
                        listaReferencias = listaReferencias + " OR referencia='" + reference + "'";
                    }
                
                }
                // Cogemos toda la cantidad de stock sin marcar de las referencias con RFID 
                // pero falta de devengar la cantidad vendida y no empaquetada
                string realStockEPCQuery = "SELECT DISTINCT referencia, count(*) FROM `proxPedidosContenido` WHERE uso = '' AND etiquetaImprimida = 'Si'" + listaReferencias + "GROUP BY referencia";

                if (OpenConnectionMySQL())
                {
                    using (MySqlCommand command = new MySqlCommand(realStockEPCQuery, connection2))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                refDeEPC.Add(reader.GetString(0));
                                stockConEPC.Add(reader.GetString(1));
                            }
                        }
                    }
                    CloseConnectionMySQL();

                }
                if (OpenConnectionMySQLPS())
                {
                    // Cogemos todos los pedidos pendientes de empaquetar y pagados
                    string queryPedidosSinEmpaquetar = "SELECT id_order FROM ps_orders WHERE current_state = 2";
                    using (MySqlCommand command = new MySqlCommand(queryPedidosSinEmpaquetar, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                stockVendidoOnline.Add(reader.GetString(0));
                            }
                        }
                    }
                    CloseConnectionMySQLPS();
                
                }

                // realStockEPCQuery(ref,cantidad) -> Contiene stock en gavetas.(refDeEPC,stockConEPC)
                // stockVendidoOnline -> Contiene un LIST de todos los pedidos con algo vendido pero no empaquetado
                // ordersPaidTextSQL -> Contendra una string con todos los id_order concatenados
                // Y preparamos una consulta para ver si se encuentra en las referencias.
                // Finalmente debe quedar realStockEPCQuery - stockVendidoOnline
                int switchX = 0;
                foreach (string ordersPaid in stockVendidoOnline)
                {
                    if (switchX == 0)
                    {
                        ordersPaidTextSQL = ordersPaidTextSQL + " AND id_order = " + ordersPaid;
                        switchX++;
                    }
                    else
                    {
                        ordersPaidTextSQL = ordersPaidTextSQL + " OR id_order = " + ordersPaid;
                    }
                }
            
                // Now check in the orders if there is any reference to check.
                using (var e1 = refDeEPC.GetEnumerator())
                using (var e2 = stockConEPC.GetEnumerator())
                {
                    while (e1.MoveNext() && e2.MoveNext())
                    {
                        var item1 = e1.Current;
                        var item2 = e2.Current;
                        string getQuantityReferencePaid = "SELECT SUM(product_quantity) FROM ps_order_detail WHERE product_reference='" + item1 + "' " + ordersPaidTextSQL;
                        string quantitySelled = "";
                        if (OpenConnectionMySQLPS())
                        {
                            using (MySqlCommand command = new MySqlCommand(getQuantityReferencePaid, connection))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        if (!reader.IsDBNull(0)){
                                            quantitySelled = reader.GetString(0);
                                        }
                                    }
                                }
                            }
                            CloseConnectionMySQLPS();
                        }
                        if (quantitySelled == null || quantitySelled == "")
                        {
                            quantitySelled = "0";
                        }
                        string stockDisponible = Convert.ToString(Convert.ToInt16(item2) - Convert.ToInt16(quantitySelled));
                        label1.Text = "Referencia: " + item1 + " cantidad vendida: " + quantitySelled + " cantidad disponible: " + stockDisponible;
                        label1.Refresh();
                        //stockVendidoOnline
                        // use item1 and item2
                        string getIdProduct = "SELECT id_product FROM ps_product WHERE reference='" + item1+"'";
                        string idProduct = "";
                        if (OpenConnectionMySQLPS())
                        {
                            using (MySqlCommand command = new MySqlCommand(getIdProduct, connection))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        idProduct = reader.GetString(0);
                                    }
                                }
                            }
                            CloseConnectionMySQLPS();
                        }
                        if (OpenConnectionMySQLPS())
                        {

                            //MessageBox.Show("Cambiando id_product:"+idProduct+" referencia:" + item1 + " stock EPC:" + item2 + " stock gastado:" + quantitySelled);
                            string updateQuantity = "UPDATE ps_stock_available SET quantity= " + stockDisponible + " WHERE id_product="+idProduct+" AND id_product_attribute=0";
                            MySqlCommand cmd = new MySqlCommand(updateQuantity, connection);
                            cmd.ExecuteNonQuery();
                            CloseConnectionMySQLPS();
                        }
                        else
                        {
                            MessageBox.Show("Ha habido un error al intentar conectarse a la BBDD");
                        }
                    }

                }
                button7.Text = "Actualizar Stock en Prestashop";
                button7.Enabled = true;
            }
            else if (dialogResult == DialogResult.No)
            {
                MessageBox.Show("Cancelado");
            }

        }

        private bool OpenConnectionMySQL()
        {
            try
            {
                connection2.Open();
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
                connection2.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        private bool OpenConnectionMySQLPS()
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
        private bool CloseConnectionMySQLPS()
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

        private void button3_Click(object sender, EventArgs e)
        {
            insertaArticulosSueltos m = new insertaArticulosSueltos();
            m.ShowDialog();
        }
    }
}
