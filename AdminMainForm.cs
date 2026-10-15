using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace PTOF
{
    public partial class AdminMainForm : Form

    {
        private Label lblHeader;
        private Label lblPrefTitle;
        private ComboBox cmbLanguageSetting;
        private string currentLanguage = "English";
        private Panel sidePanel;
        private TabControl adminTabControl;
        private Button btnLogout;

        // SQLite Connection String
        private string dbPath = "store.db";
        private string connectionString => $"Data Source={dbPath};Version=3;";

        private DataGridView dgvProductsRef;
        private DataGridView dgvInventoryRef;
        private DataGridView dgvOrdersRef;
        private DataGridView dgvCustomersRef;
        private DataGridView dgvStaffRef;
        private DataGridView dgvTransactionsRef;
        private DataGridView dgvReportsRef;

        // Dashboard UI references for dynamic refreshing
        private Label lblTodaySalesVal, lblMonthlySalesVal, lblTotalProdVal, lblTotalOrdVal, lblPendingOrdVal, lblLowStockVal;
        private DataGridView dgvDashboardRecentTrans;

        // Settings variables
        private string currentTheme = "Dark Sidebar (Default)";
        private string currentCurrency = "PHP (₱)";
        private string storeName = "HOP Clothing Store";
        private string storePhone = "09123456789";
        private string storeEmail = "roxanhomo07@gmail.com";
        private string storeAddress = "Quezon City, Philippines";
        private bool enableEmailNotif = true;
        private bool enableSmsNotif = false;

        // Settings UI controls references & Product UI references for dynamic update
        private TextBox txtSysName, txtSysPhone, txtSysEmail, txtSysAddress;
        private TextBox txtCurrUser, txtCurrPass, txtNewPass;
        private ComboBox cmbThemePref, cmbCurrencyPref;
        private CheckBox chkEmailNotif, chkSmsNotif;
        private Label lblPriceRef;

        public AdminMainForm()
        {
            InitializeComponent();
            InitializeDatabase();
            LoadSettingsFromDb();
            SetupAdminLayout();
            ApplyThemeSettings();
        }

        private string GetCurrencySymbol()
        {
            if (currentCurrency.Contains("USD")) return "$";
            if (currentCurrency.Contains("EUR")) return "€";
            return "₱";
        }

        private string GetCustomerEmailFromDb(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName)) return string.Empty;

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT CustEmail FROM Customers WHERE LOWER(CustName) = LOWER(@name) LIMIT 1";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", customerName.Trim());
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return result.ToString();
                    }
                }
            }
            return string.Empty;
        }

        private string ConvertPriceInput(string inputPrice)
        {
            if (decimal.TryParse(inputPrice, out decimal phpValue))
            {
                if (currentCurrency.Contains("USD"))
                {
                    decimal converted = phpValue / 62.81m;
                    return converted.ToString("N2");
                }
                else if (currentCurrency.Contains("EUR"))
                {
                    decimal converted = phpValue / 71.56m;
                    return converted.ToString("N2");
                }
            }
            return inputPrice;
        }

        private void SendEmailReceipt(string recipientEmail, string subject, string bodyContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipientEmail) || !recipientEmail.Contains("@"))
                {
                    MessageBox.Show("There is no valid email address for the customer.", "Email Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string senderEmail = storeEmail;
                string senderPassword = "fimarppbakcbswdu"; 

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(senderEmail, storeName);
                mail.To.Add(recipientEmail);
                mail.Subject = subject;
                mail.Body = bodyContent;
                mail.IsBodyHtml = false;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }

                MessageBox.Show($"The receipt was successfully sent to customer's email: {recipientEmail}", "Email Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send an email. Error: " + ex.Message, "SMTP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string createTablesQuery = @"
                    CREATE TABLE IF NOT EXISTS Products (
                        ProdID TEXT PRIMARY KEY,
                        ProdName TEXT,
                        Category TEXT,
                        Size TEXT,
                        Price TEXT,
                        Stock TEXT,
                        ProdImage BLOB
                    );
                    CREATE TABLE IF NOT EXISTS Orders (
                        OrdID TEXT PRIMARY KEY,
                        OrdCustomer TEXT,
                        OrdDate TEXT,
                        OrdItems TEXT,
                        OrdTotal TEXT,
                        OrdPayMethod TEXT,
                        OrdPayStatus TEXT,
                        OrdStatus TEXT
                    );
                    CREATE TABLE IF NOT EXISTS Customers (
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
                        CustProfilePic BLOB
                    );
                    CREATE TABLE IF NOT EXISTS Staff (
                        StaffID TEXT PRIMARY KEY,
                        StaffName TEXT,
                        StaffUsername TEXT,
                        StaffEmail TEXT,
                        StaffRole TEXT,
                        StaffStatus TEXT,
                        StaffDate TEXT,
                        StaffPassword TEXT,
                        StaffProfilePic BLOB
                    );
                    CREATE TABLE IF NOT EXISTS Transactions (
                        TransID TEXT PRIMARY KEY,
                        TransOrderID TEXT,
                        TransDateTime TEXT,
                        TransCustomer TEXT,
                        TransStaff TEXT,
                        TransItems TEXT,
                        TransTotal TEXT,
                        TransMethod TEXT,
                        TransPayStatus TEXT,
                        TransStatus TEXT
                    );
                    CREATE TABLE IF NOT EXISTS Returns (
                        ReturnID TEXT PRIMARY KEY,
                        TransID TEXT,
                        Customer TEXT,
                        Products TEXT,
                        PurchaseDate TEXT,
                        Reason TEXT,
                        Quantity INTEGER,
                        RefundAmount TEXT,
                        Status TEXT,
                        Cashier TEXT
                    );
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );
                ";
                using (var cmd = new SQLiteCommand(createTablesQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string checkAdminQuery = "SELECT COUNT(*) FROM Staff WHERE StaffUsername = 'admin'";
                using (var cmdCheck = new SQLiteCommand(checkAdminQuery, conn))
                {
                    long count = (long)cmdCheck.ExecuteScalar();
                    if (count == 0)
                    {
                        string insertAdmin = "INSERT INTO Staff (StaffID, StaffName, StaffUsername, StaffEmail, StaffRole, StaffStatus, StaffDate, StaffPassword) VALUES ('STF-001', 'Roxan Homo', 'admin', 'roxanhomo07@gmail.com', 'Admin', 'Active', @date, 'rox')";
                        using (var cmdIns = new SQLiteCommand(insertAdmin, conn))
                        {
                            cmdIns.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                }

                try
                {
                    string alterQuery = "ALTER TABLE Products ADD COLUMN ProdImage BLOB;";
                    using (var cmdAlter = new SQLiteCommand(alterQuery, conn))
                    {
                        cmdAlter.ExecuteNonQuery();
                    }
                }
                catch
                {
                }
            }
        }
        private void DeductStockFromItems(string ordItems, SQLiteConnection conn)
        {
            if (string.IsNullOrEmpty(ordItems))
            {
                MessageBox.Show("The ordItems is empty!");
                return;
            }


            string[] items = ordItems.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in items)
            {
                string trimmedItem = item.Trim();
                string prodName = "";
                string size = "";
                int qty = 1;

                if (trimmedItem.Contains(" x "))
                {
                    int lastIndex = trimmedItem.LastIndexOf(" x ");
                    string nameAndSize = trimmedItem.Substring(0, lastIndex).Trim();

                    if (!int.TryParse(trimmedItem.Substring(lastIndex + 3).Trim(), out qty))
                    {
                        qty = 1; 
                    }

                    if (nameAndSize.Contains("(") && nameAndSize.EndsWith(")"))
                    {
                        int openBracket = nameAndSize.LastIndexOf("(");
                        prodName = nameAndSize.Substring(0, openBracket).Trim();
                        size = nameAndSize.Substring(openBracket + 1, nameAndSize.Length - openBracket - 2).Trim();
                    }
                    else
                    {
                        prodName = nameAndSize;
                    }
                }
                else
                {
                    prodName = trimmedItem;
                }


                string updateStockQuery = "";
                using (var cmd = new SQLiteCommand(conn))
                {
                    if (!string.IsNullOrEmpty(size))
                    {
                        updateStockQuery = "UPDATE Products SET Stock = CAST(Stock AS INTEGER) - @qty WHERE ProdName = @prodName AND Size = @size";
                        cmd.Parameters.AddWithValue("@size", size);
                    }
                    else
                    {
                        updateStockQuery = "UPDATE Products SET Stock = CAST(Stock AS INTEGER) - @qty WHERE ProdName = @prodName";
                    }

                    cmd.CommandText = updateStockQuery;
                    cmd.Parameters.AddWithValue("@qty", qty);
                    cmd.Parameters.AddWithValue("@prodName", prodName);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        MessageBox.Show($"Warning: There is no product named '{prodName}' and size of '{size}'. Please check the spelling.");
                    }
                }
            }
        }

        private void LoadSettingsFromDb()
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Key, Value FROM Settings";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string key = reader["Key"]?.ToString();
                                string val = reader["Value"]?.ToString();

                                if (key == "Theme") currentTheme = val ?? "Dark Sidebar (Default)";
                                if (key == "Currency") currentCurrency = val ?? "PHP (₱)";
                                if (key == "StoreName") storeName = val ?? "HOP Clothing Store";
                                if (key == "StorePhone") storePhone = val ?? "09123456789";
                                if (key == "StoreEmail") storeEmail = val ?? "roxanhomo07@gmail.com";
                                if (key == "StoreAddress") storeAddress = val ?? "Quezon City, Philippines";
                                if (key == "EmailNotif" && bool.TryParse(val, out bool enEmail)) enableEmailNotif = enEmail;
                                if (key == "SmsNotif" && bool.TryParse(val, out bool enSms)) enableSmsNotif = enSms;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveSettingsToDb()
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    Dictionary<string, string> settingsDict = new Dictionary<string, string>()
                    {
                        { "Theme", currentTheme },
                        { "Currency", currentCurrency },
                        { "StoreName", storeName },
                        { "StorePhone", storePhone },
                        { "StoreEmail", storeEmail },
                        { "StoreAddress", storeAddress },
                        { "EmailNotif", enableEmailNotif.ToString() },
                        { "SmsNotif", enableSmsNotif.ToString() }
                    };

                    foreach (var kvp in settingsDict)
                    {
                        string query = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@key, @val)";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@key", kvp.Key);
                            cmd.Parameters.AddWithValue("@val", kvp.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyThemeSettings()
        {
            if (sidePanel == null) return;

            Color sidebarBgColor = Color.FromArgb(30, 30, 30);
            Color hoverColor = Color.FromArgb(50, 50, 50);

            if (currentTheme == "Blue")
            {
                sidebarBgColor = Color.FromArgb(41, 128, 185);
                hoverColor = Color.FromArgb(52, 152, 219);
            }
            else if (currentTheme == "Beige")
            {
                sidebarBgColor = Color.FromArgb(184, 134, 11);
                hoverColor = Color.FromArgb(205, 155, 29);
            }
            else if (currentTheme == "Grey")
            {
                sidebarBgColor = Color.FromArgb(108, 117, 125);
                hoverColor = Color.FromArgb(134, 142, 150);
            }
            else
            {
                sidebarBgColor = Color.FromArgb(30, 30, 30);
                hoverColor = Color.FromArgb(50, 50, 50);
            }

            sidePanel.BackColor = sidebarBgColor;

            foreach (Control ctrl in sidePanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = Color.Transparent;
                    btn.MouseEnter -= NavButton_MouseEnter;
                    btn.MouseLeave -= NavButton_MouseLeave;

                    btn.MouseEnter += (s, e) => { btn.BackColor = hoverColor; };
                    btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; };
                }
            }
        }

        private void NavButton_MouseEnter(object sender, EventArgs e) { }
        private void NavButton_MouseLeave(object sender, EventArgs e) { }

        private void SetupAdminLayout()
        {
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = $"{storeName} - Admin Portal";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            sidePanel = new Panel();
            sidePanel.Dock = DockStyle.Left;
            sidePanel.Width = 230;
            sidePanel.BackColor = Color.FromArgb(30, 30, 30);
            this.Controls.Add(sidePanel);

            adminTabControl = new TabControl();
            adminTabControl.Location = new Point(230, 0);
            adminTabControl.Size = new Size(1035, 685);
            adminTabControl.Appearance = TabAppearance.FlatButtons;
            adminTabControl.ItemSize = new Size(0, 1);
            adminTabControl.SizeMode = TabSizeMode.Fixed;

            adminTabControl.SelectedIndexChanged += (s, e) => {
                if (adminTabControl.SelectedTab == null) return;

                if (adminTabControl.SelectedTab.Text == "Dashboard")
                {
                    RefreshDashboardData();
                }
                else if (adminTabControl.SelectedTab.Text == "Products" && dgvProductsRef != null)
                {
                    dgvProductsRef.Rows.Clear();
                    LoadProductsFromDb(dgvProductsRef);
                }
                else if (adminTabControl.SelectedTab.Text == "Inventory" && dgvInventoryRef != null)
                {
                    dgvInventoryRef.Rows.Clear();
                    LoadInventoryData(dgvInventoryRef);
                }
                else if (adminTabControl.SelectedTab.Text == "Orders" && dgvOrdersRef != null)
                {
                    dgvOrdersRef.Rows.Clear();
                    LoadOrdersFromDb(dgvOrdersRef);
                }
                else if (adminTabControl.SelectedTab.Text == "Customers" && dgvCustomersRef != null)
                {
                    dgvCustomersRef.Rows.Clear();
                    LoadCustomersFromDb(dgvCustomersRef);
                }
                else if (adminTabControl.SelectedTab.Text == "Staff/Users" && dgvStaffRef != null)
                {
                    dgvStaffRef.Rows.Clear();
                    LoadStaffFromDb(dgvStaffRef);
                }
                else if (adminTabControl.SelectedTab.Text == "Transactions" && dgvTransactionsRef != null)
                {
                    dgvTransactionsRef.Rows.Clear();
                    LoadTransactionsFromDb(dgvTransactionsRef);
                }
                else if (adminTabControl.SelectedTab.Text == "Returns/Refunds" && dgvReturnsRef != null)
                {
                    dgvReturnsRef.Rows.Clear();
                    LoadAdminReturnsData(dgvReturnsRef);
                }
            };

            this.Controls.Add(adminTabControl);

            string[] adminModules = {
                    "Dashboard", "Products", "Inventory", "Orders", "Customers",
                    "Staff/Users", "Transactions", "Returns/Refunds", "Reports", "Settings", "Logout"
            };

            int buttonY = 20;
            for (int i = 0; i < adminModules.Length; i++)
            {
                TabPage tab = new TabPage();
                tab.Text = adminModules[i];
                tab.BackColor = Color.FromArgb(245, 247, 250);

                adminTabControl.TabPages.Add(tab);

                if (adminModules[i] == "Dashboard") SetupDashboardTab(tab);
                else if (adminModules[i] == "Products") SetupProductsTab(tab);
                else if (adminModules[i] == "Inventory") SetupInventoryTab(tab);
                else if (adminModules[i] == "Orders") SetupOrdersTab(tab);
                else if (adminModules[i] == "Customers") SetupCustomersTab(tab);
                else if (adminModules[i] == "Staff/Users") SetupStaffTab(tab);
                else if (adminModules[i] == "Transactions") SetupTransactionsTab(tab);
                else if (adminModules[i] == "Returns/Refunds") SetupAdminReturnsTab(tab);
                else if (adminModules[i] == "Reports") SetupReportsTab(tab);
                else if (adminModules[i] == "Settings") SetupSettingsTab(tab);
                else if (adminModules[i] != "Logout")
                {
                    Label lblTitle = new Label();
                    lblTitle.Text = "Admin Module: " + adminModules[i];
                    lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
                    lblTitle.Location = new Point(20, 20);
                    lblTitle.AutoSize = true;
                    tab.Controls.Add(lblTitle);
                }

                Button navBtn = new Button();
                navBtn.Text = adminModules[i];
                navBtn.Tag = i;
                navBtn.Width = 190;
                navBtn.Height = 40;
                navBtn.Location = new Point(20, buttonY);
                navBtn.FlatStyle = FlatStyle.Flat;
                navBtn.FlatAppearance.BorderSize = 0;
                navBtn.ForeColor = Color.White;
                navBtn.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                navBtn.TextAlign = ContentAlignment.MiddleLeft;
                navBtn.Padding = new Padding(10, 0, 0, 0);
                navBtn.Cursor = Cursors.Hand;

                if (adminModules[i] == "Logout")
                {
                    btnLogout = navBtn;
                    btnLogout.Click += (s, e) => {
                        if (MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            this.Close();
                        }
                    };
                }
                else
                {
                    navBtn.Click += NavButton_Click;
                }

                sidePanel.Controls.Add(navBtn);
                buttonY += 48;
            }
        }

        private DataGridView dgvReturnsRef;

        private void LoadAdminReturnsData(DataGridView dgv)
        {
            string currSymbol = GetCurrencySymbol();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ReturnID, TransID, Customer, Products, PurchaseDate, Reason, Quantity, RefundAmount, Status, Cashier FROM Returns ORDER BY PurchaseDate DESC";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawRefund = reader["RefundAmount"]?.ToString() ?? "0";
                            foreach (string sym in new string[] { "₱", "$", "€" })
                            {
                                rawRefund = rawRefund.Replace(sym, "");
                            }

                            if (decimal.TryParse(rawRefund.Trim(), out decimal refundVal))
                            {
                                rawRefund = $"{currSymbol}{refundVal:N2}";
                            }
                            else
                            {
                                rawRefund = $"{currSymbol}{rawRefund}";
                            }

                            int rIdx = dgv.Rows.Add(
                                reader["ReturnID"],
                                reader["TransID"],
                                reader["Customer"],
                                reader["Products"],
                                reader["PurchaseDate"],
                                reader["Reason"],
                                reader["Quantity"],
                                rawRefund,
                                reader["Status"],
                                reader["Cashier"]
                            );

                            string status = reader["Status"]?.ToString()?.Trim() ?? "";
                            if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                                dgv.Rows[rIdx].Cells["RetStatus"].Style.ForeColor = Color.Green;
                            else if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                                dgv.Rows[rIdx].Cells["RetStatus"].Style.ForeColor = Color.Red;
                            else
                                dgv.Rows[rIdx].Cells["RetStatus"].Style.ForeColor = Color.DarkOrange;
                        }
                    }
                }
            }
        }

        private void SetupAdminReturnsTab(TabPage returnsTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Returns and Refunds Approval";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            returnsTab.Controls.Add(lblHeader);

            DataGridView dgvReturns = new DataGridView();
            dgvReturnsRef = dgvReturns;
            dgvReturns.Location = new Point(20, 60);
            dgvReturns.Size = new Size(680, 600);
            dgvReturns.BackgroundColor = Color.White;
            dgvReturns.BorderStyle = BorderStyle.FixedSingle;
            dgvReturns.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReturns.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReturns.ReadOnly = true;

            dgvReturns.Columns.Add("RetID", "Return ID");
            dgvReturns.Columns.Add("TransID", "Trans ID");
            dgvReturns.Columns.Add("Customer", "Customer");
            dgvReturns.Columns.Add("Products", "Products");
            dgvReturns.Columns.Add("PurchaseDate", "Date");
            dgvReturns.Columns.Add("Reason", "Reason");
            dgvReturns.Columns.Add("Quantity", "Qty");
            dgvReturns.Columns.Add("RefundAmount", "Refund (₱)");
            dgvReturns.Columns.Add("RetStatus", "Status");
            dgvReturns.Columns.Add("Cashier", "Cashier");

            LoadAdminReturnsData(dgvReturns);
            returnsTab.Controls.Add(dgvReturns);

            Panel actionPanel = new Panel();
            actionPanel.Location = new Point(715, 60);
            actionPanel.Size = new Size(300, 600);
            actionPanel.BackColor = Color.White;
            actionPanel.BorderStyle = BorderStyle.FixedSingle;
            returnsTab.Controls.Add(actionPanel);

            Label lblPanelTitle = new Label() { Text = "Approval Actions", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            actionPanel.Controls.Add(lblPanelTitle);

            Label lblSelectedInfo = new Label() { Text = "Choose a return request.", Location = new Point(20, 60), Size = new Size(260, 50), Font = new Font("Segoe UI", 9), ForeColor = Color.DimGray };
            actionPanel.Controls.Add(lblSelectedInfo);

            Button btnApprove = new Button() { Text = "Approve Refund", Location = new Point(20, 130), Size = new Size(260, 40), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnReject = new Button() { Text = "Reject Request", Location = new Point(20, 185), Size = new Size(260, 40), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            actionPanel.Controls.Add(btnApprove);
            actionPanel.Controls.Add(btnReject);

            string selectedReturnId = "";

            dgvReturns.CellClick += (s, e) => {
                if (e.RowIndex >= 0 && e.RowIndex < dgvReturns.Rows.Count && !dgvReturns.Rows[e.RowIndex].IsNewRow)
                {
                    DataGridViewRow row = dgvReturns.Rows[e.RowIndex];
                    selectedReturnId = row.Cells["RetID"].Value?.ToString();
                    lblSelectedInfo.Text = $"Selected: {selectedReturnId}\nCustomer: {row.Cells["Customer"].Value}\nStatus: {row.Cells["RetStatus"].Value}";
                }
            };

            btnApprove.Click += (s, e) => {
                if (string.IsNullOrEmpty(selectedReturnId))
                {
                    MessageBox.Show("Choose a return request.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE Returns SET Status = 'Approved' WHERE ReturnID = @id";
                    using (var cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedReturnId);
                        cmd.ExecuteNonQuery();
                    }
                }

                dgvReturns.Rows.Clear();
                LoadAdminReturnsData(dgvReturns);
                MessageBox.Show("Return request approved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnReject.Click += (s, e) => {
                if (string.IsNullOrEmpty(selectedReturnId))
                {
                    MessageBox.Show("Choose a return request.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE Returns SET Status = 'Rejected' WHERE ReturnID = @id";
                    using (var cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedReturnId);
                        cmd.ExecuteNonQuery();
                    }
                }

                dgvReturns.Rows.Clear();
                LoadAdminReturnsData(dgvReturns);
                MessageBox.Show("Return request rejected successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        private void NavButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                if (index >= 0 && index < adminTabControl.TabCount)
                {
                    adminTabControl.SelectedIndex = index;
                }
            }
        }

        private void LoadProductsFromDb(DataGridView dgv)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ProdID, ProdName, Category, Size, Price, Stock FROM Products";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dgv.Rows.Add(
                                reader["ProdID"],
                                reader["ProdName"],
                                reader["Category"],
                                reader["Size"],
                                reader["Price"],
                                reader["Stock"]
                            );
                        }
                    }
                }
            }
        }

        private void LoadInventoryData(DataGridView dgv)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT ProdID, ProdName, Category, Size, Stock FROM Products";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int stock = int.TryParse(reader["Stock"]?.ToString(), out int s) ? s : 0;
                            string status = stock == 0 ? "Out of Stock" : (stock <= 10 ? "Low Stock" : "In Stock");

                            int rIdx = dgv.Rows.Add(
                                reader["ProdID"],
                                reader["ProdName"],
                                reader["Category"],
                                reader["Size"],
                                stock,
                                status
                            );

                            if (stock == 0) dgv.Rows[rIdx].Cells["InvStatus"].Style.ForeColor = Color.Red;
                            else if (stock <= 10) dgv.Rows[rIdx].Cells["InvStatus"].Style.ForeColor = Color.DarkOrange;
                            else dgv.Rows[rIdx].Cells["InvStatus"].Style.ForeColor = Color.Green;
                        }
                    }
                }
            }
        }

        private void UpdateInventoryStockInDb(string prodId, int newStock)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Products SET Stock=@stock WHERE ProdID=@id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@stock", newStock.ToString());
                    cmd.Parameters.AddWithValue("@id", prodId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void LoadOrdersFromDb(DataGridView dgv)
        {
            string currSymbol = GetCurrencySymbol();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Orders";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawTotal = reader["OrdTotal"]?.ToString() ?? "0";
                            foreach (string sym in new string[] { "₱", "$", "€" })
                            {
                                rawTotal = rawTotal.Replace(sym, "");
                            }
                            if (decimal.TryParse(rawTotal.Trim(), out decimal totalVal))
                            {
                                rawTotal = $"{currSymbol}{totalVal:N2}";
                            }
                            else
                            {
                                rawTotal = $"{currSymbol}{rawTotal}";
                            }

                            dgv.Rows.Add(
                                reader["OrdID"],
                                reader["OrdCustomer"],
                                reader["OrdDate"],
                                reader["OrdItems"],
                                rawTotal,
                                reader["OrdPayMethod"],
                                reader["OrdPayStatus"],
                                reader["OrdStatus"]
                            );
                        }
                    }
                }
            }
        }

        private void LoadCustomersFromDb(DataGridView dgv)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Customers";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int rIdx = dgv.Rows.Add(
                                reader["CustID"],
                                reader["CustName"],
                                reader["CustEmail"],
                                reader["CustContact"],
                                reader["CustAddress"],
                                reader["CustAccStatus"],
                                reader["CustRegDate"]
                            );
                            string accStatus = reader["CustAccStatus"]?.ToString()?.Trim() ?? "";
                            dgv.Rows[rIdx].Cells["CustAccStatus"].Style.ForeColor = accStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) ? Color.Green : Color.Red;
                        }
                    }
                }
            }
        }

        private void LoadStaffFromDb(DataGridView dgv)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Staff";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int rIdx = dgv.Rows.Add(
                                reader["StaffID"],
                                reader["StaffName"],
                                reader["StaffUsername"],
                                reader["StaffEmail"],
                                reader["StaffRole"],
                                reader["StaffStatus"],
                                reader["StaffDate"],
                                reader["StaffPassword"]
                            );
                            string staffStatus = reader["StaffStatus"]?.ToString()?.Trim() ?? "";
                            dgv.Rows[rIdx].Cells["StaffStatus"].Style.ForeColor = staffStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) ? Color.Green : Color.Red;
                        }
                    }
                }
            }
        }

        private void LoadTransactionsFromDb(DataGridView dgv)
        {
            string currSymbol = GetCurrencySymbol();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string syncQuery = @"INSERT OR IGNORE INTO Transactions (TransID, TransOrderID, TransDateTime, TransCustomer, TransStaff, TransItems, TransTotal, TransMethod, TransPayStatus, TransStatus) 
                     SELECT 'TRX-' || REPLACE(OrdID, 'ORD-', ''), OrdID, OrdDate, OrdCustomer, 'Admin', OrdItems, OrdTotal, OrdPayMethod, OrdPayStatus, OrdStatus 
                     FROM Orders;";
                using (var cmdSync = new SQLiteCommand(syncQuery, conn))
                {
                    cmdSync.ExecuteNonQuery();
                }

                string query = @"SELECT TransID, TransOrderID, TransDateTime, TransCustomer, TransStaff, TransItems, 
                 TransTotal, TransMethod, TransPayStatus, TransStatus 
                 FROM Transactions";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                            foreach (string sym in new string[] { "₱", "$", "€" })
                            {
                                rawTotal = rawTotal.Replace(sym, "");
                            }

                            if (decimal.TryParse(rawTotal.Trim(), out decimal amt))
                            {
                                rawTotal = $"{currSymbol}{amt:N2}";
                            }
                            else
                            {
                                rawTotal = $"{currSymbol}0.00";
                            }

                            dgv.Rows.Add(
                                reader["TransID"],
                                reader["TransOrderID"],
                                reader["TransDateTime"],
                                reader["TransCustomer"],
                                reader["TransStaff"],
                                reader["TransItems"],
                                rawTotal,
                                reader["TransMethod"],
                                reader["TransPayStatus"],
                                reader["TransStatus"]
                            );
                        }
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdminMainForm));
            this.SuspendLayout();
            
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AdminMainForm";
            this.Load += new System.EventHandler(this.AdminMainForm_Load);
            this.ResumeLayout(false);

        }

        private void AdminMainForm_Load(object sender, EventArgs e)
        {
            
        }

        private void SetupDashboardTab(TabPage dashboardTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Dashboard Overview";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            dashboardTab.Controls.Add(lblHeader);

            string[] cardTitles = { "Today's Sales", "Monthly Sales", "Total Products", "Total Orders", "Pending Orders", "Low Stock Items" };
            Color[] cardColors = { Color.FromArgb(41, 128, 185), Color.FromArgb(39, 174, 96), Color.FromArgb(142, 68, 173), Color.FromArgb(230, 126, 34), Color.FromArgb(211, 84, 0), Color.FromArgb(192, 57, 43) };

            int startX = 20;
            int startY = 50;
            int cardWidth = 300;
            int cardHeight = 78;

            Label[] valLabels = new Label[6];

            for (int i = 0; i < cardTitles.Length; i++)
            {
                Panel card = new Panel();
                card.Width = cardWidth;
                card.Height = cardHeight;
                card.BackColor = Color.White;
                card.Location = new Point(startX, startY);

                Panel accentBar = new Panel();
                accentBar.Dock = DockStyle.Left;
                accentBar.Width = 6;
                accentBar.BackColor = cardColors[i];
                card.Controls.Add(accentBar);

                Label lblTitle = new Label();
                lblTitle.Text = cardTitles[i];
                lblTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                lblTitle.ForeColor = Color.DimGray;
                lblTitle.Location = new Point(15, 10);
                lblTitle.AutoSize = true;
                card.Controls.Add(lblTitle);

                Label lblVal = new Label();
                lblVal.Text = "0";
                lblVal.Font = new Font("Segoe UI", 13, FontStyle.Bold);
                lblVal.ForeColor = Color.FromArgb(40, 40, 40);
                lblVal.Location = new Point(15, 33);
                lblVal.AutoSize = true;
                card.Controls.Add(lblVal);

                valLabels[i] = lblVal;
                dashboardTab.Controls.Add(card);

                startX += cardWidth + 15;
                if ((i + 1) % 3 == 0)
                {
                    startX = 20;
                    startY += cardHeight + 12;
                }
            }

            lblTodaySalesVal = valLabels[0];
            lblMonthlySalesVal = valLabels[1];
            lblTotalProdVal = valLabels[2];
            lblTotalOrdVal = valLabels[3];
            lblPendingOrdVal = valLabels[4];
            lblLowStockVal = valLabels[5];

            Label lblRecent = new Label();
            lblRecent.Text = "Recent Transactions";
            lblRecent.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblRecent.Location = new Point(20, 225);
            lblRecent.AutoSize = true;
            dashboardTab.Controls.Add(lblRecent);

            DataGridView dgvTransactions = new DataGridView();
            dgvDashboardRecentTrans = dgvTransactions;
            dgvTransactions.Location = new Point(20, 255);
            dgvTransactions.Size = new Size(930, 395);
            dgvTransactions.BackgroundColor = Color.White;
            dgvTransactions.BorderStyle = BorderStyle.FixedSingle;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.ReadOnly = true;

            dgvTransactions.Columns.Add("TransID", "Transaction ID");
            dgvTransactions.Columns.Add("Customer", "Customer Name");
            dgvTransactions.Columns.Add("Date", "Date & Time");
            dgvTransactions.Columns.Add("Total", "Total Amount");
            dgvTransactions.Columns.Add("Status", "Payment Status");

            dashboardTab.Controls.Add(dgvTransactions);

            RefreshDashboardData();
        }

        private void RefreshDashboardData()
        {
            string currSymbol = GetCurrencySymbol();
            decimal todaySales = 0;
            decimal monthlySales = 0;
            int totalProducts = 0;
            int totalOrders = 0;
            int pendingOrders = 0;
            int lowStockItems = 0;

            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            string currentMonthStr = DateTime.Now.ToString("yyyy-MM");

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string prodQuery = "SELECT Stock FROM Products";
                using (var cmd = new SQLiteCommand(prodQuery, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            totalProducts++;
                            if (int.TryParse(reader["Stock"]?.ToString(), out int stk) && stk <= 10)
                            {
                                lowStockItems++;
                            }
                        }
                    }
                }

                string ordQuery = "SELECT OrdID, OrdStatus FROM Orders";
                using (var cmd = new SQLiteCommand(ordQuery, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            totalOrders++;
                            string status = reader["OrdStatus"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(status) && status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                            {
                                pendingOrders++;
                            }
                        }
                    }
                }

                string transQuery = "SELECT TransDateTime, TransTotal FROM Transactions";
                using (var cmd = new SQLiteCommand(transQuery, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dtStr = reader["TransDateTime"]?.ToString() ?? "";
                            string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                            foreach (string sym in new string[] { "₱", "$", "€" })
                            {
                                rawTotal = rawTotal.Replace(sym, "");
                            }

                            if (decimal.TryParse(rawTotal.Trim(), out decimal amt))
                            {
                                if (dtStr.StartsWith(todayStr))
                                {
                                    todaySales += amt;
                                }
                                if (dtStr.StartsWith(currentMonthStr))
                                {
                                    monthlySales += amt;
                                }
                            }
                        }
                    }
                }

                if (dgvDashboardRecentTrans != null)
                {
                    dgvDashboardRecentTrans.Rows.Clear();
                    string recentQuery = "SELECT TransID, TransCustomer, TransDateTime, TransTotal, TransPayStatus FROM Transactions ORDER BY TransDateTime DESC LIMIT 5";
                    using (var cmd = new SQLiteCommand(recentQuery, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                                foreach (string sym in new string[] { "₱", "$", "€" })
                                {
                                    rawTotal = rawTotal.Replace(sym, "");
                                }

                                if (decimal.TryParse(rawTotal.Trim(), out decimal amt))
                                {
                                    rawTotal = $"{currSymbol}{amt:N2}";
                                }
                                else
                                {
                                    rawTotal = $"{currSymbol}0.00";
                                }

                                dgvDashboardRecentTrans.Rows.Add(
                                    reader["TransID"],
                                    reader["TransCustomer"],
                                    reader["TransDateTime"],
                                    rawTotal,
                                    reader["TransPayStatus"]
                                );
                            }
                        }
                    }
                }
            }

            if (lblTodaySalesVal != null) lblTodaySalesVal.Text = $"{currSymbol}{todaySales:N2}";
            if (lblMonthlySalesVal != null) lblMonthlySalesVal.Text = $"{currSymbol}{monthlySales:N2}";
            if (lblTotalProdVal != null) lblTotalProdVal.Text = totalProducts.ToString();
            if (lblTotalOrdVal != null) lblTotalOrdVal.Text = totalOrders.ToString();
            if (lblPendingOrdVal != null) lblPendingOrdVal.Text = pendingOrders.ToString();
            if (lblLowStockVal != null) lblLowStockVal.Text = lowStockItems.ToString();
        }

        private void SetupProductsTab(TabPage productsTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Product Management";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            productsTab.Controls.Add(lblHeader);

            Label lblSearchProd = new Label() { Text = "Search ID / Name:", Location = new Point(340, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSearchProd = new TextBox() { Location = new Point(465, 17), Size = new Size(180, 23) };
            Button btnSearchProd = new Button() { Text = "Search", Location = new Point(655, 15), Size = new Size(75, 26), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            productsTab.Controls.Add(lblSearchProd);
            productsTab.Controls.Add(txtSearchProd);
            productsTab.Controls.Add(btnSearchProd);

            Panel entryPanel = new Panel();
            entryPanel.Location = new Point(20, 50);
            entryPanel.Size = new Size(300, 610);
            entryPanel.BackColor = Color.White;
            entryPanel.BorderStyle = BorderStyle.FixedSingle;
            productsTab.Controls.Add(entryPanel);

            PictureBox picProduct = new PictureBox();
            picProduct.Location = new Point(75, 12);
            picProduct.Size = new Size(130, 130);
            picProduct.BorderStyle = BorderStyle.FixedSingle;
            picProduct.SizeMode = PictureBoxSizeMode.StretchImage;
            picProduct.BackColor = Color.WhiteSmoke;
            entryPanel.Controls.Add(picProduct);

            Button btnBrowse = new Button();
            btnBrowse.Text = "Browse Image";
            btnBrowse.Location = new Point(75, 148);
            btnBrowse.Size = new Size(130, 26);
            btnBrowse.BackColor = Color.FromArgb(41, 128, 185);
            btnBrowse.ForeColor = Color.White;
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.Click += (s, e) => {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select Product Image";
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    picProduct.Image = Image.FromFile(openFileDialog.FileName);
                }
            };
            entryPanel.Controls.Add(btnBrowse);

            int yPos = 180;
            Label lblCode = new Label() { Text = "Product Code:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            TextBox txtCode = new TextBox() { Text = "PRD-003", Location = new Point(20, yPos + 18), Size = new Size(255, 23) };

            yPos += 45;
            Label lblName = new Label() { Text = "Product Name:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            TextBox txtName = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(255, 23) };

            yPos += 45;
            Label lblCat = new Label() { Text = "Category:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            ComboBox cmbCategory = new ComboBox() { Location = new Point(20, yPos + 18), Size = new Size(255, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategory.Items.AddRange(new object[] { "T-Shirt (Men)", "T-Shirt (Women)", "Long Skirt", "Short Skirt", "Shorts", "Pants", "Jacket", "Dress" });

            yPos += 45;
            Label lblSize = new Label() { Text = "Size:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            ComboBox cmbSize = new ComboBox() { Location = new Point(20, yPos + 18), Size = new Size(255, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSize.Items.AddRange(new object[] { "S", "M", "L", "XL", "XXL", "XXXL" });
            cmbSize.SelectedIndex = 1;

            yPos += 45;
            string currSymbol = GetCurrencySymbol();
            lblPriceRef = new Label() { Text = $"Price ({currSymbol}):", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            TextBox txtPrice = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(255, 23) };

            yPos += 45;
            Label lblStock = new Label() { Text = "Stock Qty:", Location = new Point(20, yPos), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            TextBox txtStock = new TextBox() { Location = new Point(20, yPos + 18), Size = new Size(255, 23) };

            entryPanel.Controls.AddRange(new Control[] { lblCode, txtCode, lblName, txtName, lblCat, cmbCategory, lblSize, cmbSize, lblPriceRef, txtPrice, lblStock, txtStock });

            DataGridView dgvProducts = new DataGridView();
            dgvProductsRef = dgvProducts;
            dgvProducts.Location = new Point(340, 50);
            dgvProducts.Size = new Size(675, 610);
            dgvProducts.BackgroundColor = Color.White;
            dgvProducts.BorderStyle = BorderStyle.FixedSingle;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.ReadOnly = true;

            dgvProducts.Columns.Add("ProdID", "Code");
            dgvProducts.Columns.Add("ProdName", "Product Name");
            dgvProducts.Columns.Add("Category", "Category");
            dgvProducts.Columns.Add("Size", "Size");
            dgvProducts.Columns.Add("Price", "Price");
            dgvProducts.Columns.Add("Stock", "Stock");

            LoadProductsFromDb(dgvProducts);
            productsTab.Controls.Add(dgvProducts);

            Button btnAdd = new Button() { Text = "Add", Location = new Point(15, 565), Size = new Size(63, 32), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnUpdate = new Button() { Text = "Update", Location = new Point(83, 565), Size = new Size(63, 32), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnDelete = new Button() { Text = "Delete", Location = new Point(151, 565), Size = new Size(63, 32), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnClear = new Button() { Text = "Clear", Location = new Point(219, 565), Size = new Size(63, 32), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            entryPanel.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });

            Action executeSearch = () => {
                string keyword = txtSearchProd.Text.Trim().ToLower();
                dgvProducts.Rows.Clear();

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ProdID, ProdName, Category, Size, Price, Stock FROM Products WHERE LOWER(ProdID) LIKE @kw OR LOWER(ProdName) LIKE @kw";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dgvProducts.Rows.Add(
                                    reader["ProdID"],
                                    reader["ProdName"],
                                    reader["Category"],
                                    reader["Size"],
                                    reader["Price"],
                                    reader["Stock"]
                                );
                            }
                        }
                    }
                }
            };

            btnSearchProd.Click += (s, e) => executeSearch();
            txtSearchProd.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    executeSearch();
                    e.SuppressKeyPress = true;
                }
            };

            btnAdd.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Please fill in the product code and name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] imageBytes = null;
                if (picProduct.Image != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        picProduct.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        imageBytes = ms.ToArray();
                    }
                }

                string activeSymbol = GetCurrencySymbol();
                string finalPriceValue = ConvertPriceInput(txtPrice.Text.Trim());

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT OR REPLACE INTO Products (ProdID, ProdName, Category, Size, Price, Stock, ProdImage) VALUES (@id, @name, @cat, @size, @price, @stock, @img)";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", txtCode.Text);
                        cmd.Parameters.AddWithValue("@name", txtName.Text);
                        cmd.Parameters.AddWithValue("@cat", cmbCategory.Text);
                        cmd.Parameters.AddWithValue("@size", cmbSize.Text);
                        cmd.Parameters.AddWithValue("@price", activeSymbol + finalPriceValue);
                        cmd.Parameters.AddWithValue("@stock", txtStock.Text);
                        cmd.Parameters.AddWithValue("@img", imageBytes != null ? (object)imageBytes : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                dgvProducts.Rows.Clear();
                LoadProductsFromDb(dgvProducts);

                if (int.TryParse(txtStock.Text, out int addedStock) && addedStock <= 10)
                {
                    TriggerLowStockAlert(txtName.Text, addedStock);
                }

                MessageBox.Show("Product added and converted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            dgvProducts.CellClick += (s, e) => {
                if (e.RowIndex >= 0 && e.RowIndex < dgvProducts.Rows.Count && !dgvProducts.Rows[e.RowIndex].IsNewRow)
                {
                    DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
                    string prodId = row.Cells["ProdID"].Value?.ToString();
                    txtCode.Text = prodId;
                    txtName.Text = row.Cells["ProdName"].Value?.ToString();
                    cmbCategory.Text = row.Cells["Category"].Value?.ToString();
                    cmbSize.Text = row.Cells["Size"].Value?.ToString();

                    string rawPrice = row.Cells["Price"].Value?.ToString() ?? "";
                    foreach (string sym in new string[] { "₱", "$", "€" })
                    {
                        rawPrice = rawPrice.Replace(sym, "");
                    }
                    txtPrice.Text = rawPrice.Trim();
                    txtStock.Text = row.Cells["Stock"].Value?.ToString();

                    using (var conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        string query = "SELECT ProdImage FROM Products WHERE ProdID=@id";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", prodId);
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                byte[] imgBytes = (byte[])result;
                                using (MemoryStream ms = new MemoryStream(imgBytes))
                                {
                                    picProduct.Image = Image.FromStream(ms);
                                }
                            }
                            else
                            {
                                picProduct.Image = null;
                            }
                        }
                    }
                }
            };

            btnUpdate.Click += (s, e) => {
                if (dgvProducts.SelectedRows.Count > 0 && !dgvProducts.SelectedRows[0].IsNewRow)
                {
                    byte[] imageBytes = null;
                    if (picProduct.Image != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            picProduct.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            imageBytes = ms.ToArray();
                        }
                    }

                    string activeSymbol = GetCurrencySymbol();
                    string finalPriceValue = ConvertPriceInput(txtPrice.Text.Trim());

                    using (var conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        string query = "UPDATE Products SET ProdName=@name, Category=@cat, Size=@size, Price=@price, Stock=@stock, ProdImage=@img WHERE ProdID=@id";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", txtCode.Text);
                            cmd.Parameters.AddWithValue("@name", txtName.Text);
                            cmd.Parameters.AddWithValue("@cat", cmbCategory.Text);
                            cmd.Parameters.AddWithValue("@size", cmbSize.Text);
                            cmd.Parameters.AddWithValue("@price", activeSymbol + finalPriceValue);
                            cmd.Parameters.AddWithValue("@stock", txtStock.Text);
                            cmd.Parameters.AddWithValue("@img", imageBytes != null ? (object)imageBytes : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    dgvProducts.Rows.Clear();
                    LoadProductsFromDb(dgvProducts);

                    if (int.TryParse(txtStock.Text, out int updatedStock) && updatedStock <= 10)
                    {
                        TriggerLowStockAlert(txtName.Text, updatedStock);
                    }

                    MessageBox.Show("Product updated and converted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select a product row to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnDelete.Click += (s, e) => {
                if (dgvProducts.SelectedRows.Count > 0 && !dgvProducts.SelectedRows[0].IsNewRow)
                {
                    if (MessageBox.Show("Are you sure you want to delete this product?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string prodId = dgvProducts.SelectedRows[0].Cells["ProdID"].Value?.ToString();
                        using (var conn = new SQLiteConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM Products WHERE ProdID=@id";
                            using (var cmd = new SQLiteCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", prodId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        dgvProducts.Rows.Clear();
                        LoadProductsFromDb(dgvProducts);
                        MessageBox.Show("Product deleted successfully from database!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a product row to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnClear.Click += (s, e) => {
                txtCode.Clear();
                txtName.Clear();
                cmbCategory.SelectedIndex = -1;
                cmbSize.SelectedIndex = 0;
                txtPrice.Clear();
                txtStock.Clear();
                picProduct.Image = null;
            };
        }

        private void SetupInventoryTab(TabPage inventoryTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Inventory Stock Monitor";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            inventoryTab.Controls.Add(lblHeader);

            Label lblSearchInv = new Label() { Text = "Search Product / Item:", Location = new Point(340, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSearchInv = new TextBox() { Location = new Point(480, 17), Size = new Size(180, 23) };
            Button btnSearchInv = new Button() { Text = "Search", Location = new Point(665, 15), Size = new Size(75, 26), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            inventoryTab.Controls.Add(lblSearchInv);
            inventoryTab.Controls.Add(txtSearchInv);
            inventoryTab.Controls.Add(btnSearchInv);

            DataGridView dgvInventory = new DataGridView();
            dgvInventoryRef = dgvInventory;
            dgvInventory.Location = new Point(20, 60);
            dgvInventory.Size = new Size(680, 600);
            dgvInventory.BackgroundColor = Color.White;
            dgvInventory.BorderStyle = BorderStyle.FixedSingle;
            dgvInventory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvInventory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInventory.ReadOnly = true;

            dgvInventory.Columns.Add("InvCode", "Code");
            dgvInventory.Columns.Add("InvName", "Product Name");
            dgvInventory.Columns.Add("InvCategory", "Category");
            dgvInventory.Columns.Add("InvSize", "Size");
            dgvInventory.Columns.Add("InvStock", "Current Stock");
            dgvInventory.Columns.Add("InvStatus", "Status");

            LoadInventoryData(dgvInventory);
            inventoryTab.Controls.Add(dgvInventory);

            Panel stockPanel = new Panel();
            stockPanel.Location = new Point(715, 60);
            stockPanel.Size = new Size(300, 600);
            stockPanel.BackColor = Color.White;
            stockPanel.BorderStyle = BorderStyle.FixedSingle;
            inventoryTab.Controls.Add(stockPanel);

            Label lblPanelTitle = new Label() { Text = "Stock Management", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            stockPanel.Controls.Add(lblPanelTitle);

            Label lblSelectedInfo = new Label() { Text = "Select an item from the list to update its stock level.", Location = new Point(20, 60), Size = new Size(260, 40), Font = new Font("Segoe UI", 9, FontStyle.Regular), ForeColor = Color.DimGray };
            stockPanel.Controls.Add(lblSelectedInfo);

            Label lblAddQty = new Label() { Text = "Add / Deduct Quantity:", Location = new Point(20, 120), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtAdjustQty = new TextBox() { Location = new Point(20, 145), Size = new Size(260, 25) };
            stockPanel.Controls.Add(lblAddQty);
            stockPanel.Controls.Add(txtAdjustQty);

            Button btnStockIn = new Button() { Text = "Stock In (+)", Location = new Point(20, 190), Size = new Size(122, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnStockOut = new Button() { Text = "Stock Out (-)", Location = new Point(158, 190), Size = new Size(122, 35), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            stockPanel.Controls.Add(btnStockIn);
            stockPanel.Controls.Add(btnStockOut);

            string selectedCode = "";
            int selectedRowIdx = -1;

            dgvInventory.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < dgvInventory.Rows.Count && !dgvInventory.Rows[e.RowIndex].IsNewRow)
                {
                    DataGridViewRow row = dgvInventory.Rows[e.RowIndex];
                    selectedCode = row.Cells["InvCode"].Value?.ToString();
                    selectedRowIdx = e.RowIndex;
                    lblSelectedInfo.Text = $"Selected: {row.Cells["InvName"].Value} ({row.Cells["InvSize"].Value})\nCurrent Stock: {row.Cells["InvStock"].Value}";
                }
            };

            btnStockIn.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(selectedCode) || selectedRowIdx == -1)
                {
                    MessageBox.Show("Please select an item from the inventory table first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (int.TryParse(txtAdjustQty.Text, out int addQty) && addQty > 0)
                {
                    DataGridViewRow row = dgvInventory.Rows[selectedRowIdx];
                    int currentStock = int.Parse(row.Cells["InvStock"].Value.ToString());
                    int newStock = currentStock + addQty;

                    UpdateInventoryStockInDb(selectedCode, newStock);
                    dgvInventory.Rows.Clear();
                    LoadInventoryData(dgvInventory);

                    MessageBox.Show("Stock added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtAdjustQty.Clear();
                }
                else
                {
                    MessageBox.Show("Please enter a valid positive number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnStockOut.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(selectedCode) || selectedRowIdx == -1)
                {
                    MessageBox.Show("Please select an item from the inventory table first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (int.TryParse(txtAdjustQty.Text, out int minusQty) && minusQty > 0)
                {
                    DataGridViewRow row = dgvInventory.Rows[selectedRowIdx];
                    string prodName = row.Cells["InvName"].Value?.ToString();
                    int currentStock = int.Parse(row.Cells["InvStock"].Value.ToString());

                    if (minusQty > currentStock)
                    {
                        MessageBox.Show("Cannot deduct more than the current stock!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    int newStock = currentStock - minusQty;
                    UpdateInventoryStockInDb(selectedCode, newStock);
                    dgvInventory.Rows.Clear();
                    LoadInventoryData(dgvInventory);

                    if (newStock <= 10)
                    {
                        TriggerLowStockAlert(prodName, newStock);
                    }

                    MessageBox.Show("Stock deducted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtAdjustQty.Clear();
                }
                else
                {
                    MessageBox.Show("Please enter a valid positive number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            Action executeInvSearch = () => {
                string keyword = txtSearchInv.Text.Trim().ToLower();
                dgvInventory.Rows.Clear();

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ProdID, ProdName, Category, Size, Stock FROM Products WHERE LOWER(ProdID) LIKE @kw OR LOWER(ProdName) LIKE @kw OR LOWER(Category) LIKE @kw";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int stock = int.TryParse(reader["Stock"]?.ToString(), out int s) ? s : 0;
                                string status = stock == 0 ? "Out of Stock" : (stock <= 10 ? "Low Stock" : "In Stock");

                                int rIdx = dgvInventory.Rows.Add(
                                    reader["ProdID"],
                                    reader["ProdName"],
                                    reader["Category"],
                                    reader["Size"],
                                    stock,
                                    status
                                );

                                if (stock == 0) dgvInventory.Rows[rIdx].Cells["InvStatus"].Style.ForeColor = Color.Red;
                                else if (stock <= 10) dgvInventory.Rows[rIdx].Cells["InvStatus"].Style.ForeColor = Color.DarkOrange;
                                else dgvInventory.Rows[rIdx].Cells["InvStatus"].Style.ForeColor = Color.Green;
                            }
                        }
                    }
                }
            };

            btnSearchInv.Click += (s, e) => executeInvSearch();
            txtSearchInv.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    executeInvSearch();
                    e.SuppressKeyPress = true;
                }
            };
            
        }
        

        private void TriggerLowStockAlert(string productName, int stockLevel)
        {
            if (!enableEmailNotif) return;

            string statusText = stockLevel == 0 ? "OUT OF STOCK" : "LOW STOCK";
            string message = $"Alert: The product '{productName}' is at the level of {stockLevel} ({statusText}). Please check the inventory and restock as needed.";

            MessageBox.Show(message, $"Inventory Notification - {statusText}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetupOrdersTab(TabPage ordersTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Order Management";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            ordersTab.Controls.Add(lblHeader);

            Label lblSearch = new Label() { Text = "Search Order ID / Customer:", Location = new Point(340, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSearchOrder = new TextBox() { Location = new Point(510, 17), Size = new Size(190, 23) };
            ordersTab.Controls.Add(lblSearch);
            ordersTab.Controls.Add(txtSearchOrder);

            DataGridView dgvOrders = new DataGridView();
            dgvOrdersRef = dgvOrders;
            dgvOrders.Location = new Point(20, 60);
            dgvOrders.Size = new Size(680, 600);
            dgvOrders.BackgroundColor = Color.White;
            dgvOrders.BorderStyle = BorderStyle.FixedSingle;
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOrders.ReadOnly = true;

            dgvOrders.Columns.Add("OrdID", "Order ID");
            dgvOrders.Columns.Add("OrdCustomer", "Customer");
            dgvOrders.Columns.Add("OrdDate", "Date");
            dgvOrders.Columns.Add("OrdItems", "Items");
            dgvOrders.Columns.Add("OrdTotal", "Total");
            dgvOrders.Columns.Add("OrdPayMethod", "Method");
            dgvOrders.Columns.Add("OrdPayStatus", "Status");
            dgvOrders.Columns.Add("OrdStatus", "Order Status");

            LoadOrdersFromDb(dgvOrders);
            ordersTab.Controls.Add(dgvOrders);

            txtSearchOrder.TextChanged += (s, e) => {
                string keyword = txtSearchOrder.Text.Trim().ToLower();
                dgvOrders.Rows.Clear();

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Orders WHERE LOWER(OrdID) LIKE @kw OR LOWER(OrdCustomer) LIKE @kw";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dgvOrders.Rows.Add(reader["OrdID"], reader["OrdCustomer"], reader["OrdDate"], reader["OrdItems"], reader["OrdTotal"], reader["OrdPayMethod"], reader["OrdPayStatus"], reader["OrdStatus"]);
                            }
                        }
                    }
                }
            };

            Panel orderCtrlPanel = new Panel();
            orderCtrlPanel.Location = new Point(715, 60);
            orderCtrlPanel.Size = new Size(300, 600);
            orderCtrlPanel.BackColor = Color.White;
            orderCtrlPanel.BorderStyle = BorderStyle.FixedSingle;
            ordersTab.Controls.Add(orderCtrlPanel);

            Label lblPanelTitle = new Label() { Text = "Order Actions", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            orderCtrlPanel.Controls.Add(lblPanelTitle);

            Label lblSelectedOrder = new Label() { Text = "Select an order from the list.", Location = new Point(20, 55), Size = new Size(260, 45), Font = new Font("Segoe UI", 9, FontStyle.Regular), ForeColor = Color.DimGray };
            orderCtrlPanel.Controls.Add(lblSelectedOrder);

            Button btnViewDetails = new Button() { Text = "View Order Details", Location = new Point(20, 105), Size = new Size(260, 32), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            orderCtrlPanel.Controls.Add(btnViewDetails);

            Label lblStatusLabel = new Label() { Text = "Update Order Status:", Location = new Point(20, 148), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            ComboBox cmbNewStatus = new ComboBox() { Location = new Point(20, 170), Size = new Size(260, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbNewStatus.Items.AddRange(new object[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" });
            orderCtrlPanel.Controls.Add(lblStatusLabel);
            orderCtrlPanel.Controls.Add(cmbNewStatus);

            Button btnUpdateStatus = new Button() { Text = "Update Status", Location = new Point(20, 205), Size = new Size(260, 32), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            orderCtrlPanel.Controls.Add(btnUpdateStatus);

            Button btnCancelOrder = new Button() { Text = "Cancel Order", Location = new Point(20, 248), Size = new Size(260, 32), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            orderCtrlPanel.Controls.Add(btnCancelOrder);

            dgvOrders.CellClick += (s, e) => {
                if (e.RowIndex >= 0 && e.RowIndex < dgvOrders.Rows.Count && !dgvOrders.Rows[e.RowIndex].IsNewRow)
                {
                    DataGridViewRow row = dgvOrders.Rows[e.RowIndex];
                    lblSelectedOrder.Text = $"Selected Order: {row.Cells["OrdID"].Value}\nCustomer: {row.Cells["OrdCustomer"].Value}";
                    string val = row.Cells["OrdStatus"].Value?.ToString();
                    if (!string.IsNullOrEmpty(val) && cmbNewStatus.Items.Contains(val))
                    {
                        cmbNewStatus.SelectedItem = val;
                    }
                }
            };

            btnViewDetails.Click += (s, e) => {
                if (dgvOrders.SelectedRows.Count > 0 && !dgvOrders.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvOrders.SelectedRows[0];
                    string details = $"Order ID: {row.Cells["OrdID"].Value}\n" +
                                     $"Customer: {row.Cells["OrdCustomer"].Value}\n" +
                                     $"Date: {row.Cells["OrdDate"].Value}\n" +
                                     $"Items: {row.Cells["OrdItems"].Value}\n" +
                                     $"Total Amount: {row.Cells["OrdTotal"].Value}\n" +
                                     $"Payment Method: {row.Cells["OrdPayMethod"].Value}\n" +
                                     $"Payment Status: {row.Cells["OrdPayStatus"].Value}\n" +
                                     $"Order Status: {row.Cells["OrdStatus"].Value}";
                    MessageBox.Show(details, "Order Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select an order first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnUpdateStatus.Click += (s, e) => {
                if (dgvOrders.SelectedRows.Count > 0 && !dgvOrders.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvOrders.SelectedRows[0];
                    string ordId = row.Cells["OrdID"].Value?.ToString();
                    string newStatus = cmbNewStatus.SelectedItem?.ToString() ?? "Pending";
                    string customer = row.Cells["OrdCustomer"].Value?.ToString();
                    string date = row.Cells["OrdDate"].Value?.ToString();
                    string items = row.Cells["OrdItems"].Value?.ToString();
                    string total = row.Cells["OrdTotal"].Value?.ToString();
                    string method = row.Cells["OrdPayMethod"].Value?.ToString();
                    string payStatus = row.Cells["OrdPayStatus"].Value?.ToString();

                    string oldStatus = row.Cells["OrdStatus"].Value?.ToString();

                    using (var conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();

                        
                        string updateOrdQuery = "UPDATE Orders SET OrdStatus=@status WHERE OrdID=@id";
                        using (var cmd = new SQLiteCommand(updateOrdQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@status", newStatus);
                            cmd.Parameters.AddWithValue("@id", ordId);
                            cmd.ExecuteNonQuery();
                        }

                        if ((newStatus == "Processing" || newStatus == "Delivered") && (oldStatus == "Pending" || oldStatus == "Cancelled"))
                        {
                            DeductStockFromItems(items, conn);
                        }

                        string transId = "TRX-" + ordId.Replace("ORD-", "");
                        string upsertTransQuery = @"
                INSERT INTO Transactions (TransID, TransOrderID, TransDateTime, TransCustomer, TransStaff, TransItems, TransTotal, TransMethod, TransPayStatus, TransStatus)
                VALUES (@transId, @ordId, @date, @customer, 'Admin', @items, @total, @method, @payStatus, @status)
                ON CONFLICT(TransID) DO UPDATE SET TransStatus=@status, TransPayStatus=@payStatus;
            ";
                        using (var cmdTrans = new SQLiteCommand(upsertTransQuery, conn))
                        {
                            cmdTrans.Parameters.AddWithValue("@transId", transId);
                            cmdTrans.Parameters.AddWithValue("@ordId", ordId);
                            cmdTrans.Parameters.AddWithValue("@date", date);
                            cmdTrans.Parameters.AddWithValue("@customer", customer);
                            cmdTrans.Parameters.AddWithValue("@items", items);
                            cmdTrans.Parameters.AddWithValue("@total", total);
                            cmdTrans.Parameters.AddWithValue("@method", method);
                            cmdTrans.Parameters.AddWithValue("@payStatus", payStatus);
                            cmdTrans.Parameters.AddWithValue("@status", newStatus);
                            cmdTrans.ExecuteNonQuery();
                        }
                    }

                    dgvOrders.Rows.Clear();
                    LoadOrdersFromDb(dgvOrders);

                    if (dgvTransactionsRef != null)
                    {
                        dgvTransactionsRef.Rows.Clear();
                        LoadTransactionsFromDb(dgvTransactionsRef);
                    }

                    RefreshDashboardData();

                    MessageBox.Show("Order status updated, stock deducted, and successfully synced!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select an order to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnCancelOrder.Click += (s, e) => {
                if (dgvOrders.SelectedRows.Count > 0 && !dgvOrders.SelectedRows[0].IsNewRow)
                {
                    if (MessageBox.Show("Are you sure you want to cancel this order?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        DataGridViewRow row = dgvOrders.SelectedRows[0];
                        string ordId = row.Cells["OrdID"].Value?.ToString();
                        string customer = row.Cells["OrdCustomer"].Value?.ToString();
                        string date = row.Cells["OrdDate"].Value?.ToString();
                        string items = row.Cells["OrdItems"].Value?.ToString();
                        string total = row.Cells["OrdTotal"].Value?.ToString();
                        string method = row.Cells["OrdPayMethod"].Value?.ToString();
                        string payStatus = row.Cells["OrdPayStatus"].Value?.ToString();

                        using (var conn = new SQLiteConnection(connectionString))
                        {
                            conn.Open();

                            string updateOrdQuery = "UPDATE Orders SET OrdStatus='Cancelled' WHERE OrdID=@id";
                            using (var cmd = new SQLiteCommand(updateOrdQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", ordId);
                                cmd.ExecuteNonQuery();
                            }

                            string transId = "TRX-" + ordId.Replace("ORD-", "");
                            string upsertTransQuery = @"
                        INSERT INTO Transactions (TransID, TransOrderID, TransDateTime, TransCustomer, TransStaff, TransItems, TransTotal, TransMethod, TransPayStatus, TransStatus)
                        VALUES (@transId, @ordId, @date, @customer, 'Admin', @items, @total, @method, @payStatus, 'Cancelled')
                        ON CONFLICT(TransID) DO UPDATE SET TransStatus='Cancelled';
                    ";
                            using (var cmdTrans = new SQLiteCommand(upsertTransQuery, conn))
                            {
                                cmdTrans.Parameters.AddWithValue("@transId", transId);
                                cmdTrans.Parameters.AddWithValue("@ordId", ordId);
                                cmdTrans.Parameters.AddWithValue("@date", date);
                                cmdTrans.Parameters.AddWithValue("@customer", customer);
                                cmdTrans.Parameters.AddWithValue("@items", items);
                                cmdTrans.Parameters.AddWithValue("@total", total);
                                cmdTrans.Parameters.AddWithValue("@method", method);
                                cmdTrans.Parameters.AddWithValue("@payStatus", payStatus);
                                cmdTrans.ExecuteNonQuery();
                            }
                        }

                        dgvOrders.Rows.Clear();
                        LoadOrdersFromDb(dgvOrders);

                        if (dgvTransactionsRef != null)
                        {
                            dgvTransactionsRef.Rows.Clear();
                            LoadTransactionsFromDb(dgvTransactionsRef);
                        }

                        RefreshDashboardData();

                        MessageBox.Show("Order cancelled and synced to Transactions & Dashboard successfully.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select an order to cancel.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }

        private void SetupCustomersTab(TabPage customersTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Customer Management";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            customersTab.Controls.Add(lblHeader);

            Label lblSearch = new Label() { Text = "Search Customer:", Location = new Point(380, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSearchCustomer = new TextBox() { Location = new Point(500, 17), Size = new Size(200, 23) };
            customersTab.Controls.Add(lblSearch);
            customersTab.Controls.Add(txtSearchCustomer);

            DataGridView dgvCustomers = new DataGridView();
            dgvCustomersRef = dgvCustomers;
            dgvCustomers.Location = new Point(20, 60);
            dgvCustomers.Size = new Size(680, 600);
            dgvCustomers.BackgroundColor = Color.White;
            dgvCustomers.BorderStyle = BorderStyle.FixedSingle;
            dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCustomers.ReadOnly = true;

            dgvCustomers.Columns.Add("CustID", "Customer ID");
            dgvCustomers.Columns.Add("CustName", "Name");
            dgvCustomers.Columns.Add("CustEmail", "Email");
            dgvCustomers.Columns.Add("CustContact", "Contact Number");
            dgvCustomers.Columns.Add("CustAddress", "Address");
            dgvCustomers.Columns.Add("CustAccStatus", "Account");
            dgvCustomers.Columns.Add("CustRegDate", "Registration");

            LoadCustomersFromDb(dgvCustomers);
            customersTab.Controls.Add(dgvCustomers);

            txtSearchCustomer.TextChanged += (s, e) => {
                string keyword = txtSearchCustomer.Text.Trim().ToLower();
                dgvCustomers.Rows.Clear();
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Customers WHERE LOWER(CustID) LIKE @kw OR LOWER(CustName) LIKE @kw OR LOWER(CustEmail) LIKE @kw";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int rIdx = dgvCustomers.Rows.Add(reader["CustID"], reader["CustName"], reader["CustEmail"], reader["CustContact"], reader["CustAddress"], reader["CustAccStatus"], reader["CustRegDate"]);
                                dgvCustomers.Rows[rIdx].Cells["CustAccStatus"].Style.ForeColor = reader["CustAccStatus"].ToString().Trim().Equals("Active", StringComparison.OrdinalIgnoreCase) ? Color.Green : Color.Red;
                            }
                        }
                    }
                }
            };

            Panel custPanel = new Panel();
            custPanel.Location = new Point(715, 60);
            custPanel.Size = new Size(300, 600);
            custPanel.BackColor = Color.White;
            custPanel.BorderStyle = BorderStyle.FixedSingle;
            customersTab.Controls.Add(custPanel);

            Label lblCustAction = new Label() { Text = "Customer Actions", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            custPanel.Controls.Add(lblCustAction);

            Button btnViewCust = new Button() { Text = "View Details", Location = new Point(20, 70), Size = new Size(260, 35), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnToggleAcc = new Button() { Text = "Activate / Deactivate Account", Location = new Point(20, 115), Size = new Size(260, 35), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnDeleteCust = new Button() { Text = "Delete Customer", Location = new Point(20, 160), Size = new Size(260, 35), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            custPanel.Controls.AddRange(new Control[] { btnViewCust, btnToggleAcc, btnDeleteCust });

            btnViewCust.Click += (s, e) => {
                if (dgvCustomers.SelectedRows.Count > 0 && !dgvCustomers.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvCustomers.SelectedRows[0];
                    string details = $"Customer ID: {row.Cells["CustID"].Value}\n" +
                                     $"Name: {row.Cells["CustName"].Value}\n" +
                                     $"Email: {row.Cells["CustEmail"].Value}\n" +
                                     $"Contact: {row.Cells["CustContact"].Value}\n" +
                                     $"Address: {row.Cells["CustAddress"].Value}\n" +
                                     $"Account Status: {row.Cells["CustAccStatus"].Value}\n" +
                                     $"Registered Date: {row.Cells["CustRegDate"].Value}";
                    MessageBox.Show(details, "Customer Profile Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select a customer first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnToggleAcc.Click += (s, e) => {
                if (dgvCustomers.SelectedRows.Count > 0 && !dgvCustomers.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvCustomers.SelectedRows[0];
                    string custId = row.Cells["CustID"].Value?.ToString();
                    string currentStatus = row.Cells["CustAccStatus"].Value?.ToString().Trim();
                    string newStatus = (currentStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)) ? "Inactive" : "Active";

                    using (var conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        string query = "UPDATE Customers SET CustAccStatus=@status WHERE CustID=@id";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@status", newStatus);
                            cmd.Parameters.AddWithValue("@id", custId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    dgvCustomers.Rows.Clear();
                    LoadCustomersFromDb(dgvCustomers);
                    MessageBox.Show($"Customer status changed to {newStatus}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select a customer first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnDeleteCust.Click += (s, e) => {
                if (dgvCustomers.SelectedRows.Count > 0 && !dgvCustomers.SelectedRows[0].IsNewRow)
                {
                    if (MessageBox.Show("Are you sure you want to delete this customer?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string custId = dgvCustomers.SelectedRows[0].Cells["CustID"].Value?.ToString();
                        using (var conn = new SQLiteConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM Customers WHERE CustID=@id";
                            using (var cmd = new SQLiteCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", custId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        dgvCustomers.Rows.Clear();
                        LoadCustomersFromDb(dgvCustomers);
                        MessageBox.Show("Customer deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a customer to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }

        private void SetupStaffTab(TabPage staffTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Staff Management";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            staffTab.Controls.Add(lblHeader);

            Label lblSearch = new Label() { Text = "Search Name/Username:", Location = new Point(360, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSearchStaff = new TextBox() { Location = new Point(515, 17), Size = new Size(185, 23) };
            staffTab.Controls.Add(lblSearch);
            staffTab.Controls.Add(txtSearchStaff);

            DataGridView dgvStaff = new DataGridView();
            dgvStaffRef = dgvStaff;
            dgvStaff.Location = new Point(20, 60);
            dgvStaff.Size = new Size(680, 600);
            dgvStaff.BackgroundColor = Color.White;
            dgvStaff.BorderStyle = BorderStyle.FixedSingle;
            dgvStaff.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvStaff.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvStaff.ReadOnly = true;

            dgvStaff.Columns.Add("StaffID", "User ID");
            dgvStaff.Columns.Add("StaffName", "Name");
            dgvStaff.Columns.Add("StaffUsername", "Username");
            dgvStaff.Columns.Add("StaffEmail", "Email");
            dgvStaff.Columns.Add("StaffRole", "Role");
            dgvStaff.Columns.Add("StaffStatus", "Account status");
            dgvStaff.Columns.Add("StaffDate", "Date created");
            dgvStaff.Columns.Add("StaffPassword", "Password");

            LoadStaffFromDb(dgvStaff);
            staffTab.Controls.Add(dgvStaff);

            txtSearchStaff.TextChanged += (s, e) => {
                string keyword = txtSearchStaff.Text.Trim().ToLower();
                dgvStaff.Rows.Clear();
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Staff WHERE LOWER(StaffName) LIKE @kw OR LOWER(StaffUsername) LIKE @kw";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int rIdx = dgvStaff.Rows.Add(reader["StaffID"], reader["StaffName"], reader["StaffUsername"], reader["StaffEmail"], reader["StaffRole"], reader["StaffStatus"], reader["StaffDate"], reader["StaffPassword"]);
                                dgvStaff.Rows[rIdx].Cells["StaffStatus"].Style.ForeColor = reader["StaffStatus"].ToString().Trim().Equals("Active", StringComparison.OrdinalIgnoreCase) ? Color.Green : Color.Red;
                            }
                        }
                    }
                }
            };

            Panel staffPanel = new Panel();
            staffPanel.Location = new Point(715, 60);
            staffPanel.Size = new Size(300, 600);
            staffPanel.BackColor = Color.White;
            staffPanel.BorderStyle = BorderStyle.FixedSingle;
            staffTab.Controls.Add(staffPanel);

            Label lblStaffAction = new Label() { Text = "Staff Controls", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            staffPanel.Controls.Add(lblStaffAction);

            Button btnAddStaffAcc = new Button() { Text = "Add User / Staff Acc", Location = new Point(20, 65), Size = new Size(260, 32), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnViewStaff = new Button() { Text = "View Details", Location = new Point(20, 107), Size = new Size(260, 32), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnEditStaff = new Button() { Text = "Edit Info / Role", Location = new Point(20, 149), Size = new Size(260, 32), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnToggleStaff = new Button() { Text = "Activate / Deactivate", Location = new Point(20, 191), Size = new Size(260, 32), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnDeleteStaff = new Button() { Text = "Delete Staff", Location = new Point(20, 233), Size = new Size(260, 32), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            staffPanel.Controls.AddRange(new Control[] { btnAddStaffAcc, btnViewStaff, btnEditStaff, btnToggleStaff, btnDeleteStaff });

            btnAddStaffAcc.Click += (s, e) => {
                Form addForm = new Form()
                {
                    Text = "Add New Staff / Cashier",
                    Size = new Size(380, 430),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                Label lblId = new Label() { Text = "User ID:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                TextBox txtId = new TextBox() { Text = "STF-" + new Random().Next(100, 999), Location = new Point(20, 42), Size = new Size(320, 25) };

                Label lblName = new Label() { Text = "Full Name:", Location = new Point(20, 75), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                TextBox txtName = new TextBox() { Location = new Point(20, 97), Size = new Size(320, 25) };

                Label lblUser = new Label() { Text = "Username:", Location = new Point(20, 130), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                TextBox txtUser = new TextBox() { Location = new Point(20, 152), Size = new Size(320, 25) };

                Label lblEmail = new Label() { Text = "Email Address:", Location = new Point(20, 185), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                TextBox txtEmail = new TextBox() { Location = new Point(20, 207), Size = new Size(320, 25) };

                Label lblPass = new Label() { Text = "Password:", Location = new Point(20, 240), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                TextBox txtPass = new TextBox() { Location = new Point(20, 262), Size = new Size(320, 25), UseSystemPasswordChar = true };

                Label lblRole = new Label() { Text = "Role:", Location = new Point(20, 295), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                ComboBox cmbRole = new ComboBox() { Location = new Point(20, 317), Size = new Size(320, 25), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbRole.Items.AddRange(new object[] { "Admin", "Assistant Admin", "Cashier" });
                cmbRole.SelectedIndex = 2;

                Button btnSaveNewStaff = new Button() { Text = "Save Account", Location = new Point(20, 355), Size = new Size(150, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button btnCancelNewStaff = new Button() { Text = "Cancel", Location = new Point(190, 355), Size = new Size(150, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                btnSaveNewStaff.Click += (senderObj, ev) => {
                    if (string.IsNullOrWhiteSpace(txtId.Text) || string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
                    {
                        MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        using (var conn = new SQLiteConnection(connectionString))
                        {
                            conn.Open();
                            string insertQuery = "INSERT INTO Staff (StaffID, StaffName, StaffUsername, StaffEmail, StaffRole, StaffStatus, StaffDate, StaffPassword) VALUES (@id, @name, @user, @email, @role, 'Active', @date, @pass)";
                            using (var cmd = new SQLiteCommand(insertQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", txtId.Text.Trim());
                                cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                                cmd.Parameters.AddWithValue("@user", txtUser.Text.Trim());
                                cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                                cmd.Parameters.AddWithValue("@role", cmbRole.SelectedItem.ToString());
                                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                                cmd.Parameters.AddWithValue("@pass", txtPass.Text);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        dgvStaff.Rows.Clear();
                        LoadStaffFromDb(dgvStaff);
                        MessageBox.Show("Staff/Cashier account created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        addForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving account: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                btnCancelNewStaff.Click += (senderObj, ev) => { addForm.Close(); };

                addForm.Controls.AddRange(new Control[] { lblId, txtId, lblName, txtName, lblUser, txtUser, lblEmail, txtEmail, lblPass, txtPass, lblRole, cmbRole, btnSaveNewStaff, btnCancelNewStaff });
                addForm.ShowDialog(this);
            };

            btnViewStaff.Click += (s, e) => {
                if (dgvStaff.SelectedRows.Count > 0 && !dgvStaff.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvStaff.SelectedRows[0];
                    string details = $"Staff ID: {row.Cells["StaffID"].Value}\n" +
                                     $"Name: {row.Cells["StaffName"].Value}\n" +
                                     $"Username: {row.Cells["StaffUsername"].Value}\n" +
                                     $"Email: {row.Cells["StaffEmail"].Value}\n" +
                                     $"Role: {row.Cells["StaffRole"].Value}\n" +
                                     $"Status: {row.Cells["StaffStatus"].Value}\n" +
                                     $"Date Created: {row.Cells["StaffDate"].Value}";
                    MessageBox.Show(details, "Staff Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select a staff member first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnEditStaff.Click += (s, e) => {
                if (dgvStaff.SelectedRows.Count > 0 && !dgvStaff.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvStaff.SelectedRows[0];
                    string staffId = row.Cells["StaffID"].Value?.ToString();
                    string currentName = row.Cells["StaffName"].Value?.ToString();
                    string currentUsername = row.Cells["StaffUsername"].Value?.ToString();
                    string currentEmail = row.Cells["StaffEmail"].Value?.ToString();
                    string currentRole = row.Cells["StaffRole"].Value?.ToString();
                    string currentPass = row.Cells["StaffPassword"].Value?.ToString();

                    Form editForm = new Form()
                    {
                        Text = "Edit Staff Information & Role",
                        Size = new Size(380, 430),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false
                    };

                    Label lblName = new Label() { Text = "Full Name:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtName = new TextBox() { Text = currentName, Location = new Point(20, 42), Size = new Size(320, 25) };

                    Label lblUser = new Label() { Text = "Username:", Location = new Point(20, 75), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtUser = new TextBox() { Text = currentUsername, Location = new Point(20, 97), Size = new Size(320, 25) };

                    Label lblEmail = new Label() { Text = "Email Address:", Location = new Point(20, 130), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtEmail = new TextBox() { Text = currentEmail, Location = new Point(20, 152), Size = new Size(320, 25) };

                    Label lblPass = new Label() { Text = "Password:", Location = new Point(20, 185), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtPass = new TextBox() { Text = currentPass, Location = new Point(20, 207), Size = new Size(320, 25) };

                    Label lblRole = new Label() { Text = "Role:", Location = new Point(20, 240), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    ComboBox cmbRole = new ComboBox() { Location = new Point(20, 262), Size = new Size(320, 25), DropDownStyle = ComboBoxStyle.DropDownList };
                    cmbRole.Items.AddRange(new object[] { "Admin", "Assistant Admin", "Cashier" });
                    if (cmbRole.Items.Contains(currentRole))
                    {
                        cmbRole.SelectedItem = currentRole;
                    }
                    else
                    {
                        cmbRole.SelectedIndex = 2;
                    }

                    Button btnSaveEdit = new Button() { Text = "Update Info", Location = new Point(20, 310), Size = new Size(150, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                    Button btnCancelEdit = new Button() { Text = "Cancel", Location = new Point(190, 310), Size = new Size(150, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                    btnSaveEdit.Click += (senderObj, ev) => {
                        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
                        {
                            MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        try
                        {
                            using (var conn = new SQLiteConnection(connectionString))
                            {
                                conn.Open();
                                string updateQuery = "UPDATE Staff SET StaffName=@name, StaffUsername=@user, StaffEmail=@email, StaffRole=@role, StaffPassword=@pass WHERE StaffID=@id";
                                using (var cmd = new SQLiteCommand(updateQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                                    cmd.Parameters.AddWithValue("@user", txtUser.Text.Trim());
                                    cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                                    cmd.Parameters.AddWithValue("@role", cmbRole.SelectedItem.ToString());
                                    cmd.Parameters.AddWithValue("@pass", txtPass.Text);
                                    cmd.Parameters.AddWithValue("@id", staffId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            dgvStaff.Rows.Clear();
                            LoadStaffFromDb(dgvStaff);
                            MessageBox.Show("Staff information updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            editForm.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error updating staff: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };

                    btnCancelEdit.Click += (senderObj, ev) => { editForm.Close(); };

                    editForm.Controls.AddRange(new Control[] { lblName, txtName, lblUser, txtUser, lblEmail, txtEmail, lblPass, txtPass, lblRole, cmbRole, btnSaveEdit, btnCancelEdit });
                    editForm.ShowDialog(this);
                }
                else
                {
                    MessageBox.Show("Please select a staff member to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnToggleStaff.Click += (s, e) => {
                if (dgvStaff.SelectedRows.Count > 0 && !dgvStaff.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvStaff.SelectedRows[0];
                    string staffId = row.Cells["StaffID"].Value?.ToString();
                    string currentStatus = row.Cells["StaffStatus"].Value?.ToString().Trim();
                    string newStatus = currentStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";

                    using (var conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        string query = "UPDATE Staff SET StaffStatus=@status WHERE StaffID=@id";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@status", newStatus);
                            cmd.Parameters.AddWithValue("@id", staffId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    dgvStaff.Rows.Clear();
                    LoadStaffFromDb(dgvStaff);
                    MessageBox.Show($"Staff account status changed to {newStatus}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select a staff member first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnDeleteStaff.Click += (s, e) => {
                if (dgvStaff.SelectedRows.Count > 0 && !dgvStaff.SelectedRows[0].IsNewRow)
                {
                    if (MessageBox.Show("Are you sure you want to delete this staff member?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string staffId = dgvStaff.SelectedRows[0].Cells["StaffID"].Value?.ToString();
                        using (var conn = new SQLiteConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM Staff WHERE StaffID=@id";
                            using (var cmd = new SQLiteCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", staffId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        dgvStaff.Rows.Clear();
                        LoadStaffFromDb(dgvStaff);
                        MessageBox.Show("Staff account deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a staff member to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }

        private void SetupTransactionsTab(TabPage transactionsTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Transaction Management";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            transactionsTab.Controls.Add(lblHeader);

            Label lblSearchTrans = new Label() { Text = "Search Trans ID / Customer:", Location = new Point(320, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSearchTrans = new TextBox() { Location = new Point(500, 17), Size = new Size(180, 23) };
            Button btnSearchTrans = new Button() { Text = "Search", Location = new Point(685, 15), Size = new Size(75, 26), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            transactionsTab.Controls.Add(lblSearchTrans);
            transactionsTab.Controls.Add(txtSearchTrans);
            transactionsTab.Controls.Add(btnSearchTrans);

            DataGridView dgvTransactions = new DataGridView();
            dgvTransactionsRef = dgvTransactions;
            dgvTransactions.Location = new Point(20, 60);
            dgvTransactions.Size = new Size(680, 600);
            dgvTransactions.BackgroundColor = Color.White;
            dgvTransactions.BorderStyle = BorderStyle.FixedSingle;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.ReadOnly = true;

            dgvTransactions.Columns.Add("TransID", "Transaction ID");
            dgvTransactions.Columns.Add("TransOrderID", "Order ID");
            dgvTransactions.Columns.Add("TransDateTime", "Date & Time");
            dgvTransactions.Columns.Add("TransCustomer", "Customer");
            dgvTransactions.Columns.Add("TransStaff", "Cashier/Staff");
            dgvTransactions.Columns.Add("TransItems", "Items");
            dgvTransactions.Columns.Add("TransTotal", "Total Amount");
            dgvTransactions.Columns.Add("TransMethod", "Method");
            dgvTransactions.Columns.Add("TransPayStatus", "Payment Status");
            dgvTransactions.Columns.Add("TransStatus", "Status");

            LoadTransactionsFromDb(dgvTransactions);
            transactionsTab.Controls.Add(dgvTransactions);

            Panel transPanel = new Panel();
            transPanel.Location = new Point(715, 60);
            transPanel.Size = new Size(300, 600);
            transPanel.BackColor = Color.White;
            transPanel.BorderStyle = BorderStyle.FixedSingle;
            transactionsTab.Controls.Add(transPanel);

            Label lblTransAction = new Label() { Text = "Transaction Actions", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            transPanel.Controls.Add(lblTransAction);

            Button btnViewTransDetails = new Button() { Text = "View Details", Location = new Point(20, 70), Size = new Size(260, 38), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnPrintTrans = new Button() { Text = "Print Receipt", Location = new Point(20, 120), Size = new Size(260, 38), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnSendEmailTrans = new Button() { Text = "Send to Email", Location = new Point(20, 170), Size = new Size(260, 38), BackColor = Color.FromArgb(142, 68, 173), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            transPanel.Controls.AddRange(new Control[] { btnViewTransDetails, btnPrintTrans, btnSendEmailTrans });

            btnViewTransDetails.Click += (s, e) => {
                if (dgvTransactions.SelectedRows.Count > 0 && !dgvTransactions.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvTransactions.SelectedRows[0];
                    string details = $"Transaction ID: {row.Cells["TransID"].Value}\n" +
                                     $"Order ID: {row.Cells["TransOrderID"].Value}\n" +
                                     $"Date & Time: {row.Cells["TransDateTime"].Value}\n" +
                                     $"Customer: {row.Cells["TransCustomer"].Value}\n" +
                                     $"Cashier/Staff: {row.Cells["TransStaff"].Value}\n" +
                                     $"Items: {row.Cells["TransItems"].Value}\n" +
                                     $"Total Amount: {row.Cells["TransTotal"].Value}\n" +
                                     $"Method: {row.Cells["TransMethod"].Value}\n" +
                                     $"Payment Status: {row.Cells["TransPayStatus"].Value}\n" +
                                     $"Status: {row.Cells["TransStatus"].Value}";
                    MessageBox.Show(details, "Transaction Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please select a transaction first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnPrintTrans.Click += (s, e) => {
                if (dgvTransactions.SelectedRows.Count > 0 && !dgvTransactions.SelectedRows[0].IsNewRow)
                {
                    PrintDocument pd = new PrintDocument();
                    pd.PrintPage += (senderObj, printEvArgs) => {
                        DataGridViewRow row = dgvTransactions.SelectedRows[0];
                        string receiptText = $"{storeName}\n{storeAddress}\nTel: {storePhone}\n\n" +
                                             $"--- OFFICIAL RECEIPT ---\n" +
                                             $"Trans ID: {row.Cells["TransID"].Value}\n" +
                                             $"Date: {row.Cells["TransDateTime"].Value}\n" +
                                             $"Customer: {row.Cells["TransCustomer"].Value}\n" +
                                             $"Cashier: {row.Cells["TransStaff"].Value}\n\n" +
                                             $"Items:\n{row.Cells["TransItems"].Value}\n\n" +
                                             $"Total: {row.Cells["TransTotal"].Value}\n" +
                                             $"Method: {row.Cells["TransMethod"].Value}\n" +
                                             $"Status: {row.Cells["TransPayStatus"].Value}\n\n" +
                                             $"Thank you for your purchase!";
                        printEvArgs.Graphics.DrawString(receiptText, new Font("Courier New", 10), Brushes.Black, new PointF(100, 100));
                    };

                    PrintDialog pDialog = new PrintDialog();
                    pDialog.Document = pd;
                    if (pDialog.ShowDialog() == DialogResult.OK)
                    {
                        pd.Print();
                        MessageBox.Show("Receipt printed successfully!", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a transaction to print.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnSendEmailTrans.Click += (s, e) => {
                if (dgvTransactions.SelectedRows.Count > 0 && !dgvTransactions.SelectedRows[0].IsNewRow)
                {
                    DataGridViewRow row = dgvTransactions.SelectedRows[0];
                    string customerName = row.Cells["TransCustomer"].Value?.ToString();
                    string transId = row.Cells["TransID"].Value?.ToString();
                    string transDate = row.Cells["TransDateTime"].Value?.ToString();
                    string transItems = row.Cells["TransItems"].Value?.ToString();
                    string transTotal = row.Cells["TransTotal"].Value?.ToString();
                    string transMethod = row.Cells["TransMethod"].Value?.ToString();
                    string payStatus = row.Cells["TransPayStatus"].Value?.ToString();

                    string customerEmail = GetCustomerEmailFromDb(customerName);

                    if (string.IsNullOrEmpty(customerEmail))
                    {
                        Form inputForm = new Form()
                        {
                            Text = "Input Customer Email",
                            Size = new Size(350, 180),
                            StartPosition = FormStartPosition.CenterParent,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            MaximizeBox = false,
                            MinimizeBox = false
                        };

                        Label lblPrompt = new Label() { Text = $"There is no email for {customerName}.\nInput the email of the customer:", Location = new Point(20, 15), Size = new Size(300, 40) };
                        TextBox txtManualEmail = new TextBox() { Location = new Point(20, 60), Size = new Size(290, 25) };
                        Button btnSendManual = new Button() { Text = "Send", Location = new Point(135, 95), Size = new Size(80, 30), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                        btnSendManual.Click += (senderObj, ev) => {
                            if (!string.IsNullOrWhiteSpace(txtManualEmail.Text) && txtManualEmail.Text.Contains("@"))
                            {
                                customerEmail = txtManualEmail.Text.Trim();
                                inputForm.Close();
                            }
                            else
                            {
                                MessageBox.Show("Put a valid email.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        };

                        inputForm.Controls.AddRange(new Control[] { lblPrompt, txtManualEmail, btnSendManual });
                        inputForm.ShowDialog(this);
                    }

                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        string subject = $"Official Receipt - {storeName} ({transId})";
                        string body = $"Thank you for purchasing at {customerName}!\n\n" +
                                      $"--- TRANSACTION DETAILS ---\n" +
                                      $"Transaction ID: {transId}\n" +
                                      $"Date & Time: {transDate}\n" +
                                      $"Items:\n{transItems}\n\n" +
                                      $"Total Amount: {transTotal}\n" +
                                      $"Payment Method: {transMethod}\n" +
                                      $"Payment Status: {payStatus}\n\n" +
                                      $"{storeName}\n" +
                                      $"Address: {storeAddress}\n" +
                                      $"Contact: {storePhone}";

                        SendEmailReceipt(customerEmail, subject, body);
                    }
                }
                else
                {
                    MessageBox.Show("Choose first a transaction from the list.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            Action executeTransSearch = () => {
                string keyword = txtSearchTrans.Text.Trim().ToLower();
                dgvTransactions.Rows.Clear();
                string currSymbol = GetCurrencySymbol();

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT TransID, TransOrderID, TransDateTime, TransCustomer, TransStaff, TransItems, 
                 TransTotal, 
                 TransMethod, TransPayStatus, TransStatus 
                 FROM Transactions 
                 WHERE LOWER(TransID) LIKE @kw OR LOWER(TransCustomer) LIKE @kw OR LOWER(TransOrderID) LIKE @kw";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                                foreach (string sym in new string[] { "₱", "$", "€" })
                                {
                                    rawTotal = rawTotal.Replace(sym, "");
                                }

                                if (decimal.TryParse(rawTotal.Trim(), out decimal amt))
                                {
                                    rawTotal = $"{currSymbol}{amt:N2}";
                                }
                                else
                                {
                                    rawTotal = $"{currSymbol}0.00";
                                }

                                dgvTransactions.Rows.Add(
                                    reader["TransID"],
                                    reader["TransOrderID"],
                                    reader["TransDateTime"],
                                    reader["TransCustomer"],
                                    reader["TransStaff"],
                                    reader["TransItems"],
                                    rawTotal,
                                    reader["TransMethod"],
                                    reader["TransPayStatus"],
                                    reader["TransStatus"]
                                );
                            }
                        }
                    }
                }
            };

            btnSearchTrans.Click += (s, e) => executeTransSearch();
            txtSearchTrans.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    executeTransSearch();
                    e.SuppressKeyPress = true;
                }
            };
        }


        private void SetupReportsTab(TabPage reportsTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "Store Reports & Analytics";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            reportsTab.Controls.Add(lblHeader);

            Panel reportCtrlPanel = new Panel();
            reportCtrlPanel.Location = new Point(715, 60);
            reportCtrlPanel.Size = new Size(300, 600);
            reportCtrlPanel.BackColor = Color.White;
            reportCtrlPanel.BorderStyle = BorderStyle.FixedSingle;
            reportsTab.Controls.Add(reportCtrlPanel);

            Label lblPanelTitle = new Label() { Text = "Report Generator", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            reportCtrlPanel.Controls.Add(lblPanelTitle);

            Label lblSelectType = new Label() { Text = "Select Report Type:", Location = new Point(20, 60), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            ComboBox cmbReportType = new ComboBox() { Location = new Point(20, 85), Size = new Size(260, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            cmbReportType.Items.AddRange(new object[] {
                        "Sales & Transactions Report (All)",
                        "Daily Sales Report",
                        "Weekly Sales Report",
                        "Monthly Sales Report",
                        "Inventory Stock Report",
                        "Customer Activity Report",
                        "Staff Performance Report"
            });
            cmbReportType.SelectedIndex = 0;
            reportCtrlPanel.Controls.Add(lblSelectType);
            reportCtrlPanel.Controls.Add(cmbReportType);

            Label lblStartDate = new Label() { Text = "Start Date:", Location = new Point(20, 120), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            DateTimePicker dtpStartDate = new DateTimePicker() { Location = new Point(20, 142), Size = new Size(260, 25), Format = DateTimePickerFormat.Short };
            reportCtrlPanel.Controls.Add(lblStartDate);
            reportCtrlPanel.Controls.Add(dtpStartDate);

            Label lblEndDate = new Label() { Text = "End Date:", Location = new Point(20, 175), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            DateTimePicker dtpEndDate = new DateTimePicker() { Location = new Point(20, 197), Size = new Size(260, 25), Format = DateTimePickerFormat.Short };
            reportCtrlPanel.Controls.Add(lblEndDate);
            reportCtrlPanel.Controls.Add(dtpEndDate);

            Button btnGenerateReport = new Button() { Text = "Generate Report", Location = new Point(20, 235), Size = new Size(260, 38), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            reportCtrlPanel.Controls.Add(btnGenerateReport);

            Button btnPrintReport = new Button() { Text = "Print Report", Location = new Point(20, 285), Size = new Size(260, 38), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            reportCtrlPanel.Controls.Add(btnPrintReport);

            Button btnExportCSV = new Button() { Text = "Export to CSV / Text", Location = new Point(20, 335), Size = new Size(260, 38), BackColor = Color.FromArgb(142, 68, 173), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            reportCtrlPanel.Controls.Add(btnExportCSV);

            
            Label lblSummaryTitle = new Label() { Text = "Summary Receipt:", Location = new Point(20, 385), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            reportCtrlPanel.Controls.Add(lblSummaryTitle);

            DataGridView dgvSummary = new DataGridView();
            dgvSummary.Location = new Point(20, 410);
            dgvSummary.Size = new Size(260, 160);
            dgvSummary.BackgroundColor = Color.White;
            dgvSummary.BorderStyle = BorderStyle.FixedSingle;
            dgvSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSummary.RowHeadersVisible = false;
            dgvSummary.AllowUserToAddRows = false;
            dgvSummary.ReadOnly = true;

            dgvSummary.Columns.Add("Field", "Information");
            dgvSummary.Columns.Add("Value", "Details");
            reportCtrlPanel.Controls.Add(dgvSummary);
            

            DataGridView dgvReports = new DataGridView();
            dgvReportsRef = dgvReports;
            dgvReports.Location = new Point(20, 60);
            dgvReports.Size = new Size(680, 600);
            dgvReports.BackgroundColor = Color.White;
            dgvReports.BorderStyle = BorderStyle.FixedSingle;
            dgvReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReports.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReports.ReadOnly = true;
            reportsTab.Controls.Add(dgvReports);



            Action loadReportData = () => {
                dgvReports.Columns.Clear();
                dgvReports.Rows.Clear();
                dgvSummary.Rows.Clear(); 

                string selectedType = cmbReportType.SelectedItem?.ToString() ?? "";
                decimal totalSum = 0;
                int recordCount = 0;
                string currSymbol = GetCurrencySymbol();
                string startDateStr = dtpStartDate.Value.ToString("yyyy-MM-dd");
                string endDateStr = dtpEndDate.Value.AddDays(1).ToString("yyyy-MM-dd");

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    if (selectedType.Contains("Sales & Transactions"))
                    {
                        dgvReports.Columns.Add("Col1", "Trans ID");
                        dgvReports.Columns.Add("Col2", "Date & Time");
                        dgvReports.Columns.Add("Col3", "Customer");
                        dgvReports.Columns.Add("Col4", "Total Amount");
                        dgvReports.Columns.Add("Col5", "Payment Status");

                        string query = "SELECT TransID, TransDateTime, TransCustomer, TransTotal, TransPayStatus FROM Transactions WHERE date(TransDateTime) >= date(@start) AND date(TransDateTime) <= date(@end)";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@start", startDateStr);
                            cmd.Parameters.AddWithValue("@end", dtpEndDate.Value.ToString("yyyy-MM-dd"));
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                                    string cleanTotal = rawTotal;
                                    foreach (string sym in new string[] { "₱", "$", "€" }) cleanTotal = cleanTotal.Replace(sym, "");

                                    if (decimal.TryParse(cleanTotal.Trim(), out decimal val))
                                    {
                                        totalSum += val;
                                        rawTotal = $"{currSymbol}{val:N2}"; 
                                    }
                                    else
                                    {
                                        rawTotal = $"{currSymbol}0.00";
                                    }

                                    dgvReports.Rows.Add(reader["TransID"], reader["TransDateTime"], reader["TransCustomer"], rawTotal, reader["TransPayStatus"]);
                                    recordCount++;
                                }
                            }
                        }
                    }
                    else if (selectedType.Contains("Daily Sales"))
                    {
                        dgvReports.Columns.Add("Col1", "Trans ID");
                        dgvReports.Columns.Add("Col2", "Date & Time");
                        dgvReports.Columns.Add("Col3", "Customer");
                        dgvReports.Columns.Add("Col4", "Total Amount");
                        dgvReports.Columns.Add("Col5", "Payment Status");

                        string targetDateStr = dtpStartDate.Value.ToString("yyyy-MM-dd");
                        string query = "SELECT TransID, TransDateTime, TransCustomer, TransTotal, TransPayStatus FROM Transactions WHERE TransDateTime LIKE @today";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@today", $"{targetDateStr}%");
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                                    string cleanTotal = rawTotal;
                                    foreach (string sym in new string[] { "₱", "$", "€" }) cleanTotal = cleanTotal.Replace(sym, "");

                                    if (decimal.TryParse(cleanTotal.Trim(), out decimal val))
                                    {
                                        totalSum += val;
                                        rawTotal = $"{currSymbol}{val:N2}";
                                    }
                                    else
                                    {
                                        rawTotal = $"{currSymbol}0.00";
                                    }

                                    dgvReports.Rows.Add(reader["TransID"], reader["TransDateTime"], reader["TransCustomer"], rawTotal, reader["TransPayStatus"]);
                                    recordCount++;
                                }
                            }
                        }
                    }
                    else if (selectedType.Contains("Weekly Sales"))
                    {
                        dgvReports.Columns.Add("Col1", "Trans ID");
                        dgvReports.Columns.Add("Col2", "Date & Time");
                        dgvReports.Columns.Add("Col3", "Customer");
                        dgvReports.Columns.Add("Col4", "Total Amount");
                        dgvReports.Columns.Add("Col5", "Payment Status");

                        DateTime startFilter = dtpStartDate.Value.Date;
                        DateTime endFilter = dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1);

                        string query = "SELECT TransID, TransDateTime, TransCustomer, TransTotal, TransPayStatus FROM Transactions";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string dtStr = reader["TransDateTime"]?.ToString() ?? "";
                                    if (DateTime.TryParse(dtStr, out DateTime transDate))
                                    {
                                        if (transDate >= startFilter && transDate <= endFilter)
                                        {
                                            string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                                            string cleanTotal = rawTotal;
                                            foreach (string sym in new string[] { "₱", "$", "€" }) cleanTotal = cleanTotal.Replace(sym, "");

                                            if (decimal.TryParse(cleanTotal.Trim(), out decimal val))
                                            {
                                                totalSum += val;
                                                rawTotal = $"{currSymbol}{val:N2}";
                                            }
                                            else
                                            {
                                                rawTotal = $"{currSymbol}0.00";
                                            }

                                            dgvReports.Rows.Add(reader["TransID"], reader["TransDateTime"], reader["TransCustomer"], rawTotal, reader["TransPayStatus"]);
                                            recordCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (selectedType.Contains("Monthly Sales"))
                    {
                        dgvReports.Columns.Add("Col1", "Trans ID");
                        dgvReports.Columns.Add("Col2", "Date & Time");
                        dgvReports.Columns.Add("Col3", "Customer");
                        dgvReports.Columns.Add("Col4", "Total Amount");
                        dgvReports.Columns.Add("Col5", "Payment Status");

                        string currentMonthPrefix = dtpStartDate.Value.ToString("yyyy-MM");
                        string query = "SELECT TransID, TransDateTime, TransCustomer, TransTotal, TransPayStatus FROM Transactions WHERE TransDateTime LIKE @month";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@month", $"{currentMonthPrefix}%");
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string rawTotal = reader["TransTotal"]?.ToString() ?? "0";
                                    string cleanTotal = rawTotal;
                                    foreach (string sym in new string[] { "₱", "$", "€" }) cleanTotal = cleanTotal.Replace(sym, "");

                                    if (decimal.TryParse(cleanTotal.Trim(), out decimal val))
                                    {
                                        totalSum += val;
                                        rawTotal = $"{currSymbol}{val:N2}";
                                    }
                                    else
                                    {
                                        rawTotal = $"{currSymbol}0.00";
                                    }

                                    dgvReports.Rows.Add(reader["TransID"], reader["TransDateTime"], reader["TransCustomer"], rawTotal, reader["TransPayStatus"]);
                                    recordCount++;
                                }
                            }
                        }
                    }
                    else if (selectedType.Contains("Inventory"))
                    {
                        dgvReports.Columns.Add("Col1", "Product ID");
                        dgvReports.Columns.Add("Col2", "Product Name");
                        dgvReports.Columns.Add("Col3", "Category");
                        dgvReports.Columns.Add("Col4", "Size");
                        dgvReports.Columns.Add("Col5", "Stock");

                        int totalStockQty = 0;
                        int totalProductItems = 0;

                        string query = "SELECT ProdID, ProdName, Category, Size, Stock FROM Products";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    totalProductItems++;
                                    if (int.TryParse(reader["Stock"]?.ToString(), out int stkV))
                                    {
                                        totalStockQty += stkV;
                                    }
                                    dgvReports.Rows.Add(reader["ProdID"], reader["ProdName"], reader["Category"], reader["Size"], reader["Stock"]);
                                }
                            }
                        }
                        dgvSummary.Rows.Add("Report", "Inventory");
                        dgvSummary.Rows.Add("Total Items", totalProductItems.ToString());
                        dgvSummary.Rows.Add("Total Stocks", totalStockQty.ToString());
                        return;
                    }
                    else if (selectedType.Contains("Customer"))
                    {
                        dgvReports.Columns.Add("Col1", "Cust ID");
                        dgvReports.Columns.Add("Col2", "Customer Name");
                        dgvReports.Columns.Add("Col3", "Email");
                        dgvReports.Columns.Add("Col4", "Contact");
                        dgvReports.Columns.Add("Col5", "Account Status");

                        string query = "SELECT CustID, CustName, CustEmail, CustContact, CustAccStatus FROM Customers";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    dgvReports.Rows.Add(reader["CustID"], reader["CustName"], reader["CustEmail"], reader["CustContact"], reader["CustAccStatus"]);
                                    recordCount++;
                                }
                            }
                        }
                        dgvSummary.Rows.Add("Report", "Customers");
                        dgvSummary.Rows.Add("Total Customers", recordCount.ToString());
                        return;
                    }
                    else if (selectedType.Contains("Staff"))
                    {
                        dgvReports.Columns.Add("Col1", "Staff ID");
                        dgvReports.Columns.Add("Col2", "Name");
                        dgvReports.Columns.Add("Col3", "Username");
                        dgvReports.Columns.Add("Col4", "Role");
                        dgvReports.Columns.Add("Col5", "Status");

                        int adminAccCount = 0;
                        int assistantAccCount = 0;
                        int cashierAccCount = 0;

                        string query = "SELECT StaffID, StaffName, StaffUsername, StaffRole, StaffStatus FROM Staff";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string role = reader["StaffRole"]?.ToString()?.Trim() ?? "";
                                    if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) adminAccCount++;
                                    else if (role.Equals("Assistant Admin", StringComparison.OrdinalIgnoreCase)) assistantAccCount++;
                                    else if (role.Equals("Cashier", StringComparison.OrdinalIgnoreCase)) cashierAccCount++;

                                    dgvReports.Rows.Add(reader["StaffID"], reader["StaffName"], reader["StaffUsername"], reader["StaffRole"], reader["StaffStatus"]);
                                    recordCount++;
                                }
                            }
                        }

                        dgvSummary.Rows.Add("Total Accounts", recordCount.ToString());
                        dgvSummary.Rows.Add("Admin", adminAccCount.ToString());
                        dgvSummary.Rows.Add("Assistant Admin", assistantAccCount.ToString());
                        dgvSummary.Rows.Add("Cashier", cashierAccCount.ToString());
                        return;
                    }
                }

                string currSym = GetCurrencySymbol();
                dgvSummary.Rows.Add("Report Type", selectedType);
                dgvSummary.Rows.Add("Start Date", dtpStartDate.Value.ToString("yyyy-MM-dd"));
                dgvSummary.Rows.Add("End Date", dtpEndDate.Value.ToString("yyyy-MM-dd"));
                dgvSummary.Rows.Add("Total Records", recordCount.ToString());
                dgvSummary.Rows.Add("Total Sales", $"{currSym}{totalSum:N2}");
            };


            loadReportData();

            btnGenerateReport.Click += (s, e) => {
                loadReportData();
                MessageBox.Show("Report generated successfully from database!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };


            btnPrintReport.Click += (s, e) => {
                if (dgvReports.Rows.Count == 0)
                {
                    MessageBox.Show("The report to be printed is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (senderObj, printEvArgs) => {
                    Graphics g = printEvArgs.Graphics;
                    Font titleFont = new Font("Courier New", 11, FontStyle.Bold);
                    Font regularFont = new Font("Courier New", 9, FontStyle.Regular);
                    Font headerFont = new Font("Courier New", 9, FontStyle.Bold);
                    Brush brush = Brushes.Black;

                    float startX = 40;
                    float startY = 40;
                    float pageWidth = printEvArgs.PageBounds.Width - 80; 

                    StringFormat centerFormat = new StringFormat() { Alignment = StringAlignment.Center };
                    string storeInfo = $"{storeName}\n{storeAddress}\nTel: {storePhone}";
                    string reportTitle = $"=== {cmbReportType.SelectedItem?.ToString().ToUpper()} ===";
                    string dateGen = $"Date Generated: {DateTime.Now:yyyy-MM-dd hh:mm:ss tt}";

                    g.DrawString(storeInfo, regularFont, brush, printEvArgs.PageBounds.Width / 2, startY, centerFormat);
                    startY += 55;
                    g.DrawString(reportTitle, titleFont, brush, printEvArgs.PageBounds.Width / 2, startY, centerFormat);
                    startY += 25;
                    g.DrawString(dateGen, regularFont, brush, printEvArgs.PageBounds.Width / 2, startY, centerFormat);
                    startY += 30;

                    foreach (DataGridViewRow row in dgvSummary.Rows)
                    {
                        string summaryLine = $"{row.Cells[0].Value}: {row.Cells[1].Value}";
                        g.DrawString(summaryLine, regularFont, brush, startX, startY);
                        startY += 18;
                    }

                    startY += 15;
                    g.DrawLine(Pens.Black, startX, startY, startX + pageWidth, startY);
                    startY += 10;

                    float[] colWidths = new float[dgvReports.Columns.Count];
                    if (dgvReports.Columns.Count == 5)
                    {
                        colWidths[0] = 130; // Trans ID
                        colWidths[1] = 170; // Date & Time
                        colWidths[2] = 130; // Customer
                        colWidths[3] = 100; // Total Amount
                        colWidths[4] = 90;  // Payment Status
                    }
                    else
                    {
                        float defaultWidth = pageWidth / dgvReports.Columns.Count;
                        for (int i = 0; i < dgvReports.Columns.Count; i++)
                        {
                            colWidths[i] = defaultWidth;
                        }
                    }

                    float currentX = startX;
                    for (int i = 0; i < dgvReports.Columns.Count; i++)
                    {
                        g.DrawString(dgvReports.Columns[i].HeaderText, headerFont, brush, currentX, startY);
                        currentX += colWidths[i];
                    }
                    startY += 20;
                    g.DrawLine(Pens.Gray, startX, startY, startX + pageWidth, startY);
                    startY += 8;

                    foreach (DataGridViewRow row in dgvReports.Rows)
                    {
                        if (row.IsNewRow) continue;
                        currentX = startX;
                        for (int i = 0; i < dgvReports.Columns.Count; i++)
                        {
                            string cellVal = row.Cells[i].Value?.ToString() ?? "";

                            
                            int maxChars = (i == 1) ? 22 : 18;
                            if (cellVal.Length > maxChars)
                            {
                                cellVal = cellVal.Substring(0, maxChars - 2) + "..";
                            }

                            g.DrawString(cellVal, regularFont, brush, currentX, startY);
                            currentX += colWidths[i];
                        }
                        startY += 20;
                    }

                    startY += 10;
                    g.DrawLine(Pens.Black, startX, startY, startX + pageWidth, startY);
                };

                PrintDialog pDialog = new PrintDialog();
                pDialog.Document = pd;
                if (pDialog.ShowDialog() == DialogResult.OK)
                {
                    pd.Print();
                    MessageBox.Show("Report printed successfully!", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };


            btnExportCSV.Click += (s, e) => {
                if (dgvReports.Rows.Count == 0)
                {
                    MessageBox.Show("The report to be exported is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV File (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = $"report_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("=== SUMMARY REPORT ===");
                            foreach (DataGridViewRow sumRow in dgvSummary.Rows)
                            {
                                sw.WriteLine($"\"{sumRow.Cells[0].Value}\",\"{sumRow.Cells[1].Value}\"");
                            }
                            sw.WriteLine(""); 

                            string headers = "";
                            for (int i = 0; i < dgvReports.Columns.Count; i++)
                            {
                                headers += $"\"{dgvReports.Columns[i].HeaderText}\"" + (i == dgvReports.Columns.Count - 1 ? "" : ",");
                            }
                            sw.WriteLine(headers);

                            foreach (DataGridViewRow row in dgvReports.Rows)
                            {
                                if (row.IsNewRow) continue;
                                string line = "";
                                for (int i = 0; i < dgvReports.Columns.Count; i++)
                                {
                                    string val = row.Cells[i].Value?.ToString() ?? "";
                                   
                                    val = val.Replace("\"", "\"\"");
                                    line += $"\"{val}\"" + (i == dgvReports.Columns.Count - 1 ? "" : ",");
                                }
                                sw.WriteLine(line);
                            }
                        }
                        MessageBox.Show("Report exported successfully with proper formatting!", "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error exporting file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
        }



        private void ApplyLanguageTranslation(string lang)
        {
            currentLanguage = lang;

            Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
    {
        {
            "English", new Dictionary<string, string>()
            {
                { "Dashboard", "Dashboard" },
                { "Products", "Products" },
                { "Inventory", "Inventory" },
                { "Orders", "Orders" },
                { "Customers", "Customers" },
                { "Staff/Users", "Staff/Users" },
                { "Transactions", "Transactions" },
                { "Reports", "Reports" },
                { "Settings", "Settings" },
                { "Logout", "Logout" },
                { "AdminPortal", "Admin Portal" },
                { "RecentTrans", "Recent Transactions" },
                { "TodaySales", "Today's Sales" },
                { "MonthlySales", "Monthly Sales" },
                { "TotalProd", "Total Products" },
                { "TotalOrd", "Total Orders" },
                { "PendingOrd", "Pending Orders" },
                { "LowStock", "Low Stock Items" }
            }
        },
        {
            "Filipino", new Dictionary<string, string>()
            {
                { "Dashboard", "Dashboard" },
                { "Products", "Mga Produkto" },
                { "Inventory", "Imbentaryo" },
                { "Orders", "Mga Order" },
                { "Customers", "Mga Customer" },
                { "Staff/Users", "Mga Tauhan/User" },
                { "Transactions", "Mga Transaksyon" },
                { "Reports", "Mga Ulat" },
                { "Settings", "Mga Setting" },
                { "Logout", "Mag-logout" },
                { "AdminPortal", "Admin Portal" },
                { "RecentTrans", "Mga Kamakailang Transaksyon" },
                { "TodaySales", "Benta Ngayon" },
                { "MonthlySales", "Benta sa Buwan na Ito" },
                { "TotalProd", "Kabuuang Produkto" },
                { "TotalOrd", "Kabuuang Order" },
                { "PendingOrd", "Mga Nakabinbing Order" },
                { "LowStock", "Mababang Stock na Produkto" }
            }
        },
        {
            "Cebuano", new Dictionary<string, string>()
            {
                { "Dashboard", "Dashboard" },
                { "Products", "Mga Produkto" },
                { "Inventory", "Imbentaryo" },
                { "Orders", "Mga Order" },
                { "Customers", "Mga Kustomer" },
                { "Staff/Users", "Mga Trabahante/User" },
                { "Transactions", "Mga Transaksyon" },
                { "Reports", "Mga Report" },
                { "Settings", "Mga Setting" },
                { "Logout", "Pag-gawas" },
                { "AdminPortal", "Admin Portal" },
                { "RecentTrans", "Bag-ong mga Transaksyon" },
                { "TodaySales", "Baligya Karon" },
                { "MonthlySales", "Baligya sa Ning-abot nga Buwan" },
                { "TotalProd", "Tibuok Produkto" },
                { "TotalOrd", "Tibuok Order" },
                { "PendingOrd", "Naghuwat nga Order" },
                { "LowStock", "Mubo nga Stock" }
            }
        }
    };

            if (!translations.ContainsKey(lang))
            {
                lang = "English";
            }

            var dict = translations[lang];

            if (sidePanel != null)
            {
                foreach (Control ctrl in sidePanel.Controls)
                {
                    if (ctrl is Button btn && btn.Tag is int index)
                    {
                        string[] keys = { "Dashboard", "Products", "Inventory", "Orders", "Customers", "Staff/Users", "Transactions", "Reports", "Settings", "Logout" };
                        if (index >= 0 && index < keys.Length)
                        {
                            string key = keys[index];
                            if (dict.ContainsKey(key))
                            {
                                btn.Text = dict[key];
                            }
                        }
                    }
                }
            }

            if (adminTabControl != null)
            {
                string[] keys = { "Dashboard", "Products", "Inventory", "Orders", "Customers", "Staff/Users", "Transactions", "Reports", "Settings", "Logout" };
                for (int i = 0; i < adminTabControl.TabPages.Count && i < keys.Length; i++)
                {
                    string key = keys[i];
                    if (dict.ContainsKey(key))
                    {
                        adminTabControl.TabPages[i].Text = dict[key];
                    }
                }
            }

            this.Text = $"{storeName} - {dict["AdminPortal"]}";
        }


        private void SetupSettingsTab(TabPage settingsTab)
        {
            Label lblHeader = new Label();
            lblHeader.Text = "System Settings & Configurations";
            lblHeader.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            settingsTab.Controls.Add(lblHeader);

            TabControl settingsSubTab = new TabControl();
            settingsSubTab.Location = new Point(20, 55);
            settingsSubTab.Size = new Size(990, 600);
            settingsTab.Controls.Add(settingsSubTab);

            TabPage tpProfile = new TabPage("Profile Settings");
            tpProfile.BackColor = Color.White;
            settingsSubTab.TabPages.Add(tpProfile);

            Label lblPTitle = new Label() { Text = "Business Profile Information", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            tpProfile.Controls.Add(lblPTitle);

            int y = 60;
            tpProfile.Controls.Add(new Label() { Text = "Business Name:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtSysName = new TextBox() { Text = storeName, Location = new Point(160, y - 3), Size = new Size(300, 25) };
            tpProfile.Controls.Add(txtSysName);

            y += 40;
            tpProfile.Controls.Add(new Label() { Text = "Business Email:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtSysEmail = new TextBox() { Text = storeEmail, Location = new Point(160, y - 3), Size = new Size(300, 25) };
            tpProfile.Controls.Add(txtSysEmail);

            y += 40;
            tpProfile.Controls.Add(new Label() { Text = "Business Phone:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtSysPhone = new TextBox() { Text = storePhone, Location = new Point(160, y - 3), Size = new Size(300, 25) };
            tpProfile.Controls.Add(txtSysPhone);

            y += 40;
            tpProfile.Controls.Add(new Label() { Text = "Business Address:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtSysAddress = new TextBox() { Text = storeAddress, Location = new Point(160, y - 3), Size = new Size(300, 25) };
            tpProfile.Controls.Add(txtSysAddress);

            Button btnSaveProfile = new Button() { Text = "Save Profile", Location = new Point(160, y + 50), Size = new Size(130, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSaveProfile.Click += (s, e) => {
                storeName = txtSysName.Text;
                storeEmail = txtSysEmail.Text;
                storePhone = txtSysPhone.Text;
                storeAddress = txtSysAddress.Text;
                SaveSettingsToDb();
                MessageBox.Show("Business Profile saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tpProfile.Controls.Add(btnSaveProfile);

            TabPage tpAccount = new TabPage("Account Configuration");
            tpAccount.BackColor = Color.White;
            settingsSubTab.TabPages.Add(tpAccount);

            Label lblATitle = new Label() { Text = "Admin Credential Settings", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            tpAccount.Controls.Add(lblATitle);

            y = 60;
            tpAccount.Controls.Add(new Label() { Text = "Current Username:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtCurrUser = new TextBox() { Text = "admin", Location = new Point(160, y - 3), Size = new Size(250, 25) };
            tpAccount.Controls.Add(txtCurrUser);

            y += 40;
            tpAccount.Controls.Add(new Label() { Text = "Current Password:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtCurrPass = new TextBox() { UseSystemPasswordChar = true, Location = new Point(160, y - 3), Size = new Size(250, 25) };
            tpAccount.Controls.Add(txtCurrPass);

            y += 40;
            tpAccount.Controls.Add(new Label() { Text = "New Password:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtNewPass = new TextBox() { UseSystemPasswordChar = true, Location = new Point(160, y - 3), Size = new Size(250, 25) };
            tpAccount.Controls.Add(txtNewPass);

            Button btnSaveCreds = new Button() { Text = "Save Credentials", Location = new Point(160, y + 50), Size = new Size(150, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSaveCreds.Click += (s, e) => {
                string inputCurrentUsername = txtCurrUser.Text.Trim();
                string newPassword = txtNewPass.Text.Trim();

                if (string.IsNullOrEmpty(newPassword))
                {
                    MessageBox.Show("Please enter a new password.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string updateStaffQuery = "UPDATE Staff SET StaffPassword=@pass WHERE StaffUsername=@currUser AND StaffRole='Admin' LIMIT 1";
                    using (var cmd = new SQLiteCommand(updateStaffQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@currUser", inputCurrentUsername);
                        cmd.Parameters.AddWithValue("@pass", newPassword);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            string insertStaffQuery = "INSERT INTO Staff (StaffID, StaffName, StaffUsername, StaffEmail, StaffRole, StaffStatus, StaffDate, StaffPassword) VALUES ('STF-001', 'Admin User', @currUser, 'admin@store.com', 'Admin', 'Active', @date, @pass)";
                            using (var cmdIns = new SQLiteCommand(insertStaffQuery, conn))
                            {
                                cmdIns.Parameters.AddWithValue("@currUser", inputCurrentUsername);
                                cmdIns.Parameters.AddWithValue("@pass", newPassword);
                                cmdIns.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("Admin credentials updated and synced to Staff/Users tab successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNewPass.Clear();
                txtCurrPass.Clear();
            };
            tpAccount.Controls.Add(btnSaveCreds);

            TabPage tpPref = new TabPage("Theme & Currency");
            tpPref.BackColor = Color.White;
            settingsSubTab.TabPages.Add(tpPref);

            Label lblPrefTitle = new Label() { Text = "Appearance & Financial Sign Preferences", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            tpPref.Controls.Add(lblPrefTitle);

            y = 60;
            tpPref.Controls.Add(new Label() { Text = "Theme Color:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            cmbThemePref = new ComboBox() { Location = new Point(160, y - 3), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbThemePref.Items.AddRange(new object[] { "Dark Sidebar (Default)", "Blue", "Beige", "Grey" });
            cmbThemePref.SelectedItem = currentTheme;
            tpPref.Controls.Add(cmbThemePref);

            y += 40;
            tpPref.Controls.Add(new Label() { Text = "Currency / Bill Sign:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            cmbCurrencyPref = new ComboBox() { Location = new Point(160, y - 3), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCurrencyPref.Items.AddRange(new object[] { "PHP (₱)", "USD ($)", "EUR (€)" });
            cmbCurrencyPref.SelectedItem = currentCurrency;
            tpPref.Controls.Add(cmbCurrencyPref);

            Label lblLangHeader = new Label();
            lblLangHeader.Text = "Language Preferences";
            lblLangHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblLangHeader.ForeColor = Color.FromArgb(41, 128, 185);
            lblLangHeader.Location = new Point(20, y + 90);
            lblLangHeader.AutoSize = true;
            tpPref.Controls.Add(lblLangHeader);

            cmbLanguageSetting = new ComboBox();
            cmbLanguageSetting.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbLanguageSetting.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguageSetting.Items.AddRange(new object[] { "English", "Filipino", "Cebuano" }); 
            cmbLanguageSetting.SelectedIndex = 0; 
            cmbLanguageSetting.Location = new Point(20, y + 125);
            cmbLanguageSetting.Size = new System.Drawing.Size(250, 25);

            cmbLanguageSetting.SelectedIndexChanged += (s, e) => {
                if (cmbLanguageSetting.SelectedItem != null)
                {
                    string selectedText = cmbLanguageSetting.SelectedItem.ToString();

                    if (selectedText.StartsWith("English"))
                    {
                        currentLanguage = "English";
                    }
                    else if (selectedText.StartsWith("Filipino"))
                    {
                        currentLanguage = "Filipino";
                    }
                    else if (selectedText.StartsWith("Cebuano"))
                    {
                        currentLanguage = "Cebuano";
                    }

                    ApplyLanguageTranslation(currentLanguage);
                }
            };

            tpPref.Controls.Add(cmbLanguageSetting);

            Button btnSavePref = new Button() { Text = "Apply Preferences", Location = new Point(160, y + 50), Size = new Size(150, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSavePref.Click += (s, e) => {
                string oldSymbol = GetCurrencySymbol();
                currentTheme = cmbThemePref.SelectedItem?.ToString() ?? "Dark Sidebar (Default)";
                currentCurrency = cmbCurrencyPref.SelectedItem?.ToString() ?? "PHP (₱)";
                string newSymbol = GetCurrencySymbol();

                SaveSettingsToDb();
                ApplyThemeSettings();

                if (lblPriceRef != null)
                {
                    lblPriceRef.Text = $"Price ({newSymbol}):";
                }

                if (dgvProductsRef != null)
                {
                    dgvProductsRef.Rows.Clear();
                    LoadProductsFromDb(dgvProductsRef);
                }

                MessageBox.Show("Preferences updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tpPref.Controls.Add(btnSavePref);

            TabPage tpNotif = new TabPage("Notifications");
            tpNotif.BackColor = Color.White;
            settingsSubTab.TabPages.Add(tpNotif);

            Label lblNotifTitle = new Label() { Text = "Alert & Notification Preferences", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            tpNotif.Controls.Add(lblNotifTitle);

            y = 60;
            chkEmailNotif = new CheckBox() { Text = "Enable Email Notifications for Low Stock & Orders", Location = new Point(20, y), AutoSize = true, Checked = enableEmailNotif, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            y += 35;
            chkSmsNotif = new CheckBox() { Text = "Enable SMS / Text Alerts for Customer Orders", Location = new Point(20, y), AutoSize = true, Checked = enableSmsNotif, Font = new Font("Segoe UI", 9, FontStyle.Regular) };
            tpNotif.Controls.Add(chkEmailNotif);
            tpNotif.Controls.Add(chkSmsNotif);

            Button btnSaveNotif = new Button() { Text = "Save Notifications", Location = new Point(20, y + 50), Size = new Size(160, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSaveNotif.Click += (s, e) => {
                enableEmailNotif = chkEmailNotif.Checked;
                enableSmsNotif = chkSmsNotif.Checked;
                SaveSettingsToDb();
                MessageBox.Show("Notification settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tpNotif.Controls.Add(btnSaveNotif);

            TabPage tpBackup = new TabPage("Backup & Recovery");
            tpBackup.BackColor = Color.White;
            settingsSubTab.TabPages.Add(tpBackup);

            Label lblBTitle = new Label() { Text = "Database Backup and Recovery", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            tpBackup.Controls.Add(lblBTitle);

            Button btnBackup = new Button() { Text = "Backup Database Now", Location = new Point(20, 70), Size = new Size(220, 40), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnBackup.Click += (s, e) => {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "SQLite Database (*.db)|*.db";
                sfd.FileName = $"store_backup_{DateTime.Now:yyyy-MM-dd hh:mm:ss tt}.db";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(dbPath, sfd.FileName, true);
                    MessageBox.Show("Database backup created successfully!", "Backup Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            tpBackup.Controls.Add(btnBackup);

            Button btnRestore = new Button() { Text = "Restore Database...", Location = new Point(20, 125), Size = new Size(220, 40), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRestore.Click += (s, e) => {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "SQLite Database (*.db)|*.db";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (MessageBox.Show("Are you sure you want to restore? Current unsaved state will be overwritten.", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        File.Copy(ofd.FileName, dbPath, true);
                        MessageBox.Show("Database restored successfully! Please restart the admin portal.", "Restore Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Application.Exit();
                    }
                }
            };
            tpBackup.Controls.Add(btnRestore);

            TabPage tpReset = new TabPage("System Reset");
            tpReset.BackColor = Color.White;
            settingsSubTab.TabPages.Add(tpReset);

            Label lblResetTitle = new Label() { Text = "Database Reset & Clearance", Location = new Point(20, 20), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true };
            tpReset.Controls.Add(lblResetTitle);

            Label lblResetDesc = new Label()
            {
                Text = "Choose which module you want to reset or clear. You can select any of the options below.",
                Location = new Point(20, 55),
                Size = new Size(600, 40),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.DimGray
            };
            tpReset.Controls.Add(lblResetDesc);

            Label lblSelectTarget = new Label() { Text = "Select target to reset:", Location = new Point(20, 115), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            ComboBox cmbResetTarget = new ComboBox() { Location = new Point(20, 140), Size = new Size(320, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbResetTarget.Items.AddRange(new object[] {
                "Products",
                "Inventory (Sets products stock to zero)",
                "Transactions",
                "Orders",
                "Returns/Refunds",
                "Customers"
            });
            cmbResetTarget.SelectedIndex = 0;
            tpReset.Controls.Add(lblSelectTarget);
            tpReset.Controls.Add(cmbResetTarget);

            Button btnExecuteReset = new Button()
            {
                Text = "Execute Reset",
                Location = new Point(20, 185),
                Size = new Size(180, 40),
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnExecuteReset.Click += (s, e) => {
                string selectedTarget = cmbResetTarget.SelectedItem?.ToString() ?? "";

                DialogResult dr = MessageBox.Show(
                    $"Are you sure you want to reset {selectedTarget}?\n\nThis action cannot be undone.",
                    "Confirm Reset Action",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (dr == DialogResult.Yes)
                {
                    try
                    {
                        using (var conn = new SQLiteConnection(connectionString))
                        {
                            conn.Open();
                            List<string> queriesToExecute = new List<string>();

                            if (selectedTarget.Contains("Lahat"))
                            {
                                queriesToExecute.Add("DELETE FROM Products;");
                                queriesToExecute.Add("DELETE FROM Orders;");
                                queriesToExecute.Add("DELETE FROM Transactions;");
                                queriesToExecute.Add("DELETE FROM Returns;");
                                queriesToExecute.Add("DELETE FROM Customers;");
                                queriesToExecute.Add("DELETE FROM CustomerCart;");
                                queriesToExecute.Add("DELETE FROM CustomerWishlist;");
                            }
                            else if (selectedTarget.Contains("Products"))
                            {
                                queriesToExecute.Add("DELETE FROM Products;");
                            }
                            else if (selectedTarget.Contains("Inventory"))
                            {
                                queriesToExecute.Add("UPDATE Products SET Stock = '0';");
                            }
                            else if (selectedTarget.Contains("Transactions"))
                            {
                                queriesToExecute.Add("DELETE FROM Transactions;");
                            }
                            else if (selectedTarget.Contains("Orders"))
                            {
                                queriesToExecute.Add("DELETE FROM Orders;");
                            }
                            else if (selectedTarget.Contains("Returns/Refunds"))
                            {
                                queriesToExecute.Add("DELETE FROM Returns;");
                            }
                            else if (selectedTarget.Contains("Customers"))
                            {
                                queriesToExecute.Add("DELETE FROM Customers;");
                            }

                            foreach (string q in queriesToExecute)
                            {
                                using (var cmd = new SQLiteCommand(q, conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        if (dgvProductsRef != null) { dgvProductsRef.Rows.Clear(); LoadProductsFromDb(dgvProductsRef); }
                        if (dgvInventoryRef != null) { dgvInventoryRef.Rows.Clear(); LoadInventoryData(dgvInventoryRef); }
                        if (dgvOrdersRef != null) { dgvOrdersRef.Rows.Clear(); LoadOrdersFromDb(dgvOrdersRef); }
                        if (dgvTransactionsRef != null) { dgvTransactionsRef.Rows.Clear(); LoadTransactionsFromDb(dgvTransactionsRef); }
                        if (dgvCustomersRef != null) { dgvCustomersRef.Rows.Clear(); LoadCustomersFromDb(dgvCustomersRef); }
                        if (dgvReturnsRef != null) { dgvReturnsRef.Rows.Clear(); LoadAdminReturnsData(dgvReturnsRef); }
                        RefreshDashboardData();

                        MessageBox.Show($"Successfully reset: {selectedTarget}!", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error resetting database: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            tpReset.Controls.Add(btnExecuteReset);

        }
    }
}