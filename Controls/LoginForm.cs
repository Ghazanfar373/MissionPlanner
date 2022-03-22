using MissionPlanner.Mavlink;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MissionPlanner.Controls.Login;
using static MissionPlanner.Mavlink.MAVMemAuthKeys;

namespace MissionPlanner.Controls
{
    public partial class LoginForm : Form
    {

        static Crypto Rij = new Crypto();  //::os::
        public LoginForm()
        {
            InitializeComponent();
        }

        private void loginBtn_Click(object sender, EventArgs e)
        {
            //define local variables from the user inputs 
            string user = nametxtbox.Text;
            string pass = pwdtxtbox.Text;

            //login = new Login(user, pass);


            if (IsLoggedIn(user, pass))
            {

                MessageBox.Show("You are logged in successfully");
                //::TODO:: ::fix:: close/hide current login win and change key shape in MainV2.cs + load user keys UI
                //this.Hide();


                //remove old keys from ram
                //MAVMemAuthKeys.currUserRecord = new MAVMemAuthKeys.UserRecord();
                //Username = user;
                if ((Application.OpenForms["MemAuthKeys"] as MemAuthKeys) == null)
                {
                    loadMemKeysUI();
                    this.Close();
                }
                else
                {
                    memKeysUI.LoadKeys();

                }
            }
            else
            {

                //MessageBox.Show("Login Error!");
                // ::TODO:: in log maybe, msg already shown
            }
        }
        private MemAuthKeys memKeysUI;
        private void loadMemKeysUI()
        {
            memKeysUI = new MemAuthKeys();
            memKeysUI.Show();
        }
        //method to check if eligible to be logged in //internal 
        public static bool IsLoggedIn(string user, string pass)
        {
            //check user name empty 
            if (string.IsNullOrEmpty(user))
            {
                MessageBox.Show("Enter the user name!");
                return false;

            }
            //check user name is valid type 
            else if (StringValidator(user) == true)
            {
                MessageBox.Show("Enter only text here");
                ClearTexts(user, pass);
                return false;
            }
            //check user name is correct 
            else
            {
                if (verifyPass(user, pass))
                {
                    return true;
                }
                //check password is empty 
                else
                {
                    if (string.IsNullOrEmpty(pass))
                    {
                        MessageBox.Show("Enter the passowrd!");
                        return false;
                    }
                    /*
                    //check password is valid 
                    else if (IntegerValidator(pass) == true)
                    {
                        MessageBox.Show("Enter only integer pass here");
                        return false;
                    }
                    */
                    //check password is correct 
                    else
                    {

                        MessageBox.Show("Password is incorrect");
                        return false;
                    }
                }
            }
        }
       
        //clear user inputs 
        private static void ClearTexts(string user, string pass)
        {
            user = String.Empty;
            pass = String.Empty;
        }
        //validate string 
        private static bool StringValidator(string input)
        {
            string pattern = "[^a-zA-Z]";
            if (Regex.IsMatch(input, pattern))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void signupBtn_Click(object sender, EventArgs e)
        {
            //define local variables from the user inputs 
            string user = nametxtbox.Text;
            string pass = pwdtxtbox.Text;

            if (StringValidator(user) == true)
            {
                MessageBox.Show("Enter only characters as Username");
                ClearTexts(user, pass);
            }
            else
            {
                //check password

                string err_msg = "";
                bool isValid = ValidatePassword(pass, out err_msg);
                //isValid = true;  //::fix:: ::TODO:: fix constraints at delivery time, inforce special characters? 
                if (!isValid)
                {

                    MessageBox.Show(err_msg);
                }
                else
                {
                    Login.signup(user, pass);
                }
            }
        }
        private bool ValidatePassword(string password, out string ErrorMessage)
        {
            var input = password;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new Exception("Password should not be empty");
            }

            //^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,15}$
            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMiniMaxChars = new Regex(@".{8,15}");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

            if (!hasLowerChar.IsMatch(input))
            {
                ErrorMessage = "Password should contain at least one lower case letter!";
                return false;
            }
            else if (!hasUpperChar.IsMatch( input))
            {
                ErrorMessage = "Password should contain at least one upper case letter";
                return false;
            }
            else if (!hasMiniMaxChars.IsMatch(input))
            {
                ErrorMessage = "Password should not be less than 12 characters";
                return false;
            }
            else if (input.Length > 20)
            {
                ErrorMessage = "Password is too long (max 20)";
                return false;
            }
            else if (!hasNumber.IsMatch(input))
            {
                ErrorMessage = "Password should contain At least one numeric value";
                return false;
            }

            else if (!hasSymbols.IsMatch(input))
            {
                ErrorMessage = "Password should contain At least one special case characters";
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
