namespace CSharpMySqlSample
{
    partial class rfidCheckArticles
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnFinishOrder = new System.Windows.Forms.Button();
            this.cancelOrder = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.ID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.EPC = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PC = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.NumberTagPassed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.RSSI = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CarrierFreq = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Enabled = false;
            this.dataGridView1.Location = new System.Drawing.Point(199, 46);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.dataGridView1.RowTemplate.Height = 50;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.ShowEditingIcon = false;
            this.dataGridView1.Size = new System.Drawing.Size(396, 360);
            this.dataGridView1.TabIndex = 1;
            // 
            // btnFinishOrder
            // 
            this.btnFinishOrder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFinishOrder.ForeColor = System.Drawing.Color.Green;
            this.btnFinishOrder.Location = new System.Drawing.Point(12, 46);
            this.btnFinishOrder.Name = "btnFinishOrder";
            this.btnFinishOrder.Size = new System.Drawing.Size(171, 80);
            this.btnFinishOrder.TabIndex = 2;
            this.btnFinishOrder.Text = "Finalizar Pedido";
            this.btnFinishOrder.UseVisualStyleBackColor = true;
            this.btnFinishOrder.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cancelOrder
            // 
            this.cancelOrder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.cancelOrder.Location = new System.Drawing.Point(12, 236);
            this.cancelOrder.Name = "cancelOrder";
            this.cancelOrder.Size = new System.Drawing.Size(171, 77);
            this.cancelOrder.TabIndex = 3;
            this.cancelOrder.Text = "Cancelar preparacion";
            this.cancelOrder.UseVisualStyleBackColor = true;
            this.cancelOrder.Click += new System.EventHandler(this.cancelOrder_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 150);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 65);
            this.label1.TabIndex = 4;
            this.label1.Text = "Pasa los artículos por el \r\nlector RFID e insértalo \r\nen la caja hasta que no \r\nh" +
    "aya artículos pendientes \r\nde meter..";
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ID,
            this.EPC,
            this.PC,
            this.NumberTagPassed,
            this.RSSI,
            this.CarrierFreq});
            this.listView1.Location = new System.Drawing.Point(649, 47);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(604, 359);
            this.listView1.TabIndex = 6;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // ID
            // 
            this.ID.Text = "ID";
            this.ID.Width = 73;
            // 
            // EPC
            // 
            this.EPC.Text = "EPC";
            this.EPC.Width = 267;
            // 
            // PC
            // 
            this.PC.Text = "PC";
            this.PC.Width = 45;
            // 
            // NumberTagPassed
            // 
            this.NumberTagPassed.Text = "Count Passed";
            this.NumberTagPassed.Width = 75;
            // 
            // RSSI
            // 
            this.RSSI.Text = "RSSI";
            // 
            // CarrierFreq
            // 
            this.CarrierFreq.Text = "Frequency";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(92, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 18);
            this.label3.TabIndex = 7;
            this.label3.Text = "label3";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(12, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 18);
            this.label4.TabIndex = 8;
            this.label4.Text = "PEDIDO";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 342);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(171, 64);
            this.button1.TabIndex = 9;
            this.button1.Text = "IMPRIMIR PEDIDO";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // rfidCheckArticles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1284, 418);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelOrder);
            this.Controls.Add(this.btnFinishOrder);
            this.Controls.Add(this.dataGridView1);
            this.Location = new System.Drawing.Point(416, 239);
            this.Name = "rfidCheckArticles";
            this.Text = "Pase los productos por el lector de RFID - DOCTOR PRO";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.rfidCheckArticles_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnFinishOrder;
        private System.Windows.Forms.Button cancelOrder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader ID;
        private System.Windows.Forms.ColumnHeader EPC;
        private System.Windows.Forms.ColumnHeader PC;
        private System.Windows.Forms.ColumnHeader NumberTagPassed;
        private System.Windows.Forms.ColumnHeader RSSI;
        private System.Windows.Forms.ColumnHeader CarrierFreq;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
    }
}