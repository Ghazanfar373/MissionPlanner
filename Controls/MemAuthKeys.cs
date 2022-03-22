using MissionPlanner.Mavlink;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissionPlanner.Controls
{
    public partial class MemAuthKeys : Form
    {

        public MemAuthKeys()
        {
            InitializeComponent();

            //::!::MAVMemAuthKeys.Load();
            //::prot:: 
            ThemeManager.ApplyThemeTo(this);
            LoadKeys();


        }
      

        public void LoadKeys()
        {
            dataGridView1.Rows.Clear();
            try
            {

                foreach (var authKey in MAVMemAuthKeys.currUserRecord.dict)
                {
                    int row = dataGridView1.Rows.Add();
                    dataGridView1[FName.Index, row].Value = authKey.Key;
                    dataGridView1[Key.Index, row].Value = Convert.ToBase64String(authKey.Value.MemKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Save()
        {

            //::!::MAVMemAuthKeys.Save();
            Login.SaveAndEncryptDataOnly();
        }
       

        
        // Include the exact path to the SwiftRNG.dll (it uses additional dependencies from same location) available with the software kit.
        [DllImport("SwiftRNG.dll", EntryPoint = "swftGetEntropySynchronized")]
        public static extern int getRandomBytes(byte[] bytes, long byteCount);

        private void randomizeRow(int keyIndex, int row)
        {
            /*
             Type[] extDLLTypes = Assembly.Load("DynamicDLL").GetTypes();
            
            foreach(Type item in extDLLTypes)
            {
                Console.WriteLine(item.ToString());
            }

            //dynamic randObj = Activator.CreateInstance(extDLLTypes[0], "");
            */

            byte[] number = new byte[16];
            int status = getRandomBytes(number, number.Length);

            if (status != 0) // Non zero status indicates an error
            {
                Console.Out.WriteLine("Could not retrieve an array of random bytes from SwiftRNG device  (Pseudo random number will be used for now)");

                MessageBox.Show("Could not retrieve true randoms from SwiftRNG device, please plug the device (Pseudo random number will be used for now)");
                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                rng.GetBytes(number);

            }
            else
            {
                /*
                for (int i = 0; i < number.Length; i++)
                {
                    Console.Out.WriteLine(number[i]);
                }
                */
            }
            /////////
            /*
            byte[] number = new byte[16];
            
            */
            string hexStr = (BitConverter.ToString(number));
            Console.WriteLine(hexStr);
            hexStr = hexStr.Replace("-", "");


            MAVMemAuthKeys.AddKey(dataGridView1[FName.Index, row].Value.ToString(), hexStr);

            //MAVMemAuthKeys.AddKey("Memory key", hexStr);
            Save();

            LoadKeys();


            ///////////////////////


            //txt_Raeskey.Text = hexStr;//new Random((int)DateTime.UtcNow.Ticks).ToString();
            //txt_aeskey.Text = hexStr;// "4532FDC";// new Random((int)DateTime.UtcNow.Ticks).ToString();
            //var random = new Random((int)DateTime.UtcNow.Ticks);
            //::prot::

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == Use.Index)
            {
                //MainV2.comPort.setupSigning("", Convert.FromBase64String(dataGridView1[Key.Index, e.RowIndex].Value.ToString()));
                MainV2.comPort.sendKey("", Convert.FromBase64String(dataGridView1[Key.Index, e.RowIndex].Value.ToString()));

            }
            else if (e.ColumnIndex == Randomize.Index)
            {
                if (e.RowIndex >= 0)
                {
                    Console.WriteLine(dataGridView1[Key.Index, e.RowIndex].Value.ToString());
                    // dataGridView1[Key.Index, e.RowIndex].Value.ToString()
                    randomizeRow(Key.Index, e.RowIndex);
                }
            }
        }

        private void dataGridView1_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            MAVMemAuthKeys.currUserRecord.dict.Remove(e.Row.Cells[FName.Index].Value.ToString());
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            //dataGridView1[Use.Index, e.RowIndex].Value = "Use";
        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            string name = "";//= Prompt.ShowDialog("Enter UAV Name", "New..");
            if (InputBox.Show("New UAV..", "Please enter a UAV name", ref name) == DialogResult.OK)
                if (name.Length > 0)
                {
                    int row = dataGridView1.Rows.Add();

                    dataGridView1[FName.Index, row].Value = name;

                    byte[] number = new byte[16];
                    int status = getRandomBytes(number, number.Length);

                    if (status != 0) // Non zero status indicates an error
                    {
                        Console.Out.WriteLine("Could not retrieve an array of random bytes from SwiftRNG device  (Pseudo random number will be used for now)");

                        MessageBox.Show("Could not retrieve true randoms from SwiftRNG device, please plug the device (Pseudo random number will be used for now)");
                        RandomNumberGenerator rng = RandomNumberGenerator.Create();
                        rng.GetBytes(number);
                    }
                    else
                    {
                    }

                    string hexStr = (BitConverter.ToString(number));
                    Console.WriteLine(hexStr);
                    hexStr = hexStr.Replace("-", "");


                    MAVMemAuthKeys.AddKey(dataGridView1[FName.Index, row].Value.ToString(), hexStr);

                    //MAVMemAuthKeys.AddKey("Memory key", hexStr);
                    Save();

                    LoadKeys();
                    //txt_Raeskey.Text = hexStr;//new Random((int)DateTime.UtcNow.Ticks).ToString();
                    //txt_aeskey.Text = hexStr;// "4532FDC";// new Random((int)DateTime.UtcNow.Ticks).ToString();
                    //var random = new Random((int)DateTime.UtcNow.Ticks);
                }
        }


        private void deleteBtn_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                //dataGridView1.SelectedCells[0].Value;
                //dataGridView1.SelectedRows[0].Cells
                //MAVMemAuthKeys.Keys.Remove(dataGridView1.SelectedRows[0].Cells[0].Value.ToString());
                int selectedRow = dataGridView1.SelectedCells[0].RowIndex;
                MAVMemAuthKeys.currUserRecord.dict.Remove(dataGridView1.Rows[selectedRow].Cells[0].Value.ToString());
                //dataGridView1.Rows.RemoveAt(currentRow);

                Save();

                LoadKeys();
            }
            else
            {
                MessageBox.Show("please select row to be deleted");

            }

        }

        private void Load_Click(object sender, EventArgs e)
        {
            LoadKeys();
        }

        private void but_save_Click(object sender, EventArgs e)
        {
            Save();
        }
    }
  
}
