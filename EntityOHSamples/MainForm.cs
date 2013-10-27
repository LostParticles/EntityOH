using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EntityOH;

namespace EntityOHSamples
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

        }


        private void button1_Click(object sender, EventArgs e)
        {

            string key = "Maddy";
            if (CompactRadioButton.Checked) key = "MaddyCompact";

            if (ExpressRadioButton.Checked) key = "MaddyExpress";

            if (AccessRadioButton.Checked) key = "MaddyAccess";

            if (ExcelRadioButton.Checked) key = "MaddyExcel";

            using (DbProbe db = DbProbe.Db(key))
            {
                //dataGridView1.DataSource = db.Select<Person>();
                dataGridView1.DataSource = db.Tables;

            }

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // connect to sql server 

            using (DbProbe db = DbProbe.Db("Server=localhost;Database=Distracted;Trusted_Connection=True;", "System.Data.SqlClient"))
            {


                db.CreateTable<Person>();
                
                

                dataGridView1.DataSource = db.Select<Person>(textBox1.Text);

            }
        }
    }
}
