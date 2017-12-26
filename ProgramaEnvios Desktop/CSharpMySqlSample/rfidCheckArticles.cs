using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.Configuration;
using System.Xml.Linq;
using System.Net;
using System.Threading;
using System.Net.Mail;
using Reader;
using GridPrintPreviewLib;


namespace CSharpMySqlSample
{
    public partial class rfidCheckArticles : Form
    {
        string numPedido;
        string ConnectionString = ConfigurationSettings.AppSettings["ConnectionString"];
        string ConnectionString2 = ConfigurationSettings.AppSettings["ConnectionString2"];
        string EPCPassed = string.Empty;
        string lastEPC = string.Empty;
        MySqlConnection connection, connection2;
        MySqlDataAdapter adapter;
        DataTable DTItems;
        // El objeto Reader es el objeto que crea la conexión
        private Reader.ReaderMethod reader;
        private ReaderSetting m_curSetting = new ReaderSetting();
        private OperateTagBuffer m_curOperateTagBuffer = new OperateTagBuffer();
        private bool m_bDisplayLog = false;
        private bool m_bInventory = false;
        private int m_nTotal = 0;
        private int m_nRealRate = 20;
        private bool m_bLockTab = false;
        private InventoryBuffer m_curInventoryBuffer = new InventoryBuffer();
        private List<string> EPCsPasados = new List<string>();

        public rfidCheckArticles()
        {
            InitializeComponent();
        }

        //Get all items from database into datatable
        DataTable GetAllItems()
        {
            try
            {
                //prepare query to get all records from items table
                string query = "SELECT ps_order_detail.product_id,ps_product.reference,ps_order_detail.product_name,ps_order_detail.product_quantity FROM ps_order_detail INNER JOIN ps_product ON ps_order_detail.product_id=ps_product.id_product WHERE id_order=" + numPedido + ";";
                adapter = new MySqlDataAdapter(query, connection);
                DataSet DS = new DataSet();
                //get query results in dataset
                adapter.Fill(DS);
                DS.Tables[0].Columns.Add("UNIDADES EN CAJAS", Type.GetType("System.String"));
                //prepare adapter to run query
                return DS.Tables[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }

        public rfidCheckArticles(string number, ReaderMethod readerTransfered, string refPedido)
        {
            InitializeComponent();
            this.ControlBox = false;
            reader = readerTransfered;
            label3.Text = refPedido;
            reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;
            // Cargar con datos de la bbdd
            numPedido = number;
            //Initialize mysql connection
            connection = new MySqlConnection(ConnectionString);
            connection2 = new MySqlConnection(ConnectionString2);
            //Get all items in datatable
            DTItems = GetAllItems();

            //Fill grid with items
            dataGridView1.DataSource = DTItems;
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[1].HeaderText = "REFERENCIA";
            dataGridView1.Columns[2].HeaderText = "DESCRIPCION";
            dataGridView1.Columns[3].HeaderText = "CANTIDAD";
            dataGridView1.ClearSelection();
            refreshQuantityGridInBoxes();
            btRealTimeInventory_Click();
        }

        private void refreshQuantityGridInBoxes()
        {
            for (int h = 0; h<dataGridView1.Rows.Count; h++)
            {
                dataGridView1[4, h].Value = getItemsInBox(dataGridView1[1, h].Value.ToString());
            }
        }

        private void btRealTimeInventory_Click()
        {
            try
            {
                m_curInventoryBuffer.ClearInventoryPar();
                m_curInventoryBuffer.btRepeat = Convert.ToByte("1"); // Repeat per command
                m_curInventoryBuffer.bLoopCustomizedSession = false; // Not session ID Defined
                m_curInventoryBuffer.lAntenna.Add(0x00); // Antenna 1 (00) as source
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;

                }
                else
                {
                    m_bInventory = true;
                    m_curInventoryBuffer.bLoopInventory = true;
                }
                m_curInventoryBuffer.bLoopInventoryReal = true;
                m_curInventoryBuffer.ClearInventoryRealResult();
                m_nTotal = 0;
                byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private delegate void RunLoopInventoryUnsafe();
        private void RunLoopInventroy()
        {
            if (this.InvokeRequired)
            {
                RunLoopInventoryUnsafe InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                this.Invoke(InvokeRunLoopInventory, new object[] { });
            }
            else
            {
                //校验盘存是否所有天线均完成
                if (m_curInventoryBuffer.nIndexAntenna < m_curInventoryBuffer.lAntenna.Count - 1 || m_curInventoryBuffer.nCommond == 0)
                {
                    if (m_curInventoryBuffer.nCommond == 0)
                    {
                        m_curInventoryBuffer.nCommond = 1;

                        if (m_curInventoryBuffer.bLoopInventoryReal)
                        {
                            //m_bLockTab = true;
                            //btnInventory.Enabled = false;
                            if (m_curInventoryBuffer.bLoopCustomizedSession)//自定义Session和Inventoried Flag 
                            {
                                reader.CustomizedInventory(m_curSetting.btReadId, m_curInventoryBuffer.btSession, m_curInventoryBuffer.btTarget, m_curInventoryBuffer.btRepeat);
                            }
                            else //实时盘存
                            {
                                reader.InventoryReal(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);

                            }
                        }
                        else
                        {
                            if (m_curInventoryBuffer.bLoopInventory)
                                reader.Inventory(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);
                        }
                    }
                    else
                    {
                        m_curInventoryBuffer.nCommond = 0;
                        m_curInventoryBuffer.nIndexAntenna++;

                        byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                        reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                        m_curSetting.btWorkAntenna = btWorkAntenna;
                    }
                }
                //校验是否循环盘存
                else if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_curInventoryBuffer.nIndexAntenna = 0;
                    m_curInventoryBuffer.nCommond = 0;

                    byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                    reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                    m_curSetting.btWorkAntenna = btWorkAntenna;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // EXTRAER Nº REFERENCIA
            string endOrder = "Si";
            // Si el pedido no está finalizado abrir un MessageBox que indique si está seguro.
            if (getQuantityRestToFinish() != 0)
            {
                btRealTimeInventory_Click();
                DialogResult decision = MessageBox.Show("¿Estas seguro de que quieres finalizar el pedido sin haberlo acabado?", "PEDIDO INCOMPLETO", MessageBoxButtons.YesNo);
                if (decision == DialogResult.Yes)
                {
                    DialogResult decision2 = MessageBox.Show("¿Estas realmente seguro de que no hay material en las cubetas? ¿Enviar pedido incompleto?", "PEDIDO INCOMPLETO", MessageBoxButtons.YesNo);
                    if (decision2 == DialogResult.Yes)
                    {
                        // Insert into a list products and ID with not 0 in quantity.
                        List<string> itemsToFinish = getItemsRestToFinish();
                        List<string> idItemsToFinish = getIdRestToFinish();
                        List<string> quantityItemsToFinish = getQuantityEachProduct();
                        // Get firstname, secondname and email with Reference
                        List<string> personalDate = getNameAndEmail();
                        string id_order = getIdOrder(label3.Text);
                        // Put item at Alert for restock
                        putItemAtAlertList(personalDate, idItemsToFinish);
                        // Send email with Miss Quantity Order
                        sendEmailWithMissQuantityOrder(personalDate, itemsToFinish);
                        // Check if have stock in boxes and techman forget to insert. Then send an email to administrator.
                        //alertAdministrator();
                        // Insert into BBDD items not sent to have list of items not sent
                        putItemAtNotSentBBDD(id_order, idItemsToFinish, quantityItemsToFinish);
                        // Set off RFID 
                        MessageBox.Show("Pedido incompleto, se ha enviado un email al cliente.", "Pedido cerrado");
                    }
                    else
                    {
                        endOrder = "No";
                        btRealTimeInventory_Click();
                    }
                }
                else
                {
                    endOrder = "No";
                    btRealTimeInventory_Click();
                }
            }
            if (endOrder == "Si")
            {
                // LLAMAR A FUNCION:
                // Aquí marcamos el pedido como finalizado en Prestashop (estado listo para ser enviado)
                //updatePrestashopStatus(label3.Text);
                // Imprimimos una etiqueta con el destino en directorio que escuche Bartender
                // Marcamos todos los EPCsPasados con el nº REFERENCIA del pedido y uso
                updateEPCs(label3.Text);
                // Close Windows
                this.Close();
                // FIN LLAMAR A FUNCION
            }

        }

        private void updateEPCs(string referencia)
        {   // Update State of EPCs
            string queryEPCs = string.Empty;
            foreach (string EPC in EPCsPasados)
            {
                if (queryEPCs != string.Empty)
                {
                    queryEPCs = queryEPCs + " OR EPC='" + EPC + "'";
                }
                else
                {
                    queryEPCs = "EPC='" + EPC + "'";
                }
            }
            // FALTA METER PRECIO_VENTA
            string query = "UPDATE proxPedidosContenido SET uso='online', fechaSalida=NOW(), numAsociado='" + referencia + "' WHERE " + queryEPCs;
            if (OpenConnectionMySQL())
            {
                //MySqlCommand cmd = new MySqlCommand(query, connection2);
                //cmd.ExecuteNonQuery();
                CloseConnectionMySQL();
            }
        }


        private void updatePrestashopStatus(string Referencia)
        {   // Update State of Order in PS
            string query = "UPDATE ps_orders SET current_state=3 WHERE reference='" + Referencia + "'";
            if (OpenConnectionMySQLPS())
            {
                //MySqlCommand cmd = new MySqlCommand(query, connection);
                //cmd.ExecuteNonQuery();
                CloseConnectionMySQLPS();
            }
        }

        private void cancelOrder_Click(object sender, EventArgs e)
        {
            DialogResult decision = MessageBox.Show("¡Todo lo que hayas metido a la caja deberás devolverlo a las gavetas!,¿Estas seguro que deseas cancelar el pedido?", "CANCELAR PEDIDO", MessageBoxButtons.YesNo);
            if (decision == DialogResult.Yes)
            {
                btRealTimeInventory_Click();
                MessageBox.Show("Preparación de pedido cancelada", "Preparación cancelada");
                this.Close();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void rfidCheckArticles_Load(object sender, EventArgs e)
        {

        }

        private void ReceiveData(byte[] btAryReceiveData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);

                //WriteLog(lrtxtDataTran, strLog, 1);
            }
        }

        private void SendData(byte[] btArySendData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);

                //WriteLog(lrtxtDataTran, strLog, 0);
            }
        }
        private void AnalyData(Reader.MessageTran msgTran)
        {
            if (msgTran.PacketType != 0xA0)
            {
                return;
            }
            switch (msgTran.Cmd)
            {
                // ONLY FOR SETUP
                /*case 0x69:
                    ProcessSetProfile(msgTran);
                    break;
                case 0x6A:
                    ProcessGetProfile(msgTran);
                    break;
                case 0x71:
                    ProcessSetUartBaudrate(msgTran);
                    break;
                case 0x72:
                    ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x73:
                    ProcessSetReadAddress(msgTran);
                    break;
                case 0x75:
                    ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x77:
                    ProcessGetOutputPower(msgTran);
                    break;
                case 0x78:
                    ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x79:
                    ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x7A:
                    ProcessSetBeeperMode(msgTran);
                    break;
                case 0x7B:
                    ProcessGetReaderTemperature(msgTran);
                    break;
                case 0x7C:
                    ProcessSetDrmMode(msgTran);
                    break;
                case 0x7D:
                    ProcessGetDrmMode(msgTran);
                    break;
                case 0x7E:
                    ProcessGetImpedanceMatch(msgTran);
                    break;
                case 0x60:
                    ProcessReadGpioValue(msgTran);
                    break;
                case 0x61:
                    ProcessWriteGpioValue(msgTran);
                    break;
                case 0x62:
                    ProcessSetAntDetector(msgTran);
                    break;
                case 0x63:
                    ProcessGetAntDetector(msgTran);
                    break;
                case 0x67:
                    ProcessSetReaderIdentifier(msgTran);
                    break;
                case 0x68:
                    ProcessGetReaderIdentifier(msgTran);
                    break;
                case 0x82:
                    ProcessWriteTag(msgTran);
                    break;
                case 0x83:
                    ProcessLockTag(msgTran);
                    break;
                case 0x84:
                    ProcessKillTag(msgTran);
                    break;
                case 0x8D:
                    ProcessSetMonzaStatus(msgTran);
                    break;
                case 0x8E:
                    ProcessGetMonzaStatus(msgTran);
                    break;
                case 0x90:
                    ProcessGetInventoryBuffer(msgTran);
                    break;
                case 0x91:
                    ProcessGetAndResetInventoryBuffer(msgTran);
                    break;
                case 0x92:
                    ProcessGetInventoryBufferTagCount(msgTran);
                    break;
                case 0x93:
                    ProcessResetInventoryBuffer(msgTran);
                    break;
                case 0xb2:
                    ProcessWriteTagISO18000(msgTran);
                    break;
                case 0xb3:
                    ProcessLockTagISO18000(msgTran);
                    break;
                 case 0x85:
                    ProcessSetAccessEpcMatch(msgTran);
                    break;
                case 0x86:
                    ProcessGetAccessEpcMatch(msgTran);
                    break;
                case 0x80:
                    ProcessInventory(msgTran);
                    break;
                case 0x81:
                    ProcessReadTag(msgTran);
                    break;
                  case 0x8A:
                    ProcessFastSwitch(msgTran);
                    break; 
                case 0xb0:
                    ProcessInventoryISO18000(msgTran);
                    break;
                case 0xb1:
                    ProcessReadTagISO18000(msgTran);
                    break;
                case 0xb4:
                    ProcessQueryISO18000(msgTran);
                    break;   
                 */
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x79:
                    ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                default:
                    break;
            }
        }
        private void ProcessInventoryReal(Reader.MessageTran msgTran)
        {
            string strCmd = "";
            if (msgTran.Cmd == 0x89)
            {
                strCmd = "Real time mode inventory ";
            }
            if (msgTran.Cmd == 0x8B)
            {
                strCmd = "Customized Session and Inventoried Flag inventory ";
            }
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "failed, due to： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
                RefreshInventoryReal(0x00);
                RunLoopInventroy();
            }
            else if (msgTran.AryData.Length == 7)
            {
                m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                //WriteLog(lrtxtLog, strCmd, 0);
                RefreshInventoryReal(0x01);
                RunLoopInventroy();
            }
            else
            {
                m_nTotal++;
                int nLength = msgTran.AryData.Length;
                int nEpcLength = nLength - 4;
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                string strRSSI = msgTran.AryData[nLength - 1].ToString();
                SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 1]));
                byte btTemp = msgTran.AryData[0];
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                m_curInventoryBuffer.nCurrentAnt = btAntId;
                string strAntId = btAntId.ToString();
                EPCPassed = strEPC;

                byte btFreq = (byte)(btTemp >> 2);
                string strFreq = GetFreqString(btFreq);

                DataRow[] drs = m_curInventoryBuffer.dtTagTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                if (drs.Length == 0)
                {
                    DataRow row1 = m_curInventoryBuffer.dtTagTable.NewRow();
                    row1[0] = strPC;
                    row1[2] = strEPC;
                    row1[4] = strRSSI;
                    row1[5] = "1";
                    row1[6] = strFreq;

                    m_curInventoryBuffer.dtTagTable.Rows.Add(row1);
                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }
                else
                {
                    foreach (DataRow dr in drs)
                    {
                        dr.BeginEdit();

                        dr[4] = strRSSI;
                        dr[5] = (Convert.ToInt32(dr[5]) + 1).ToString();
                        dr[6] = strFreq;

                        dr.EndEdit();
                    }
                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RefreshInventoryReal(0x89);
            }
        }
        private delegate void RefreshInventoryRealUnsafe(byte btCmd);
        private void RefreshInventoryReal(byte btCmd)
        {
            //Create a list to store the result
            List<string>[] list = new List<string>[3];
            Int32 numArticulosRestantes = 1; // 1 = not finished
            list[0] = new List<string>();
            if (this.InvokeRequired)
            {
                RefreshInventoryRealUnsafe InvokeRefresh = new RefreshInventoryRealUnsafe(RefreshInventoryReal);
                this.Invoke(InvokeRefresh, new object[] { btCmd });
            }
            else
            {
                switch (btCmd)
                {
                    case 0x89:
                    case 0x8B:
                        {
                            int nTagCount = m_curInventoryBuffer.dtTagTable.Rows.Count;
                            int nTotalRead = m_nTotal;// m_curInventoryBuffer.dtTagDetailTable.Rows.Count;
                            TimeSpan ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                            int nTotalTime = ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds;
                            int nCaculatedReadRate = 0;
                            int nCommandDuation = 0;

                            if (m_curInventoryBuffer.nReadRate == 0) //读写器没有返回速度前软件测速度
                            {
                                if (nTotalTime > 0)
                                {
                                    nCaculatedReadRate = (nTotalRead * 1000 / nTotalTime);
                                }
                            }
                            else
                            {
                                nCommandDuation = m_curInventoryBuffer.nDataCount * 1000 / m_curInventoryBuffer.nReadRate;
                                nCaculatedReadRate = m_curInventoryBuffer.nReadRate;
                            }

                            int nEpcCount = 0;
                            int nEpcLength = m_curInventoryBuffer.dtTagTable.Rows.Count;

                            // Aqui hacemos lo que sea necesario con el EPC y el listado.
                            if (nEpcCount < nEpcLength)
                            {
                                // Last DataRow added
                                DataRow row = m_curInventoryBuffer.dtTagTable.Rows[nEpcLength - 1];
                                // Select Reference from Tag and compare with order. If exist on 
                                string EPC = EPCPassed.Replace(" ", "");
                                string query2 = "SELECT referencia FROM proxPedidosContenido WHERE EPC='" + EPC + "' and uso='' and fechaSalida IS NULL and numAsociado='' and defectuosa='No' and etiquetaImprimida='Si'";
                                if (OpenConnectionMySQL())
                                {
                                    MySqlCommand cmd = new MySqlCommand(query2, connection2);
                                    MySqlDataReader dataReader = cmd.ExecuteReader();
                                    while (dataReader.Read())
                                    {
                                        list[0].Add(dataReader["referencia"] + "");
                                    }
                                    dataReader.Close();
                                    CloseConnectionMySQL();
                                }
                                // Assign reference of EPC
                                if (lastEPC != EPC)
                                {
                                    if (list[0].Count > 0)
                                    {
                                        string referencia = list[0][0].ToString();
                                        if (!EPCsPasados.Contains(EPC))
                                        {
                                            numArticulosRestantes = 0;
                                            for (int h = 0; h < dataGridView1.Rows.Count; h++)
                                            {
                                                if (dataGridView1[1, h].Value != null && referencia == dataGridView1[1, h].Value.ToString())
                                                {
                                                    if (Convert.ToInt32(dataGridView1[3, h].Value) == 0)
                                                    {
                                                        MessageBox.Show("NO ES NECESARIO MAS CANTIDAD DE ESTE PRODUCTO");
                                                        lastEPC = EPC;
                                                    }
                                                    else
                                                    {
                                                        //Hacemos lo que haya que hacer en datagridView para restar el producto
                                                        dataGridView1[3, h].Value = (Convert.ToInt32(dataGridView1[3, h].Value) - 1).ToString();
                                                        dataGridView1[4, h].Value = (Convert.ToInt32(dataGridView1[4, h].Value) - 1).ToString();
                                                        if (Convert.ToInt32(dataGridView1[3, h].Value) == 0)
                                                        {
                                                            dataGridView1.Rows[h].DefaultCellStyle.BackColor = Color.Green;
                                                        }
                                                        EPCsPasados.Add(EPC);
                                                        lastEPC = EPC;
                                                    }
                                                }
                                                numArticulosRestantes = numArticulosRestantes + Convert.ToInt32(dataGridView1[3, h].Value);
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("ESTE PRODUCTO YA ESTA EN ESTE PAQUETE");
                                            lastEPC = EPC;
                                        }
                                    }
                                    else
                                    {
                                        MessageBox.Show("ARTICULO NO PEDIDO EN ESTE PEDIDO");
                                        lastEPC = EPC;
                                    }
                                }//close if (if the product passed is the same)
                                //if (dataGridView1)
                                ListViewItem item = new ListViewItem();
                                if (!listView1.Items.ContainsKey(row[2].ToString()))
                                {
                                    item.Name = row[2].ToString();
                                    item.Text = (nEpcCount + 1).ToString();
                                    item.SubItems.Add(row[2].ToString());
                                    item.SubItems.Add(row[0].ToString());
                                    item.SubItems.Add(row[5].ToString());
                                    item.SubItems.Add((Convert.ToInt32(row[4]) - 129).ToString() + "dBm");
                                    item.SubItems.Add(row[6].ToString());
                                    listView1.Items.Add(item);
                                    listView1.Items[nEpcCount].EnsureVisible();
                                }
                            }

                            //更新列表中读取的次数
                            if (m_nTotal % m_nRealRate == 1)
                            {
                                int nIndex = 0;
                                foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                                {
                                    ListViewItem item;
                                    item = listView1.Items[nIndex];
                                    item.SubItems[3].Text = row[5].ToString();
                                    item.SubItems[4].Text = (Convert.ToInt32(row[4]) - 129).ToString() + "dBm";
                                    item.SubItems[5].Text = row[6].ToString();

                                    nIndex++;
                                }
                            }
                            if (numArticulosRestantes == 0)
                            {
                                MessageBox.Show("PEDIDO COMPLETADO");
                                // Hacer lo que sea necesario cuando se completo el pedido (un nuevo formulario o algo).
                            }
                        }
                        break;


                    case 0x00:
                    case 0x01:
                        {
                            m_bLockTab = false;


                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void sendEmailWithMissQuantityOrder(List<string> personalDate,List<string> itemsMissing){
            string to = personalDate[2].ToString();
            string from = "contacto@proconsolas.es";
            string subject = "Envío incompleto";
            string message = "Hola" + personalDate[0].ToString()+" "+ personalDate[1].ToString()+"<br>Los siguientes artículos no han podido ser añadidos por no existencias en el almacen:";
            foreach(string itemMiss in itemsMissing)
            {
                message = message + "<br> -" + itemMiss;
            }
            SmtpClient client = new SmtpClient("de80.mihosting.net", 26);
            MailMessage mail = new MailMessage(from, to, subject, message);
            mail.IsBodyHtml = true;
            // Add credentials if the SMTP server requires them.
            client.UseDefaultCredentials = false;
            NetworkCredential myCreds = new NetworkCredential("contacto@proconsolas.es", "Elpidi0e78cgbu");
            try
            {
                client.Credentials = myCreds;
                client.EnableSsl = false;
                client.Send(mail);
                Console.WriteLine("Goodbye.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception is raised. ");
                Console.WriteLine("Message: {0} ", e.Message);
            }
        }

        private Int32 getItemsInBox(string reference)
        {
            Int32 stockInBoxes=0;
            if (OpenConnectionMySQL())
            {
                // Select number of EPC available (products in box)
                string checkStock = "SELECT count(*) FROM proxPedidosContenido WHERE referencia='" + reference + "' and uso='' and etiquetaImprimida='Si'";
                MySqlCommand cmd = new MySqlCommand(checkStock, connection2);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    stockInBoxes = Convert.ToInt32(dataReader.GetValue(0));
                }
                dataReader.Close();
                CloseConnectionMySQL();
            }
            return stockInBoxes;
        }

        private Int32 getItemsSoldAndNotBoxed(string reference)
        {
            List<string> ordersWithReferenceAndNotBoxed = new List<string>();
            Int32 totalStockOfArticleSelled = 0;
            // Get all orders without boxed and paid
            if (OpenConnectionMySQLPS())
            {
                // Select all orders with status paid and not boxed.
                string checkOrdersPaid = "SELECT id_order FROM ps_orders WHERE current_state = 2";
                MySqlCommand cmd = new MySqlCommand(checkOrdersPaid, connection2);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    ordersWithReferenceAndNotBoxed.Add(dataReader.GetValue(0).ToString());
                }
                dataReader.Close();
                CloseConnectionMySQLPS();
            }
            if (ordersWithReferenceAndNotBoxed.Count > 0)
            {
                string ordersPaidTextSQL = string.Empty;
                // Get all orders and save in array
                foreach (string order in ordersWithReferenceAndNotBoxed)
                {
                    ordersPaidTextSQL = ordersPaidTextSQL + " AND id_order =" + order;
                }
                if (OpenConnectionMySQLPS())
                {
                    // Get Products selled in all ps_order_detail with reference X and id_order in state paid and not boxed
                    string getQuantityReferencePaid = "SELECT SUM(product_quantity) FROM ps_order_detail WHERE product_reference='" + reference + "'" + ordersPaidTextSQL;
                    MySqlCommand cmd = new MySqlCommand(getQuantityReferencePaid, connection2);
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        totalStockOfArticleSelled = Convert.ToInt32(dataReader.GetValue(0));
                    }
                    dataReader.Close();
                    CloseConnectionMySQLPS();

                }
            }
            return totalStockOfArticleSelled;
        }
        private Int32 getStockRealInWarehouse(string reference) // Get real stock available to show in grid
        {
            Int32 totalAmountOfStock = getItemsInBox(reference) - getItemsSoldAndNotBoxed(reference);

            return totalAmountOfStock;
        }
        private void putItemAtAlertList(List<string> customer,List<string> idItemsNotInserted)
        {
            foreach (string id in idItemsNotInserted)
            {
                string query = "INSERT INTO ps_mailalert_customer_oos(id_customer, customer_email,id_product,id_product_attribute,id_shop,id_lang) VALUES('"+customer[3].ToString()+
                    "','"+customer[2].ToString()+"',"+id.ToString()+",0,1,1)";
                if (OpenConnectionMySQLPS())
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                    CloseConnectionMySQLPS();
                }
            }

        }
        private void putItemAtNotSentBBDD(string id_order,List<string> idItem, List<string> quantityItem)
        {
            string query = string.Empty;
            for(int x=0;x < idItem.Count; x++)
            {
                query = "INSERT INTO ps_products_canceled(id_order, id_product, quantity, date_canceled) VALUES(" +id_order +"," + idItem[x] + "," + quantityItem[x] + ",NOW())";
                if (OpenConnectionMySQLPS())
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                    CloseConnectionMySQLPS();
                }
            }
            
        }
        
        private Int32 getQuantityRestToFinish()
        {
            // Give us the total number article rest to insert in the box
            Int32 numArticulosRestantes = 0;
            for (int h = 0; h<dataGridView1.Rows.Count; h++)
            {
                    numArticulosRestantes = numArticulosRestantes + Convert.ToInt32(dataGridView1[3, h].Value);
                   
            }
            return numArticulosRestantes;
        }

        private List<string> getNameAndEmail()
        {
            string query = "SELECT ps_customer.firstname,ps_customer.lastname,ps_customer.email,ps_customer.id_customer FROM ps_customer INNER JOIN ps_orders ON ps_customer.id_customer = ps_orders.id_customer WHERE ps_orders.reference ='" + label3.Text + "'";
            List<string> personalDate = new List<string>();
            if (OpenConnectionMySQLPS())
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    personalDate.Add(reader.GetString(0));
                    personalDate.Add(reader.GetString(1));
                    personalDate.Add(reader.GetString(2));
                    personalDate.Add(reader.GetString(3));
                }
                CloseConnectionMySQLPS();
            }

            // Return list with Name(0) Second Name(1) and email(2)
            return personalDate;
        }

        private string getIdOrder(string reference)
        {
            string query = "SELECT id_order FROM ps_orders WHERE reference='" + reference + "'";
            string id_order = string.Empty;
            if (OpenConnectionMySQLPS())
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    id_order = reader.GetString(0);
                }
                CloseConnectionMySQLPS();
            }

            // Return list with Name(0) Second Name(1) and email(2)
            return id_order;
        }

        private List<string> getItemsRestToFinish()
        {
            // Give us the articles rest to insert in the box
            List<string> itemsNotSent = new List<string>();

            for (int h = 0; h < dataGridView1.Rows.Count; h++)
            {
                if (Convert.ToInt32(dataGridView1[3, h].Value) != 0) // If is not with quantity 0, its a fault in the order.
                {
                    itemsNotSent.Add(dataGridView1[2, h].Value.ToString());
                }
            }
            return itemsNotSent;
        }

        private List<string> getIdRestToFinish()
        {
            // Give us the ID articles rest to insert in the box
            List<string> itemsIDNotSent = new List<string>();

            for (int h = 0; h < dataGridView1.Rows.Count; h++)
            {
                if (Convert.ToInt32(dataGridView1[3, h].Value) != 0) // If is not with quantity 0, its a fault in the order.
                {
                    itemsIDNotSent.Add(dataGridView1[0, h].Value.ToString());
                }
            }
            return itemsIDNotSent;
        }

        
        private List<string> getQuantityEachProduct()
        {
            // Give us the number of articles rest to insert in the box by each
            List<string> itemsQuantityNotSent = new List<string>();

            for (int h = 0; h < dataGridView1.Rows.Count; h++)
            {
                if (Convert.ToInt32(dataGridView1[3, h].Value) != 0) // If is not with quantity 0, its a fault in the order.
                {
                    itemsQuantityNotSent.Add(dataGridView1[3, h].Value.ToString());
                }
            }
            return itemsQuantityNotSent;
        }

        private void SetMaxMinRSSI(int nRSSI)
        {
            if (m_curInventoryBuffer.nMaxRSSI < nRSSI)
            {
                m_curInventoryBuffer.nMaxRSSI = nRSSI;
            }

            if (m_curInventoryBuffer.nMinRSSI == 0)
            {
                m_curInventoryBuffer.nMinRSSI = nRSSI;
            }
            else if (m_curInventoryBuffer.nMinRSSI > nRSSI)
            {
                m_curInventoryBuffer.nMinRSSI = nRSSI;
            }
        }
        private string GetFreqString(byte btFreq)
        {
            string strFreq = string.Empty;

            if (m_curSetting.btRegion == 4)
            {
                float nExtraFrequency = btFreq * m_curSetting.btUserDefineFrequencyInterval * 10;
                float nstartFrequency = ((float)m_curSetting.nUserDefineStartFrequency) / 1000;
                float nStart = nstartFrequency + nExtraFrequency / 1000;
                string strTemp = nStart.ToString("0.000");
                return strTemp;
            }
            else
            {
                if (btFreq < 0x07)
                {
                    float nStart = 865.00f + Convert.ToInt32(btFreq) * 0.5f;

                    string strTemp = nStart.ToString("0.00");

                    return strTemp;
                }
                else
                {
                    float nStart = 902.00f + (Convert.ToInt32(btFreq) - 7) * 0.5f;

                    string strTemp = nStart.ToString("0.00");

                    return strTemp;
                }
            }
        }
        private delegate void WriteLogUnSafe(CustomControl.LogRichTextBox logRichTxt, string strLog, int nType);
        private void WriteLog(CustomControl.LogRichTextBox logRichTxt, string strLog, int nType)
        {
            if (this.InvokeRequired)
            {
                WriteLogUnSafe InvokeWriteLog = new WriteLogUnSafe(WriteLog);
                this.Invoke(InvokeWriteLog, new object[] { logRichTxt, strLog, nType });
            }
            else
            {
                if (nType == 0)
                {
                    logRichTxt.AppendTextEx(strLog, Color.Indigo);
                }
                else
                {
                    logRichTxt.AppendTextEx(strLog, Color.Red);
                }

                /*if (ckClearOperationRec.Checked)
                {
                    if (logRichTxt.Lines.Length > 50)
                    {
                        logRichTxt.Clear();
                    }
                }*/

            logRichTxt.Select(logRichTxt.TextLength, 0);
                logRichTxt.ScrollToCaret();
            }
        }
        private void ProcessSetWorkAntenna(Reader.MessageTran msgTran)
        {
            int intCurrentAnt = 0;
            intCurrentAnt = m_curSetting.btWorkAntenna + 1;
            string strCmd = "Successfully set working antenna, current working antenna : Ant " + intCurrentAnt.ToString();

            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    //校验是否盘存操作
                    if (m_bInventory)
                    {
                        RunLoopInventroy();
                    }
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "Unknown error";
            }

            string strLog = strCmd + "failed , due to: " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);

            if (m_bInventory)
            {
                m_curInventoryBuffer.nCommond = 1;
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RunLoopInventroy();
            }
        }
        private void ProcessGetFrequencyRegion(Reader.MessageTran msgTran)
        {
            string strCmd = "Get RF spectrum ";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 3)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btRegion = msgTran.AryData[0];
                m_curSetting.btFrequencyStart = msgTran.AryData[1];
                m_curSetting.btFrequencyEnd = msgTran.AryData[2];

                //RefreshReadSetting(0x79);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 6)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btRegion = msgTran.AryData[0];
                m_curSetting.btUserDefineFrequencyInterval = msgTran.AryData[1];
                m_curSetting.btUserDefineChannelQuantity = msgTran.AryData[2];
                m_curSetting.nUserDefineStartFrequency = msgTran.AryData[3] * 256 * 256 + msgTran.AryData[4] * 256 + msgTran.AryData[5];
                //RefreshReadSetting(0x79);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;


            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "Unknown error";
            }

            string strLog = strCmd + "failed , due to: " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
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

        private void button1_Click(object sender, EventArgs e)
        {
            /*GridPrintDocument doc = new GridPrintDocument(this.dataGridView1, this.dataGridView1.Font, true);
            doc.DocumentName = "Preview Test";
            doc.DrawCellBox = true;
            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.ClientSize = new Size(600, 500);
            printPreviewDialog.Location = new Point(29, 29);
            printPreviewDialog.Name = "Print Preview Dialog";
            printPreviewDialog.UseAntiAlias = true;
            printPreviewDialog.Document = doc;
            printPreviewDialog.ShowDialog();
            doc.Dispose();
            doc = null;*/
            GridPrintDocument doc = new GridPrintDocument(this.dataGridView1, this.dataGridView1.Font, true);
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
            doc = null;

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
    }
}
