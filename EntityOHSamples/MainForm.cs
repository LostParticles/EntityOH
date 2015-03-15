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

        public string CurrentKey
        {
            get
            {
                string key = "Maddy";
                if (CompactRadioButton.Checked) key = "MaddyCompact";

                if (ExpressRadioButton.Checked) key = "MaddyExpress";

                if (AccessRadioButton.Checked) key = "MaddyAccess";

                if (ExcelRadioButton.Checked) key = "MaddyExcel";

                if (PostgresRadioButton.Checked) key = "postgres";

                return key;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {


            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                //dataGridView1.DataSource = db.Select<Person>();
                dataGridView1.DataSource = db.Tables;

            }

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                db.CreateTable<Person>();

            }
        }


        Person[] CurrentPersons;

        private void button5_Click(object sender, EventArgs e)
        {
            
            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                CurrentPersons = db.Select<Person>().ToArray();
                dataGridView1.DataSource = CurrentPersons;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                db.DropTable<Person>();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Random rr = new Random(Environment.TickCount);

            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                for (int i = 0; i < 20; i++)
                {
                    db.Insert<Person>(
                        new Person
                        {
                            FirstName = "System " + System.Environment.TickCount.ToString()
                            , IntegerNumber = rr.Next()
                        });
                }
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                foreach(DataGridViewRow xx in dataGridView1.SelectedRows)
                { 
                    
                    var ix = xx.Index;

                    var ptodel = CurrentPersons[ix];

                    db.Delete<Person>(ptodel);
                }
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var ptodel = CurrentPersons[e.RowIndex];

            using (DbProbe db = DbProbe.Db(CurrentKey))
            {
                db.Update<Person>(ptodel);
            }
            
        }
    }
}
