using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace PTOF
{
    public partial class CustomerPortalForm : Form
    
    {
        private Panel sidePanel;
        private TabControl customerTabControl;

        // Navigation Buttons
        private Button btnDashboard;
        private Button btnShop;
        private Button btnCart;
        private Button btnMyOrders;
        private Button btnWishlist;
        private Button btnMyProfile;
        private Button btnSettings;
        private Button btnLogout;

        // Tabs
        private TabPage tabDashboard;
        private TabPage tabShop;
        private TabPage tabCart;
        private TabPage tabMyOrders;
        private TabPage tabWishlist;
        private TabPage tabMyProfile;
        private TabPage tabSettings;

        // Shop Controls
        private FlowLayoutPanel flowShopProducts;
        private TextBox txtSearchShop;
        private ComboBox cmbCategoryFilter;
        private Label lblShopTitle;
        private Button btnSearchShop;

        // Cart Controls
        private FlowLayoutPanel flowCartItems;
        private Label lblCartTitle;
        private Label lblCartTotalAmount;
        private Label lblCartTotalLabel;
        private Label lblCartPaymentLabel;
        private Button btnCartCheckout;
        private ComboBox cmbCartPaymentMethod;

        // My Orders Controls
        private FlowLayoutPanel flowMyOrdersList;
        private Label lblMyOrdersTitle;

        // Wishlist Controls
        private FlowLayoutPanel flowWishlistProducts;
        private Label lblWishlistTitle;

        // Profile Controls
        private PictureBox picCustomerProfile;
        private TextBox txtProfileName;
        private TextBox txtProfileEmail;
        private TextBox txtProfileContact;
        private TextBox txtProfileAddress;
        private Button btnEditProfile;
        private Button btnSaveProfile;
        private Button btnUploadPic;
        private Button btnChangePassword;
        private Label lblProfileTitle;
        private Label lblNameLabel;
        private Label lblEmailLabel;
        private Label lblContactLabel;
        private Label lblAddressLabel;
        private byte[] currentProfileImageBytes = null;
        private bool isEditingProfile = false;

        // Settings Controls & Variables
        private Label lblSettingsTitle;
        private ComboBox cmbLanguageSetting;
        private ComboBox cmbThemeSetting;
        private ComboBox cmbCurrencySetting;
        private CheckBox chkNotifOrder;
        private CheckBox chkNotifPromo;
        private CheckBox chkNotifSMS;
        private CheckBox chkPrivacyProfile;
        private CheckBox chkPrivacySharing;
        private Button btnSaveSettings;
        private Button btnResetSettings;

        private string currentLanguage = "English";
        private string currentCurrency = "Peso";
        private string currencySymbol = "₱";
        private decimal currencyMultiplier = 1.0m;

        // Data structure for Cart Item
        public class CartItem
        {
            public string ProductName { get; set; }
            public string Size { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public byte[] ImageBytes { get; set; }
            public CheckBox ChkSelect { get; set; }
        }

        public class WishlistItem
        {
            public string ProductName { get; set; }
            public string Category { get; set; }
            public Dictionary<string, ProductVariant> Variants { get; set; }
            public byte[] ImageBytes { get; set; }
        }

        private List<CartItem> customerCart = new List<CartItem>();
        private List<WishlistItem> customerWishlist = new List<WishlistItem>();

        private Color activeColor = Color.FromArgb(50, 50, 50);
        private Color hoverColor = Color.FromArgb(70, 70, 70);
        private string connectionString = "Data Source=store.db;Version=3;";

        public CustomerPortalForm()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            InitializeCustomerTables();
            LoadCartFromDatabase();
            LoadWishlistFromDatabase();

            SetupCustomerPortalLayout();
            LoadShopProducts();
            RefreshCartDisplay();
            RefreshWishlistDisplay();
        }

        private void CustomerPortalForm_Load(object sender, EventArgs e)
        {
            LoadShopProducts();
            LoadCustomerProfileData();
        }

        private void InitializeCustomerTables()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string cartTableQuery = @"CREATE TABLE IF NOT EXISTS CustomerCart (
                                                Username TEXT,
                                                ProductName TEXT,
                                                Size TEXT,
                                                Price DECIMAL,
                                                Quantity INTEGER,
                                                PRIMARY KEY(Username, ProductName, Size)
                                            );";
                    using (SQLiteCommand cmd = new SQLiteCommand(cartTableQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    string wishlistTableQuery = @"CREATE TABLE IF NOT EXISTS CustomerWishlist (
                                                    Username TEXT,
                                                    ProductName TEXT,
                                                    Category TEXT,
                                                    PRIMARY KEY(Username, ProductName)
                                                );";
                    using (SQLiteCommand cmd2 = new SQLiteCommand(wishlistTableQuery, conn))
                    {
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error initializing cart/wishlist tables: " + ex.Message);
            }
        }

        private void LoadCartFromDatabase()
        {
            customerCart.Clear();
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM CustomerCart WHERE Username = @username";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string prodName = reader["ProductName"].ToString();
                                string size = reader["Size"].ToString();
                                decimal price = Convert.ToDecimal(reader["Price"]);
                                int qty = Convert.ToInt32(reader["Quantity"]);
                                byte[] imgBytes = GetProductImageByNameStatic(prodName, conn);

                                customerCart.Add(new CartItem
                                {
                                    ProductName = prodName,
                                    Size = size,
                                    Price = price,
                                    Quantity = qty,
                                    ImageBytes = imgBytes
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading cart: " + ex.Message);
            }
        }

        private void SaveCartToDatabase()
        {
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string deleteQuery = "DELETE FROM CustomerCart WHERE Username = @username";
                    using (SQLiteCommand delCmd = new SQLiteCommand(deleteQuery, conn))
                    {
                        delCmd.Parameters.AddWithValue("@username", username);
                        delCmd.ExecuteNonQuery();
                    }

                    foreach (var item in customerCart)
                    {
                        string insertQuery = "INSERT OR REPLACE INTO CustomerCart (Username, ProductName, Size, Price, Quantity) VALUES (@username, @prodName, @size, @price, @qty)";
                        using (SQLiteCommand insCmd = new SQLiteCommand(insertQuery, conn))
                        {
                            insCmd.Parameters.AddWithValue("@username", username);
                            insCmd.Parameters.AddWithValue("@prodName", item.ProductName);
                            insCmd.Parameters.AddWithValue("@size", item.Size);
                            insCmd.Parameters.AddWithValue("@price", item.Price);
                            insCmd.Parameters.AddWithValue("@qty", item.Quantity);
                            insCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving cart: " + ex.Message);
            }
        }

        private void LoadWishlistFromDatabase()
        {
            customerWishlist.Clear();
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM CustomerWishlist WHERE Username = @username";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string prodName = reader["ProductName"].ToString();
                                string category = reader["Category"].ToString();
                                byte[] imgBytes = GetProductImageByNameStatic(prodName, conn);
                                var variants = GetProductVariantsByName(prodName, conn);

                                customerWishlist.Add(new WishlistItem
                                {
                                    ProductName = prodName,
                                    Category = category,
                                    Variants = variants,
                                    ImageBytes = imgBytes
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading wishlist: " + ex.Message);
            }
        }

        private void SaveWishlistToDatabase()
        {
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string deleteQuery = "DELETE FROM CustomerWishlist WHERE Username = @username";
                    using (SQLiteCommand delCmd = new SQLiteCommand(deleteQuery, conn))
                    {
                        delCmd.Parameters.AddWithValue("@username", username);
                        delCmd.ExecuteNonQuery();
                    }

                    foreach (var item in customerWishlist)
                    {
                        string insertQuery = "INSERT OR REPLACE INTO CustomerWishlist (Username, ProductName, Category) VALUES (@username, @prodName, @category)";
                        using (SQLiteCommand insCmd = new SQLiteCommand(insertQuery, conn))
                        {
                            insCmd.Parameters.AddWithValue("@username", username);
                            insCmd.Parameters.AddWithValue("@prodName", item.ProductName);
                            insCmd.Parameters.AddWithValue("@category", item.Category);
                            insCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving wishlist: " + ex.Message);
            }
        }

        private byte[] GetProductImageByNameStatic(string prodName, SQLiteConnection conn)
        {
            try
            {
                string query = "SELECT ProdImage FROM Products WHERE ProdName = @prodName LIMIT 1";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@prodName", prodName);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return (byte[])result;
                    }
                }
            }
            catch { }
            return null;
        }

        private Dictionary<string, ProductVariant> GetProductVariantsByName(string prodName, SQLiteConnection conn)
        {
            var variants = new Dictionary<string, ProductVariant>();
            try
            {
                string query = "SELECT Size, Price, Stock FROM Products WHERE ProdName = @prodName";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@prodName", prodName);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string size = reader["Size"] != DBNull.Value ? reader["Size"].ToString().ToUpper() : "S";
                            decimal price = 0;
                            if (reader["Price"] != DBNull.Value)
                            {
                                decimal.TryParse(reader["Price"].ToString().Replace("₱", "").Replace("$", "").Replace("€", "").Trim(), out price);
                            }
                            int stock = 0;
                            if (reader["Stock"] != DBNull.Value)
                            {
                                int.TryParse(reader["Stock"].ToString(), out stock);
                            }
                            variants[size] = new ProductVariant { Price = price, Stock = stock };
                        }
                    }
                }
            }
            catch { }
            return variants;
        }

        private void DeductStockFromItems(string ordItems, SQLiteConnection conn)
        {
            if (string.IsNullOrEmpty(ordItems)) return;
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
                    if (!int.TryParse(trimmedItem.Substring(lastIndex + 3).Trim(), out qty)) qty = 1;

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
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void RestoreStockFromItems(string ordItems, SQLiteConnection conn)
        {
            if (string.IsNullOrEmpty(ordItems)) return;
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
                    if (!int.TryParse(trimmedItem.Substring(lastIndex + 3).Trim(), out qty)) qty = 1;

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
                        updateStockQuery = "UPDATE Products SET Stock = CAST(Stock AS INTEGER) + @qty WHERE ProdName = @prodName AND Size = @size";
                        cmd.Parameters.AddWithValue("@size", size);
                    }
                    else
                    {
                        updateStockQuery = "UPDATE Products SET Stock = CAST(Stock AS INTEGER) + @qty WHERE ProdName = @prodName";
                    }

                    cmd.CommandText = updateStockQuery;
                    cmd.Parameters.AddWithValue("@qty", qty);
                    cmd.Parameters.AddWithValue("@prodName", prodName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SetupCustomerPortalLayout()
        {
            this.Size = new System.Drawing.Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Customer Portal";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            sidePanel = new BufferedPanel();
            sidePanel.Dock = DockStyle.Left;
            sidePanel.Width = 230;
            sidePanel.BackColor = Color.FromArgb(30, 30, 30);
            this.Controls.Add(sidePanel);

            customerTabControl = new TabControl();
            customerTabControl.Location = new Point(230, 0);
            customerTabControl.Size = new System.Drawing.Size(1035, 685);
            customerTabControl.Appearance = TabAppearance.FlatButtons;
            customerTabControl.ItemSize = new System.Drawing.Size(0, 1);
            customerTabControl.SizeMode = TabSizeMode.Fixed;
            this.Controls.Add(customerTabControl);

            tabDashboard = new TabPage();
            tabShop = new TabPage();
            tabCart = new TabPage();
            tabMyOrders = new TabPage();
            tabWishlist = new TabPage();
            tabMyProfile = new TabPage();
            tabSettings = new TabPage();

            customerTabControl.TabPages.Add(tabDashboard);
            customerTabControl.TabPages.Add(tabShop);
            customerTabControl.TabPages.Add(tabCart);
            customerTabControl.TabPages.Add(tabMyOrders);
            customerTabControl.TabPages.Add(tabWishlist);
            customerTabControl.TabPages.Add(tabMyProfile);
            customerTabControl.TabPages.Add(tabSettings);

            int startY = 20;
            int btnHeight = 45;

            btnDashboard = CreateNavButton("Dashboard", startY);
            btnShop = CreateNavButton("Shop", startY + btnHeight);
            btnCart = CreateNavButton("Cart", startY + (btnHeight * 2));
            btnMyOrders = CreateNavButton("My Orders", startY + (btnHeight * 3));
            btnWishlist = CreateNavButton("Wishlist", startY + (btnHeight * 4));
            btnMyProfile = CreateNavButton("My Profile", startY + (btnHeight * 5));
            btnSettings = CreateNavButton("Settings", startY + (btnHeight * 6));

            btnLogout = CreateNavButton("Logout", 420);
            btnLogout.ForeColor = Color.IndianRed;

            btnDashboard.Click += (s, e) => {
                customerTabControl.SelectedTab = tabDashboard;
                HighlightButton(btnDashboard);
                LoadDashboardProfilePicture();
            };
            btnShop.Click += (s, e) => { customerTabControl.SelectedTab = tabShop; HighlightButton(btnShop); LoadShopProducts(); };
            btnCart.Click += (s, e) => { customerTabControl.SelectedTab = tabCart; HighlightButton(btnCart); RefreshCartDisplay(); };
            btnMyOrders.Click += (s, e) => { customerTabControl.SelectedTab = tabMyOrders; HighlightButton(btnMyOrders); LoadCustomerOrders(); };
            btnWishlist.Click += (s, e) => { customerTabControl.SelectedTab = tabWishlist; HighlightButton(btnWishlist); RefreshWishlistDisplay(); };
            btnMyProfile.Click += (s, e) => { customerTabControl.SelectedTab = tabMyProfile; HighlightButton(btnMyProfile); LoadCustomerProfileData(); };
            btnSettings.Click += (s, e) => { customerTabControl.SelectedTab = tabSettings; HighlightButton(btnSettings); };

            btnLogout.Click += (s, e) => {
                DialogResult dialogResult = MessageBox.Show(
                    currentLanguage == "Filipino" ? "Sigurado ka bang gusto mong mag-logout?" :
                    currentLanguage == "Cebuano" ? "Sigurado ka ba nga gusto kang mag-logout?" :
                    "Are you sure you want to logout?",
                    "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    this.Close();
                }
            };

            SetupDashboardContent();
            SetupShopContent();
            SetupCartContent();
            SetupMyOrdersContent();
            SetupWishlistContent();
            SetupMyProfileContent();
            SetupSettingsContent();

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

        private class BufferedFlowLayoutPanel : FlowLayoutPanel
        {
            public BufferedFlowLayoutPanel()
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

        private PictureBox picDashProfile;
        private Label lblDashWelcomeName;

        private void SetupDashboardContent()
        {
            tabDashboard.BackColor = Color.FromArgb(245, 247, 250);
            tabDashboard.Controls.Clear();

            Panel pnlHeaderCard = new BufferedPanel();
            pnlHeaderCard.Location = new Point(30, 20);
            pnlHeaderCard.Size = new System.Drawing.Size(975, 110);
            pnlHeaderCard.BackColor = Color.White;
            tabDashboard.Controls.Add(pnlHeaderCard);

            picDashProfile = new PictureBox();
            picDashProfile.Location = new Point(20, 15);
            picDashProfile.Size = new System.Drawing.Size(80, 80);
            picDashProfile.SizeMode = PictureBoxSizeMode.Zoom;
            picDashProfile.BackColor = Color.FromArgb(235, 235, 235);
            pnlHeaderCard.Controls.Add(picDashProfile);

            lblDashWelcomeName = new Label();
            string custName = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Valued Customer" : Form1.CurrentUsername;
            lblDashWelcomeName.Text = $"Welcome to our store, {custName}!";
            lblDashWelcomeName.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblDashWelcomeName.ForeColor = Color.FromArgb(40, 40, 40);
            lblDashWelcomeName.Location = new Point(120, 30);
            lblDashWelcomeName.Size = new System.Drawing.Size(500, 50);
            pnlHeaderCard.Controls.Add(lblDashWelcomeName);

            LoadDashboardProfilePicture();

            int btnX = 640;
            int btnY = 32;

            Button btnQuickShop = new Button();
            btnQuickShop.Text = "View Shop";
            btnQuickShop.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnQuickShop.BackColor = Color.FromArgb(0, 122, 204);
            btnQuickShop.ForeColor = Color.White;
            btnQuickShop.FlatStyle = FlatStyle.Flat;
            btnQuickShop.FlatAppearance.BorderSize = 0;
            btnQuickShop.Location = new Point(btnX, btnY);
            btnQuickShop.Size = new System.Drawing.Size(100, 38);
            btnQuickShop.Cursor = Cursors.Hand;
            btnQuickShop.Click += (s, e) => { customerTabControl.SelectedTab = tabShop; HighlightButton(btnShop); LoadShopProducts(); };
            pnlHeaderCard.Controls.Add(btnQuickShop);

            Button btnQuickCart = new Button();
            btnQuickCart.Text = "View Cart";
            btnQuickCart.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnQuickCart.BackColor = Color.FromArgb(40, 167, 69);
            btnQuickCart.ForeColor = Color.White;
            btnQuickCart.FlatStyle = FlatStyle.Flat;
            btnQuickCart.FlatAppearance.BorderSize = 0;
            btnQuickCart.Location = new Point(btnX + 110, btnY);
            btnQuickCart.Size = new System.Drawing.Size(100, 38);
            btnQuickCart.Cursor = Cursors.Hand;
            btnQuickCart.Click += (s, e) => { customerTabControl.SelectedTab = tabCart; HighlightButton(btnCart); RefreshCartDisplay(); };
            pnlHeaderCard.Controls.Add(btnQuickCart);

            Button btnQuickOrders = new Button();
            btnQuickOrders.Text = "View Orders";
            btnQuickOrders.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnQuickOrders.BackColor = Color.FromArgb(108, 117, 125);
            btnQuickOrders.ForeColor = Color.White;
            btnQuickOrders.FlatStyle = FlatStyle.Flat;
            btnQuickOrders.FlatAppearance.BorderSize = 0;
            btnQuickOrders.Location = new Point(btnX + 220, btnY);
            btnQuickOrders.Size = new System.Drawing.Size(105, 38);
            btnQuickOrders.Cursor = Cursors.Hand;
            btnQuickOrders.Click += (s, e) => { customerTabControl.SelectedTab = tabMyOrders; HighlightButton(btnMyOrders); LoadCustomerOrders(); };
            pnlHeaderCard.Controls.Add(btnQuickOrders);

            Label lblFeaturedTitle = new Label();
            lblFeaturedTitle.Text = "Featured Products";
            lblFeaturedTitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblFeaturedTitle.Location = new Point(30, 145);
            lblFeaturedTitle.AutoSize = true;
            tabDashboard.Controls.Add(lblFeaturedTitle);

            FlowLayoutPanel flowFeatured = new BufferedFlowLayoutPanel();
            flowFeatured.Location = new Point(30, 175);
            flowFeatured.Size = new System.Drawing.Size(975, 230);
            flowFeatured.AutoScroll = true;
            flowFeatured.BackColor = Color.Transparent;
            tabDashboard.Controls.Add(flowFeatured);
            LoadDashboardFeaturedProducts(flowFeatured);

            Label lblRecentTitle = new Label();
            lblRecentTitle.Text = "Recent Transaction Order";
            lblRecentTitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblRecentTitle.Location = new Point(30, 420);
            lblRecentTitle.AutoSize = true;
            tabDashboard.Controls.Add(lblRecentTitle);

            Panel pnlRecentOrder = new BufferedPanel();
            pnlRecentOrder.Location = new Point(30, 450);
            pnlRecentOrder.Size = new System.Drawing.Size(975, 120);
            pnlRecentOrder.BackColor = Color.White;
            tabDashboard.Controls.Add(pnlRecentOrder);
            LoadDashboardRecentOrder(pnlRecentOrder);
        }

        private void LoadDashboardProfilePicture()
        {
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT CustProfilePic FROM Customers WHERE CustName = @username LIMIT 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            byte[] imgBytes = (byte[])result;
                            using (MemoryStream ms = new MemoryStream(imgBytes))
                            {
                                picDashProfile.Image = Image.FromStream(ms);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadDashboardFeaturedProducts(FlowLayoutPanel flowContainer)
        {
            flowContainer.Controls.Clear();
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT ProdName, Category, ProdImage FROM Products LIMIT 5";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string prodName = reader["ProdName"].ToString();
                                string category = reader["Category"].ToString();
                                byte[] imgBytes = reader["ProdImage"] != DBNull.Value ? (byte[])reader["ProdImage"] : null;

                                Panel miniCard = new BufferedPanel();
                                miniCard.Size = new System.Drawing.Size(160, 200);
                                miniCard.BackColor = Color.White;
                                miniCard.Margin = new Padding(5);

                                PictureBox pBox = new PictureBox();
                                pBox.Size = new System.Drawing.Size(140, 100);
                                pBox.Location = new Point(10, 10);
                                pBox.SizeMode = PictureBoxSizeMode.Zoom;
                                pBox.BackColor = Color.FromArgb(240, 240, 240);
                                if (imgBytes != null)
                                {
                                    try { using (MemoryStream ms = new MemoryStream(imgBytes)) { pBox.Image = Image.FromStream(ms); } } catch { }
                                }
                                miniCard.Controls.Add(pBox);

                                Label lblPName = new Label();
                                lblPName.Text = prodName;
                                lblPName.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                                lblPName.Location = new Point(10, 115);
                                lblPName.Size = new System.Drawing.Size(140, 40);
                                miniCard.Controls.Add(lblPName);

                                Button btnGoShop = new Button();
                                btnGoShop.Text = "View Shop";
                                btnGoShop.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                                btnGoShop.BackColor = Color.FromArgb(0, 122, 204);
                                btnGoShop.ForeColor = Color.White;
                                btnGoShop.FlatStyle = FlatStyle.Flat;
                                btnGoShop.FlatAppearance.BorderSize = 0;
                                btnGoShop.Location = new Point(10, 158);
                                btnGoShop.Size = new System.Drawing.Size(140, 28);
                                btnGoShop.Cursor = Cursors.Hand;
                                btnGoShop.Click += (s, e) => { customerTabControl.SelectedTab = tabShop; HighlightButton(btnShop); LoadShopProducts(); };
                                miniCard.Controls.Add(btnGoShop);

                                flowContainer.Controls.Add(miniCard);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadDashboardRecentOrder(Panel pnlContainer)
        {
            pnlContainer.Controls.Clear();
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Orders WHERE OrdCustomer = @customer ORDER BY OrdDate DESC LIMIT 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@customer", username);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string ordId = reader["OrdID"].ToString();
                                string ordDate = reader["OrdDate"].ToString();
                                string ordItems = reader["OrdItems"].ToString();
                                string ordTotal = reader["OrdTotal"].ToString();
                                string ordStatus = reader["OrdStatus"].ToString();

                                Label lblInfo = new Label();
                                lblInfo.Text = $"Latest Order ID: {ordId}\nDate: {ordDate}\nItems: {ordItems}\nTotal: {ordTotal}  |  Status: {ordStatus}";
                                lblInfo.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                                lblInfo.Location = new Point(20, 20);
                                lblInfo.Size = new System.Drawing.Size(700, 80);
                                pnlContainer.Controls.Add(lblInfo);

                                Button btnViewAllOrders = new Button();
                                btnViewAllOrders.Text = "Manage Orders";
                                btnViewAllOrders.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                                btnViewAllOrders.BackColor = Color.FromArgb(0, 122, 204);
                                btnViewAllOrders.ForeColor = Color.White;
                                btnViewAllOrders.FlatStyle = FlatStyle.Flat;
                                btnViewAllOrders.FlatAppearance.BorderSize = 0;
                                btnViewAllOrders.Location = new Point(780, 40);
                                btnViewAllOrders.Size = new System.Drawing.Size(160, 38);
                                btnViewAllOrders.Cursor = Cursors.Hand;
                                btnViewAllOrders.Click += (s, e) => { customerTabControl.SelectedTab = tabMyOrders; HighlightButton(btnMyOrders); LoadCustomerOrders(); };
                                pnlContainer.Controls.Add(btnViewAllOrders);
                            }
                            else
                            {
                                Label lblNoOrder = new Label();
                                lblNoOrder.Text = "No recent orders.";
                                lblNoOrder.Font = new Font("Segoe UI", 10, FontStyle.Italic);
                                lblNoOrder.ForeColor = Color.Gray;
                                lblNoOrder.Location = new Point(20, 45);
                                lblNoOrder.AutoSize = true;
                                pnlContainer.Controls.Add(lblNoOrder);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Label lblErr = new Label();
                lblErr.Text = "Error loading recent order: " + ex.Message;
                lblErr.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                lblErr.ForeColor = Color.Red;
                lblErr.Location = new Point(20, 45);
                lblErr.AutoSize = true;
                pnlContainer.Controls.Add(lblErr);
            }
        }

        private void SetupShopContent()
        {
            tabShop.BackColor = Color.FromArgb(245, 247, 250);

            lblShopTitle = new Label();
            lblShopTitle.Text = "PRODUCTS";
            lblShopTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblShopTitle.Location = new Point(30, 20);
            lblShopTitle.AutoSize = true;
            tabShop.Controls.Add(lblShopTitle);

            txtSearchShop = new TextBox();
            txtSearchShop.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            txtSearchShop.Location = new Point(30, 65);
            txtSearchShop.Size = new System.Drawing.Size(280, 30);
            tabShop.Controls.Add(txtSearchShop);

            btnSearchShop = new Button();
            btnSearchShop.Text = "Search";
            btnSearchShop.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnSearchShop.BackColor = Color.FromArgb(0, 122, 204);
            btnSearchShop.ForeColor = Color.White;
            btnSearchShop.FlatStyle = FlatStyle.Flat;
            btnSearchShop.FlatAppearance.BorderSize = 0;
            btnSearchShop.Location = new Point(315, 64);
            btnSearchShop.Size = new System.Drawing.Size(90, 28);
            btnSearchShop.Cursor = Cursors.Hand;
            btnSearchShop.Click += (s, e) => LoadShopProducts();
            tabShop.Controls.Add(btnSearchShop);

            cmbCategoryFilter = new ComboBox();
            cmbCategoryFilter.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            cmbCategoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategoryFilter.Items.AddRange(new object[] {
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
            cmbCategoryFilter.SelectedIndex = 0;
            cmbCategoryFilter.Location = new Point(420, 65);
            cmbCategoryFilter.Size = new System.Drawing.Size(200, 30);
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => LoadShopProducts();
            tabShop.Controls.Add(cmbCategoryFilter);

            flowShopProducts = new BufferedFlowLayoutPanel();
            flowShopProducts.Location = new Point(30, 110);
            flowShopProducts.Size = new System.Drawing.Size(980, 550);
            flowShopProducts.AutoScroll = true;
            flowShopProducts.BackColor = Color.FromArgb(245, 247, 250);
            tabShop.Controls.Add(flowShopProducts);
        }

        public class ProductVariant
        {
            public decimal Price { get; set; }
            public int Stock { get; set; }
        }

        private void LoadShopProducts()
        {
            flowShopProducts.Controls.Clear();

            string search = txtSearchShop != null ? txtSearchShop.Text.Trim() : "";
            string category = cmbCategoryFilter != null && cmbCategoryFilter.SelectedIndex > 0 ? cmbCategoryFilter.SelectedItem.ToString() : "";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Products WHERE 1=1";

                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " AND (ProdName LIKE @search OR Category LIKE @search)";
                    }
                    if (!string.IsNullOrEmpty(category))
                    {
                        query += " AND Category LIKE @category";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(search))
                        {
                            cmd.Parameters.AddWithValue("@search", "%" + search + "%");
                        }
                        if (!string.IsNullOrEmpty(category))
                        {
                            cmd.Parameters.AddWithValue("@category", "%" + category + "%");
                        }

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            var groupedProducts = new Dictionary<string, Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                string prodName = reader["ProdName"] != DBNull.Value ? reader["ProdName"].ToString() : "Unnamed";
                                string prodCategory = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : "";
                                string prodSize = reader["Size"] != DBNull.Value ? reader["Size"].ToString().ToUpper() : "S";

                                decimal prodPrice = 0;
                                if (reader["Price"] != DBNull.Value)
                                {
                                    decimal.TryParse(reader["Price"].ToString().Replace("₱", "").Replace("$", "").Replace("€", "").Trim(), out prodPrice);
                                }

                                int prodStock = 0;
                                if (reader["Stock"] != DBNull.Value)
                                {
                                    int.TryParse(reader["Stock"].ToString(), out prodStock);
                                }

                                byte[] imageBytes = null;
                                if (reader["ProdImage"] != DBNull.Value)
                                {
                                    imageBytes = (byte[])reader["ProdImage"];
                                }

                                if (!groupedProducts.ContainsKey(prodName))
                                {
                                    groupedProducts[prodName] = new Dictionary<string, object>
                                    {
                                        { "Category", prodCategory },
                                        { "ImageBytes", imageBytes },
                                        { "Variants", new Dictionary<string, ProductVariant>() }
                                    };
                                }

                                var variants = (Dictionary<string, ProductVariant>)groupedProducts[prodName]["Variants"];
                                variants[prodSize] = new ProductVariant { Price = prodPrice, Stock = prodStock };
                            }

                            bool hasProducts = false;
                            foreach (var kvp in groupedProducts)
                            {
                                hasProducts = true;
                                string prodName = kvp.Key;
                                string prodCategory = kvp.Value["Category"].ToString();
                                byte[] imageBytes = (byte[])kvp.Value["ImageBytes"];
                                var variants = (Dictionary<string, ProductVariant>)kvp.Value["Variants"];

                                AddGroupedProductCard(prodName, prodCategory, variants, imageBytes);
                            }

                            if (!hasProducts)
                            {
                                Label lblNoProd = new Label();
                                lblNoProd.Text = currentLanguage == "Filipino" ? "Walang nakitang produkto na akma sa iyong hinahanap." :
                                                 currentLanguage == "Cebuano" ? "Walay nakit-an nga produkto nga pareha sa imong gipangita." :
                                                 "No products found matching your criteria.";
                                lblNoProd.Font = new Font("Segoe UI", 12, FontStyle.Italic);
                                lblNoProd.ForeColor = Color.Gray;
                                lblNoProd.AutoSize = true;
                                flowShopProducts.Controls.Add(lblNoProd);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Label lblError = new Label();
                lblError.Text = "Database Error: " + ex.Message;
                lblError.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblError.ForeColor = Color.Red;
                lblError.AutoSize = true;
                flowShopProducts.Controls.Add(lblError);
            }
        }

        private void AddGroupedProductCard(string name, string category, Dictionary<string, ProductVariant> variants, byte[] imageBytes)
        {
            Panel card = new BufferedPanel();
            card.Size = new System.Drawing.Size(220, 355);
            card.BackColor = Color.White;
            card.Margin = new Padding(10);

            PictureBox picBox = new PictureBox();
            picBox.Size = new System.Drawing.Size(200, 100);
            picBox.Location = new Point(10, 10);
            picBox.SizeMode = PictureBoxSizeMode.Zoom;
            picBox.BackColor = Color.FromArgb(240, 240, 240);

            if (imageBytes != null && imageBytes.Length > 0)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        picBox.Image = Image.FromStream(ms);
                    }
                }
                catch { }
            }
            card.Controls.Add(picBox);

            Label lblCat = new Label();
            lblCat.Text = category.ToUpper();
            lblCat.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblCat.ForeColor = Color.Gray;
            lblCat.Location = new Point(10, 115);
            lblCat.Size = new System.Drawing.Size(160, 16);
            card.Controls.Add(lblCat);

            Button btnWishlistReact = new Button();
            bool isLiked = customerWishlist.Exists(w => w.ProductName == name);
            btnWishlistReact.Text = isLiked ? "♥" : "♡";
            btnWishlistReact.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnWishlistReact.ForeColor = Color.Red;
            btnWishlistReact.BackColor = Color.Transparent;
            btnWishlistReact.FlatStyle = FlatStyle.Flat;
            btnWishlistReact.FlatAppearance.BorderSize = 0;
            btnWishlistReact.Location = new Point(175, 112);
            btnWishlistReact.Size = new System.Drawing.Size(35, 25);
            btnWishlistReact.Cursor = Cursors.Hand;

            btnWishlistReact.Click += (s, e) => {
                isLiked = !isLiked;
                btnWishlistReact.Text = isLiked ? "♥" : "♡";

                if (isLiked)
                {
                    if (!customerWishlist.Exists(w => w.ProductName == name))
                    {
                        customerWishlist.Add(new WishlistItem
                        {
                            ProductName = name,
                            Category = category,
                            Variants = variants,
                            ImageBytes = imageBytes
                        });
                        SaveWishlistToDatabase();
                    }
                    SaveWishlistToDatabase();
                    MessageBox.Show($"'{name}' has been added to your Wishlist!", "Wishlist Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    customerWishlist.RemoveAll(w => w.ProductName == name);
                    SaveWishlistToDatabase();
                    MessageBox.Show($"'{name}' has been removed from your Wishlist.", "Wishlist Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            card.Controls.Add(btnWishlistReact);

            Label lblName = new Label();
            lblName.Text = name;
            lblName.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblName.ForeColor = Color.FromArgb(40, 40, 40);
            lblName.Location = new Point(10, 133);
            lblName.Size = new System.Drawing.Size(200, 35);
            card.Controls.Add(lblName);

            Label lblSizeText = new Label();
            lblSizeText.Text = currentLanguage == "Filipino" ? "Sukat:" : currentLanguage == "Cebuano" ? "Gidak-on:" : "Size:";
            lblSizeText.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblSizeText.Location = new Point(10, 173);
            lblSizeText.Size = new System.Drawing.Size(45, 22);
            card.Controls.Add(lblSizeText);

            ComboBox cmbSizes = new ComboBox();
            cmbSizes.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            cmbSizes.DropDownStyle = ComboBoxStyle.DropDownList;
            string[] standardSizes = { "S", "M", "L", "XL", "XXL" };
            foreach (var sz in standardSizes)
            {
                cmbSizes.Items.Add(sz);
            }
            cmbSizes.Location = new Point(55, 171);
            cmbSizes.Size = new System.Drawing.Size(155, 25);
            card.Controls.Add(cmbSizes);

            Label lblStock = new Label();
            lblStock.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblStock.Location = new Point(10, 201);
            lblStock.Size = new System.Drawing.Size(200, 18);
            card.Controls.Add(lblStock);

            Label lblPrice = new Label();
            lblPrice.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblPrice.ForeColor = Color.FromArgb(0, 122, 204);
            lblPrice.Location = new Point(10, 220);
            lblPrice.AutoSize = true;
            card.Controls.Add(lblPrice);

            cmbSizes.SelectedIndexChanged += (s, e) => {
                string selectedSize = cmbSizes.SelectedItem.ToString();
                if (variants.ContainsKey(selectedSize))
                {
                    var variant = variants[selectedSize];
                    lblStock.Text = $"Stock: {variant.Stock}";
                    lblStock.ForeColor = variant.Stock > 0 ? Color.DarkGreen : Color.Red;
                    decimal convertedPrice = variant.Price * currencyMultiplier;
                    lblPrice.Text = $"{currencySymbol}{convertedPrice:N2}";
                }
                else
                {
                    lblStock.Text = currentLanguage == "Filipino" ? "Stock: 0 (Hindi Available)" : currentLanguage == "Cebuano" ? "Stock: 0 (Walay Stock)" : "Stock: 0 (Not Available)";
                    lblStock.ForeColor = Color.Red;
                    lblPrice.Text = $"{currencySymbol}0.00";
                }
            };

            if (cmbSizes.Items.Count > 0)
            {
                int defaultIndex = 0;
                for (int i = 0; i < cmbSizes.Items.Count; i++)
                {
                    if (variants.ContainsKey(cmbSizes.Items[i].ToString()))
                    {
                        defaultIndex = i;
                        break;
                    }
                }
                cmbSizes.SelectedIndex = defaultIndex;
            }

            Button btnAddToCart = new Button();
            btnAddToCart.Text = currentLanguage == "Filipino" ? "Ilagay sa Cart" : currentLanguage == "Cebuano" ? "Ibutang sa Cart" : "Add to Cart";
            btnAddToCart.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnAddToCart.BackColor = Color.FromArgb(40, 167, 69);
            btnAddToCart.ForeColor = Color.White;
            btnAddToCart.FlatStyle = FlatStyle.Flat;
            btnAddToCart.FlatAppearance.BorderSize = 0;
            btnAddToCart.Location = new Point(10, 250);
            btnAddToCart.Size = new System.Drawing.Size(200, 32);
            btnAddToCart.Cursor = Cursors.Hand;

            btnAddToCart.Click += (s, e) => {
                string selectedSize = cmbSizes.SelectedItem.ToString();
                if (!variants.ContainsKey(selectedSize))
                {
                    MessageBox.Show("Selected size is not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var variant = variants[selectedSize];
                if (variant.Stock <= 0)
                {
                    MessageBox.Show("Sorry, this item size is currently out of stock.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var existingItem = customerCart.Find(c => c.ProductName == name && c.Size == selectedSize);
                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    customerCart.Add(new CartItem
                    {
                        ProductName = name,
                        Size = selectedSize,
                        Price = variant.Price,
                        Quantity = 1,
                        ImageBytes = imageBytes
                    });
                    SaveCartToDatabase();
                }
                SaveCartToDatabase();
                MessageBox.Show($"'{name} ({selectedSize})' has been successfully added to your Cart!", "Added to Cart", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            card.Controls.Add(btnAddToCart);

            Button btnBuyNow = new Button();
            btnBuyNow.Text = currentLanguage == "Filipino" ? "Bumili Na" : currentLanguage == "Cebuano" ? "Paliton Na" : "Buy Now";
            btnBuyNow.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuyNow.BackColor = Color.FromArgb(0, 122, 204);
            btnBuyNow.ForeColor = Color.White;
            btnBuyNow.FlatStyle = FlatStyle.Flat;
            btnBuyNow.FlatAppearance.BorderSize = 0;
            btnBuyNow.Location = new Point(10, 288);
            btnBuyNow.Size = new System.Drawing.Size(200, 32);
            btnBuyNow.Cursor = Cursors.Hand;

            btnBuyNow.Click += (s, e) => {

                if (Form1.CurrentUserRole == "Guest")
                {
                    DialogResult result = MessageBox.Show(
                        "Create an account or register to proceed with the purchase. Would you like to register now?",
                        "Account Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        this.Close();
                    }
                    return;
                }

                string selectedSize = cmbSizes.SelectedItem.ToString();
                if (!variants.ContainsKey(selectedSize))
                {
                    MessageBox.Show("Selected size is not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var variant = variants[selectedSize];
                if (variant.Stock <= 0)
                {
                    MessageBox.Show("Sorry, this item size is currently out of stock.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ShowCredentialPopupAndCheckout(paymentMethod: "GCash", isCartCheckout: false, buyNowName: name, buyNowSize: selectedSize, buyNowPrice: variant.Price, buyNowQty: 1);
            };
            card.Controls.Add(btnBuyNow);

            flowShopProducts.Controls.Add(card);
        }

        private void ShowCredentialPopupAndCheckout(string paymentMethod, bool isCartCheckout, string buyNowName = "", string buyNowSize = "", decimal buyNowPrice = 0, int buyNowQty = 1)
        {
            Form popup = new Form();
            popup.Size = new System.Drawing.Size(420, 540);
            popup.StartPosition = FormStartPosition.CenterParent;
            popup.FormBorderStyle = FormBorderStyle.FixedDialog;
            popup.MaximizeBox = false;
            popup.MinimizeBox = false;
            popup.Text = "Account Credentials & Payment";
            popup.BackColor = Color.White;

            Label lblHeader = new Label();
            lblHeader.Text = "Select Payment Method & Details";
            lblHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblHeader.Location = new Point(25, 20);
            lblHeader.AutoSize = true;
            popup.Controls.Add(lblHeader);

            Label lblSelectPay = new Label();
            lblSelectPay.Text = "Payment Method:";
            lblSelectPay.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblSelectPay.Location = new Point(25, 55);
            lblSelectPay.AutoSize = true;
            popup.Controls.Add(lblSelectPay);

            ComboBox cmbPaymentMethod = new ComboBox();
            cmbPaymentMethod.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Items.AddRange(new object[] { "GCash", "PayMaya", "GoTyme" });

            if (!string.IsNullOrEmpty(paymentMethod) && cmbPaymentMethod.Items.Contains(paymentMethod))
            {
                cmbPaymentMethod.SelectedItem = paymentMethod;
            }
            else
            {
                cmbPaymentMethod.SelectedIndex = 0;
            }
            cmbPaymentMethod.Location = new Point(25, 80);
            cmbPaymentMethod.Size = new System.Drawing.Size(350, 28);
            popup.Controls.Add(cmbPaymentMethod);
            
            Label lblAccName = new Label();
            lblAccName.Text = "Account Name:";
            lblAccName.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblAccName.Location = new Point(25, 120); 
            lblAccName.AutoSize = true;
            popup.Controls.Add(lblAccName);

            TextBox txtAccName = new TextBox();
            txtAccName.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtAccName.Location = new Point(25, 145); 
            txtAccName.Size = new System.Drawing.Size(350, 28);
            popup.Controls.Add(txtAccName);

            Label lblAccNum = new Label();
            lblAccNum.Text = "Account Number / Mobile No:";
            lblAccNum.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblAccNum.Location = new Point(25, 185); 
            lblAccNum.AutoSize = true;
            popup.Controls.Add(lblAccNum);

            TextBox txtAccNum = new TextBox();
            txtAccNum.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtAccNum.Location = new Point(25, 210); 
            txtAccNum.Size = new System.Drawing.Size(350, 28);
            popup.Controls.Add(txtAccNum);

            Label lblPin = new Label();
            lblPin.Text = "PIN / Password / OTP:";
            lblPin.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblPin.Location = new Point(25, 250); 
            lblPin.AutoSize = true;
            popup.Controls.Add(lblPin);

            TextBox txtPin = new TextBox();
            txtPin.PasswordChar = '•';
            txtPin.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtPin.Location = new Point(25, 275); 
            txtPin.Size = new System.Drawing.Size(350, 28);
            popup.Controls.Add(txtPin);

            CheckBox chkConfirmDetails = new CheckBox();
            chkConfirmDetails.Text = "I confirm that the account details provided are correct.";
            chkConfirmDetails.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            chkConfirmDetails.Location = new Point(25, 320); 
            chkConfirmDetails.Size = new System.Drawing.Size(350, 40);
            popup.Controls.Add(chkConfirmDetails);

            Button btnConfirmAcc = new Button();
            btnConfirmAcc.Text = "Confirm Account & Pay";
            btnConfirmAcc.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnConfirmAcc.BackColor = Color.FromArgb(40, 167, 69);
            btnConfirmAcc.ForeColor = Color.White;
            btnConfirmAcc.FlatStyle = FlatStyle.Flat;
            btnConfirmAcc.FlatAppearance.BorderSize = 0;
            btnConfirmAcc.Location = new Point(25, 375); 
            btnConfirmAcc.Size = new System.Drawing.Size(350, 40);
            btnConfirmAcc.Cursor = Cursors.Hand;

            btnConfirmAcc.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtAccName.Text) || string.IsNullOrWhiteSpace(txtAccNum.Text) || string.IsNullOrWhiteSpace(txtPin.Text))
                {
                    MessageBox.Show("Please fill in all required account credentials.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!chkConfirmDetails.Checked)
                {
                    MessageBox.Show("Please check the confirmation box to proceed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string finalPaymentMethod = cmbPaymentMethod.SelectedItem != null ? cmbPaymentMethod.SelectedItem.ToString() : "GCash";

                popup.Close();

                if (isCartCheckout)
                {
                    ExecuteCartCheckout(finalPaymentMethod);
                }
                else
                {
                    ExecuteBuyNowOrder(finalPaymentMethod, buyNowName, buyNowSize, buyNowPrice, buyNowQty);
                }
            };
            popup.Controls.Add(btnConfirmAcc);

            Button btnCancelAcc = new Button();
            btnCancelAcc.Text = "Cancel";
            btnCancelAcc.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnCancelAcc.BackColor = Color.IndianRed;
            btnCancelAcc.ForeColor = Color.White;
            btnCancelAcc.FlatStyle = FlatStyle.Flat;
            btnCancelAcc.FlatAppearance.BorderSize = 0;
            btnCancelAcc.Location = new Point(25, 425); 
            btnCancelAcc.Size = new System.Drawing.Size(350, 35);
            btnCancelAcc.Cursor = Cursors.Hand;
            btnCancelAcc.Click += (s, e) => popup.Close();
            popup.Controls.Add(btnCancelAcc);

            popup.ShowDialog();
        }

        private void ExecuteBuyNowOrder(string paymentMethod, string productName, string size, decimal price, int quantity)
        {
            decimal finalPrice = price * currencyMultiplier;
            decimal totalAmount = finalPrice * quantity;
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;
            string orderId = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string ordItemsText = $"{productName} ({size}) x {quantity}";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string insertQuery = "INSERT INTO Orders (OrdID, OrdCustomer, OrdDate, OrdItems, OrdTotal, OrdPayMethod, OrdPayStatus, OrdStatus) " +
                                         "VALUES (@ordId, @customer, @date, @items, @total, @method, 'Paid', 'Pending')";

                    using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ordId", orderId);
                        cmd.Parameters.AddWithValue("@customer", username);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@items", ordItemsText);
                        cmd.Parameters.AddWithValue("@total", currencySymbol + totalAmount.ToString("N2"));
                        cmd.Parameters.AddWithValue("@method", paymentMethod);
                        cmd.ExecuteNonQuery();
                    }

                    DeductStockFromItems(ordItemsText, conn);
                }

                MessageBox.Show("Order placed successfully via Buy Now with secure payment!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadShopProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error placing order: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupCartContent()
        {
            tabCart.BackColor = Color.FromArgb(245, 247, 250);

            lblCartTitle = new Label();
            lblCartTitle.Text = "Shopping Cart";
            lblCartTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblCartTitle.Location = new Point(30, 25);
            lblCartTitle.AutoSize = true;
            tabCart.Controls.Add(lblCartTitle);

            flowCartItems = new BufferedFlowLayoutPanel();
            flowCartItems.Location = new Point(30, 75);
            flowCartItems.Size = new System.Drawing.Size(640, 560);
            flowCartItems.AutoScroll = true;
            flowCartItems.BackColor = Color.White;
            tabCart.Controls.Add(flowCartItems);

            Panel summaryPanel = new BufferedPanel();
            summaryPanel.Location = new Point(690, 75);
            summaryPanel.Size = new System.Drawing.Size(315, 560);
            summaryPanel.BackColor = Color.White;
            tabCart.Controls.Add(summaryPanel);

            Label lblSummaryTitle = new Label();
            lblSummaryTitle.Text = "Order Summary";
            lblSummaryTitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblSummaryTitle.Location = new Point(20, 20);
            lblSummaryTitle.AutoSize = true;
            summaryPanel.Controls.Add(lblSummaryTitle);

            Panel pnlBreakdownContainer = new BufferedPanel();
            pnlBreakdownContainer.Location = new Point(20, 60);
            pnlBreakdownContainer.Size = new System.Drawing.Size(275, 260);
            pnlBreakdownContainer.AutoScroll = true;
            summaryPanel.Controls.Add(pnlBreakdownContainer);

            summaryPanel.Tag = pnlBreakdownContainer;

            lblCartTotalLabel = new Label();
            lblCartTotalLabel.Text = "Total Amount:";
            lblCartTotalLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblCartTotalLabel.Location = new Point(20, 335);
            lblCartTotalLabel.AutoSize = true;
            summaryPanel.Controls.Add(lblCartTotalLabel);

            lblCartTotalAmount = new Label();
            lblCartTotalAmount.Text = $"{currencySymbol}0.00";
            lblCartTotalAmount.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblCartTotalAmount.ForeColor = Color.FromArgb(0, 122, 204);
            lblCartTotalAmount.Location = new Point(20, 360);
            lblCartTotalAmount.AutoSize = true;
            summaryPanel.Controls.Add(lblCartTotalAmount);

            lblCartPaymentLabel = new Label();
            lblCartPaymentLabel.Text = "Payment Method:";
            lblCartPaymentLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblCartPaymentLabel.Location = new Point(20, 410);
            lblCartPaymentLabel.AutoSize = true;
            summaryPanel.Controls.Add(lblCartPaymentLabel);

            cmbCartPaymentMethod = new ComboBox();
            cmbCartPaymentMethod.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbCartPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCartPaymentMethod.Items.AddRange(new object[] { "GCash", "PayMaya", "GoTyme" });
            cmbCartPaymentMethod.SelectedIndex = 0;
            cmbCartPaymentMethod.Location = new Point(20, 435);
            cmbCartPaymentMethod.Size = new System.Drawing.Size(275, 30);
            summaryPanel.Controls.Add(cmbCartPaymentMethod);

            btnCartCheckout = new Button();
            btnCartCheckout.Text = "Checkout Selected Items";
            btnCartCheckout.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnCartCheckout.BackColor = Color.FromArgb(40, 167, 69);
            btnCartCheckout.ForeColor = Color.White;
            btnCartCheckout.FlatStyle = FlatStyle.Flat;
            btnCartCheckout.FlatAppearance.BorderSize = 0;
            btnCartCheckout.Location = new Point(20, 485);
            btnCartCheckout.Size = new System.Drawing.Size(275, 45);
            btnCartCheckout.Cursor = Cursors.Hand;

            btnCartCheckout.Click += (s, e) => {

                if (Form1.CurrentUserRole == "Guest")
                {
                    DialogResult result = MessageBox.Show(
                        "Create an account or register to proceed with the purchase. Would you like to register now?",
                        "Account Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        this.Close();
                    }
                    return;
                }

                var selectedItems = customerCart.FindAll(c => c.ChkSelect != null && c.ChkSelect.Checked);
                if (selectedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one item to checkout.", "No Items Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string paymentMethod = cmbCartPaymentMethod.SelectedItem != null ? cmbCartPaymentMethod.SelectedItem.ToString() : "GCash";

                ShowCredentialPopupAndCheckout(paymentMethod, isCartCheckout: true);
            };
            summaryPanel.Controls.Add(btnCartCheckout);
        }

        private void RefreshCartDisplay()
        {
            flowCartItems.Controls.Clear();

            Panel summaryPanel = null;
            foreach (Control ctrl in tabCart.Controls)
            {
                if (ctrl is Panel p && p.Location.X == 690) { summaryPanel = p; break; }
            }

            Panel pnlBreakdownContainer = summaryPanel?.Tag as Panel;
            if (pnlBreakdownContainer != null)
            {
                pnlBreakdownContainer.Controls.Clear();
            }

            decimal grandTotal = 0;
            int breakdownY = 0;

            foreach (var item in customerCart)
            {
                Panel itemPanel = new BufferedPanel();
                itemPanel.Size = new System.Drawing.Size(610, 95);
                itemPanel.BackColor = Color.FromArgb(250, 250, 250);
                itemPanel.Margin = new Padding(5);

                CheckBox chkSelect = new CheckBox();
                chkSelect.Location = new Point(10, 36);
                chkSelect.Size = new System.Drawing.Size(20, 20);

                
                chkSelect.Checked = item.ChkSelect != null ? item.ChkSelect.Checked : true;
                item.ChkSelect = chkSelect;
                chkSelect.CheckedChanged += (s, e) => UpdateCartSummaryCalculation();
                itemPanel.Controls.Add(chkSelect);

                PictureBox picCart = new PictureBox();
                picCart.Size = new System.Drawing.Size(75, 75);
                picCart.Location = new Point(35, 10);
                picCart.SizeMode = PictureBoxSizeMode.Zoom;
                picCart.BackColor = Color.FromArgb(235, 235, 235);
                if (item.ImageBytes != null && item.ImageBytes.Length > 0)
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(item.ImageBytes))
                        {
                            picCart.Image = Image.FromStream(ms);
                        }
                    }
                    catch { }
                }
                itemPanel.Controls.Add(picCart);

                Label lblNameSize = new Label();
                lblNameSize.Text = $"{item.ProductName}\nSize: {item.Size}";
                lblNameSize.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                lblNameSize.Location = new Point(125, 12);
                lblNameSize.Size = new System.Drawing.Size(200, 40);
                itemPanel.Controls.Add(lblNameSize);

                decimal convertedUnitPrice = item.Price * currencyMultiplier;
                Label lblUnitPrice = new Label();
                lblUnitPrice.Text = $"Unit Price: {currencySymbol}{convertedUnitPrice:N2}";
                lblUnitPrice.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                lblUnitPrice.ForeColor = Color.Gray;
                lblUnitPrice.Location = new Point(125, 58);
                lblUnitPrice.AutoSize = true;
                itemPanel.Controls.Add(lblUnitPrice);

                Label lblQtyText = new Label();
                lblQtyText.Text = $"Qty: {item.Quantity}";
                lblQtyText.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                lblQtyText.Location = new Point(340, 36);
                lblQtyText.AutoSize = true;
                itemPanel.Controls.Add(lblQtyText);

                Button btnMinus = new Button();
                btnMinus.Text = "-";
                btnMinus.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                btnMinus.Size = new System.Drawing.Size(25, 25);
                btnMinus.Location = new Point(410, 33);
                btnMinus.Cursor = Cursors.Hand;
                btnMinus.Click += (s, e) => {
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                    }
                    else
                    {
                        customerCart.Remove(item);
                    }
                    SaveCartToDatabase();
                    RefreshCartDisplay();
                };
                itemPanel.Controls.Add(btnMinus);

                Button btnPlus = new Button();
                btnPlus.Text = "+";
                btnPlus.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                btnPlus.Size = new System.Drawing.Size(25, 25);
                btnPlus.Location = new Point(440, 33);
                btnPlus.Cursor = Cursors.Hand;
                btnPlus.Click += (s, e) => {
                    item.Quantity++;
                    SaveCartToDatabase();
                    RefreshCartDisplay();
                };
                itemPanel.Controls.Add(btnPlus);

                decimal subtotal = convertedUnitPrice * item.Quantity;
                Label lblSubtotal = new Label();
                lblSubtotal.Text = $"Subtotal: {currencySymbol}{subtotal:N2}";
                lblSubtotal.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                lblSubtotal.ForeColor = Color.FromArgb(0, 122, 204);
                lblSubtotal.Location = new Point(480, 15);
                lblSubtotal.AutoSize = true;
                itemPanel.Controls.Add(lblSubtotal);

                Button btnRemove = new Button();
                btnRemove.Text = currentLanguage == "Filipino" ? "Tanggalin" : currentLanguage == "Cebuano" ? "Tangtangon" : "Remove";
                btnRemove.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                btnRemove.BackColor = Color.IndianRed;
                btnRemove.ForeColor = Color.White;
                btnRemove.FlatStyle = FlatStyle.Flat;
                btnRemove.FlatAppearance.BorderSize = 0;
                btnRemove.Location = new Point(480, 48);
                btnRemove.Size = new System.Drawing.Size(110, 26);
                btnRemove.Cursor = Cursors.Hand;
                btnRemove.Click += (s, e) => {
                    customerCart.Remove(item);
                    SaveCartToDatabase();
                    RefreshCartDisplay();
                };
                itemPanel.Controls.Add(btnRemove);

                flowCartItems.Controls.Add(itemPanel);

                if (chkSelect.Checked)
                {
                    grandTotal += subtotal;

                    if (pnlBreakdownContainer != null)
                    {
                        Label lblSummaryLine = new Label();
                        lblSummaryLine.Text = $"{item.ProductName} ({item.Size}) x{item.Quantity}\n({currencySymbol}{convertedUnitPrice:N2} x {item.Quantity}) = {currencySymbol}{subtotal:N2}";
                        lblSummaryLine.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                        lblSummaryLine.Location = new Point(5, breakdownY);
                        lblSummaryLine.Size = new System.Drawing.Size(250, 35);
                        pnlBreakdownContainer.Controls.Add(lblSummaryLine);

                        breakdownY += 42;
                    }
                }
            }

            if (lblCartTotalAmount != null)
            {
                lblCartTotalAmount.Text = $"{currencySymbol}{grandTotal:N2}";
            }

            if (customerCart.Count == 0)
            {
                Label lblEmpty = new Label();
                lblEmpty.Text = currentLanguage == "Filipino" ? "Walang laman ang iyong shopping cart." : currentLanguage == "Cebuano" ? "Wala nay sulod ang imong shopping cart." : "Your shopping cart is empty.";
                lblEmpty.Font = new Font("Segoe UI", 11, FontStyle.Italic);
                lblEmpty.ForeColor = Color.Gray;
                lblEmpty.AutoSize = true;
                flowCartItems.Controls.Add(lblEmpty);
            }
        }

        private void UpdateCartSummaryCalculation()
        {
            RefreshCartDisplay();
        }

        private void ExecuteCartCheckout(string paymentMethod)
        {
            var selectedItems = customerCart.FindAll(c => c.ChkSelect != null && c.ChkSelect.Checked);
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    foreach (var item in selectedItems)
                    {
                        string orderId = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + new Random().Next(10, 99);
                        string ordItemsText = $"{item.ProductName} ({item.Size}) x {item.Quantity}";
                        decimal convertedUnitPrice = item.Price * currencyMultiplier;
                        decimal subtotal = convertedUnitPrice * item.Quantity;

                        string insertQuery = "INSERT INTO Orders (OrdID, OrdCustomer, OrdDate, OrdItems, OrdTotal, OrdPayMethod, OrdPayStatus, OrdStatus) " +
                                             "VALUES (@ordId, @customer, @date, @items, @total, @method, 'Paid', 'Pending')";

                        using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@ordId", orderId);
                            cmd.Parameters.AddWithValue("@customer", username);
                            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                            cmd.Parameters.AddWithValue("@items", ordItemsText);
                            cmd.Parameters.AddWithValue("@total", currencySymbol + subtotal.ToString("N2"));
                            cmd.Parameters.AddWithValue("@method", paymentMethod);
                            cmd.ExecuteNonQuery();
                        }

                        DeductStockFromItems(ordItemsText, conn);
                    }
                }

                customerCart.RemoveAll(c => c.ChkSelect != null && c.ChkSelect.Checked);
                SaveCartToDatabase();
                RefreshCartDisplay();
                LoadShopProducts();

                MessageBox.Show("Checkout successful with verified account credentials! Your orders have been placed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Checkout Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupMyOrdersContent()
        {
            tabMyOrders.BackColor = Color.FromArgb(245, 247, 250);

            lblMyOrdersTitle = new Label();
            lblMyOrdersTitle.Text = "My Orders";
            lblMyOrdersTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblMyOrdersTitle.Location = new Point(30, 25);
            lblMyOrdersTitle.AutoSize = true;
            tabMyOrders.Controls.Add(lblMyOrdersTitle);

            flowMyOrdersList = new BufferedFlowLayoutPanel();
            flowMyOrdersList.Location = new Point(30, 75);
            flowMyOrdersList.Size = new System.Drawing.Size(975, 565);
            flowMyOrdersList.AutoScroll = true;
            flowMyOrdersList.BackColor = Color.FromArgb(245, 247, 250);
            tabMyOrders.Controls.Add(flowMyOrdersList);
        }

        private void LoadCustomerOrders()
        {
            if (flowMyOrdersList == null) return;
            flowMyOrdersList.Controls.Clear();

            string currentCustomer = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Orders WHERE OrdCustomer = @customer ORDER BY OrdDate DESC";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@customer", currentCustomer);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            bool hasOrders = false;

                            while (reader.Read())
                            {
                                hasOrders = true;
                                string ordId = reader["OrdID"] != DBNull.Value ? reader["OrdID"].ToString() : "";
                                string ordDate = reader["OrdDate"] != DBNull.Value ? reader["OrdDate"].ToString() : "";
                                string ordItems = reader["OrdItems"] != DBNull.Value ? reader["OrdItems"].ToString() : "";
                                string ordTotal = reader["OrdTotal"] != DBNull.Value ? reader["OrdTotal"].ToString() : "";
                                string payMethod = reader["OrdPayMethod"] != DBNull.Value ? reader["OrdPayMethod"].ToString() : "";
                                string ordStatus = reader["OrdStatus"] != DBNull.Value ? reader["OrdStatus"].ToString() : "Pending";

                                byte[] productImgBytes = GetProductImageByName(ordItems, conn);

                                AddOrderCardItem(ordId, ordDate, ordItems, ordTotal, payMethod, ordStatus, productImgBytes);
                            }

                            if (!hasOrders)
                            {
                                Label lblNoOrders = new Label();
                                lblNoOrders.Text = currentLanguage == "Filipino" ? "Wala ka pang mga order." : currentLanguage == "Cebuano" ? "Wala pa kay mga order." : "You have no orders yet.";
                                lblNoOrders.Font = new Font("Segoe UI", 12, FontStyle.Italic);
                                lblNoOrders.ForeColor = Color.Gray;
                                lblNoOrders.AutoSize = true;
                                flowMyOrdersList.Controls.Add(lblNoOrders);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Label lblErr = new Label();
                lblErr.Text = "Error loading orders: " + ex.Message;
                lblErr.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblErr.ForeColor = Color.Red;
                lblErr.AutoSize = true;
                flowMyOrdersList.Controls.Add(lblErr);
            }
        }

        private byte[] GetProductImageByName(string ordItems, SQLiteConnection conn)
        {
            if (string.IsNullOrEmpty(ordItems)) return null;

            string firstItemPart = ordItems.Split(',')[0].Trim();
            string prodName = firstItemPart;

            if (firstItemPart.Contains(" x "))
            {
                int lastIndex = firstItemPart.LastIndexOf(" x ");
                prodName = firstItemPart.Substring(0, lastIndex).Trim();
                if (prodName.Contains("(") && prodName.EndsWith(")"))
                {
                    int openBr = prodName.LastIndexOf("(");
                    prodName = prodName.Substring(0, openBr).Trim();
                }
            }

            try
            {
                string query = "SELECT ProdImage FROM Products WHERE ProdName = @prodName LIMIT 1";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@prodName", prodName);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return (byte[])result;
                    }
                }
            }
            catch { }

            return null;
        }

        private void AddOrderCardItem(string ordId, string ordDate, string ordItems, string ordTotal, string payMethod, string ordStatus, byte[] imageBytes)
        {
            Panel card = new BufferedPanel();
            card.Size = new System.Drawing.Size(945, 120);
            card.BackColor = Color.White;
            card.Margin = new Padding(0, 0, 0, 10);

            PictureBox picOrder = new PictureBox();
            picOrder.Size = new System.Drawing.Size(90, 90);
            picOrder.Location = new Point(15, 15);
            picOrder.SizeMode = PictureBoxSizeMode.Zoom;
            picOrder.BackColor = Color.FromArgb(240, 240, 240);
            if (imageBytes != null && imageBytes.Length > 0)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        picOrder.Image = Image.FromStream(ms);
                    }
                }
                catch { }
            }
            card.Controls.Add(picOrder);

            Label lblIdStatus = new Label();
            lblIdStatus.Text = $"Order ID: {ordId}";
            lblIdStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblIdStatus.Location = new Point(120, 15);
            lblIdStatus.AutoSize = true;
            card.Controls.Add(lblIdStatus);

            Label lblStatusBadge = new Label();
            lblStatusBadge.Text = $"Status: {ordStatus}";
            lblStatusBadge.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            if (ordStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                lblStatusBadge.ForeColor = Color.DarkOrange;
            else if (ordStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) || ordStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                lblStatusBadge.ForeColor = Color.ForestGreen;
            else if (ordStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                lblStatusBadge.ForeColor = Color.IndianRed;
            else
                lblStatusBadge.ForeColor = Color.FromArgb(0, 122, 204);

            lblStatusBadge.Location = new Point(720, 15);
            lblStatusBadge.AutoSize = true;
            card.Controls.Add(lblStatusBadge);


            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string checkReturnQuery = "SELECT Status FROM Returns WHERE TransID = @ordId LIMIT 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(checkReturnQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ordId", ordId);
                        object retStatus = cmd.ExecuteScalar();
                        if (retStatus != null && retStatus != DBNull.Value)
                        {
                            lblStatusBadge.Text = $"Return: {retStatus.ToString()}";
                            if (retStatus.ToString().Equals("Approved", StringComparison.OrdinalIgnoreCase))
                                lblStatusBadge.ForeColor = Color.Green;
                            else if (retStatus.ToString().Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                                lblStatusBadge.ForeColor = Color.Red;
                            else
                                lblStatusBadge.ForeColor = Color.DarkOrange;
                        }
                    }
                }
            }
            catch { }




            string formattedDate = ordDate;
            if (DateTime.TryParse(ordDate, out DateTime parsedDt))
            {
                formattedDate = parsedDt.ToString("yyyy-MM-dd hh:mm:ss tt");
            }

            Label lblDate = new Label();
            lblDate.Text = $"Date Ordered: {formattedDate}";
            lblDate.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblDate.ForeColor = Color.Gray;
            lblDate.Location = new Point(120, 40);
            lblDate.AutoSize = true;
            card.Controls.Add(lblDate);

            Label lblProducts = new Label();
            lblProducts.Text = $"Products: {ordItems}";
            lblProducts.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblProducts.Location = new Point(120, 62);
            lblProducts.Size = new System.Drawing.Size(580, 20);
            card.Controls.Add(lblProducts);

            Label lblTotalPay = new Label();
            lblTotalPay.Text = $"Total: {ordTotal}  |  Payment: {payMethod}";
            lblTotalPay.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblTotalPay.ForeColor = Color.FromArgb(0, 122, 204);
            lblTotalPay.Location = new Point(120, 87);
            lblTotalPay.AutoSize = true;
            card.Controls.Add(lblTotalPay);

            Button btnViewDetails = new Button();
            btnViewDetails.Text = currentLanguage == "Filipino" ? "Tingnan" : currentLanguage == "Cebuano" ? "Tan-awa" : "View Details";
            btnViewDetails.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btnViewDetails.BackColor = Color.FromArgb(0, 122, 204);
            btnViewDetails.ForeColor = Color.White;
            btnViewDetails.FlatStyle = FlatStyle.Flat;
            btnViewDetails.FlatAppearance.BorderSize = 0;
            btnViewDetails.Location = new Point(720, 45);
            btnViewDetails.Size = new System.Drawing.Size(100, 30);
            btnViewDetails.Cursor = Cursors.Hand;
            btnViewDetails.Click += (s, e) => {
                ShowOrderDetailsPopup(ordId, formattedDate, ordItems, ordTotal, payMethod, ordStatus, imageBytes);
            };
            card.Controls.Add(btnViewDetails);

            Button btnCancelOrder = new Button();
            btnCancelOrder.Text = currentLanguage == "Filipino" ? "Kanselahin" : currentLanguage == "Cebuano" ? "Kanselahon" : "Cancel Order";
            btnCancelOrder.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btnCancelOrder.BackColor = Color.IndianRed;
            btnCancelOrder.ForeColor = Color.White;
            btnCancelOrder.FlatStyle = FlatStyle.Flat;
            btnCancelOrder.FlatAppearance.BorderSize = 0;
            btnCancelOrder.Location = new Point(830, 45);
            btnCancelOrder.Size = new System.Drawing.Size(100, 30);
            btnCancelOrder.Cursor = Cursors.Hand;

            if (!ordStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                btnCancelOrder.Enabled = false;
                btnCancelOrder.BackColor = Color.LightGray;
            }

            btnCancelOrder.Click += (s, e) => {
                DialogResult dr = MessageBox.Show($"Are you sure you want to cancel order {ordId}?\nThis will return the items back to the inventory.", "Cancel Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    CancelCustomerOrder(ordId, ordItems);
                }
            };
            card.Controls.Add(btnCancelOrder);

            Button btnRequestReturn = new Button();
            btnRequestReturn.Text = currentLanguage == "Filipino" ? "Ibalik/Refund" : currentLanguage == "Cebuano" ? "Iuli/Refund" : "Return/Refund";
            btnRequestReturn.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            btnRequestReturn.BackColor = Color.FromArgb(220, 53, 69);
            btnRequestReturn.ForeColor = Color.White;
            btnRequestReturn.FlatStyle = FlatStyle.Flat;
            btnRequestReturn.FlatAppearance.BorderSize = 0;
            btnRequestReturn.Location = new Point(830, 80); 
            btnRequestReturn.Size = new System.Drawing.Size(100, 30);
            btnRequestReturn.Cursor = Cursors.Hand;

            bool isDeliveredOrCompleted = ordStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
                                          ordStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            if (!isDeliveredOrCompleted)
            {
                btnRequestReturn.Enabled = false;
                btnRequestReturn.BackColor = Color.LightGray;
            }

            btnRequestReturn.Click += (s, e) => {
                ShowReturnRequestPopup(ordId, ordItems, ordTotal);
            };
            card.Controls.Add(btnRequestReturn);

            flowMyOrdersList.Controls.Add(card);
        }

        private void ShowReturnRequestPopup(string ordId, string ordItems, string ordTotal)
        {
            Form returnForm = new Form();
            returnForm.Size = new System.Drawing.Size(420, 450);
            returnForm.StartPosition = FormStartPosition.CenterParent;
            returnForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            returnForm.MaximizeBox = false;
            returnForm.MinimizeBox = false;
            returnForm.Text = $"Request Return / Refund - {ordId}";
            returnForm.BackColor = Color.White;

            Label lblHeader = new Label();
            lblHeader.Text = "Product Return & Refund Form";
            lblHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblHeader.Location = new Point(25, 20);
            lblHeader.AutoSize = true;
            returnForm.Controls.Add(lblHeader);

            Label lblInfo = new Label();
            lblInfo.Text = $"Order ID: {ordId}\nItems: {ordItems}";
            lblInfo.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblInfo.Location = new Point(25, 55);
            lblInfo.Size = new System.Drawing.Size(350, 45);
            returnForm.Controls.Add(lblInfo);

            Label lblReasonTitle = new Label();
            lblReasonTitle.Text = "Reason for Return / Refund:";
            lblReasonTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblReasonTitle.Location = new Point(25, 110);
            lblReasonTitle.AutoSize = true;
            returnForm.Controls.Add(lblReasonTitle);

            ComboBox cmbReason = new ComboBox();
            cmbReason.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbReason.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbReason.Items.AddRange(new object[] {
                        "Damaged or Defective Item",
                        "Wrong Item / Size Delivered",
                        "Poor Quality",
                        "Missing Parts / Accessories",
                        "Change of Mind / Doesn't Fit"
            });
            cmbReason.SelectedIndex = 0;
            cmbReason.Location = new Point(25, 135);
            cmbReason.Size = new System.Drawing.Size(350, 28);
            returnForm.Controls.Add(cmbReason);

            Label lblDetailsTitle = new Label();
            lblDetailsTitle.Text = "Additional Details / Notes:";
            lblDetailsTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblDetailsTitle.Location = new Point(25, 180);
            lblDetailsTitle.AutoSize = true;
            returnForm.Controls.Add(lblDetailsTitle);

            TextBox txtDetails = new TextBox();
            txtDetails.Multiline = true;
            txtDetails.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            txtDetails.Location = new Point(25, 205);
            txtDetails.Size = new System.Drawing.Size(350, 80);
            returnForm.Controls.Add(txtDetails);

            Button btnSubmitReturn = new Button();
            btnSubmitReturn.Text = "Submit Return Request";
            btnSubmitReturn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSubmitReturn.BackColor = Color.FromArgb(40, 167, 69);
            btnSubmitReturn.ForeColor = Color.White;
            btnSubmitReturn.FlatStyle = FlatStyle.Flat;
            btnSubmitReturn.FlatAppearance.BorderSize = 0;
            btnSubmitReturn.Location = new Point(25, 305);
            btnSubmitReturn.Size = new System.Drawing.Size(350, 40);
            btnSubmitReturn.Cursor = Cursors.Hand;

            btnSubmitReturn.Click += (s, e) => {
                string reason = cmbReason.SelectedItem.ToString() + (!string.IsNullOrWhiteSpace(txtDetails.Text) ? " - " + txtDetails.Text.Trim() : "");
                string returnId = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        
                        string query = @"INSERT INTO Returns (ReturnID, TransID, Customer, Products, PurchaseDate, Reason, Quantity, RefundAmount, Status, Cashier) 
                                 VALUES (@returnId, @transId, @customer, @products, @date, @reason, @qty, @amount, 'Pending', 'Online System')";

                        using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@returnId", returnId);
                            cmd.Parameters.AddWithValue("@transId", ordId);
                            cmd.Parameters.AddWithValue("@customer", username);
                            cmd.Parameters.AddWithValue("@products", ordItems);
                            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                            cmd.Parameters.AddWithValue("@reason", reason);
                            cmd.Parameters.AddWithValue("@qty", 1); 
                            cmd.Parameters.AddWithValue("@amount", ordTotal);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Ang iyong Return/Refund request ay matagumpay na naisumite! Maaari mong makita ang status nito rito.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    returnForm.Close();
                    LoadCustomerOrders(); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error sa pag-submit ng return request: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            returnForm.Controls.Add(btnSubmitReturn);
            returnForm.ShowDialog();
        }

        private void ShowOrderDetailsPopup(string ordId, string ordDate, string ordItems, string ordTotal, string payMethod, string ordStatus, byte[] imageBytes)
        {
            Form detailsForm = new Form();
            detailsForm.Size = new System.Drawing.Size(460, 430);
            detailsForm.StartPosition = FormStartPosition.CenterParent;
            detailsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            detailsForm.MaximizeBox = false;
            detailsForm.MinimizeBox = false;
            detailsForm.Text = $"Order Details - {ordId}";
            detailsForm.BackColor = Color.White;

            Label lblH = new Label();
            lblH.Text = "Order Summary Details";
            lblH.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblH.Location = new Point(25, 20);
            lblH.AutoSize = true;
            detailsForm.Controls.Add(lblH);

            PictureBox picPop = new PictureBox();
            picPop.Size = new System.Drawing.Size(80, 80);
            picPop.Location = new Point(25, 60);
            picPop.SizeMode = PictureBoxSizeMode.Zoom;
            picPop.BackColor = Color.FromArgb(240, 240, 240);
            if (imageBytes != null && imageBytes.Length > 0)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        picPop.Image = Image.FromStream(ms);
                    }
                }
                catch { }
            }
            detailsForm.Controls.Add(picPop);

            Label lblInfo = new Label();
            lblInfo.Text = $"Order ID: {ordId}\nDate Ordered: {ordDate}\nStatus: {ordStatus}\nPayment Method: {payMethod}";
            lblInfo.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblInfo.Location = new Point(120, 60);
            lblInfo.Size = new System.Drawing.Size(300, 80);
            detailsForm.Controls.Add(lblInfo);

            Label lblItemsTitle = new Label();
            lblItemsTitle.Text = "Ordered Items:";
            lblItemsTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblItemsTitle.Location = new Point(25, 155);
            lblItemsTitle.AutoSize = true;
            detailsForm.Controls.Add(lblItemsTitle);

            TextBox txtItemsBox = new TextBox();
            txtItemsBox.Multiline = true;
            txtItemsBox.ReadOnly = true;
            txtItemsBox.Text = ordItems.Replace(",", "\r\n");
            txtItemsBox.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            txtItemsBox.Location = new Point(25, 180);
            txtItemsBox.Size = new System.Drawing.Size(395, 90);
            detailsForm.Controls.Add(txtItemsBox);

            Label lblTotalAmt = new Label();
            lblTotalAmt.Text = $"Total Amount: {ordTotal}";
            lblTotalAmt.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTotalAmt.ForeColor = Color.FromArgb(0, 122, 204);
            lblTotalAmt.Location = new Point(25, 290);
            lblTotalAmt.AutoSize = true;
            detailsForm.Controls.Add(lblTotalAmt);

            Button btnClosePop = new Button();
            btnClosePop.Text = currentLanguage == "Filipino" ? "Isara" : currentLanguage == "Cebuano" ? "Sirad-i" : "Close";
            btnClosePop.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnClosePop.BackColor = Color.FromArgb(50, 50, 50);
            btnClosePop.ForeColor = Color.White;
            btnClosePop.FlatStyle = FlatStyle.Flat;
            btnClosePop.FlatAppearance.BorderSize = 0;
            btnClosePop.Location = new Point(25, 330);
            btnClosePop.Size = new System.Drawing.Size(395, 35);
            btnClosePop.Cursor = Cursors.Hand;
            btnClosePop.Click += (s, e) => detailsForm.Close();
            detailsForm.Controls.Add(btnClosePop);

            detailsForm.ShowDialog();
        }

        private void CancelCustomerOrder(string ordId, string ordItems)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string updateOrderQuery = "UPDATE Orders SET OrdStatus = 'Cancelled' WHERE OrdID = @ordId";
                    using (SQLiteCommand cmd = new SQLiteCommand(updateOrderQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ordId", ordId);
                        cmd.ExecuteNonQuery();
                    }

                    RestoreStockFromItems(ordItems, conn);
                }

                MessageBox.Show($"Order {ordId} has been successfully cancelled, and inventory stocks have been restored.", "Order Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadCustomerOrders();
                LoadShopProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cancelling order: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupWishlistContent()
        {
            tabWishlist.BackColor = Color.FromArgb(245, 247, 250);

            lblWishlistTitle = new Label();
            lblWishlistTitle.Text = "My Wishlist";
            lblWishlistTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblWishlistTitle.Location = new Point(30, 25);
            lblWishlistTitle.AutoSize = true;
            tabWishlist.Controls.Add(lblWishlistTitle);

            flowWishlistProducts = new BufferedFlowLayoutPanel();
            flowWishlistProducts.Location = new Point(30, 75);
            flowWishlistProducts.Size = new System.Drawing.Size(975, 565);
            flowWishlistProducts.AutoScroll = true;
            flowWishlistProducts.BackColor = Color.FromArgb(245, 247, 250);
            tabWishlist.Controls.Add(flowWishlistProducts);
        }

        private void RefreshWishlistDisplay()
        {
            if (flowWishlistProducts == null) return;
            flowWishlistProducts.Controls.Clear();

            if (customerWishlist.Count == 0)
            {
                Label lblEmpty = new Label();
                lblEmpty.Text = currentLanguage == "Filipino" ? "Walang laman ang iyong wishlist. I-heart react ang mga produkto sa Shop tab!" :
                                 currentLanguage == "Cebuano" ? "Wala nay sulod ang imong wishlist. I-heart react ang mga produkto sa Shop tab!" :
                                 "Your wishlist is empty. Heart react products from the Shop tab to save them here!";
                lblEmpty.Font = new Font("Segoe UI", 12, FontStyle.Italic);
                lblEmpty.ForeColor = Color.Gray;
                lblEmpty.AutoSize = true;
                flowWishlistProducts.Controls.Add(lblEmpty);
                return;
            }

            foreach (var wishItem in customerWishlist)
            {
                Panel card = new BufferedPanel();
                card.Size = new System.Drawing.Size(220, 355);
                card.BackColor = Color.White;
                card.Margin = new Padding(10);

                PictureBox picBox = new PictureBox();
                picBox.Size = new System.Drawing.Size(200, 100);
                picBox.Location = new Point(10, 10);
                picBox.SizeMode = PictureBoxSizeMode.Zoom;
                picBox.BackColor = Color.FromArgb(240, 240, 240);

                if (wishItem.ImageBytes != null && wishItem.ImageBytes.Length > 0)
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(wishItem.ImageBytes))
                        {
                            picBox.Image = Image.FromStream(ms);
                        }
                    }
                    catch { }
                }
                card.Controls.Add(picBox);

                Label lblCat = new Label();
                lblCat.Text = wishItem.Category.ToUpper();
                lblCat.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                lblCat.ForeColor = Color.Gray;
                lblCat.Location = new Point(10, 115);
                lblCat.Size = new System.Drawing.Size(160, 16);
                card.Controls.Add(lblCat);

                Button btnRemoveWishlist = new Button();
                btnRemoveWishlist.Text = "♥";
                btnRemoveWishlist.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                btnRemoveWishlist.ForeColor = Color.Red;
                btnRemoveWishlist.BackColor = Color.Transparent;
                btnRemoveWishlist.FlatStyle = FlatStyle.Flat;
                btnRemoveWishlist.FlatAppearance.BorderSize = 0;
                btnRemoveWishlist.Location = new Point(175, 112);
                btnRemoveWishlist.Size = new System.Drawing.Size(35, 25);
                btnRemoveWishlist.Cursor = Cursors.Hand;
                btnRemoveWishlist.Click += (s, e) => {
                    customerWishlist.Remove(wishItem);
                    SaveWishlistToDatabase();
                    RefreshWishlistDisplay();
                    MessageBox.Show($"'{wishItem.ProductName}' has been removed from your Wishlist.", "Wishlist Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                card.Controls.Add(btnRemoveWishlist);

                Label lblName = new Label();
                lblName.Text = wishItem.ProductName;
                lblName.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblName.ForeColor = Color.FromArgb(40, 40, 40);
                lblName.Location = new Point(10, 133);
                lblName.Size = new System.Drawing.Size(200, 35);
                card.Controls.Add(lblName);

                Label lblSizeText = new Label();
                lblSizeText.Text = currentLanguage == "Filipino" ? "Sukat:" : currentLanguage == "Cebuano" ? "Gidak-on:" : "Size:";
                lblSizeText.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                lblSizeText.Location = new Point(10, 173);
                lblSizeText.Size = new System.Drawing.Size(45, 22);
                card.Controls.Add(lblSizeText);

                ComboBox cmbSizes = new ComboBox();
                cmbSizes.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                cmbSizes.DropDownStyle = ComboBoxStyle.DropDownList;
                string[] standardSizes = { "S", "M", "L", "XL", "XXL" };
                foreach (var sz in standardSizes)
                {
                    cmbSizes.Items.Add(sz);
                }
                cmbSizes.Location = new Point(55, 171);
                cmbSizes.Size = new System.Drawing.Size(155, 25);
                card.Controls.Add(cmbSizes);

                Label lblStock = new Label();
                lblStock.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                lblStock.Location = new Point(10, 201);
                lblStock.Size = new System.Drawing.Size(200, 18);
                card.Controls.Add(lblStock);

                Label lblPrice = new Label();
                lblPrice.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                lblPrice.ForeColor = Color.FromArgb(0, 122, 204);
                lblPrice.Location = new Point(10, 220);
                lblPrice.AutoSize = true;
                card.Controls.Add(lblPrice);

                cmbSizes.SelectedIndexChanged += (s, e) => {
                    string selectedSize = cmbSizes.SelectedItem.ToString();
                    if (wishItem.Variants.ContainsKey(selectedSize))
                    {
                        var variant = wishItem.Variants[selectedSize];
                        lblStock.Text = $"Stock: {variant.Stock}";
                        lblStock.ForeColor = variant.Stock > 0 ? Color.DarkGreen : Color.Red;
                        decimal convertedPrice = variant.Price * currencyMultiplier;
                        lblPrice.Text = $"{currencySymbol}{convertedPrice:N2}";
                    }
                    else
                    {
                        lblStock.Text = currentLanguage == "Filipino" ? "Stock: 0 (Hindi Available)" : currentLanguage == "Cebuano" ? "Stock: 0 (Walay Stock)" : "Stock: 0 (Not Available)";
                        lblStock.ForeColor = Color.Red;
                        lblPrice.Text = $"{currencySymbol}0.00";
                    }
                };

                if (cmbSizes.Items.Count > 0)
                {
                    int defaultIndex = 0;
                    for (int i = 0; i < cmbSizes.Items.Count; i++)
                    {
                        if (wishItem.Variants.ContainsKey(cmbSizes.Items[i].ToString()))
                        {
                            defaultIndex = i;
                            break;
                        }
                    }
                    cmbSizes.SelectedIndex = defaultIndex;
                }

                Button btnAddToCart = new Button();
                btnAddToCart.Text = currentLanguage == "Filipino" ? "Ilagay sa Cart" : currentLanguage == "Cebuano" ? "Ibutang sa Cart" : "Add to Cart";
                btnAddToCart.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                btnAddToCart.BackColor = Color.FromArgb(40, 167, 69);
                btnAddToCart.ForeColor = Color.White;
                btnAddToCart.FlatStyle = FlatStyle.Flat;
                btnAddToCart.FlatAppearance.BorderSize = 0;
                btnAddToCart.Location = new Point(10, 250);
                btnAddToCart.Size = new System.Drawing.Size(200, 32);
                btnAddToCart.Cursor = Cursors.Hand;

                btnAddToCart.Click += (s, e) => {
                    string selectedSize = cmbSizes.SelectedItem.ToString();
                    if (!wishItem.Variants.ContainsKey(selectedSize))
                    {
                        MessageBox.Show("Selected size is not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var variant = wishItem.Variants[selectedSize];
                    if (variant.Stock <= 0)
                    {
                        MessageBox.Show("Sorry, this item size is currently out of stock.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var existingItem = customerCart.Find(c => c.ProductName == wishItem.ProductName && c.Size == selectedSize);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += 1;
                    }
                    else
                    {
                        customerCart.Add(new CartItem
                        {
                            ProductName = wishItem.ProductName,
                            Size = selectedSize,
                            Price = variant.Price,
                            Quantity = 1,
                            ImageBytes = wishItem.ImageBytes
                        });
                    }

                    SaveCartToDatabase();
                    MessageBox.Show($"'{wishItem.ProductName} ({selectedSize})' has been successfully added to your Cart!", "Added to Cart", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                card.Controls.Add(btnAddToCart);

                Button btnRemoveText = new Button();
                btnRemoveText.Text = currentLanguage == "Filipino" ? "Tanggalin sa Wishlist" : currentLanguage == "Cebuano" ? "Tangtangon sa Wishlist" : "Remove from Wishlist";
                btnRemoveText.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                btnRemoveText.BackColor = Color.IndianRed;
                btnRemoveText.ForeColor = Color.White;
                btnRemoveText.FlatStyle = FlatStyle.Flat;
                btnRemoveText.FlatAppearance.BorderSize = 0;
                btnRemoveText.Location = new Point(10, 288);
                btnRemoveText.Size = new System.Drawing.Size(200, 32);
                btnRemoveText.Cursor = Cursors.Hand;

                btnRemoveText.Click += (s, e) => {
                    customerWishlist.Remove(wishItem);
                    SaveWishlistToDatabase();
                    RefreshWishlistDisplay();
                    MessageBox.Show($"'{wishItem.ProductName}' has been removed from your Wishlist.", "Wishlist Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                card.Controls.Add(btnRemoveText);

                flowWishlistProducts.Controls.Add(card);
            }
        }

        private void SetupMyProfileContent()
        {
            tabMyProfile.BackColor = Color.FromArgb(245, 247, 250);

            lblProfileTitle = new Label();
            lblProfileTitle.Text = "My Profile";
            lblProfileTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblProfileTitle.Location = new Point(30, 25);
            lblProfileTitle.AutoSize = true;
            tabMyProfile.Controls.Add(lblProfileTitle);

            Panel pnlProfileCard = new BufferedPanel();
            pnlProfileCard.Location = new Point(30, 75);
            pnlProfileCard.Size = new System.Drawing.Size(975, 560);
            pnlProfileCard.BackColor = Color.White;
            tabMyProfile.Controls.Add(pnlProfileCard);

            picCustomerProfile = new PictureBox();
            picCustomerProfile.Location = new Point(40, 40);
            picCustomerProfile.Size = new System.Drawing.Size(160, 160);
            picCustomerProfile.SizeMode = PictureBoxSizeMode.Zoom;
            picCustomerProfile.BackColor = Color.FromArgb(235, 235, 235);
            pnlProfileCard.Controls.Add(picCustomerProfile);

            btnUploadPic = new Button();
            btnUploadPic.Text = currentLanguage == "Filipino" ? "Pumili ng Larawan" : currentLanguage == "Cebuano" ? "Pilia ang Hulagway" : "Select Picture";
            btnUploadPic.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btnUploadPic.BackColor = Color.FromArgb(100, 100, 100);
            btnUploadPic.ForeColor = Color.White;
            btnUploadPic.FlatStyle = FlatStyle.Flat;
            btnUploadPic.FlatAppearance.BorderSize = 0;
            btnUploadPic.Location = new Point(40, 210);
            btnUploadPic.Size = new System.Drawing.Size(160, 32);
            btnUploadPic.Cursor = Cursors.Hand;
            btnUploadPic.Enabled = false;
            btnUploadPic.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select Profile Picture";
                    ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            picCustomerProfile.Image = Image.FromFile(ofd.FileName);
                            using (MemoryStream ms = new MemoryStream())
                            {
                                picCustomerProfile.Image.Save(ms, picCustomerProfile.Image.RawFormat);
                                currentProfileImageBytes = ms.ToArray();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            pnlProfileCard.Controls.Add(btnUploadPic);

            int labelX = 240;
            int inputX = 400;
            int startPosY = 45;
            int gapY = 55;

            lblNameLabel = new Label();
            lblNameLabel.Text = "Full Name:";
            lblNameLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblNameLabel.Location = new Point(labelX, startPosY);
            lblNameLabel.AutoSize = true;
            pnlProfileCard.Controls.Add(lblNameLabel);

            txtProfileName = new TextBox();
            txtProfileName.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtProfileName.Location = new Point(inputX, startPosY - 3);
            txtProfileName.Size = new System.Drawing.Size(350, 28);
            txtProfileName.ReadOnly = true;
            pnlProfileCard.Controls.Add(txtProfileName);

            lblEmailLabel = new Label();
            lblEmailLabel.Text = "Email Address:";
            lblEmailLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblEmailLabel.Location = new Point(labelX, startPosY + gapY);
            lblEmailLabel.AutoSize = true;
            pnlProfileCard.Controls.Add(lblEmailLabel);

            txtProfileEmail = new TextBox();
            txtProfileEmail.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtProfileEmail.Location = new Point(inputX, startPosY + gapY - 3);
            txtProfileEmail.Size = new System.Drawing.Size(350, 28);
            txtProfileEmail.ReadOnly = true;
            pnlProfileCard.Controls.Add(txtProfileEmail);

            lblContactLabel = new Label();
            lblContactLabel.Text = "Contact Number:";
            lblContactLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblContactLabel.Location = new Point(labelX, startPosY + (gapY * 2));
            lblContactLabel.AutoSize = true;
            pnlProfileCard.Controls.Add(lblContactLabel);

            txtProfileContact = new TextBox();
            txtProfileContact.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtProfileContact.Location = new Point(inputX, startPosY + (gapY * 2) - 3);
            txtProfileContact.Size = new System.Drawing.Size(350, 28);
            txtProfileContact.ReadOnly = true;
            pnlProfileCard.Controls.Add(txtProfileContact);

            lblAddressLabel = new Label();
            lblAddressLabel.Text = "Shipping Address:";
            lblAddressLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblAddressLabel.Location = new Point(labelX, startPosY + (gapY * 3));
            lblAddressLabel.AutoSize = true;
            pnlProfileCard.Controls.Add(lblAddressLabel);

            txtProfileAddress = new TextBox();
            txtProfileAddress.Multiline = true;
            txtProfileAddress.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtProfileAddress.Location = new Point(inputX, startPosY + (gapY * 3) - 3);
            txtProfileAddress.Size = new System.Drawing.Size(350, 75);
            txtProfileAddress.ReadOnly = true;
            pnlProfileCard.Controls.Add(txtProfileAddress);

            btnEditProfile = new Button();
            btnEditProfile.Text = currentLanguage == "Filipino" ? "I-edit ang Profile" : currentLanguage == "Cebuano" ? "I-edit ang Profile" : "Edit Profile";
            btnEditProfile.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnEditProfile.BackColor = Color.FromArgb(0, 122, 204);
            btnEditProfile.ForeColor = Color.White;
            btnEditProfile.FlatStyle = FlatStyle.Flat;
            btnEditProfile.FlatAppearance.BorderSize = 0;
            btnEditProfile.Location = new Point(400, 315);
            btnEditProfile.Size = new System.Drawing.Size(165, 40);
            btnEditProfile.Cursor = Cursors.Hand;
            btnEditProfile.Click += (s, e) => {
                isEditingProfile = true;
                txtProfileName.ReadOnly = false;
                txtProfileEmail.ReadOnly = false;
                txtProfileContact.ReadOnly = false;
                txtProfileAddress.ReadOnly = false;

                btnUploadPic.Enabled = true;
                btnEditProfile.Visible = false;
                btnSaveProfile.Visible = true;
            };
            pnlProfileCard.Controls.Add(btnEditProfile);

            btnSaveProfile = new Button();
            btnSaveProfile.Text = currentLanguage == "Filipino" ? "I-save ang mga Pagbabago" : currentLanguage == "Cebuano" ? "I-save ang mga Pagbag-o" : "Save Changes";
            btnSaveProfile.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSaveProfile.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveProfile.ForeColor = Color.White;
            btnSaveProfile.FlatStyle = FlatStyle.Flat;
            btnSaveProfile.FlatAppearance.BorderSize = 0;
            btnSaveProfile.Location = new Point(400, 315);
            btnSaveProfile.Size = new System.Drawing.Size(165, 40);
            btnSaveProfile.Cursor = Cursors.Hand;
            btnSaveProfile.Visible = false;
            btnSaveProfile.Click += (s, e) => {
                SaveCustomerProfileData();
            };
            pnlProfileCard.Controls.Add(btnSaveProfile);

            btnChangePassword = new Button();
            btnChangePassword.Text = currentLanguage == "Filipino" ? "Palitan ang Password" : currentLanguage == "Cebuano" ? "Usba ang Password" : "Change Password";
            btnChangePassword.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnChangePassword.BackColor = Color.FromArgb(108, 117, 125);
            btnChangePassword.ForeColor = Color.White;
            btnChangePassword.FlatStyle = FlatStyle.Flat;
            btnChangePassword.FlatAppearance.BorderSize = 0;
            btnChangePassword.Location = new Point(585, 315);
            btnChangePassword.Size = new System.Drawing.Size(165, 40);
            btnChangePassword.Cursor = Cursors.Hand;
            btnChangePassword.Click += (s, e) => {
                ShowChangePasswordPopup();
            };
            pnlProfileCard.Controls.Add(btnChangePassword);
        }

        private void LoadCustomerProfileData()
        {
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Customers WHERE CustName = @username LIMIT 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtProfileName.Text = reader["CustName"] != DBNull.Value ? reader["CustName"].ToString() : username;
                                txtProfileEmail.Text = reader["CustEmail"] != DBNull.Value ? reader["CustEmail"].ToString() : "";
                                txtProfileContact.Text = reader["CustContact"] != DBNull.Value ? reader["CustContact"].ToString() : "";
                                txtProfileAddress.Text = reader["CustAddress"] != DBNull.Value ? reader["CustAddress"].ToString() : "";

                                try
                                {
                                    if (reader["CustProfilePic"] != DBNull.Value)
                                    {
                                        currentProfileImageBytes = (byte[])reader["CustProfilePic"];
                                        using (MemoryStream ms = new MemoryStream(currentProfileImageBytes))
                                        {
                                            picCustomerProfile.Image = Image.FromStream(ms);
                                        }
                                    }
                                    else
                                    {
                                        picCustomerProfile.Image = null;
                                        currentProfileImageBytes = null;
                                    }
                                }
                                catch { }
                            }
                            else
                            {
                                txtProfileName.Text = username;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading profile: " + ex.Message);
                txtProfileName.Text = username;
            }

            isEditingProfile = false;
            txtProfileName.ReadOnly = true;
            txtProfileEmail.ReadOnly = true;
            txtProfileContact.ReadOnly = true;
            txtProfileAddress.ReadOnly = true;
            if (btnUploadPic != null) btnUploadPic.Enabled = false;
            if (btnEditProfile != null) btnEditProfile.Visible = true;
            if (btnSaveProfile != null) btnSaveProfile.Visible = false;
        }

        private void SaveCustomerProfileData()
        {
            string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "Guest" : Form1.CurrentUsername;
            string newName = txtProfileName.Text.Trim();
            string newEmail = txtProfileEmail.Text.Trim();
            string newContact = txtProfileContact.Text.Trim();
            string newAddress = txtProfileAddress.Text.Trim();

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE Customers SET CustName = @name, CustEmail = @email, CustContact = @contact, CustAddress = @address, CustProfilePic = @pic WHERE CustName = @username";

                    using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", newName);
                        cmd.Parameters.AddWithValue("@email", newEmail);
                        cmd.Parameters.AddWithValue("@contact", newContact);
                        cmd.Parameters.AddWithValue("@address", newAddress);

                        if (currentProfileImageBytes != null)
                            cmd.Parameters.AddWithValue("@pic", currentProfileImageBytes);
                        else
                            cmd.Parameters.AddWithValue("@pic", DBNull.Value);

                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCustomerProfileData();
                LoadDashboardProfilePicture(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving profile: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowChangePasswordPopup()
        {
            Form pwdForm = new Form();
            pwdForm.Size = new System.Drawing.Size(380, 310);
            pwdForm.StartPosition = FormStartPosition.CenterParent;
            pwdForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            pwdForm.MaximizeBox = false;
            pwdForm.MinimizeBox = false;
            pwdForm.Text = "Change Password";
            pwdForm.BackColor = Color.White;

            Label lblH = new Label();
            lblH.Text = "Update Account Password";
            lblH.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblH.Location = new Point(25, 20);
            lblH.AutoSize = true;
            pwdForm.Controls.Add(lblH);

            Label lblOld = new Label();
            lblOld.Text = "Current Password:";
            lblOld.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblOld.Location = new Point(25, 60);
            lblOld.AutoSize = true;
            pwdForm.Controls.Add(lblOld);

            TextBox txtOld = new TextBox();
            txtOld.PasswordChar = '•';
            txtOld.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtOld.Location = new Point(25, 82);
            txtOld.Size = new System.Drawing.Size(310, 25);
            pwdForm.Controls.Add(txtOld);

            Label lblNew = new Label();
            lblNew.Text = "New Password:";
            lblNew.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblNew.Location = new Point(25, 115);
            lblNew.AutoSize = true;
            pwdForm.Controls.Add(lblNew);

            TextBox txtNew = new TextBox();
            txtNew.PasswordChar = '•';
            txtNew.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtNew.Location = new Point(25, 137);
            txtNew.Size = new System.Drawing.Size(310, 25);
            pwdForm.Controls.Add(txtNew);

            Button btnSubmitPwd = new Button();
            btnSubmitPwd.Text = "Update Password";
            btnSubmitPwd.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSubmitPwd.BackColor = Color.FromArgb(40, 167, 69);
            btnSubmitPwd.ForeColor = Color.White;
            btnSubmitPwd.FlatStyle = FlatStyle.Flat;
            btnSubmitPwd.FlatAppearance.BorderSize = 0;
            btnSubmitPwd.Location = new Point(25, 190);
            btnSubmitPwd.Size = new System.Drawing.Size(310, 38);
            btnSubmitPwd.Cursor = Cursors.Hand;

            btnSubmitPwd.Click += (s, e) => {
                string oldPass = txtOld.Text.Trim();
                string newPass = txtNew.Text.Trim();
                string username = string.IsNullOrEmpty(Form1.CurrentUsername) ? "" : Form1.CurrentUsername;

                if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
                {
                    MessageBox.Show("Please fill in both fields.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();
                        string checkQuery = "SELECT Password FROM Users WHERE Username = @username";
                        using (SQLiteCommand cmdCheck = new SQLiteCommand(checkQuery, conn))
                        {
                            cmdCheck.Parameters.AddWithValue("@username", username);
                            object res = cmdCheck.ExecuteScalar();

                            if (res != null && res.ToString() == oldPass)
                            {
                                string updatePwdQuery = "UPDATE Users SET Password = @newPass WHERE Username = @username";
                                using (SQLiteCommand cmdUpdate = new SQLiteCommand(updatePwdQuery, conn))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@newPass", newPass);
                                    cmdUpdate.Parameters.AddWithValue("@username", username);
                                    cmdUpdate.ExecuteNonQuery();
                                }

                                MessageBox.Show("Password changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                pwdForm.Close();
                            }
                            else
                            {
                                MessageBox.Show("Current password is incorrect.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating password: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pwdForm.Controls.Add(btnSubmitPwd);
            pwdForm.ShowDialog();
        }

        private void SetupSettingsContent()
        {
            tabSettings.BackColor = Color.FromArgb(245, 247, 250);

            lblSettingsTitle = new Label();
            lblSettingsTitle.Text = "Account Settings & Preferences";
            lblSettingsTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblSettingsTitle.Location = new Point(30, 25);
            lblSettingsTitle.AutoSize = true;
            tabSettings.Controls.Add(lblSettingsTitle);

            Panel pnlSettingsCard = new BufferedPanel();
            pnlSettingsCard.Location = new Point(30, 75);
            pnlSettingsCard.Size = new System.Drawing.Size(975, 560);
            pnlSettingsCard.BackColor = Color.White;
            tabSettings.Controls.Add(pnlSettingsCard);

            Label lblNotifHeader = new Label();
            lblNotifHeader.Text = "Notification Preferences";
            lblNotifHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblNotifHeader.ForeColor = Color.FromArgb(0, 122, 204);
            lblNotifHeader.Location = new Point(30, 25);
            lblNotifHeader.AutoSize = true;
            pnlSettingsCard.Controls.Add(lblNotifHeader);

            chkNotifOrder = new CheckBox();
            chkNotifOrder.Text = "Enable Order Status Updates (Real-time tracking notifications)";
            chkNotifOrder.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            chkNotifOrder.Location = new Point(30, 55);
            chkNotifOrder.AutoSize = true;
            chkNotifOrder.Checked = true;
            chkNotifOrder.CheckedChanged += (s, e) => {
                MessageBox.Show(chkNotifOrder.Checked ? "Order Status notifications enabled." : "Order Status notifications disabled.", "Notification Setting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(chkNotifOrder);

            chkNotifPromo = new CheckBox();
            chkNotifPromo.Text = "Enable Promos & Discount Alerts";
            chkNotifPromo.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            chkNotifPromo.Location = new Point(30, 85);
            chkNotifPromo.AutoSize = true;
            chkNotifPromo.Checked = true;
            chkNotifPromo.CheckedChanged += (s, e) => {
                MessageBox.Show(chkNotifPromo.Checked ? "Promo alerts enabled." : "Promo alerts disabled.", "Notification Setting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(chkNotifPromo);

            chkNotifSMS = new CheckBox();
            chkNotifSMS.Text = "Enable SMS text message alerts";
            chkNotifSMS.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            chkNotifSMS.Location = new Point(30, 115);
            chkNotifSMS.AutoSize = true;
            chkNotifSMS.Checked = false;
            chkNotifSMS.CheckedChanged += (s, e) => {
                MessageBox.Show(chkNotifSMS.Checked ? "SMS alerts enabled." : "SMS alerts disabled.", "Notification Setting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(chkNotifSMS);

            Label lblPrivacyHeader = new Label();
            lblPrivacyHeader.Text = "Privacy Settings";
            lblPrivacyHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblPrivacyHeader.ForeColor = Color.FromArgb(0, 122, 204);
            lblPrivacyHeader.Location = new Point(30, 155);
            lblPrivacyHeader.AutoSize = true;
            pnlSettingsCard.Controls.Add(lblPrivacyHeader);

            chkPrivacyProfile = new CheckBox();
            chkPrivacyProfile.Text = "Make my profile public to other community members";
            chkPrivacyProfile.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            chkPrivacyProfile.Location = new Point(30, 185);
            chkPrivacyProfile.AutoSize = true;
            chkPrivacyProfile.Checked = false;
            chkPrivacyProfile.CheckedChanged += (s, e) => {
                MessageBox.Show(chkPrivacyProfile.Checked ? "Profile is now Public." : "Profile is now Private.", "Privacy Setting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(chkPrivacyProfile);

            chkPrivacySharing = new CheckBox();
            chkPrivacySharing.Text = "Allow data sharing for personalized recommendations";
            chkPrivacySharing.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            chkPrivacySharing.Location = new Point(30, 215);
            chkPrivacySharing.AutoSize = true;
            chkPrivacySharing.Checked = true;
            chkPrivacySharing.CheckedChanged += (s, e) => {
                MessageBox.Show(chkPrivacySharing.Checked ? "Data sharing enabled." : "Data sharing disabled.", "Privacy Setting", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(chkPrivacySharing);

            Label lblLangHeader = new Label();
            lblLangHeader.Text = "Language Preferences";
            lblLangHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblLangHeader.ForeColor = Color.FromArgb(0, 122, 204);
            lblLangHeader.Location = new Point(520, 25);
            lblLangHeader.AutoSize = true;
            pnlSettingsCard.Controls.Add(lblLangHeader);

            cmbLanguageSetting = new ComboBox();
            cmbLanguageSetting.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbLanguageSetting.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguageSetting.Items.AddRange(new object[] { "English (default)", "Filipino", "Cebuano" });
            cmbLanguageSetting.SelectedIndex = 0;
            cmbLanguageSetting.Location = new Point(520, 55);
            cmbLanguageSetting.Size = new System.Drawing.Size(350, 30);
            cmbLanguageSetting.SelectedIndexChanged += (s, e) => {
                if (cmbLanguageSetting.SelectedIndex == 0) currentLanguage = "English";
                else if (cmbLanguageSetting.SelectedIndex == 1) currentLanguage = "Filipino";
                else if (cmbLanguageSetting.SelectedIndex == 2) currentLanguage = "Cebuano";

                ApplyLanguageTranslation();
            };
            pnlSettingsCard.Controls.Add(cmbLanguageSetting);

            Label lblThemeHeader = new Label();
            lblThemeHeader.Text = "System Preferences (Sidebar Theme)";
            lblThemeHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblThemeHeader.ForeColor = Color.FromArgb(0, 122, 204);
            lblThemeHeader.Location = new Point(520, 105);
            lblThemeHeader.AutoSize = true;
            pnlSettingsCard.Controls.Add(lblThemeHeader);

            cmbThemeSetting = new ComboBox();
            cmbThemeSetting.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbThemeSetting.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbThemeSetting.Items.AddRange(new object[] { "Dark (default)", "Beige", "Blue", "Grey" });
            cmbThemeSetting.SelectedIndex = 0;
            cmbThemeSetting.Location = new Point(520, 135);
            cmbThemeSetting.Size = new System.Drawing.Size(350, 30);
            cmbThemeSetting.SelectedIndexChanged += (s, e) => {
                string selectedTheme = cmbThemeSetting.SelectedItem.ToString();
                if (selectedTheme.StartsWith("Dark"))
                {
                    sidePanel.BackColor = Color.FromArgb(30, 30, 30);
                    activeColor = Color.FromArgb(50, 50, 50);
                    hoverColor = Color.FromArgb(70, 70, 70);
                }
                else if (selectedTheme == "Beige")
                {
                    sidePanel.BackColor = Color.FromArgb(210, 180, 140);
                    activeColor = Color.FromArgb(180, 140, 100);
                    hoverColor = Color.FromArgb(195, 160, 120);
                }
                else if (selectedTheme == "Blue")
                {
                    sidePanel.BackColor = Color.FromArgb(20, 60, 100);
                    activeColor = Color.FromArgb(0, 100, 180);
                    hoverColor = Color.FromArgb(10, 120, 200);
                }
                else if (selectedTheme == "Grey")
                {
                    sidePanel.BackColor = Color.FromArgb(100, 100, 100);
                    activeColor = Color.FromArgb(70, 70, 70);
                    hoverColor = Color.FromArgb(85, 85, 85);
                }
                HighlightButton(btnSettings);
            };
            pnlSettingsCard.Controls.Add(cmbThemeSetting);

            Label lblCurrHeader = new Label();
            lblCurrHeader.Text = "Currency Preferences";
            lblCurrHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblCurrHeader.ForeColor = Color.FromArgb(0, 122, 204);
            lblCurrHeader.Location = new Point(520, 185);
            lblCurrHeader.AutoSize = true;
            pnlSettingsCard.Controls.Add(lblCurrHeader);

            cmbCurrencySetting = new ComboBox();
            cmbCurrencySetting.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            cmbCurrencySetting.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCurrencySetting.Items.AddRange(new object[] { "Peso (₱ - default)", "Dollar ($)", "Euro (€)" });
            cmbCurrencySetting.SelectedIndex = 0;
            cmbCurrencySetting.Location = new Point(520, 215);
            cmbCurrencySetting.Size = new System.Drawing.Size(350, 30);
            cmbCurrencySetting.SelectedIndexChanged += (s, e) => {
                if (cmbCurrencySetting.SelectedIndex == 0)
                {
                    currentCurrency = "Peso";
                    currencySymbol = "₱";
                    currencyMultiplier = 1.0m;
                }
                else if (cmbCurrencySetting.SelectedIndex == 1)
                {
                    currentCurrency = "Dollar";
                    currencySymbol = "$";
                    currencyMultiplier = 0.018m;
                }
                else if (cmbCurrencySetting.SelectedIndex == 2)
                {
                    currentCurrency = "Euro";
                    currencySymbol = "€";
                    currencyMultiplier = 0.016m;
                }

                RefreshCartDisplay();
                LoadShopProducts();
                MessageBox.Show($"Currency updated to {currentCurrency} ({currencySymbol}). All prices across the portal have been updated.", "Currency Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(cmbCurrencySetting);

            btnSaveSettings = new Button();
            btnSaveSettings.Text = "Save Settings";
            btnSaveSettings.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSaveSettings.BackColor = Color.FromArgb(40, 167, 69);
            btnSaveSettings.ForeColor = Color.White;
            btnSaveSettings.FlatStyle = FlatStyle.Flat;
            btnSaveSettings.FlatAppearance.BorderSize = 0;
            btnSaveSettings.Location = new Point(520, 275);
            btnSaveSettings.Size = new System.Drawing.Size(165, 40);
            btnSaveSettings.Cursor = Cursors.Hand;
            btnSaveSettings.Click += (s, e) => {
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(btnSaveSettings);

            btnResetSettings = new Button();
            btnResetSettings.Text = "Reset to Default";
            btnResetSettings.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnResetSettings.BackColor = Color.IndianRed;
            btnResetSettings.ForeColor = Color.White;
            btnResetSettings.FlatStyle = FlatStyle.Flat;
            btnResetSettings.FlatAppearance.BorderSize = 0;
            btnResetSettings.Location = new Point(705, 275);
            btnResetSettings.Size = new System.Drawing.Size(165, 40);
            btnResetSettings.Cursor = Cursors.Hand;
            btnResetSettings.Click += (s, e) => {
                chkNotifOrder.Checked = true;
                chkNotifPromo.Checked = true;
                chkNotifSMS.Checked = false;
                chkPrivacyProfile.Checked = false;
                chkPrivacySharing.Checked = true;
                cmbLanguageSetting.SelectedIndex = 0;
                cmbThemeSetting.SelectedIndex = 0;
                cmbCurrencySetting.SelectedIndex = 0;
                MessageBox.Show("Settings have been reset to default values.", "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSettingsCard.Controls.Add(btnResetSettings);
        }

        private void ApplyLanguageTranslation()
        {
            if (currentLanguage == "Filipino")
            {
                if (btnDashboard != null) btnDashboard.Text = "  Dashboard";
                if (btnShop != null) btnShop.Text = "  Shop";
                if (btnCart != null) btnCart.Text = "  Cart";
                if (btnMyOrders != null) btnMyOrders.Text = "  Aking mga Order";
                if (btnWishlist != null) btnWishlist.Text = "  Wishlist";
                if (btnMyProfile != null) btnMyProfile.Text = "  Aking Profile";
                if (btnSettings != null) btnSettings.Text = "  Settings";
                if (btnLogout != null) logoutTextFix(btnLogout, "  Mag-logout");
                if (lblShopTitle != null) lblShopTitle.Text = "MGA PRODUKTO";
                if (btnSearchShop != null) btnSearchShop.Text = "Hanapin";
                if (lblCartTitle != null) lblCartTitle.Text = "Shopping Cart";
                if (lblCartTotalLabel != null) lblCartTotalLabel.Text = "Kabuuang Halaga:";
                if (lblCartPaymentLabel != null) lblCartPaymentLabel.Text = "Paraan ng Pagbabayad:";
                if (btnCartCheckout != null) btnCartCheckout.Text = "I-checkout ang mga Napili";
                if (lblMyOrdersTitle != null) lblMyOrdersTitle.Text = "Aking mga Order";
                if (lblWishlistTitle != null) lblWishlistTitle.Text = "Aking Wishlist";
                if (lblProfileTitle != null) lblProfileTitle.Text = "Aking Profile";
                if (lblNameLabel != null) lblNameLabel.Text = "Buong Pangalan:";
                if (lblEmailLabel != null) lblEmailLabel.Text = "Email Address:";
                if (lblContactLabel != null) lblContactLabel.Text = "Numero ng Telepono:";
                if (lblAddressLabel != null) lblAddressLabel.Text = "Address sa Pagpapadala:";
                if (btnEditProfile != null) btnEditProfile.Text = "I-edit ang Profile";
                if (btnSaveProfile != null) btnSaveProfile.Text = "I-save ang Pagbabago";
                if (btnChangePassword != null) btnChangePassword.Text = "Palitan ang Password";
                if (lblSettingsTitle != null) lblSettingsTitle.Text = "Mga Setting ng Account at Kagustuhan";
                if (btnSaveSettings != null) btnSaveSettings.Text = "I-save ang Setting";
                if (btnResetSettings != null) btnResetSettings.Text = "Ibalik sa Default";
            }
            else if (currentLanguage == "Cebuano")
            {
                if (btnDashboard != null) btnDashboard.Text = "  Dashboard";
                if (btnShop != null) btnShop.Text = "  Shop";
                if (btnCart != null) btnCart.Text = "  Cart";
                if (btnMyOrders != null) btnMyOrders.Text = "  Akong mga Order";
                if (btnWishlist != null) btnWishlist.Text = "  Wishlist";
                if (btnMyProfile != null) btnMyProfile.Text = "  Akong Profile";
                if (btnSettings != null) btnSettings.Text = "  Settings";
                if (btnLogout != null) logoutTextFix(btnLogout, "  Pag-logout");
                if (lblShopTitle != null) lblShopTitle.Text = "MGA PRODUKTO";
                if (btnSearchShop != null) btnSearchShop.Text = "Pangitaa";
                if (lblCartTitle != null) lblCartTitle.Text = "Shopping Cart";
                if (lblCartTotalLabel != null) lblCartTotalLabel.Text = "Kinatibuk-ang Bayrunon:";
                if (lblCartPaymentLabel != null) lblCartPaymentLabel.Text = "Paagi sa Pagbayad:";
                if (btnCartCheckout != null) btnCartCheckout.Text = "I-checkout ang mga Gipili";
                if (lblMyOrdersTitle != null) lblMyOrdersTitle.Text = "Akong mga Order";
                if (lblWishlistTitle != null) lblWishlistTitle.Text = "Akong Wishlist";
                if (lblProfileTitle != null) lblProfileTitle.Text = "Akong Profile";
                if (lblNameLabel != null) lblNameLabel.Text = "Tibuok Ngalan:";
                if (lblEmailLabel != null) lblEmailLabel.Text = "Email Address:";
                if (lblContactLabel != null) lblContactLabel.Text = "Numero sa Telepono:";
                if (lblAddressLabel != null) lblAddressLabel.Text = "Address sa Paghatod:";
                if (btnEditProfile != null) btnEditProfile.Text = "I-edit ang Profile";
                if (btnSaveProfile != null) btnSaveProfile.Text = "I-save ang Pagbag-o";
                if (btnChangePassword != null) btnChangePassword.Text = "Usba ang Password";
                if (lblSettingsTitle != null) lblSettingsTitle.Text = "Mga Setting sa Account ug Gusto";
                if (btnSaveSettings != null) btnSaveSettings.Text = "I-save ang Setting";
                if (btnResetSettings != null) btnResetSettings.Text = "Ibalik sa Default";
            }
            else
            {
                if (btnDashboard != null) btnDashboard.Text = "  Dashboard";
                if (btnShop != null) btnShop.Text = "  Shop";
                if (btnCart != null) btnCart.Text = "  Cart";
                if (btnMyOrders != null) btnMyOrders.Text = "  My Orders";
                if (btnWishlist != null) btnWishlist.Text = "  Wishlist";
                if (btnMyProfile != null) btnMyProfile.Text = "  My Profile";
                if (btnSettings != null) btnSettings.Text = "  Settings";
                if (btnLogout != null) logoutTextFix(btnLogout, "  Logout");
                if (lblShopTitle != null) lblShopTitle.Text = "PRODUCTS";
                if (btnSearchShop != null) btnSearchShop.Text = "Search";
                if (lblCartTitle != null) lblCartTitle.Text = "Shopping Cart";
                if (lblCartTotalLabel != null) lblCartTotalLabel.Text = "Total Amount:";
                if (lblCartPaymentLabel != null) lblCartPaymentLabel.Text = "Payment Method:";
                if (btnCartCheckout != null) btnCartCheckout.Text = "Checkout Selected Items";
                if (lblMyOrdersTitle != null) lblMyOrdersTitle.Text = "My Orders";
                if (lblWishlistTitle != null) lblWishlistTitle.Text = "My Wishlist";
                if (lblProfileTitle != null) lblProfileTitle.Text = "My Profile";
                if (lblNameLabel != null) lblNameLabel.Text = "Full Name:";
                if (lblEmailLabel != null) lblEmailLabel.Text = "Email Address:";
                if (lblContactLabel != null) lblContactLabel.Text = "Contact Number:";
                if (lblAddressLabel != null) lblAddressLabel.Text = "Shipping Address:";
                if (btnEditProfile != null) btnEditProfile.Text = "Edit Profile";
                if (btnSaveProfile != null) btnSaveProfile.Text = "Save Changes";
                if (btnChangePassword != null) btnChangePassword.Text = "Change Password";
                if (lblSettingsTitle != null) lblSettingsTitle.Text = "Account Settings & Preferences";
                if (btnSaveSettings != null) btnSaveSettings.Text = "Save Settings";
                if (btnResetSettings != null) btnResetSettings.Text = "Reset to Default";
            }
        }

        private void logoutTextFix(Button btn, string text)
        {
            btn.Text = text;
        }
    }
}