using PTOF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PTOF
{
    public partial class Form1 : Form
    {
        private string connectionString = "Data Source=store.db;Version=3;";

        public static string CurrentUserRole = "";
        public static string CurrentUsername = "";

        public static List<string> SessionCart = new List<string>();
        public static List<string> SessionOrders = new List<string>();
        public static List<string> SessionWishlist = new List<string>();

        public Form1()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string createSettingsTable = @"CREATE TABLE IF NOT EXISTS Settings (
                                    Key TEXT PRIMARY KEY,
                                    Value TEXT)";
                using (SQLiteCommand cmdSettings = new SQLiteCommand(createSettingsTable, conn))
                {
                    cmdSettings.ExecuteNonQuery();
                }

                string createStaffTable = @"CREATE TABLE IF NOT EXISTS Staff (
                                    StaffID TEXT PRIMARY KEY,
                                    StaffName TEXT,
                                    StaffUsername TEXT,
                                    StaffEmail TEXT,
                                    StaffRole TEXT,
                                    StaffStatus TEXT,
                                    StaffDate TEXT,
                                    StaffPassword TEXT)";

                string createCustomerTable = @"CREATE TABLE IF NOT EXISTS Customers (
                                    CustID TEXT PRIMARY KEY,
                                    CustName TEXT,
                                    CustEmail TEXT,
                                    CustContact TEXT,
                                    CustAddress TEXT,
                                    CustOrders TEXT,
                                    CustMOP TEXT,
                                    CustPayStatus TEXT,
                                    CustAccStatus TEXT,
                                    CustRegDate TEXT,
                                    CustPassword TEXT)";

                using (SQLiteCommand cmd = new SQLiteCommand(createStaffTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }
                using (SQLiteCommand cmd = new SQLiteCommand(createCustomerTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                try
                {
                    string alterCust = "ALTER TABLE Customers ADD COLUMN CustPassword TEXT;";
                    using (SQLiteCommand cmdAlt = new SQLiteCommand(alterCust, conn))
                    {
                        cmdAlt.ExecuteNonQuery();
                    }
                }
                catch (Exception)
                {

                }

                string checkAdminQuery = "SELECT COUNT(*) FROM Staff WHERE StaffUsername = 'admin'";
                using (SQLiteCommand cmdCheck = new SQLiteCommand(checkAdminQuery, conn))
                {
                    long count = (long)cmdCheck.ExecuteScalar();
                    if (count == 0)
                    {
                        string insertDefaultAdmin = @"INSERT INTO Staff (StaffID, StaffName, StaffUsername, StaffEmail, StaffRole, StaffStatus, StaffDate, StaffPassword) 
                                            VALUES ('STF-001', 'Administrator', 'admin', 'admin@store.com', 'Admin', 'Active', @date, 'admin123')";
                        using (SQLiteCommand cmdIns = new SQLiteCommand(insertDefaultAdmin, conn))
                        {
                            cmdIns.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadStoreNameFromDb();
        }

        private void LoadStoreNameFromDb()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Value FROM Settings WHERE Key = 'StoreName'";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            lblStoreTitle.Text = result.ToString();

                            if (lblStoreTitle.Parent != null)
                            {
                                lblStoreTitle.Left = (lblStoreTitle.Parent.Width - lblStoreTitle.Width) / 2;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string selectedRole = cmbUserRole.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedRole))
            {
                MessageBox.Show("Please select your User Role before logging in!", "Login Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Username and Password fields cannot be empty!", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (selectedRole == "Admin")
            {
                bool isAdminAuthenticated = false;
                string adminLoggedInName = "";

                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Staff WHERE (StaffUsername = @user OR StaffEmail = @user) AND StaffPassword = @pass AND StaffStatus = 'Active'";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@pass", pass);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string csvRole = reader["StaffRole"].ToString();
                                if (csvRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) || csvRole.Equals("Assistant Admin", StringComparison.OrdinalIgnoreCase))
                                {
                                    isAdminAuthenticated = true;
                                    adminLoggedInName = reader["StaffName"].ToString();
                                    break;
                                }
                            }
                        }
                    }
                }

                if (isAdminAuthenticated)
                {
                    CurrentUserRole = "Admin";
                    CurrentUsername = adminLoggedInName;
                    MessageBox.Show($"Welcome, {adminLoggedInName} (Admin)! All core modules unlocked.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    AdminMainForm adminForm = new AdminMainForm();
                    this.Hide();
                    adminForm.ShowDialog();

                    LoadStoreNameFromDb();
                    this.Show();
                }
                else
                {
                    MessageBox.Show("Invalid Administrator credentials! Please try again.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
            else if (selectedRole == "Cashier" || selectedRole == "Inventory Staff" || selectedRole == "Assistant Admin")
            {
                bool isStaffAuthenticated = false;
                string staffLoggedInName = "";

                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Staff WHERE (StaffUsername = @user OR StaffEmail = @user) AND StaffPassword = @pass AND StaffRole = @role AND StaffStatus = 'Active'";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@pass", pass);
                        cmd.Parameters.AddWithValue("@role", selectedRole);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isStaffAuthenticated = true;
                                staffLoggedInName = reader["StaffName"].ToString();
                            }
                        }
                    }
                }

                if (isStaffAuthenticated)
                {
                    CurrentUserRole = selectedRole;
                    CurrentUsername = staffLoggedInName;
                    MessageBox.Show($"Welcome, {staffLoggedInName} ({selectedRole})! System access granted.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    CashierPortalForm cashierPortal = new CashierPortalForm(staffLoggedInName);
                    this.Hide();
                    cashierPortal.ShowDialog();
                    this.Show();
                }
                else
                {
                    MessageBox.Show("Invalid credentials, inactive account, or role mismatch! Access denied.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
            else if (selectedRole == "Customer")
            {
                bool isAuthenticated = false;
                string loggedInName = "";

                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    // Sinusuri na rin ang CustPassword para sa customer login
                    string query = "SELECT * FROM Customers WHERE (CustName = @user OR CustEmail = @user) AND CustPassword = @pass AND CustAccStatus = 'Active'";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@pass", pass);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isAuthenticated = true;
                                loggedInName = reader["CustName"].ToString();
                            }
                        }
                    }
                }

                if (isAuthenticated)
                {
                    CurrentUserRole = "Customer";
                    CurrentUsername = loggedInName;
                    MessageBox.Show($"Welcome, {loggedInName}! Enjoy shopping.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    CustomerPortalForm customerPortal = new CustomerPortalForm();
                    this.Hide();
                    customerPortal.ShowDialog();
                    this.Show();
                }
                else
                {
                    MessageBox.Show("Account not found, inactive, incorrect password, or uninitialized password! Please check your credentials.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void btnGuestMode_Click(object sender, EventArgs e)
        {
            CurrentUserRole = "Guest";
            CurrentUsername = "Guest Buyer";
            MessageBox.Show("Entering as Guest. You can browse and add items to cart, but you need to create a customer account to check out or buy now.", "Guest Mode Active", MessageBoxButtons.OK, MessageBoxIcon.Information);

            CustomerPortalForm customerPortal = new CustomerPortalForm();
            this.Hide();
            customerPortal.ShowDialog();
            this.Show();
        }

        private void btnGotoRegister_Click(object sender, EventArgs e)
        {
            string newCustomerName = ShowInputBox("Enter your Full Name for registration:", "Customer Registration Portal").Trim();
            if (string.IsNullOrEmpty(newCustomerName)) return;

            string newCustomerEmail = ShowInputBox("Enter your Email Address:", "Customer Registration Portal").Trim();
            if (string.IsNullOrEmpty(newCustomerEmail)) return;

            string newCustomerContact = ShowInputBox("Enter your Contact Number:", "Customer Registration Portal").Trim();
            if (string.IsNullOrEmpty(newCustomerContact)) return;

            string newCustomerAddress = ShowInputBox("Enter your Delivery Address:", "Customer Registration Portal").Trim();
            if (string.IsNullOrEmpty(newCustomerAddress)) return;

            string newCustomerPassword = ShowInputBox("Enter your Password:", "Customer Registration Portal").Trim();
            if (string.IsNullOrEmpty(newCustomerPassword)) return;

            string newCustID = "CST-" + new Random().Next(100, 999);
            string regDate = DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string insertQuery = @"INSERT INTO Customers (CustID, CustName, CustEmail, CustContact, CustAddress, CustOrders, CustMOP, CustPayStatus, CustAccStatus, CustRegDate, CustPassword) 
                                           VALUES (@custID, @name, @email, @contact, @address, '0', 'COD', 'Unpaid', 'Active', @regDate, @password)";

                    using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@custID", newCustID);
                        cmd.Parameters.AddWithValue("@name", newCustomerName);
                        cmd.Parameters.AddWithValue("@email", newCustomerEmail);
                        cmd.Parameters.AddWithValue("@contact", newCustomerContact);
                        cmd.Parameters.AddWithValue("@address", newCustomerAddress);
                        cmd.Parameters.AddWithValue("@regDate", regDate);
                        cmd.Parameters.AddWithValue("@password", newCustomerPassword);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Account successfully created! You can now log in using your Name or Email.", "Registration Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtUsername.Text = newCustomerName;
                txtPassword.Text = "";
                cmbUserRole.SelectedItem = "Customer";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving account: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void label6_Click(object sender, EventArgs e) { }

        private void pnlAuth_Paint(object sender, PaintEventArgs e) { }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void lnkForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // 1. Humingi ng Username, Email, o Customer Name
            string identifier = ShowInputBox("Enter your Staff Username/Email or Customer Name/Email:", "Forgot Password Portal").Trim();
            if (string.IsNullOrEmpty(identifier)) return;

            bool isStaff = false;
            bool isCustomer = false;

            // 2. Suriin kung nasa Staff table
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string checkStaff = "SELECT COUNT(*) FROM Staff WHERE StaffUsername = @id OR StaffEmail = @id";
                using (SQLiteCommand cmd = new SQLiteCommand(checkStaff, conn))
                {
                    cmd.Parameters.AddWithValue("@id", identifier);
                    if ((long)cmd.ExecuteScalar() > 0) isStaff = true;
                }
            }

            // 3. Kung wala sa Staff, suriin kung nasa Customers table
            if (!isStaff)
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string checkCust = "SELECT COUNT(*) FROM Customers WHERE CustName = @id OR CustEmail = @id";
                    using (SQLiteCommand cmd = new SQLiteCommand(checkCust, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", identifier);
                        if ((long)cmd.ExecuteScalar() > 0) isCustomer = true;
                    }
                }
            }

            if (!isStaff && !isCustomer)
            {
                MessageBox.Show("No account found with that Username, Email, or Name.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 4. Humingi ng Bagong Password
            string newPass = ShowInputBox("Enter your New Password:", "Reset Password").Trim();
            if (string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("Password cannot be empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string confirmPass = ShowInputBox("Confirm your New Password:", "Reset Password").Trim();
            if (newPass != confirmPass)
            {
                MessageBox.Show("Passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 5. I-update agad nang sarili sa database nang walang admin approval
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery = "";

                    if (isStaff)
                    {
                        updateQuery = "UPDATE Staff SET StaffPassword = @pass WHERE StaffUsername = @id OR StaffEmail = @id";
                    }
                    else if (isCustomer)
                    {
                        updateQuery = "UPDATE Customers SET CustPassword = @pass WHERE CustName = @id OR CustEmail = @id";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@pass", newPass);
                        cmd.Parameters.AddWithValue("@id", identifier);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Password successfully updated! You can now log in using your new password.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtUsername.Text = identifier;
                txtPassword.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating password: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}