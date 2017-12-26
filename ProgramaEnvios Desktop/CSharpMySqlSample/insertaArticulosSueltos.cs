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
using System.IO;

namespace CSharpMySqlSample
{
    public partial class insertaArticulosSueltos : Form
    {
        string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString"];
        string ConnectionString2 = ConfigurationSettings.AppSettings["ConnectionString2"];
        MySqlConnection connection;
        MySqlConnection connection2;
        string fileName = "archivoCSV";
        string extension = ".csv";
        string path = @"Z:\";
        string targetPath = @"Z:\IMPRIMIR\";
        List<string> CSVParaExportarAArchivo = new List<string>();
        public insertaArticulosSueltos()
        {
            InitializeComponent();
            connection = new MySqlConnection(ConnectionString);
            connection2 = new MySqlConnection(ConnectionString2);
            string getAllProduct = "SELECT ps_product.reference,'-',ps_product_lang.name FROM ps_product INNER JOIN ps_product_lang ON ps_product.id_product=ps_product_lang.id_product WHERE id_lang=1";
            if (OpenConnectionMySQLPS())
            {
                using (MySqlCommand command = new MySqlCommand(getAllProduct, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBox1.Items.Add(reader.GetString(0)+ " "+reader.GetString(1)+" "+ reader.GetString(2));
                        }
                    }
                }
                CloseConnectionMySQLPS();
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
        private void button1_Click(object sender, EventArgs e)
        {
            if (CSVParaExportarAArchivo.Count > 0)
            {
                MessageBox.Show("Recuerda actualizar stock de prestashop si todo es correcto");
            }
            this.Close();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Hacer inserción EPC en el sistema. (proxPedidosContenido) (MessageBox)
            //EPC = numSerie, fecha(fecha actual), proveedor (99 a mano)
            //CREATE EPC
            button2.Text = "Generando impresion...";
            button2.Enabled = false;
            string EPC = "";
            DateTime now = DateTime.Now;
            string dayToday = now.ToString("dd");
            string monthToday = now.ToString("MM");
            string yearToday = now.ToString("yy");
            string fechaEPC = dayToday + monthToday + yearToday;
            string referenciaEPC = comboBox1.Text;
            string referencia = "";
            string proveedor = "";
            if (textBox3.Text == "")
            {
                proveedor = "99";
            }
            else
            {
                if (Convert.ToInt16(textBox3.Text)>=0 && (Convert.ToInt16(textBox3.Text)) <= 99)
                {
                    proveedor = textBox3.Text;

                }
                else
                {
                    proveedor = "99";
                }
            }
            DialogResult dialogResult2 = MessageBox.Show("¿Estas seguro que esta todo bien?", "ADVERTENCIA", MessageBoxButtons.YesNo);
            if (dialogResult2 == DialogResult.Yes)
            {
             
                proveedor = proveedor.PadLeft(2, '0');
                // Separamos la referencia
                referencia = referenciaEPC.Split(' ')[0];
                referenciaEPC = referenciaEPC.Split(' ')[0];
                // Pasamos a Hexadecimal
                referenciaEPC = Convert.ToInt16(referenciaEPC).ToString("X");
                // Rellenamos con ceros hasta 6 digitos
                referenciaEPC = referenciaEPC.PadLeft(6, '0');
                // Cogemos el numero de serie actual en la bbdd.
                int value;
                if (int.TryParse(textBox1.Text, out value)){

                    if (Convert.ToInt32(textBox1.Text) > 0 && (Convert.ToInt32(textBox1.Text) <= 99))
                    {

                        for (int a = 0; a < Convert.ToInt32(textBox1.Text); a++)
                        {
                            string ultimoNSerieQuery = "SELECT `auto_increment` FROM INFORMATION_SCHEMA.TABLES WHERE table_name = 'proxPedidosContenido'";
                            string proxID = "";
                            if (OpenConnectionMySQL())
                            {
                                using (MySqlCommand command = new MySqlCommand(ultimoNSerieQuery, connection2))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            proxID = reader.GetString(0).ToString();
                                        }
                                    }
                                }
                                CloseConnectionMySQL();
                            }
                            else
                            {
                                MessageBox.Show("Error en la conexion");
                            }
                            // Pasamos a Hexadecimal
                            string nSerieEPC = Convert.ToInt32(proxID).ToString("X");
                            // Rellenamos con ceros hasta 10 digitos
                            nSerieEPC = nSerieEPC.PadLeft(10, '0');
                            EPC = referenciaEPC + fechaEPC + proveedor + nSerieEPC;
                            EPC = EPC.ToLower();
                            //MessageBox.Show(EPC);
                            //CREATE QUERY
                            string precio = "";
                            if (textBox2.Text == "")
                            {
                                precio = "0";
                            }
                            else
                            {
                                precio = textBox2.Text;
                            }
                            string queryEPC = "INSERT INTO proxPedidosContenido(referencia,EPC,etiquetaImprimida,precio) VALUES('" + referencia + "','" + EPC + "','Si'," + precio + ")";
                            if (OpenConnectionMySQL())
                            {
                                MySqlCommand cmd = new MySqlCommand(queryEPC, connection2);
                                cmd.ExecuteNonQuery();
                                CloseConnectionMySQL();
                                //MessageBox.Show("Producto añadido");
                                CSVParaExportarAArchivo.Add(referencia + ";" + EPC+";" + extraeDescripciónDeMySQL(referencia) + ";" + proxID); //proxID es el numero de serie
                                CloseConnectionMySQL();
                            }
                            else
                            {
                                MessageBox.Show("Ha habido un error al intentar conectarse a la BBDD");
                            }
                        }
                        if (CSVParaExportarAArchivo.Count>0)
                        {
                            creaCSVEnDirectorioVirtual(CSVParaExportarAArchivo);
                            CSVParaExportarAArchivo.Clear();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Inserta un numero entre 1 y 99 en etiquetas a imprimir");

                    }
                }
                else
                {
                    MessageBox.Show("Inserta numeros.");

                }
            }
            else if (dialogResult2 == DialogResult.No)
            {
                MessageBox.Show("Cancelado");
            }
            button2.Text = "Generar etiquetas e imprimir";
            button2.Enabled = true;
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text != "" && textBox1.Text != "")
            {
                button2.Enabled = true;
            }
            else
            {
                button2.Enabled = false;
            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text != "" && textBox1.Text != "")
            {
                button2.Enabled = true;
            }
            else
            {
                button2.Enabled = false;
            }
        }

        private void creaCSVEnDirectorioVirtual(List<string> cadenaEPCyReferencia)
        {
            string fechaActual = DateTime.Now.ToString("hh-mm-ss tt");
            using (var file = File.CreateText(path+fileName+fechaActual+extension))
            {
                foreach (var arr in cadenaEPCyReferencia)
                {
                    if (String.IsNullOrEmpty(arr)) continue;
                    file.Write(arr);
                    file.WriteLine();
                }
            }
            System.IO.File.Move(path + fileName + fechaActual + extension, targetPath + fileName + fechaActual + extension);
        }


        private string extraeDescripciónDeMySQL(string referenciaDeProducto)
        {
            string querySacaDescripción = "SELECT ps_product_lang.name FROM ps_product_lang INNER JOIN ps_product ON ps_product_lang.id_product=ps_product.id_product WHERE ps_product_lang.id_lang=1 AND ps_product.reference='" + referenciaDeProducto + "'";
            string nombreDelProducto = "";
            connection = new MySqlConnection(ConnectionString);
            if (OpenConnectionMySQLPS())
            {
                using (MySqlCommand command = new MySqlCommand(querySacaDescripción, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nombreDelProducto = reader.GetString(0).ToString();
                        }
                    }
                }
                CloseConnectionMySQLPS();
            }
            return nombreDelProducto;
        }
    }
}
