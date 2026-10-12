using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PTOF
{
    public partial class Form1 : Form
    {
        private Dictionary<string, string> CustomerAccounts = new Dictionary<string, string>()
        {
            { "buyer1", "customer123" }
        };

        public static string CurrentUserRole = "";
        public static string CurrentUsername = "";

        public static List<string> SessionCart = new List<string>();
        public static List<string> SessionOrders = new List<string>();
        public static List<string> SessionWishlist = new List<string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "HOP Clothing Store - Login";
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string selectedRole = cmbUserRole.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedRole))
            {
                MessageBox.Show("Please select your User Role (Admin, Cashier, or Customer) before logging in!", "Login Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Username and Password fields cannot be empty!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (selectedRole == "Admin")
            {
                if (user == "admin" && pass == "admin123")
                {
                    CurrentUserRole = "Admin";
                    CurrentUsername = "Administrator";
                    MessageBox.Show("Welcome, Administrator! All core modules unlocked.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    pnlAuth.Visible = false;
                }
                else
                {
                    MessageBox.Show("Invalid Administrator credentials! Please try again.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
            else if (selectedRole == "Cashier")
            {
                if (user == "cashier" && pass == "cashier123")
                {
                    CurrentUserRole = "Cashier";
                    CurrentUsername = "Cashier Staff";
                    MessageBox.Show("Welcome, Cashier/Staff! POS Counter activated.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    pnlAuth.Visible = false;
                }
                else
                {
                    MessageBox.Show("Invalid Cashier credentials! Access denied.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
            else if (selectedRole == "Customer")
            {
                if (CustomerAccounts.ContainsKey(user) && CustomerAccounts[user] == pass)
                {
                    CurrentUserRole = "Customer";
                    CurrentUsername = user;
                    MessageBox.Show($"Welcome, {user}! Enjoy shopping at HOP Clothing Store.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Hide();
     
                  
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Account not found or password incorrect! If you don't have an account yet, please click 'Create Account' to register.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnGuestMode_Click(object sender, EventArgs e)
        {
            CurrentUserRole = "Guest";
            CurrentUsername = "Guest Buyer";

            MessageBox.Show("Entering as Guest. You can browse all available apparel items and add them to your virtual cart, but you will be required to log in or register before checking out your items.", "Guest Mode Active", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Hide();
         
       
        }

        private void btnGotoRegister_Click(object sender, EventArgs e)
        {
            string newCustomerUser = ShowInputBox("Enter your desired Username for registration:", "Customer Registration Portal").Trim();
            if (string.IsNullOrEmpty(newCustomerUser)) return;

            if (CustomerAccounts.ContainsKey(newCustomerUser))
            {
                MessageBox.Show("This username is already taken! Please choose a unique name.", "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newCustomerPass = ShowInputBox("Enter your strong Password for " + newCustomerUser + ":", "Customer Registration Portal");
            if (string.IsNullOrEmpty(newCustomerPass))
            {
                MessageBox.Show("Password field cannot be left blank!", "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CustomerAccounts.Add(newCustomerUser, newCustomerPass);
            MessageBox.Show("Account successfully created! Your credentials have been pre-filled. Click 'Log In' to access the system.", "Registration Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            txtUsername.Text = newCustomerUser;
            txtPassword.Text = newCustomerPass;
            cmbUserRole.SelectedItem = "Customer";
        }

        private string ShowInputBox(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 350 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            Button confirmation = new Button() { Text = "OK", Left = 260, Width = 100, Top = 85, DialogResult = DialogResult.OK };

            confirmation.Click += (s, ev) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}
