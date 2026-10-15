using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace PTOF
{
    public partial class CashierPortalForm : Form
    
    {
        private Panel sidePanel;
        private TabControl cashierTabControl;

        // Navigation Buttons
        private Button btnDashboard;
        private Button btnPOS;
        private Button btnTransactions;
        private Button btnReturnsRefunds;
        private Button btnSalesSummary;
        private Button btnMyAccount;
        private Button btnLogout;

        // Tabs
        private TabPage tabDashboard;
        private TabPage tabPOS;
        private TabPage tabTransactions;
        private TabPage tabReturnsRefunds;
        private TabPage tabSalesSummary;
        private TabPage tabMyAccount;

        // Dashboard UI Elements
        private Label lblCashierName;
        private PictureBox picProfile;
        private Label lblTodaySalesVal;
        private Label lblNumTransVal;
        private Label lblPendingTransVal;
        private DataGridView dgvRecentTransactions;

        // POS UI Elements
        private TextBox txtSearchProduct;
        private ComboBox cmbCategories;
        private DataGridView dgvProducts;
        private DataGridView dgvCart;
        private ComboBox cmbPaymentMethod;
        private TextBox txtAmountReceived;
        private TextBox txtDiscount;
        private Button btnApplySeniorDiscount;
        private Label lblSubtotalVal;
        private Label lblTaxVal;
        private Label lblTotalVal;
        private Label lblChangeVal;
        private Button btnAddToCart;
        private Button btnRemoveFromCart;
        private Button btnCompleteTrans;
        private Button btnPrintReceipt;

        // Transactions UI Elements
        private TextBox txtSearchTrans;
        private DataGridView dgvTransactions;
        private Button btnViewTransDetails;
        private Button btnPrintTransReceipt;

        // Returns/Refunds UI Elements
        private TextBox txtSearchReturn;
        private DataGridView dgvReturns;
        private Button btnProcessReturn;

        // Sales Summary UI Elements
        private Label lblSumTodaySales;
        private Label lblSumWeeklySales;
        private Label lblSumMonthlySales;
        private Label lblSumNumTrans;
        private Label lblSumTotalItemsSold;
        private Label lblSumCashSales;
        private Label lblSumOtherSales;
        private DateTimePicker dtpSalesDate;
        private DateTimePicker dtpSalesWeek;
        private ComboBox cmbSalesMonth;
        private ComboBox cmbSalesYear;
        private ComboBox cmbSummaryFilterMode;
        private DataGridView dgvSalesSummary;
        private Button btnPrintSalesSummary;

        // My Account UI Elements (Settings / Profile Function)
        private TextBox txtAccUsername;
        private TextBox txtAccFullName;
        private TextBox txtAccEmail;
        private TextBox txtAccContact;
        private TextBox txtAccRole;
        private TextBox txtAccNewPassword;
        private PictureBox picAccProfile;
        private Button btnBrowsePic;
        private Button btnSaveAccount;

        // Discount Metadata
        private string appliedDiscountInfo = "None";

        // Receipt Printing Data
        private string lastReceiptText = "";

        private Color activeColor = Color.FromArgb(50, 50, 50);
        private Color hoverColor = Color.FromArgb(70, 70, 70);
        private string connectionString = "Data Source=store.db;Version=3;";

        private string loggedInCashier = "Cashier";
        private byte[] currentProfilePicBytes = null;

        public CashierPortalForm(string cashierName = "Cashier")
        {
            InitializeComponent();
            loggedInCashier = cashierName;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            SetupCashierPortalLayout();
        }

        private string GetCurrencySymbol()
        {
            string symbol = "₱";
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Value FROM Settings WHERE Key = 'CurrencySymbol' LIMIT 1";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && !string.IsNullOrEmpty(result.ToString()))
                        {
                            symbol = result.ToString();
                        }
                    }
                }
            }
            catch { }
            return symbol;
        }

        private void CashierPortalForm_Load(object sender, EventArgs e)
        {
            EnsureDatabaseTables();
            LoadDashboardData();
            LoadPOSProducts();
            LoadTransactionsData();
            LoadReturnsData();
            LoadSalesSummaryData();
            LoadAccountData();
        }

        private void EnsureDatabaseTables()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
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
                        );";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    try
                    {
                        using (SQLiteCommand cmdAlter = new SQLiteCommand("ALTER TABLE Staff ADD COLUMN StaffContact TEXT;", conn))
                        {
                            cmdAlter.ExecuteNonQuery();
                        }
                    }
                    catch { }

                    try
                    {
                        using (SQLiteCommand cmdAlter = new SQLiteCommand("ALTER TABLE Staff ADD COLUMN StaffProfilePic BLOB;", conn))
                        {
                            cmdAlter.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void SetupCashierPortalLayout()
        {
            this.Size = new System.Drawing.Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Cashier Portal - POS System";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            sidePanel = new BufferedPanel();
            sidePanel.Dock = DockStyle.Left;
            sidePanel.Width = 230;
            sidePanel.BackColor = Color.FromArgb(30, 30, 30);
            this.Controls.Add(sidePanel);

            cashierTabControl = new TabControl();
            cashierTabControl.Location = new Point(230, 0);
            cashierTabControl.Size = new System.Drawing.Size(1035, 685);
            cashierTabControl.Appearance = TabAppearance.FlatButtons;
            cashierTabControl.ItemSize = new System.Drawing.Size(0, 1);
            cashierTabControl.SizeMode = TabSizeMode.Fixed;
            this.Controls.Add(cashierTabControl);

            tabDashboard = new TabPage();
            tabPOS = new TabPage();
            tabTransactions = new TabPage();
            tabReturnsRefunds = new TabPage();
            tabSalesSummary = new TabPage();
            tabMyAccount = new TabPage();

            cashierTabControl.TabPages.Add(tabDashboard);
            cashierTabControl.TabPages.Add(tabPOS);
            cashierTabControl.TabPages.Add(tabTransactions);
            cashierTabControl.TabPages.Add(tabReturnsRefunds);
            cashierTabControl.TabPages.Add(tabSalesSummary);
            cashierTabControl.TabPages.Add(tabMyAccount);

            int startY = 20;
            int btnHeight = 45;

            btnDashboard = CreateNavButton("Dashboard", startY);
            btnPOS = CreateNavButton("POS", startY + btnHeight);
            btnTransactions = CreateNavButton("Transactions", startY + (btnHeight * 2));
            btnReturnsRefunds = CreateNavButton("Returns/Refunds", startY + (btnHeight * 3));
            btnSalesSummary = CreateNavButton("Sales Summary", startY + (btnHeight * 4));
            btnMyAccount = CreateNavButton("Settings / Account", startY + (btnHeight * 5));

            btnLogout = CreateNavButton("Logout", 420);
            btnLogout.ForeColor = Color.IndianRed;

            btnDashboard.Click += (s, e) => { cashierTabControl.SelectedTab = tabDashboard; HighlightButton(btnDashboard); LoadDashboardData(); };
            btnPOS.Click += (s, e) => { cashierTabControl.SelectedTab = tabPOS; HighlightButton(btnPOS); LoadPOSProducts(); };
            btnTransactions.Click += (s, e) => { cashierTabControl.SelectedTab = tabTransactions; HighlightButton(btnTransactions); LoadTransactionsData(); };
            btnReturnsRefunds.Click += (s, e) => { cashierTabControl.SelectedTab = tabReturnsRefunds; HighlightButton(btnReturnsRefunds); LoadReturnsData(); };
            btnSalesSummary.Click += (s, e) => { cashierTabControl.SelectedTab = tabSalesSummary; HighlightButton(btnSalesSummary); LoadSalesSummaryData(); };
            btnMyAccount.Click += (s, e) => { cashierTabControl.SelectedTab = tabMyAccount; HighlightButton(btnMyAccount); LoadAccountData(); };

            btnLogout.Click += (s, e) => {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    this.Close();
                }
            };

            SetupDashboardContent();
            SetupPOSContent();
            SetupTransactionsContent();
            SetupReturnsRefundsContent();
            SetupSalesSummaryContent();
            SetupMyAccountContent();

            HighlightButton(btnDashboard);
        }

        private class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }

        private Button CreateNavButton(string text, int top)
        {
            Button btn = new Button();
            btn.Text = "  " + text;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.ForeColor = Color.White;
            btn.BackColor = Color.Transparent;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            btn.Size = new System.Drawing.Size(230, 45);
            btn.Location = new Point(0, top);
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += (s, e) => { if (btn.BackColor != activeColor) btn.BackColor = hoverColor; };
            btn.MouseLeave += (s, e) => { if (btn.BackColor != activeColor) btn.BackColor = Color.Transparent; };

            sidePanel.Controls.Add(btn);
            return btn;
        }

        private void HighlightButton(Button selectedBtn)
        {
            foreach (Control ctrl in sidePanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.BackColor = Color.Transparent;
                }
            }
            selectedBtn.BackColor = activeColor;
        }

        private void SetupDashboardContent()
        {
            tabDashboard.BackColor = Color.FromArgb(245, 247, 250);

            Panel pnlProfile = new Panel();
            pnlProfile.Location = new Point(30, 20);
            pnlProfile.Size = new System.Drawing.Size(500, 70);
            pnlProfile.BackColor = Color.White;
            pnlProfile.BorderStyle = BorderStyle.FixedSingle;
            tabDashboard.Controls.Add(pnlProfile);

            picProfile = new PictureBox();
            picProfile.Location = new Point(10, 10);
            picProfile.Size = new System.Drawing.Size(50, 50);
            picProfile.SizeMode = PictureBoxSizeMode.StretchImage;
            picProfile.BackColor = Color.LightGray;
            pnlProfile.Controls.Add(picProfile);

            lblCashierName = new Label();
            lblCashierName.Text = $"Welcome, {loggedInCashier}!";
            lblCashierName.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblCashierName.Location = new Point(70, 20);
            lblCashierName.AutoSize = true;
            pnlProfile.Controls.Add(lblCashierName);

            Button btnQuickPOS = new Button();
            btnQuickPOS.Text = "Quick POS";
            btnQuickPOS.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnQuickPOS.BackColor = Color.FromArgb(40, 167, 69);
            btnQuickPOS.ForeColor = Color.White;
            btnQuickPOS.FlatStyle = FlatStyle.Flat;
            btnQuickPOS.FlatAppearance.BorderSize = 0;
            btnQuickPOS.Location = new Point(715, 20);
            btnQuickPOS.Size = new System.Drawing.Size(135, 70);
            btnQuickPOS.Cursor = Cursors.Hand;
            btnQuickPOS.Click += (s, e) => { cashierTabControl.SelectedTab = tabPOS; HighlightButton(btnPOS); LoadPOSProducts(); };
            tabDashboard.Controls.Add(btnQuickPOS);

            Button btnQuickSummary = new Button();
            btnQuickSummary.Text = "Sales Summary";
            btnQuickSummary.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnQuickSummary.BackColor = Color.FromArgb(0, 123, 255);
            btnQuickSummary.ForeColor = Color.White;
            btnQuickSummary.FlatStyle = FlatStyle.Flat;
            btnQuickSummary.FlatAppearance.BorderSize = 0;
            btnQuickSummary.Location = new Point(860, 20);
            btnQuickSummary.Size = new System.Drawing.Size(135, 70);
            btnQuickSummary.Cursor = Cursors.Hand;
            btnQuickSummary.Click += (s, e) => { cashierTabControl.SelectedTab = tabSalesSummary; HighlightButton(btnSalesSummary); LoadSalesSummaryData(); };
            tabDashboard.Controls.Add(btnQuickSummary);

            int cardWidth = 310;
            int cardHeight = 100;
            int cardY = 110;

            Panel pnlCard1 = CreateCard("Today's Sales", out lblTodaySalesVal, 30, cardY, cardWidth, cardHeight, Color.FromArgb(23, 162, 184));
            tabDashboard.Controls.Add(pnlCard1);

            Panel pnlCard2 = CreateCard("Number of Transactions", out lblNumTransVal, 360, cardY, cardWidth, cardHeight, Color.FromArgb(108, 117, 125));
            tabDashboard.Controls.Add(pnlCard2);

            Panel pnlCard3 = CreateCard("Pending Transactions", out lblPendingTransVal, 690, cardY, cardWidth, cardHeight, Color.FromArgb(255, 193, 7));
            tabDashboard.Controls.Add(pnlCard3);

            Label lblRecentTitle = new Label();
            lblRecentTitle.Text = "Recent Transactions";
            lblRecentTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblRecentTitle.Location = new Point(30, 230);
            lblRecentTitle.AutoSize = true;
            tabDashboard.Controls.Add(lblRecentTitle);

            dgvRecentTransactions = new DataGridView();
            dgvRecentTransactions.Location = new Point(30, 265);
            dgvRecentTransactions.Size = new System.Drawing.Size(965, 380);
            dgvRecentTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRecentTransactions.ReadOnly = true;
            dgvRecentTransactions.RowHeadersVisible = false;
            dgvRecentTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabDashboard.Controls.Add(dgvRecentTransactions);
        }

        private Panel CreateCard(string title, out Label valLabel, int x, int y, int width, int height, Color accentColor)
        {
            Panel panel = new Panel();
            panel.Location = new Point(x, y);
            panel.Size = new System.Drawing.Size(width, height);
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;

            Panel topStrip = new Panel();
            topStrip.Dock = DockStyle.Top;
            topStrip.Height = 5;
            topStrip.BackColor = accentColor;
            panel.Controls.Add(topStrip);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblTitle.ForeColor = Color.DimGray;
            lblTitle.Location = new Point(12, 12);
            lblTitle.AutoSize = true;
            panel.Controls.Add(lblTitle);

            valLabel = new Label();
            valLabel.Text = "0";
            valLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            valLabel.ForeColor = Color.Black;
            valLabel.Location = new Point(12, 38);
            valLabel.AutoSize = true;
            panel.Controls.Add(valLabel);

            return panel;
        }

        private void LoadDashboardData()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string currentUsername = "";
                    string staffQuery = "SELECT StaffUsername, StaffName, StaffProfilePic FROM Staff WHERE StaffName = @cashier OR StaffUsername = @cashier";
                    using (SQLiteCommand cmd = new SQLiteCommand(staffQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentUsername = reader["StaffUsername"]?.ToString() ?? "";
                                string realName = reader["StaffName"]?.ToString();
                                lblCashierName.Text = !string.IsNullOrEmpty(realName) ? $"Welcome, {realName}!" : $"Welcome, {loggedInCashier}!";

                                try
                                {
                                    if (reader["StaffProfilePic"] != DBNull.Value)
                                    {
                                        byte[] imgData = (byte[])reader["StaffProfilePic"];
                                        using (MemoryStream ms = new MemoryStream(imgData))
                                        {
                                            picProfile.Image = Image.FromStream(ms);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                    string querySales = "SELECT COALESCE(SUM(CAST(TransTotal AS REAL)), 0) FROM Transactions WHERE (TransStaff = @cashier OR TransStaff = @username) AND (date(TransDateTime) = @today OR TransDateTime LIKE @todayLike)";
                    using (SQLiteCommand cmd = new SQLiteCommand(querySales, conn))
                    {
                        cmd.Parameters.AddWithValue("@today", todayDate);
                        cmd.Parameters.AddWithValue("@todayLike", "%" + todayDate + "%");
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        cmd.Parameters.AddWithValue("@username", currentUsername);
                        object result = cmd.ExecuteScalar();
                        lblTodaySalesVal.Text = "₱" + Convert.ToDecimal(result ?? 0).ToString("N2");
                    }

                    string queryTransCount = "SELECT COUNT(*) FROM Transactions WHERE (TransStaff = @cashier OR TransStaff = @username) AND (date(TransDateTime) = @today OR TransDateTime LIKE @todayLike)";
                    using (SQLiteCommand cmd = new SQLiteCommand(queryTransCount, conn))
                    {
                        cmd.Parameters.AddWithValue("@today", todayDate);
                        cmd.Parameters.AddWithValue("@todayLike", "%" + todayDate + "%");
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        cmd.Parameters.AddWithValue("@username", currentUsername);
                        lblNumTransVal.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    string queryPending = "SELECT COUNT(*) FROM Transactions WHERE (TransStaff = @cashier OR TransStaff = @username) AND (TransStatus = 'Pending' OR TransPayStatus = 'Pending')";
                    using (SQLiteCommand cmd = new SQLiteCommand(queryPending, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        cmd.Parameters.AddWithValue("@username", currentUsername);
                        lblPendingTransVal.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }


                    string queryRecent = "SELECT TransID AS 'Trans ID', TransTotal AS 'Total Amount', TransMethod AS 'Payment Method', TransStatus AS 'Status', TransDateTime AS 'Date/Time' FROM Transactions WHERE TransStaff = @cashier OR TransStaff = @username ORDER BY TransID DESC LIMIT 10";
                    using (SQLiteCommand cmdRecent = new SQLiteCommand(queryRecent, conn))
                    {
                        cmdRecent.Parameters.AddWithValue("@cashier", loggedInCashier);
                        cmdRecent.Parameters.AddWithValue("@username", currentUsername);
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmdRecent))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            string currSymbol = GetCurrencySymbol();
                            foreach (DataRow row in dt.Rows)
                            {
                                if (row["Total Amount"] != DBNull.Value && decimal.TryParse(row["Total Amount"].ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                                {
                                    row["Total Amount"] = $"{currSymbol}{amt:N2}";
                                }
                            }
                            dgvRecentTransactions.DataSource = dt;
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dashboard Load Error: " + ex.Message);
            }
        }

        private void SetupPOSContent()
        {
            tabPOS.BackColor = Color.FromArgb(245, 247, 250);

            Label lblSearch = new Label();
            lblSearch.Text = "Product Search (Barcode / Name / ID):";
            lblSearch.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblSearch.Location = new Point(20, 15);
            lblSearch.AutoSize = true;
            tabPOS.Controls.Add(lblSearch);

            txtSearchProduct = new TextBox();
            txtSearchProduct.Location = new Point(20, 35);
            txtSearchProduct.Size = new System.Drawing.Size(240, 25);
            txtSearchProduct.Font = new Font("Segoe UI", 10);
            txtSearchProduct.TextChanged += (s, e) => FilterProducts();
            tabPOS.Controls.Add(txtSearchProduct);

            Label lblCategory = new Label();
            lblCategory.Text = "Category:";
            lblCategory.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblCategory.Location = new Point(275, 15);
            lblCategory.AutoSize = true;
            tabPOS.Controls.Add(lblCategory);

            cmbCategories = new ComboBox();
            cmbCategories.Location = new Point(275, 35);
            cmbCategories.Size = new System.Drawing.Size(220, 25);
            cmbCategories.Font = new Font("Segoe UI", 10);
            cmbCategories.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategories.Items.AddRange(new string[] {
                "All Categories",
                "T-SHIRT (men)",
                "T-SHIRT (women)",
                "LONG SKIRT",
                "SHORT SKIRT",
                "SHORTS",
                "PANTS",
                "DRESS",
                "JACKET"
            });
            cmbCategories.SelectedIndex = 0;
            cmbCategories.SelectedIndexChanged += (s, e) => FilterProducts();
            tabPOS.Controls.Add(cmbCategories);

            dgvProducts = new DataGridView();
            dgvProducts.Location = new Point(20, 75);
            dgvProducts.Size = new System.Drawing.Size(475, 520);
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.ReadOnly = true;
            dgvProducts.RowHeadersVisible = false;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.DoubleClick += (s, e) => BtnAddToCart_Click(s, e);
            tabPOS.Controls.Add(dgvProducts);

            btnAddToCart = new Button();
            btnAddToCart.Text = "Add product to transaction >>";
            btnAddToCart.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnAddToCart.BackColor = Color.FromArgb(40, 167, 69);
            btnAddToCart.ForeColor = Color.White;
            btnAddToCart.FlatStyle = FlatStyle.Flat;
            btnAddToCart.FlatAppearance.BorderSize = 0;
            btnAddToCart.Location = new Point(20, 605);
            btnAddToCart.Size = new System.Drawing.Size(475, 40);
            btnAddToCart.Cursor = Cursors.Hand;
            btnAddToCart.Click += BtnAddToCart_Click;
            tabPOS.Controls.Add(btnAddToCart);

            Label lblCartTitle = new Label();
            lblCartTitle.Text = "Current Order";
            lblCartTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblCartTitle.Location = new Point(515, 15);
            lblCartTitle.AutoSize = true;
            tabPOS.Controls.Add(lblCartTitle);

            dgvCart = new DataGridView();
            dgvCart.Location = new Point(515, 45);
            dgvCart.Size = new System.Drawing.Size(500, 280);
            dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCart.ReadOnly = true;
            dgvCart.RowHeadersVisible = false;
            dgvCart.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvCart.Columns.Add("ProdID", "ID");
            dgvCart.Columns.Add("ProdName", "Product Name");
            dgvCart.Columns.Add("Price", "Price");
            dgvCart.Columns.Add("Quantity", "Qty");
            dgvCart.Columns.Add("Total", "Total");
            tabPOS.Controls.Add(dgvCart);

            btnRemoveFromCart = new Button();
            btnRemoveFromCart.Text = "Remove Item";
            btnRemoveFromCart.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnRemoveFromCart.BackColor = Color.FromArgb(220, 53, 69);
            btnRemoveFromCart.ForeColor = Color.White;
            btnRemoveFromCart.FlatStyle = FlatStyle.Flat;
            btnRemoveFromCart.FlatAppearance.BorderSize = 0;
            btnRemoveFromCart.Location = new Point(890, 15);
            btnRemoveFromCart.Size = new System.Drawing.Size(125, 26);
            btnRemoveFromCart.Cursor = Cursors.Hand;
            btnRemoveFromCart.Click += BtnRemoveFromCart_Click;
            tabPOS.Controls.Add(btnRemoveFromCart);

            Panel pnlSummary = new Panel();
            pnlSummary.Location = new Point(515, 335);
            pnlSummary.Size = new System.Drawing.Size(500, 175);
            pnlSummary.BackColor = Color.White;
            pnlSummary.BorderStyle = BorderStyle.FixedSingle;
            tabPOS.Controls.Add(pnlSummary);

            AddSummaryRow(pnlSummary, "Subtotal:", out lblSubtotalVal, 15);
            AddSummaryRow(pnlSummary, "Tax (12%):", out lblTaxVal, 45);

            Label lblDisc = new Label();
            lblDisc.Text = "Discount (₱):";
            lblDisc.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblDisc.Location = new Point(15, 75);
            lblDisc.AutoSize = true;
            pnlSummary.Controls.Add(lblDisc);

            txtDiscount = new TextBox();
            txtDiscount.Location = new Point(230, 73);
            txtDiscount.Size = new System.Drawing.Size(95, 25);
            txtDiscount.Text = "0.00";
            txtDiscount.TextChanged += (s, e) => RecalculateTotals();
            pnlSummary.Controls.Add(txtDiscount);

            btnApplySeniorDiscount = new Button();
            btnApplySeniorDiscount.Text = "Senior/PWD ID";
            btnApplySeniorDiscount.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            btnApplySeniorDiscount.BackColor = Color.FromArgb(23, 162, 184);
            btnApplySeniorDiscount.ForeColor = Color.White;
            btnApplySeniorDiscount.FlatStyle = FlatStyle.Flat;
            btnApplySeniorDiscount.FlatAppearance.BorderSize = 0;
            btnApplySeniorDiscount.Location = new Point(335, 73);
            btnApplySeniorDiscount.Size = new System.Drawing.Size(150, 26);
            btnApplySeniorDiscount.Cursor = Cursors.Hand;
            btnApplySeniorDiscount.Click += BtnApplySeniorDiscount_Click;
            pnlSummary.Controls.Add(btnApplySeniorDiscount);

            Panel pnlLine = new Panel();
            pnlLine.Location = new Point(15, 105);
            pnlLine.Size = new System.Drawing.Size(470, 2);
            pnlLine.BackColor = Color.LightGray;
            pnlSummary.Controls.Add(pnlLine);

            Label lblTotalText = new Label();
            lblTotalText.Text = "Total Amount:";
            lblTotalText.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTotalText.Location = new Point(15, 118);
            lblTotalText.AutoSize = true;
            pnlSummary.Controls.Add(lblTotalText);

            lblTotalVal = new Label();
            lblTotalVal.Text = "₱0.00";
            lblTotalVal.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTotalVal.ForeColor = Color.FromArgb(40, 167, 69);
            lblTotalVal.Location = new Point(350, 116);
            lblTotalVal.TextAlign = ContentAlignment.TopRight;
            pnlSummary.Controls.Add(lblTotalVal);

            Panel pnlCheckout = new Panel();
            pnlCheckout.Location = new Point(515, 520);
            pnlCheckout.Size = new System.Drawing.Size(500, 125);
            pnlCheckout.BackColor = Color.White;
            pnlCheckout.BorderStyle = BorderStyle.FixedSingle;
            tabPOS.Controls.Add(pnlCheckout);

            Label lblMop = new Label();
            lblMop.Text = "Payment Method:";
            lblMop.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblMop.Location = new Point(15, 15);
            lblMop.AutoSize = true;
            pnlCheckout.Controls.Add(lblMop);

            cmbPaymentMethod = new ComboBox();
            cmbPaymentMethod.Location = new Point(135, 12);
            cmbPaymentMethod.Size = new System.Drawing.Size(180, 25);
            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Items.AddRange(new string[] {
                "Cash",
                "GCash QR / Card",
                "PayMaya QR / Card",
                "GoTyme QR / Card"
            });
            cmbPaymentMethod.SelectedIndex = 0;
            pnlCheckout.Controls.Add(cmbPaymentMethod);

            Label lblReceived = new Label();
            lblReceived.Text = "Amount Received:";
            lblReceived.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblReceived.Location = new Point(15, 50);
            lblReceived.AutoSize = true;
            pnlCheckout.Controls.Add(lblReceived);

            txtAmountReceived = new TextBox();
            txtAmountReceived.Location = new Point(135, 47);
            txtAmountReceived.Size = new System.Drawing.Size(180, 25);
            txtAmountReceived.TextChanged += (s, e) => RecalculateTotals();
            pnlCheckout.Controls.Add(txtAmountReceived);

            Label lblChangeText = new Label();
            lblChangeText.Text = "Change:";
            lblChangeText.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblChangeText.Location = new Point(15, 85);
            lblChangeText.AutoSize = true;
            pnlCheckout.Controls.Add(lblChangeText);

            lblChangeVal = new Label();
            lblChangeVal.Text = "₱0.00";
            lblChangeVal.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblChangeVal.ForeColor = Color.Blue;
            lblChangeVal.Location = new Point(135, 83);
            lblChangeVal.AutoSize = true;
            pnlCheckout.Controls.Add(lblChangeVal);

            btnCompleteTrans = new Button();
            btnCompleteTrans.Text = "Complete Transaction";
            btnCompleteTrans.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCompleteTrans.BackColor = Color.FromArgb(0, 123, 255);
            btnCompleteTrans.ForeColor = Color.White;
            btnCompleteTrans.FlatStyle = FlatStyle.Flat;
            btnCompleteTrans.FlatAppearance.BorderSize = 0;
            btnCompleteTrans.Location = new Point(330, 12);
            btnCompleteTrans.Size = new System.Drawing.Size(155, 50);
            btnCompleteTrans.Cursor = Cursors.Hand;
            btnCompleteTrans.Click += BtnCompleteTrans_Click;
            pnlCheckout.Controls.Add(btnCompleteTrans);

            btnPrintReceipt = new Button();
            btnPrintReceipt.Text = "Print Receipt";
            btnPrintReceipt.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnPrintReceipt.BackColor = Color.FromArgb(108, 117, 125);
            btnPrintReceipt.ForeColor = Color.White;
            btnPrintReceipt.FlatStyle = FlatStyle.Flat;
            btnPrintReceipt.FlatAppearance.BorderSize = 0;
            btnPrintReceipt.Location = new Point(330, 68);
            btnPrintReceipt.Size = new System.Drawing.Size(155, 42);
            btnPrintReceipt.Cursor = Cursors.Hand;
            btnPrintReceipt.Click += BtnPrintReceipt_Click;
            pnlCheckout.Controls.Add(btnPrintReceipt);
        }

        private void BtnApplySeniorDiscount_Click(object sender, EventArgs e)
        {
            Form inputForm = new Form()
            {
                Width = 380,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Senior Citizen / PWD Verification",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblName = new Label() { Left = 20, Top = 20, Text = "Customer's Name:", AutoSize = true };
            TextBox txtCustName = new TextBox() { Left = 20, Top = 45, Width = 320 };

            Label lblIdNo = new Label() { Left = 20, Top = 80, Text = "Senior/PWD ID Number:", AutoSize = true };
            TextBox txtCustIdNum = new TextBox() { Left = 20, Top = 105, Width = 320 };

            Button btnConfirm = new Button() { Text = "Apply Discount", Left = 190, Top = 140, Width = 150, Height = 30, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White };

            inputForm.Controls.Add(lblName);
            inputForm.Controls.Add(txtCustName);
            inputForm.Controls.Add(lblIdNo);
            inputForm.Controls.Add(txtCustIdNum);
            inputForm.Controls.Add(btnConfirm);
            inputForm.AcceptButton = btnConfirm;

            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtCustName.Text) || string.IsNullOrWhiteSpace(txtCustIdNum.Text))
                {
                    MessageBox.Show("Required fields are empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string currSymbol = GetCurrencySymbol();
                decimal grossTotal = 0;
                foreach (DataGridViewRow row in dgvCart.Rows)
                {
                    if (row.Cells["Total"].Value != null)
                    {
                        string cleanVal = row.Cells["Total"].Value.ToString().Replace(currSymbol, "").Replace("₱", "").Replace(",", "").Trim();
                        if (decimal.TryParse(cleanVal, out decimal itemTot))
                        {
                            grossTotal += itemTot;
                        }
                    }
                }

                decimal vatableSales = grossTotal / 1.12m;
                decimal discountAmount = vatableSales * 0.20m;

                txtDiscount.Text = discountAmount.ToString("N2");
                appliedDiscountInfo = $"Senior/PWD - Name: {txtCustName.Text}, ID: {txtCustIdNum.Text}";

                MessageBox.Show("Successfully applied Senior Citizen / PWD discount (VAT-Exempt + 20%)!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RecalculateTotals();
            }
        }

        private void AddSummaryRow(Panel panel, string title, out Label valLabel, int top)
        {
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lbl.Location = new Point(15, top);
            lbl.AutoSize = true;
            panel.Controls.Add(lbl);

            valLabel = new Label();
            valLabel.Text = "₱0.00";
            valLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            valLabel.Location = new Point(350, top);
            valLabel.TextAlign = ContentAlignment.TopRight;
            panel.Controls.Add(valLabel);
        }

        private void LoadPOSProducts()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ProdID AS 'ID', ProdName AS 'Product Name', Category, Size, Price, Stock AS 'Available Stock' FROM Products";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvProducts.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading products: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterProducts()
        {
            try
            {
                string search = txtSearchProduct.Text.Trim();
                string category = cmbCategories.SelectedItem?.ToString() ?? "All Categories";

                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ProdID AS 'ID', ProdName AS 'Product Name', Category, Size, Price, Stock AS 'Available Stock' FROM Products WHERE (ProdName LIKE @search OR ProdID LIKE @search)";

                    if (category != "All Categories")
                    {
                        query += " AND Category = @category";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + search + "%");
                        if (category != "All Categories")
                        {
                            cmd.Parameters.AddWithValue("@category", category);
                        }

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            dgvProducts.DataSource = dt;
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnAddToCart_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select or scan a product from the list.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow row = dgvProducts.SelectedRows[0];
            string prodId = row.Cells["ID"].Value?.ToString() ?? "";
            string prodName = row.Cells["Product Name"].Value?.ToString() ?? "";

            string currSymbol = GetCurrencySymbol();

            decimal price = 0;
            string priceStr = row.Cells["Price"].Value?.ToString().Replace(currSymbol, "").Replace("₱", "").Replace(",", "").Trim();
            decimal.TryParse(priceStr, out price);

            int availableStock = 0;
            string stockStr = row.Cells["Available Stock"].Value?.ToString().Trim();
            int.TryParse(stockStr, out availableStock);

            if (availableStock <= 0)
            {
                MessageBox.Show("This product is out of stock.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool found = false;
            foreach (DataGridViewRow cartRow in dgvCart.Rows)
            {
                if (cartRow.Cells["ProdID"].Value?.ToString() == prodId)
                {
                    int currentQty = Convert.ToInt32(cartRow.Cells["Quantity"].Value);
                    if (currentQty + 1 > availableStock)
                    {
                        MessageBox.Show("You have reached the available stock limit.", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cartRow.Cells["Quantity"].Value = currentQty + 1;
                    decimal newTotal = (currentQty + 1) * price;
                    cartRow.Cells["Total"].Value = $"{currSymbol}{newTotal:N2}";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                dgvCart.Rows.Add(prodId, prodName, $"{currSymbol}{price:N2}", 1, $"{currSymbol}{price:N2}");
            }

            RecalculateTotals();
        }

        private void BtnRemoveFromCart_Click(object sender, EventArgs e)
        {
            if (dgvCart.SelectedRows.Count > 0)
            {
                dgvCart.Rows.RemoveAt(dgvCart.SelectedRows[0].Index);
                RecalculateTotals();
            }
            else
            {
                MessageBox.Show("Please select an item from the cart.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool ProcessEWalletPayment(string walletName, decimal totalAmount)
        {
            bool isConfirmed = false;

            Form qrForm = new Form()
            {
                Width = 400,
                Height = 510,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"{walletName} Payment",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblTitle = new Label()
            {
                Text = $"Scan QR or Input {walletName} Details",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new System.Drawing.Point(20, 15),
                AutoSize = true
            };
            qrForm.Controls.Add(lblTitle);

            PictureBox picQrCode = new PictureBox()
            {
                Location = new System.Drawing.Point(100, 50),
                Size = new System.Drawing.Size(180, 180),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            string cleanName = walletName.ToLower().Replace(" ", "");
            string fileName = cleanName + "_qr.jpg";

            string pathInResources = Path.Combine(Application.StartupPath, "Resources", fileName);
            string pathInRoot = Path.Combine(Application.StartupPath, fileName);

            Image qrImage = null;
            try
            {
                if (File.Exists(pathInResources))
                {
                    using (var tempImg = Image.FromFile(pathInResources))
                    {
                        qrImage = new Bitmap(tempImg);
                    }
                }
                else if (File.Exists(pathInRoot))
                {
                    using (var tempImg = Image.FromFile(pathInRoot))
                    {
                        qrImage = new Bitmap(tempImg);
                    }
                }
            }
            catch { }

            if (qrImage != null)
            {
                picQrCode.Image = qrImage;
            }
            else
            {
                
                Bitmap bmp = new Bitmap(180, 180);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(245, 245, 245));
                    using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
                    {
                        string msg = $"{walletName.ToUpper()}\n\n[ Image Missing ]\nI-check ang filename:\n{fileName}";
                        g.DrawString(msg, font, Brushes.DimGray, new RectangleF(10, 40, 160, 100),
                            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    }
                }
                picQrCode.Image = bmp;
            }

            qrForm.Controls.Add(picQrCode);

            Label lblAmountDisplay = new Label()
            {
                Text = $"Amount to Pay: ₱{totalAmount:N2}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                Location = new System.Drawing.Point(100, 235),
                AutoSize = true
            };
            lblAmountDisplay.Left = (qrForm.ClientSize.Width - lblAmountDisplay.Width) / 2;
            qrForm.Controls.Add(lblAmountDisplay);

            Label lblAccNo = new Label()
            {
                Text = "Account Number / Mobile No:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new System.Drawing.Point(40, 270),
                AutoSize = true
            };
            TextBox txtWalletAcc = new TextBox()
            {
                Location = new System.Drawing.Point(40, 295),
                Font = new Font("Segoe UI", 10),
                Width = 300
            };
            qrForm.Controls.Add(lblAccNo);
            qrForm.Controls.Add(txtWalletAcc);

            Label lblRefNo = new Label()
            {
                Text = "Reference Number:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new System.Drawing.Point(40, 330),
                AutoSize = true
            };
            TextBox txtWalletRef = new TextBox()
            {
                Location = new System.Drawing.Point(40, 355),
                Font = new Font("Segoe UI", 10),
                Width = 300
            };
            qrForm.Controls.Add(lblRefNo);
            qrForm.Controls.Add(txtWalletRef);

            Button btnOk = new Button()
            {
                Text = "OK (Confirm)",
                Location = new System.Drawing.Point(120, 400),
                Size = new System.Drawing.Size(140, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            btnOk.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtWalletAcc.Text) || string.IsNullOrWhiteSpace(txtWalletRef.Text))
                {
                    MessageBox.Show("Please fill in the account and reference numbers.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                isConfirmed = true;
                qrForm.Close();
            };

            qrForm.Controls.Add(btnOk);
            qrForm.AcceptButton = btnOk;
            qrForm.ShowDialog();

            return isConfirmed;
        }

        private void BtnCompleteTrans_Click(object sender, EventArgs e)
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("There are no items in the cart.", "Empty Cart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            decimal grossTotal = 0;
            string currSymbol = GetCurrencySymbol();
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.Cells["Total"].Value != null)
                {
                    string cleanVal = row.Cells["Total"].Value.ToString().Replace(currSymbol, "").Replace("₱", "").Replace(",", "").Trim();
                    if (decimal.TryParse(cleanVal, out decimal itemTotal))
                    {
                        grossTotal += itemTotal;
                    }
                }
            }
            decimal discount = 0;
            decimal.TryParse(txtDiscount.Text, out discount);
            decimal totalAmount = grossTotal;
            decimal tax = 0;
            decimal grandTotal = 0;
            if (discount > 0)
            {
                totalAmount = grossTotal / 1.12m; 
                tax = 0;                         
                grandTotal = totalAmount - discount;
            }
            else
            {
                tax = totalAmount * 0.12m;   
                grandTotal = totalAmount + tax;
            }
            decimal amountReceived = 0;
            decimal.TryParse(txtAmountReceived.Text, out amountReceived);
            string paymentMethod = cmbPaymentMethod.SelectedItem?.ToString() ?? "Cash";
            if (paymentMethod.Contains("GCash") || paymentMethod.Contains("PayMaya") || paymentMethod.Contains("GoTyme"))
            {
                string walletName = paymentMethod.Contains("GCash") ? "GCash" : (paymentMethod.Contains("PayMaya") ? "PayMaya" : "GoTyme");
                bool confirmed = ProcessEWalletPayment(walletName, grandTotal);
                if (!confirmed)
                {
                    return; 
                }
                amountReceived = grandTotal;
            }
            else if (amountReceived < grandTotal && paymentMethod == "Cash")
            {
                MessageBox.Show("Insufficient payment received.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string currentStoreName = "PTOF STORE";
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string storeQuery = "SELECT Value FROM Settings WHERE Key = 'StoreName' LIMIT 1";
                    using (SQLiteCommand cmdStore = new SQLiteCommand(storeQuery, conn))
                    {
                        object storeResult = cmdStore.ExecuteScalar();
                        if (storeResult != null && !string.IsNullOrEmpty(storeResult.ToString()))
                        {
                            currentStoreName = storeResult.ToString().ToUpper();
                        }
                    }
                }
            }
            catch { }
            string transId = "TRX-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            string orderId = "ORD-" + DateTime.Now.ToString("mmss");
            string dateTimeNow = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");
            System.Text.StringBuilder sbReceipt = new System.Text.StringBuilder();
            sbReceipt.AppendLine("====================================");
            int totalWidth = 33;
            int leftPadding = Math.Max(0, (totalWidth - currentStoreName.Length) / 2);
            sbReceipt.AppendLine(currentStoreName.PadLeft(leftPadding + currentStoreName.Length).PadRight(totalWidth));
            sbReceipt.AppendLine("====================================");
            sbReceipt.AppendLine($"Trans ID: {transId}");
            sbReceipt.AppendLine($"Date: {dateTimeNow}");
            sbReceipt.AppendLine($"Cashier: {loggedInCashier}");
            sbReceipt.AppendLine($"Payment: {paymentMethod}");
            if (appliedDiscountInfo != "None")
                sbReceipt.AppendLine($"Customer Info: {appliedDiscountInfo}");
            sbReceipt.AppendLine("------------------------------------");
            sbReceipt.AppendLine(string.Format("{0,-13} {1,3} {2,8} {3,8}", "Item", "Qty", "Price", "Total"));
            sbReceipt.AppendLine("------------------------------------");
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (SQLiteTransaction sqliteTrans = conn.BeginTransaction())
                    {
                        try
                        {
                            string itemsSummary = "";
                            foreach (DataGridViewRow row in dgvCart.Rows)
                            {
                                if (row.Cells["ProdID"].Value == null) continue;
                                string pId = row.Cells["ProdID"].Value.ToString();
                                string pName = row.Cells["ProdName"].Value?.ToString() ?? "";
                                int qty = Convert.ToInt32(row.Cells["Quantity"].Value);
                                string cleanPrice = row.Cells["Price"].Value?.ToString().Replace(currSymbol, "").Replace("₱", "").Replace(",", "").Trim() ?? "0";
                                string cleanTotal = row.Cells["Total"].Value?.ToString().Replace(currSymbol, "").Replace("₱", "").Replace(",", "").Trim() ?? "0";
                                decimal.TryParse(cleanPrice, out decimal price);
                                decimal.TryParse(cleanTotal, out decimal tot);
                                itemsSummary += $"{pName} (x{qty}), ";
                                string truncatedName = pName.Length > 13 ? pName.Substring(0, 13) : pName;
                                sbReceipt.AppendLine(string.Format("{0,-13} {1,3} {2,8:N2} {3,8:N2}", truncatedName, qty, price, tot));
                                string updateStock = "UPDATE Products SET Stock = CAST(Stock AS INTEGER) - @qty WHERE ProdID = @prodId";
                                using (SQLiteCommand cmdStock = new SQLiteCommand(updateStock, conn, sqliteTrans))
                                {
                                    cmdStock.Parameters.AddWithValue("@qty", qty);
                                    cmdStock.Parameters.AddWithValue("@prodId", pId);
                                    cmdStock.ExecuteNonQuery();
                                }
                            }
                            string insertTrans = "INSERT INTO Transactions (TransID, TransOrderID, TransDateTime, TransCustomer, TransStaff, TransItems, TransTotal, TransMethod, TransPayStatus, TransStatus) VALUES (@transId, @orderId, @dateTime, @customer, @staff, @items, @total, @method, @payStatus, @status)";
                            using (SQLiteCommand cmdTrans = new SQLiteCommand(insertTrans, conn, sqliteTrans))
                            {
                                cmdTrans.Parameters.AddWithValue("@transId", transId);
                                cmdTrans.Parameters.AddWithValue("@orderId", orderId);
                                cmdTrans.Parameters.AddWithValue("@dateTime", dateTimeNow);
                                cmdTrans.Parameters.AddWithValue("@customer", appliedDiscountInfo != "None" ? appliedDiscountInfo : "Walk-in Customer");
                                cmdTrans.Parameters.AddWithValue("@staff", loggedInCashier);
                                cmdTrans.Parameters.AddWithValue("@items", itemsSummary);
                                cmdTrans.Parameters.AddWithValue("@total", grandTotal.ToString("F2"));
                                cmdTrans.Parameters.AddWithValue("@method", paymentMethod);
                                cmdTrans.Parameters.AddWithValue("@payStatus", "Paid");
                                cmdTrans.Parameters.AddWithValue("@status", "Completed");
                                cmdTrans.ExecuteNonQuery();
                            }

                            sqliteTrans.Commit();
                        }
                        catch
                        {
                            sqliteTrans.Rollback();
                            throw;
                        }
                    }
                }

                sbReceipt.AppendLine("------------------------------------");
                sbReceipt.AppendLine($"Subtotal: ₱{totalAmount:N2}");
                sbReceipt.AppendLine($"Tax: ₱{tax:N2}");
                sbReceipt.AppendLine($"Discount: ₱{discount:N2}");
                sbReceipt.AppendLine($"TOTAL: ₱{grandTotal:N2}");
                sbReceipt.AppendLine($"Amount Received: ₱{amountReceived:N2}");
                sbReceipt.AppendLine($"Change: ₱{(amountReceived - grandTotal):N2}");
                sbReceipt.AppendLine("====================================");
                sbReceipt.AppendLine("            THANK YOU !!!       ");
                sbReceipt.AppendLine("====================================");

                lastReceiptText = sbReceipt.ToString();

                MessageBox.Show("Successfully completed transaction!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                dgvCart.Rows.Clear();
                txtDiscount.Text = "0.00";
                txtAmountReceived.Text = "";
                appliedDiscountInfo = "None";
                RecalculateTotals();
                LoadPOSProducts();
                LoadTransactionsData();
                LoadSalesSummaryData();
                LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error to complete transaction: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RecalculateTotals()
        {
            string currSymbol = GetCurrencySymbol();
            decimal grossTotal = 0;
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                if (row.Cells["Total"].Value != null)
                {
                    string cleanTotal = row.Cells["Total"].Value.ToString().Replace(currSymbol, "").Replace("₱", "").Replace(",", "").Trim();
                    if (decimal.TryParse(cleanTotal, out decimal totVal))
                    {
                        grossTotal += totVal;
                    }
                }
            }

            decimal discount = 0;
            decimal.TryParse(txtDiscount.Text, out discount);

            decimal subtotal = grossTotal;
            decimal tax = 0;
            decimal total = 0;

            if (discount > 0)
            {
                subtotal = grossTotal / 1.12m; 
                tax = 0;                      
                total = subtotal - discount; 
            }
            else
            {
                tax = subtotal * 0.12m;       
                total = (subtotal + tax);
            }

            if (total < 0) total = 0;

            lblSubtotalVal.Text = $"{currSymbol}{subtotal:N2}";
            lblTaxVal.Text = $"{currSymbol}{tax:N2}";
            lblTotalVal.Text = $"{currSymbol}{total:N2}";

            decimal amountReceived = 0;
            decimal.TryParse(txtAmountReceived.Text, out amountReceived);

            decimal change = amountReceived - total;
            lblChangeVal.Text = change >= 0 ? $"{currSymbol}{change:N2}" : $"{currSymbol}0.00";
        }

        private void BtnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(lastReceiptText))
            {
                MessageBox.Show("There is no receipt available to print. Complete a transaction first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDocument printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) => {
                Font font = new Font("Courier New", 9);
                ev.Graphics.DrawString(lastReceiptText, font, Brushes.Black, new PointF(10, 10));
            };

            PrintDialog printDlg = new PrintDialog();
            printDlg.Document = printDoc;
            if (printDlg.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
        }

        private void SetupTransactionsContent()
        {
            tabTransactions.BackColor = Color.FromArgb(245, 247, 250);

            Label lblTitle = new Label();
            lblTitle.Text = "Transactions Management";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.Location = new Point(30, 20);
            lblTitle.AutoSize = true;
            tabTransactions.Controls.Add(lblTitle);

            Label lblSearch = new Label();
            lblSearch.Text = "Search Transaction (ID / Customer / Method):";
            lblSearch.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblSearch.Location = new Point(30, 65);
            lblSearch.AutoSize = true;
            tabTransactions.Controls.Add(lblSearch);

            txtSearchTrans = new TextBox();
            txtSearchTrans.Location = new Point(30, 88);
            txtSearchTrans.Size = new System.Drawing.Size(350, 25);
            txtSearchTrans.Font = new Font("Segoe UI", 10);
            txtSearchTrans.TextChanged += (s, e) => FilterTransactions();
            tabTransactions.Controls.Add(txtSearchTrans);

            dgvTransactions = new DataGridView();
            dgvTransactions.Location = new Point(30, 130);
            dgvTransactions.Size = new System.Drawing.Size(975, 460);
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.ReadOnly = true;
            dgvTransactions.RowHeadersVisible = false;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabTransactions.Controls.Add(dgvTransactions);

            btnViewTransDetails = new Button();
            btnViewTransDetails.Text = "View Details (Receipt)";
            btnViewTransDetails.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnViewTransDetails.BackColor = Color.FromArgb(23, 162, 184);
            btnViewTransDetails.ForeColor = Color.White;
            btnViewTransDetails.FlatStyle = FlatStyle.Flat;
            btnViewTransDetails.FlatAppearance.BorderSize = 0;
            btnViewTransDetails.Location = new Point(715, 605);
            btnViewTransDetails.Size = new System.Drawing.Size(145, 40);
            btnViewTransDetails.Cursor = Cursors.Hand;
            btnViewTransDetails.Click += BtnViewTransDetails_Click;
            tabTransactions.Controls.Add(btnViewTransDetails);

            btnPrintTransReceipt = new Button();
            btnPrintTransReceipt.Text = "Print Receipt";
            btnPrintTransReceipt.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnPrintTransReceipt.BackColor = Color.FromArgb(40, 167, 69);
            btnPrintTransReceipt.ForeColor = Color.White;
            btnPrintTransReceipt.FlatStyle = FlatStyle.Flat;
            btnPrintTransReceipt.FlatAppearance.BorderSize = 0;
            btnPrintTransReceipt.Location = new Point(870, 605);
            btnPrintTransReceipt.Size = new System.Drawing.Size(135, 40);
            btnPrintTransReceipt.Cursor = Cursors.Hand;
            btnPrintTransReceipt.Click += BtnPrintTransReceipt_Click;
            tabTransactions.Controls.Add(btnPrintTransReceipt);
        }

        private void LoadTransactionsData()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT TransID AS 'Transaction ID', TransDateTime AS 'Date & Time', TransCustomer AS 'Customer', TransItems AS 'Items', TransTotal AS 'Total', TransMethod AS 'Payment Method', TransStatus AS 'Transaction Status' FROM Transactions ORDER BY TransDateTime DESC";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        string currSymbol = GetCurrencySymbol();
                        foreach (DataRow row in dt.Rows)
                        {
                            if (row["Total"] != DBNull.Value && decimal.TryParse(row["Total"].ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                            {
                                row["Total"] = $"{currSymbol}{amt:N2}";
                            }
                        }
                        dgvTransactions.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading transactions: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterTransactions()
        {
            try
            {
                string search = txtSearchTrans.Text.Trim();
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT TransID AS 'Transaction ID', TransDateTime AS 'Date & Time', TransCustomer AS 'Customer', TransItems AS 'Items', TransTotal AS 'Total', TransMethod AS 'Payment Method', TransStatus AS 'Transaction Status' FROM Transactions WHERE (TransID LIKE @search OR TransCustomer LIKE @search OR TransMethod LIKE @search) ORDER BY TransDateTime DESC";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + search + "%");
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            string currSymbol = GetCurrencySymbol();
                            foreach (DataRow row in dt.Rows)
                            {
                                if (row["Total"] != DBNull.Value && decimal.TryParse(row["Total"].ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                                {
                                    row["Total"] = $"{currSymbol}{amt:N2}";
                                }
                            }
                            dgvTransactions.DataSource = dt;
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnViewTransDetails_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a transaction to view details.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow row = dgvTransactions.SelectedRows[0];
            string transId = row.Cells["Transaction ID"].Value?.ToString() ?? "";

            string receiptDetails = GenerateReceiptTextFromTransID(transId);

            Form receiptForm = new Form()
            {
                Width = 380,
                Height = 500,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"Transaction Receipt - {transId}",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            TextBox txtReceiptBox = new TextBox()
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Courier New", 9),
                Text = receiptDetails,
                ScrollBars = ScrollBars.Vertical
            };

            receiptForm.Controls.Add(txtReceiptBox);
            receiptForm.ShowDialog();
        }

        private void BtnPrintTransReceipt_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a transaction to print.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow row = dgvTransactions.SelectedRows[0];
            string transId = row.Cells["Transaction ID"].Value?.ToString() ?? "";
            string receiptDetails = GenerateReceiptTextFromTransID(transId);

            PrintDocument printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) => {
                Font font = new Font("Courier New", 9);
                ev.Graphics.DrawString(receiptDetails, font, Brushes.Black, new PointF(10, 10));
            };

            PrintDialog printDlg = new PrintDialog();
            printDlg.Document = printDoc;
            if (printDlg.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
        }

        private string GenerateReceiptTextFromTransID(string transId)
        {
            string currentStoreName = "PTOF STORE";
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string storeQuery = "SELECT Value FROM Settings WHERE Key = 'StoreName' LIMIT 1";
                    using (SQLiteCommand cmdStore = new SQLiteCommand(storeQuery, conn))
                    {
                        object storeResult = cmdStore.ExecuteScalar();
                        if (storeResult != null && !string.IsNullOrEmpty(storeResult.ToString()))
                        {
                            currentStoreName = storeResult.ToString().ToUpper();
                        }
                    }
                }
            }
            catch { }

            string dateTime = "";
            string cashier = "";
            string customer = "";
            string items = "";
            string total = "";
            string method = "";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Transactions WHERE TransID = @transId";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@transId", transId);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                dateTime = reader["TransDateTime"]?.ToString() ?? "";
                                cashier = reader["TransStaff"]?.ToString() ?? "";
                                customer = reader["TransCustomer"]?.ToString() ?? "";
                                items = reader["TransItems"]?.ToString() ?? "";
                                total = reader["TransTotal"]?.ToString() ?? "0.00";
                                method = reader["TransMethod"]?.ToString() ?? "";
                            }
                        }
                    }
                }
            }
            catch { }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("====================================");
            int totalWidth = 33;
            int leftPadding = Math.Max(0, (totalWidth - currentStoreName.Length) / 2);
            sb.AppendLine(currentStoreName.PadLeft(leftPadding + currentStoreName.Length).PadRight(totalWidth));
            sb.AppendLine("====================================");
            sb.AppendLine($"Trans ID: {transId}");
            sb.AppendLine($"Date: {dateTime}");
            sb.AppendLine($"Cashier: {cashier}");
            sb.AppendLine($"Payment: {method}");
            if (!string.IsNullOrEmpty(customer) && customer != "Walk-in Customer")
                sb.AppendLine($"Customer Info: {customer}");
            sb.AppendLine("------------------------------------");
            sb.AppendLine("Items Summary:");
            sb.AppendLine(items);
            sb.AppendLine("------------------------------------");
            sb.AppendLine($"TOTAL: ₱{total}");
            sb.AppendLine("====================================");
            sb.AppendLine("            THANK YOU !!!       ");
            sb.AppendLine("====================================");

            return sb.ToString();
        }

        private void SetupReturnsRefundsContent()
        {
            tabReturnsRefunds.BackColor = Color.FromArgb(245, 247, 250);

            Label lblTitle = new Label();
            lblTitle.Text = "Returns and Refunds Management";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.Location = new Point(30, 20);
            lblTitle.AutoSize = true;
            tabReturnsRefunds.Controls.Add(lblTitle);

            Label lblSearch = new Label();
            lblSearch.Text = "Search Return (Transaction ID / Customer):";
            lblSearch.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblSearch.Location = new Point(30, 65);
            lblSearch.AutoSize = true;
            tabReturnsRefunds.Controls.Add(lblSearch);

            txtSearchReturn = new TextBox();
            txtSearchReturn.Location = new Point(30, 88);
            txtSearchReturn.Size = new System.Drawing.Size(350, 25);
            txtSearchReturn.Font = new Font("Segoe UI", 10);
            txtSearchReturn.TextChanged += (s, e) => FilterReturns();
            tabReturnsRefunds.Controls.Add(txtSearchReturn);

            dgvReturns = new DataGridView();
            dgvReturns.Location = new Point(30, 130);
            dgvReturns.Size = new System.Drawing.Size(975, 460);
            dgvReturns.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReturns.ReadOnly = true;
            dgvReturns.RowHeadersVisible = false;
            dgvReturns.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabReturnsRefunds.Controls.Add(dgvReturns);

            btnProcessReturn = new Button();
            btnProcessReturn.Text = "+ Request New Return/Refund";
            btnProcessReturn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnProcessReturn.BackColor = Color.FromArgb(40, 167, 69);
            btnProcessReturn.ForeColor = Color.White;
            btnProcessReturn.FlatStyle = FlatStyle.Flat;
            btnProcessReturn.FlatAppearance.BorderSize = 0;
            btnProcessReturn.Location = new Point(780, 605);
            btnProcessReturn.Size = new System.Drawing.Size(225, 40);
            btnProcessReturn.Cursor = Cursors.Hand;
            btnProcessReturn.Click += BtnProcessReturn_Click;
            tabReturnsRefunds.Controls.Add(btnProcessReturn);
        }

        private void LoadReturnsData()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ReturnID AS 'Return ID', TransID AS 'Transaction ID', Customer, Products AS 'Purchased Products', PurchaseDate AS 'Purchase Date', Reason AS 'Reason for Return', Quantity AS 'Qty to Return', RefundAmount AS 'Refund Amount', Status AS 'Return Status' FROM Returns ORDER BY PurchaseDate DESC";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        string currSymbol = GetCurrencySymbol();
                        foreach (DataRow row in dt.Rows)
                        {
                            if (row["Refund Amount"] != DBNull.Value && decimal.TryParse(row["Refund Amount"].ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                            {
                                row["Refund Amount"] = $"{currSymbol}{amt:N2}";
                            }
                        }
                        dgvReturns.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading returns data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterReturns()
        {
            try
            {
                string search = txtSearchReturn.Text.Trim();
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ReturnID AS 'Return ID', TransID AS 'Transaction ID', Customer, Products AS 'Purchased Products', PurchaseDate AS 'Purchase Date', Reason AS 'Reason for Return', Quantity AS 'Qty to Return', RefundAmount AS 'Refund Amount', Status AS 'Return Status' FROM Returns WHERE (TransID LIKE @search OR Customer LIKE @search) ORDER BY PurchaseDate DESC";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + search + "%");
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            string currSymbol = GetCurrencySymbol();
                            foreach (DataRow row in dt.Rows)
                            {
                                if (row["Refund Amount"] != DBNull.Value && decimal.TryParse(row["Refund Amount"].ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                                {
                                    row["Refund Amount"] = $"{currSymbol}{amt:N2}";
                                }
                            }
                            dgvReturns.DataSource = dt;
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnProcessReturn_Click(object sender, EventArgs e)
        {
            Form returnForm = new Form()
            {
                Width = 450,
                Height = 420,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Process Return / Refund Request",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblTrans = new Label() { Left = 25, Top = 20, Text = "Transaction ID:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            TextBox txtTransId = new TextBox() { Left = 25, Top = 45, Width = 380, Font = new Font("Segoe UI", 10) };

            Label lblCust = new Label() { Left = 25, Top = 80, Text = "Customer Name / Info:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            TextBox txtCust = new TextBox() { Left = 25, Top = 105, Width = 380, Font = new Font("Segoe UI", 10) };

            Label lblProd = new Label() { Left = 25, Top = 140, Text = "Purchased Products / Items:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            TextBox txtProd = new TextBox() { Left = 25, Top = 165, Width = 380, Font = new Font("Segoe UI", 10) };

            Label lblReason = new Label() { Left = 25, Top = 200, Text = "Reason for Return:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            ComboBox cmbReason = new ComboBox() { Left = 25, Top = 225, Width = 380, Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReason.Items.AddRange(new string[] { "Wrong Size / Color", "Defective / Damaged Item", "Change of Mind", "Wrong Item Delivered", "Others" });
            cmbReason.SelectedIndex = 0;

            Label lblQty = new Label() { Left = 25, Top = 260, Text = "Quantity to Return:", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            NumericUpDown numQty = new NumericUpDown() { Left = 25, Top = 285, Width = 150, Font = new Font("Segoe UI", 10), Minimum = 1, Maximum = 100, Value = 1 };

            Label lblRefund = new Label() { Left = 195, Top = 260, Text = "Refund Amount (₱):", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true };
            TextBox txtRefundAmt = new TextBox() { Left = 195, Top = 285, Width = 210, Font = new Font("Segoe UI", 10), Text = "0.00" };

            Button btnSubmit = new Button() { Text = "Submit for Approval", Left = 255, Top = 330, Width = 150, Height = 35, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            returnForm.Controls.Add(lblTrans);
            returnForm.Controls.Add(txtTransId);
            returnForm.Controls.Add(lblCust);
            returnForm.Controls.Add(txtCust);
            returnForm.Controls.Add(lblProd);
            returnForm.Controls.Add(txtProd);
            returnForm.Controls.Add(lblReason);
            returnForm.Controls.Add(cmbReason);
            returnForm.Controls.Add(lblQty);
            returnForm.Controls.Add(numQty);
            returnForm.Controls.Add(lblRefund);
            returnForm.Controls.Add(txtRefundAmt);
            returnForm.Controls.Add(btnSubmit);
            returnForm.AcceptButton = btnSubmit;

            if (returnForm.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtTransId.Text) || string.IsNullOrWhiteSpace(txtProd.Text))
                {
                    MessageBox.Show("Please fill in the required fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string returnId = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string purchaseDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        string query = "INSERT INTO Returns (ReturnID, TransID, Customer, Products, PurchaseDate, Reason, Quantity, RefundAmount, Status, Cashier) VALUES (@returnId, @transId, @cust, @prod, @date, @reason, @qty, @refund, @status, @cashier)";
                        using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@returnId", returnId);
                            cmd.Parameters.AddWithValue("@transId", txtTransId.Text.Trim());
                            cmd.Parameters.AddWithValue("@cust", string.IsNullOrWhiteSpace(txtCust.Text) ? "Walk-in Customer" : txtCust.Text.Trim());
                            cmd.Parameters.AddWithValue("@prod", txtProd.Text.Trim());
                            cmd.Parameters.AddWithValue("@date", purchaseDate);
                            cmd.Parameters.AddWithValue("@reason", cmbReason.SelectedItem.ToString());
                            cmd.Parameters.AddWithValue("@qty", (int)numQty.Value);
                            cmd.Parameters.AddWithValue("@refund", txtRefundAmt.Text.Trim());
                            cmd.Parameters.AddWithValue("@status", "Pending Approval");
                            cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Successfully submitted return/refund request to Admin Portal for approval!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadReturnsData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving return request: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetupSalesSummaryContent()
        {
            tabSalesSummary.BackColor = Color.FromArgb(245, 247, 250);

            Label lblTitle = new Label();
            lblTitle.Text = "Comprehensive Sales Summary Report";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.Location = new Point(30, 15);
            lblTitle.AutoSize = true;
            tabSalesSummary.Controls.Add(lblTitle);

            Panel pnlFilter = new Panel();
            pnlFilter.Location = new Point(30, 50);
            pnlFilter.Size = new System.Drawing.Size(975, 50);
            pnlFilter.BackColor = Color.White;
            pnlFilter.BorderStyle = BorderStyle.FixedSingle;
            tabSalesSummary.Controls.Add(pnlFilter);

            Label lblFilterLabel = new Label();
            lblFilterLabel.Text = "Filter By:";
            lblFilterLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblFilterLabel.Location = new Point(15, 14);
            lblFilterLabel.AutoSize = true;
            pnlFilter.Controls.Add(lblFilterLabel);

            cmbSummaryFilterMode = new ComboBox();
            cmbSummaryFilterMode.Location = new Point(80, 11);
            cmbSummaryFilterMode.Size = new System.Drawing.Size(120, 25);
            cmbSummaryFilterMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSummaryFilterMode.Items.AddRange(new string[] { "Daily", "Weekly", "Monthly" });
            cmbSummaryFilterMode.SelectedIndex = 0;
            cmbSummaryFilterMode.SelectedIndexChanged += (s, e) => ToggleSummaryFilterControls();
            pnlFilter.Controls.Add(cmbSummaryFilterMode);

            dtpSalesDate = new DateTimePicker();
            dtpSalesDate.Location = new Point(215, 11);
            dtpSalesDate.Size = new System.Drawing.Size(150, 25);
            dtpSalesDate.Format = DateTimePickerFormat.Short;
            dtpSalesDate.ValueChanged += (s, e) => LoadSalesSummaryData();
            pnlFilter.Controls.Add(dtpSalesDate);

            dtpSalesWeek = new DateTimePicker();
            dtpSalesWeek.Location = new Point(215, 11);
            dtpSalesWeek.Size = new System.Drawing.Size(150, 25);
            dtpSalesWeek.Format = DateTimePickerFormat.Short;
            dtpSalesWeek.Visible = false;
            dtpSalesWeek.ValueChanged += (s, e) => LoadSalesSummaryData();
            pnlFilter.Controls.Add(dtpSalesWeek);

            cmbSalesMonth = new ComboBox();
            cmbSalesMonth.Location = new Point(215, 11);
            cmbSalesMonth.Size = new System.Drawing.Size(110, 25);
            cmbSalesMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSalesMonth.Items.AddRange(new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
            cmbSalesMonth.SelectedIndex = DateTime.Now.Month - 1;
            cmbSalesMonth.Visible = false;
            cmbSalesMonth.SelectedIndexChanged += (s, e) => LoadSalesSummaryData();
            pnlFilter.Controls.Add(cmbSalesMonth);

            cmbSalesYear = new ComboBox();
            cmbSalesYear.Location = new Point(335, 11);
            cmbSalesYear.Size = new System.Drawing.Size(80, 25);
            cmbSalesYear.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int y = 2024; y <= 2030; y++) cmbSalesYear.Items.Add(y.ToString());
            cmbSalesYear.SelectedItem = DateTime.Now.Year.ToString();
            cmbSalesYear.Visible = false;
            cmbSalesYear.SelectedIndexChanged += (s, e) => LoadSalesSummaryData();
            pnlFilter.Controls.Add(cmbSalesYear);

            btnPrintSalesSummary = new Button();
            btnPrintSalesSummary.Text = "🖨️ Print Summary";
            btnPrintSalesSummary.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnPrintSalesSummary.BackColor = Color.FromArgb(40, 167, 69);
            btnPrintSalesSummary.ForeColor = Color.White;
            btnPrintSalesSummary.FlatStyle = FlatStyle.Flat;
            btnPrintSalesSummary.FlatAppearance.BorderSize = 0;
            btnPrintSalesSummary.Location = new Point(830, 8);
            btnPrintSalesSummary.Size = new System.Drawing.Size(130, 32);
            btnPrintSalesSummary.Cursor = Cursors.Hand;
            btnPrintSalesSummary.Click += BtnPrintSalesSummary_Click;
            pnlFilter.Controls.Add(btnPrintSalesSummary);

            int cWidth = 230;
            int cHeight = 75;

            Panel pnlCard1 = CreateCard("Today's Sales", out lblSumTodaySales, 30, 110, cWidth, cHeight, Color.FromArgb(40, 167, 69));
            Panel pnlCard2 = CreateCard("Weekly Sales", out lblSumWeeklySales, 275, 110, cWidth, cHeight, Color.FromArgb(23, 162, 184));
            Panel pnlCard3 = CreateCard("Monthly Sales", out lblSumMonthlySales, 520, 110, cWidth, cHeight, Color.FromArgb(0, 123, 255));
            Panel pnlCard4 = CreateCard("Total Transactions", out lblSumNumTrans, 765, 110, cWidth, cHeight, Color.FromArgb(108, 117, 125));

            Panel pnlCard5 = CreateCard("Total Items Sold", out lblSumTotalItemsSold, 30, 195, cWidth, cHeight, Color.FromArgb(111, 66, 193));
            Panel pnlCard6 = CreateCard("Cash Sales", out lblSumCashSales, 275, 195, cWidth, cHeight, Color.FromArgb(255, 140, 0));
            Panel pnlCard7 = CreateCard("Other Payment Methods", out lblSumOtherSales, 520, 195, cWidth, cHeight, Color.FromArgb(220, 53, 69));

            tabSalesSummary.Controls.Add(pnlCard1);
            tabSalesSummary.Controls.Add(pnlCard2);
            tabSalesSummary.Controls.Add(pnlCard3);
            tabSalesSummary.Controls.Add(pnlCard4);
            tabSalesSummary.Controls.Add(pnlCard5);
            tabSalesSummary.Controls.Add(pnlCard6);
            tabSalesSummary.Controls.Add(pnlCard7);

            Label lblHist = new Label();
            lblHist.Text = "Sales History / Breakdown";
            lblHist.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblHist.Location = new Point(30, 280);
            lblHist.AutoSize = true;
            tabSalesSummary.Controls.Add(lblHist);

            dgvSalesSummary = new DataGridView();
            dgvSalesSummary.Location = new Point(30, 310);
            dgvSalesSummary.Size = new System.Drawing.Size(975, 335);
            dgvSalesSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSalesSummary.ReadOnly = true;
            dgvSalesSummary.RowHeadersVisible = false;
            dgvSalesSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabSalesSummary.Controls.Add(dgvSalesSummary);
        }

        private void ToggleSummaryFilterControls()
        {
            string mode = cmbSummaryFilterMode.SelectedItem?.ToString() ?? "Daily";
            dtpSalesDate.Visible = (mode == "Daily");
            dtpSalesWeek.Visible = (mode == "Weekly");
            cmbSalesMonth.Visible = (mode == "Monthly");
            cmbSalesYear.Visible = (mode == "Monthly");
            LoadSalesSummaryData();
        }

        private void LoadSalesSummaryData()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string filterMode = cmbSummaryFilterMode.SelectedItem?.ToString() ?? "Daily";
                    string dateCondition = "";

                    if (filterMode == "Daily")
                    {
                        string selectedDate = dtpSalesDate.Value.ToString("yyyy-MM-dd");
                        dateCondition = $" AND (date(TransDateTime) = '{selectedDate}' OR TransDateTime LIKE '%{selectedDate}%')";
                    }
                    else if (filterMode == "Weekly")
                    {
                        DateTime startWeek = dtpSalesWeek.Value.Date;
                        DateTime endWeek = startWeek.AddDays(7);
                        dateCondition = $" AND date(TransDateTime) >= '{startWeek:yyyy-MM-dd}' AND date(TransDateTime) < '{endWeek:yyyy-MM-dd}'";
                    }
                    else if (filterMode == "Monthly")
                    {
                        int monthNum = cmbSalesMonth.SelectedIndex + 1;
                        string yearStr = cmbSalesYear.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString();
                        string monthPrefix = $"{yearStr}-{monthNum:D2}";
                        dateCondition = $" AND TransDateTime LIKE '{monthPrefix}%'";
                    }

                    string qToday = $"SELECT COALESCE(SUM(CAST(TransTotal AS REAL)), 0) FROM Transactions WHERE TransStaff = @cashier{dateCondition}";
                    using (SQLiteCommand cmd = new SQLiteCommand(qToday, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        if (lblSumTodaySales != null) lblSumTodaySales.Text = "₱" + Convert.ToDecimal(cmd.ExecuteScalar() ?? 0).ToString("N2");
                    }

                    string qWeek = "SELECT COALESCE(SUM(CAST(TransTotal AS REAL)), 0) FROM Transactions WHERE TransStaff = @cashier";
                    using (SQLiteCommand cmd = new SQLiteCommand(qWeek, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        if (lblSumWeeklySales != null) lblSumWeeklySales.Text = "₱" + Convert.ToDecimal(cmd.ExecuteScalar() ?? 0).ToString("N2");
                    }

                    string qMonth = $"SELECT COALESCE(SUM(CAST(TransTotal AS REAL)), 0) FROM Transactions WHERE TransStaff = @cashier AND TransDateTime LIKE '%{DateTime.Now:yyyy-MM}%'";
                    using (SQLiteCommand cmd = new SQLiteCommand(qMonth, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        if (lblSumMonthlySales != null) lblSumMonthlySales.Text = "₱" + Convert.ToDecimal(cmd.ExecuteScalar() ?? 0).ToString("N2");
                    }

                    string qCount = $"SELECT COUNT(*) FROM Transactions WHERE TransStaff = @cashier{dateCondition}";
                    using (SQLiteCommand cmd = new SQLiteCommand(qCount, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        if (lblSumNumTrans != null) lblSumNumTrans.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    string qItems = $"SELECT TransItems FROM Transactions WHERE TransStaff = @cashier{dateCondition}";
                    int totalItems = 0;
                    using (SQLiteCommand cmd = new SQLiteCommand(qItems, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string itemsStr = rdr["TransItems"]?.ToString() ?? "";
                                foreach (string part in itemsStr.Split(','))
                                {
                                    if (part.Contains("(x"))
                                    {
                                        try
                                        {
                                            int startIdx = part.IndexOf("(x") + 2;
                                            int endIdx = part.IndexOf(")", startIdx);
                                            if (endIdx > startIdx)
                                            {
                                                int qty = 0;
                                                if (int.TryParse(part.Substring(startIdx, endIdx - startIdx), out qty))
                                                {
                                                    totalItems += qty;
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                    if (lblSumTotalItemsSold != null) lblSumTotalItemsSold.Text = totalItems.ToString();

                    string qCash = $"SELECT COALESCE(SUM(CAST(TransTotal AS REAL)), 0) FROM Transactions WHERE TransStaff = @cashier AND TransMethod = 'Cash'{dateCondition}";
                    using (SQLiteCommand cmd = new SQLiteCommand(qCash, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        if (lblSumCashSales != null) lblSumCashSales.Text = "₱" + Convert.ToDecimal(cmd.ExecuteScalar() ?? 0).ToString("N2");
                    }

                    string qOther = $"SELECT COALESCE(SUM(CAST(TransTotal AS REAL)), 0) FROM Transactions WHERE TransStaff = @cashier AND TransMethod != 'Cash'{dateCondition}";
                    using (SQLiteCommand cmd = new SQLiteCommand(qOther, conn))
                    {
                        cmd.Parameters.AddWithValue("@cashier", loggedInCashier);
                        if (lblSumOtherSales != null) lblSumOtherSales.Text = "₱" + Convert.ToDecimal(cmd.ExecuteScalar() ?? 0).ToString("N2");
                    }

                    string qTable = $"SELECT TransID AS 'Transaction ID', TransDateTime AS 'Date & Time', TransCustomer AS 'Customer', TransItems AS 'Items Sold', TransTotal AS 'Total Amount', TransMethod AS 'Payment Method' FROM Transactions WHERE TransStaff = @cashier{dateCondition} ORDER BY TransDateTime DESC";
                    using (SQLiteCommand cmdTable = new SQLiteCommand(qTable, conn))
                    {
                        cmdTable.Parameters.AddWithValue("@cashier", loggedInCashier);
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmdTable))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            string currSymbol = GetCurrencySymbol();
                            foreach (DataRow row in dt.Rows)
                            {
                                if (row["Total Amount"] != DBNull.Value && decimal.TryParse(row["Total Amount"].ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                                {
                                    row["Total Amount"] = $"{currSymbol}{amt:N2}";
                                }
                            }

                            if (dgvSalesSummary != null) dgvSalesSummary.DataSource = dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sales summary: " + ex.Message);
            }
        }

        private void BtnPrintSalesSummary_Click(object sender, EventArgs e)
        {
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) => {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine("         SALES SUMMARY REPORT           ");
                sb.AppendLine("========================================");
                sb.AppendLine($"Cashier: {loggedInCashier}");
                sb.AppendLine($"Filter Mode: {cmbSummaryFilterMode.SelectedItem}");
                sb.AppendLine($"Generated: {DateTime.Now}");
                sb.AppendLine("----------------------------------------");
                sb.AppendLine($"Today's Sales / Filtered: {lblSumTodaySales?.Text}");
                sb.AppendLine($"Total Transactions: {lblSumNumTrans?.Text}");
                sb.AppendLine($"Total Items Sold: {lblSumTotalItemsSold?.Text}");
                sb.AppendLine($"Cash Sales: {lblSumCashSales?.Text}");
                sb.AppendLine($"Other Methods: {lblSumOtherSales?.Text}");
                sb.AppendLine("========================================");

                Font font = new Font("Courier New", 9);
                ev.Graphics.DrawString(sb.ToString(), font, Brushes.Black, new PointF(10, 10));
            };

            PrintDialog printDlg = new PrintDialog();
            printDlg.Document = printDoc;
            if (printDlg.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
        }

        private void SetupMyAccountContent()
        {
            tabMyAccount.BackColor = Color.FromArgb(245, 247, 250);

            Label lblTitle = new Label();
            lblTitle.Text = "Settings / Account Profile";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.Location = new Point(30, 20);
            lblTitle.AutoSize = true;
            tabMyAccount.Controls.Add(lblTitle);

            Panel pnlAccount = new Panel();
            pnlAccount.Location = new Point(30, 70);
            pnlAccount.Size = new System.Drawing.Size(750, 420);
            pnlAccount.BackColor = Color.White;
            pnlAccount.BorderStyle = BorderStyle.FixedSingle;
            tabMyAccount.Controls.Add(pnlAccount);

            picAccProfile = new PictureBox();
            picAccProfile.Location = new Point(30, 30);
            picAccProfile.Size = new System.Drawing.Size(120, 120);
            picAccProfile.SizeMode = PictureBoxSizeMode.StretchImage;
            picAccProfile.BackColor = Color.LightGray;
            pnlAccount.Controls.Add(picAccProfile);

            btnBrowsePic = new Button();
            btnBrowsePic.Text = "Browse Photo";
            btnBrowsePic.Location = new Point(30, 160);
            btnBrowsePic.Size = new System.Drawing.Size(120, 30);
            btnBrowsePic.BackColor = Color.FromArgb(108, 117, 125);
            btnBrowsePic.ForeColor = Color.White;
            btnBrowsePic.FlatStyle = FlatStyle.Flat;
            btnBrowsePic.Cursor = Cursors.Hand;
            btnBrowsePic.Click += BtnBrowsePic_Click;
            pnlAccount.Controls.Add(btnBrowsePic);

            int leftX = 180;

            Label lblUNameText = new Label() { Text = "Username:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(leftX, 30), AutoSize = true };
            pnlAccount.Controls.Add(lblUNameText);

            txtAccUsername = new TextBox() { Location = new Point(leftX + 130, 28), Size = new System.Drawing.Size(380, 25), ReadOnly = true, Font = new Font("Segoe UI", 10) };
            pnlAccount.Controls.Add(txtAccUsername);

            Label lblFNameText = new Label() { Text = "Name (Full Name):", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(leftX, 75), AutoSize = true };
            pnlAccount.Controls.Add(lblFNameText);

            txtAccFullName = new TextBox() { Location = new Point(leftX + 130, 73), Size = new System.Drawing.Size(380, 25), Font = new Font("Segoe UI", 10) };
            pnlAccount.Controls.Add(txtAccFullName);

            Label lblEmailText = new Label() { Text = "Email:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(leftX, 120), AutoSize = true };
            pnlAccount.Controls.Add(lblEmailText);

            txtAccEmail = new TextBox() { Location = new Point(leftX + 130, 118), Size = new System.Drawing.Size(380, 25), Font = new Font("Segoe UI", 10) };
            pnlAccount.Controls.Add(txtAccEmail);

            Label lblContactText = new Label() { Text = "Contact Number:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(leftX, 165), AutoSize = true };
            pnlAccount.Controls.Add(lblContactText);

            txtAccContact = new TextBox() { Location = new Point(leftX + 130, 163), Size = new System.Drawing.Size(380, 25), Font = new Font("Segoe UI", 10) };
            pnlAccount.Controls.Add(txtAccContact);

            Label lblRoleText = new Label() { Text = "Role / Position:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(leftX, 210), AutoSize = true };
            pnlAccount.Controls.Add(lblRoleText);

            txtAccRole = new TextBox() { Location = new Point(leftX + 130, 208), Size = new System.Drawing.Size(380, 25), ReadOnly = true, Font = new Font("Segoe UI", 10) };
            pnlAccount.Controls.Add(txtAccRole);

            Label lblPassText = new Label() { Text = "New Password:", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(leftX, 255), AutoSize = true };
            pnlAccount.Controls.Add(lblPassText);

            txtAccNewPassword = new TextBox() { Location = new Point(leftX + 130, 253), Size = new System.Drawing.Size(380, 25), UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10) };
            pnlAccount.Controls.Add(txtAccNewPassword);

            btnSaveAccount = new Button() { Text = "Update Settings", Location = new Point(leftX + 130, 300), Size = new System.Drawing.Size(180, 40), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSaveAccount.FlatAppearance.BorderSize = 0;
            btnSaveAccount.Click += BtnSaveAccount_Click;
            pnlAccount.Controls.Add(btnSaveAccount);
        }

        private void BtnBrowsePic_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        picAccProfile.Image = Image.FromFile(ofd.FileName);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            picAccProfile.Image.Save(ms, picAccProfile.Image.RawFormat);
                            currentProfilePicBytes = ms.ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadAccountData()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT StaffUsername, StaffName, StaffEmail, StaffContact, StaffRole, StaffProfilePic FROM Staff WHERE StaffUsername = @username OR StaffName = @username";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", loggedInCashier);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (txtAccUsername != null) txtAccUsername.Text = reader["StaffUsername"]?.ToString() ?? "";
                                if (txtAccFullName != null) txtAccFullName.Text = reader["StaffName"]?.ToString() ?? "";
                                if (txtAccEmail != null) txtAccEmail.Text = reader["StaffEmail"]?.ToString() ?? "";
                                if (txtAccContact != null) txtAccContact.Text = reader["StaffContact"]?.ToString() ?? "";
                                if (txtAccRole != null) txtAccRole.Text = reader["StaffRole"]?.ToString() ?? "Cashier";

                                try
                                {
                                    if (reader["StaffProfilePic"] != DBNull.Value)
                                    {
                                        currentProfilePicBytes = (byte[])reader["StaffProfilePic"];
                                        using (MemoryStream ms = new MemoryStream(currentProfilePicBytes))
                                        {
                                            picAccProfile.Image = Image.FromStream(ms);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                { }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading account data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void BtnSaveAccount_Click(object sender, EventArgs e)
        {
            try
            {
                string newName = txtAccFullName.Text.Trim();
                string newEmail = txtAccEmail.Text.Trim();
                string newContact = txtAccContact.Text.Trim();
                string newPass = txtAccNewPassword.Text.Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Field cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "";
                    bool hasPassword = !string.IsNullOrEmpty(newPass);
                    bool hasPic = (currentProfilePicBytes != null);

                    if (hasPassword && hasPic)
                    {
                        query = "UPDATE Staff SET StaffName = @name, StaffEmail = @email, StaffContact = @contact, StaffPassword = @pass, StaffProfilePic = @pic WHERE StaffUsername = @username OR StaffName = @username";
                    }
                    else if (hasPassword && !hasPic)
                    {
                        query = "UPDATE Staff SET StaffName = @name, StaffEmail = @email, StaffContact = @contact, StaffPassword = @pass WHERE StaffUsername = @username OR StaffName = @username";
                    }
                    else if (!hasPassword && hasPic)
                    {
                        query = "UPDATE Staff SET StaffName = @name, StaffEmail = @email, StaffContact = @contact, StaffProfilePic = @pic WHERE StaffUsername = @username OR StaffName = @username";
                    }
                    else
                    {
                        query = "UPDATE Staff SET StaffName = @name, StaffEmail = @email, StaffContact = @contact WHERE StaffUsername = @username OR StaffName = @username";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", newName);
                        cmd.Parameters.AddWithValue("@email", newEmail);
                        cmd.Parameters.AddWithValue("@contact", newContact);
                        if (hasPassword)
                        {
                            cmd.Parameters.AddWithValue("@pass", newPass);
                        }
                        if (hasPic)
                        {
                            cmd.Parameters.AddWithValue("@pic", currentProfilePicBytes);
                        }
                        cmd.Parameters.AddWithValue("@username", loggedInCashier);
                        cmd.ExecuteNonQuery();
                    }
                }

                loggedInCashier = newName;
                MessageBox.Show("Successfully updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtAccNewPassword.Text = "";
                LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}