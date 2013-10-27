namespace EntityOHSamples
{
    partial class MainForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ExcelRadioButton = new System.Windows.Forms.RadioButton();
            this.AccessRadioButton = new System.Windows.Forms.RadioButton();
            this.ExpressRadioButton = new System.Windows.Forms.RadioButton();
            this.CompactRadioButton = new System.Windows.Forms.RadioButton();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(292, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(177, 66);
            this.button1.TabIndex = 0;
            this.button1.Text = "Get Tables";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(18, 84);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(617, 307);
            this.dataGridView1.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ExcelRadioButton);
            this.panel1.Controls.Add(this.AccessRadioButton);
            this.panel1.Controls.Add(this.ExpressRadioButton);
            this.panel1.Controls.Add(this.CompactRadioButton);
            this.panel1.Location = new System.Drawing.Point(18, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(268, 66);
            this.panel1.TabIndex = 2;
            // 
            // ExcelRadioButton
            // 
            this.ExcelRadioButton.AutoSize = true;
            this.ExcelRadioButton.Location = new System.Drawing.Point(4, 27);
            this.ExcelRadioButton.Name = "ExcelRadioButton";
            this.ExcelRadioButton.Size = new System.Drawing.Size(50, 17);
            this.ExcelRadioButton.TabIndex = 3;
            this.ExcelRadioButton.TabStop = true;
            this.ExcelRadioButton.Text = "Excel";
            this.ExcelRadioButton.UseVisualStyleBackColor = true;
            // 
            // AccessRadioButton
            // 
            this.AccessRadioButton.AutoSize = true;
            this.AccessRadioButton.Location = new System.Drawing.Point(179, 3);
            this.AccessRadioButton.Name = "AccessRadioButton";
            this.AccessRadioButton.Size = new System.Drawing.Size(58, 17);
            this.AccessRadioButton.TabIndex = 2;
            this.AccessRadioButton.TabStop = true;
            this.AccessRadioButton.Text = "Access";
            this.AccessRadioButton.UseVisualStyleBackColor = true;
            // 
            // ExpressRadioButton
            // 
            this.ExpressRadioButton.AutoSize = true;
            this.ExpressRadioButton.Location = new System.Drawing.Point(93, 3);
            this.ExpressRadioButton.Name = "ExpressRadioButton";
            this.ExpressRadioButton.Size = new System.Drawing.Size(80, 17);
            this.ExpressRadioButton.TabIndex = 1;
            this.ExpressRadioButton.TabStop = true;
            this.ExpressRadioButton.Text = "Sql Express";
            this.ExpressRadioButton.UseVisualStyleBackColor = true;
            // 
            // CompactRadioButton
            // 
            this.CompactRadioButton.AutoSize = true;
            this.CompactRadioButton.Checked = true;
            this.CompactRadioButton.Location = new System.Drawing.Point(3, 3);
            this.CompactRadioButton.Name = "CompactRadioButton";
            this.CompactRadioButton.Size = new System.Drawing.Size(84, 17);
            this.CompactRadioButton.TabIndex = 0;
            this.CompactRadioButton.TabStop = true;
            this.CompactRadioButton.Text = "Sql Compact";
            this.CompactRadioButton.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(475, 51);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(160, 27);
            this.button2.TabIndex = 3;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(476, 15);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(157, 20);
            this.textBox1.TabIndex = 4;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(645, 435);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button1);
            this.Name = "MainForm";
            this.Text = "Main Form";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton ExpressRadioButton;
        private System.Windows.Forms.RadioButton CompactRadioButton;
        private System.Windows.Forms.RadioButton AccessRadioButton;
        private System.Windows.Forms.RadioButton ExcelRadioButton;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox1;
    }
}

