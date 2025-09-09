using System.ComponentModel;
using System.Reflection;

namespace BookStore
{
    public partial class BookStore : Form
    {
        private DataGridView orderBookGrid;
        private DataGridView tradeBookGrid;
        private DataStore dataStore;
        private IngestWorker ingestWorker;
        private LatencyLogger latencyLogger;
        private System.Windows.Forms.Timer uiRefreshTimer;
        public BookStore()
        {
            InitializeComponent();

            #region UI Setup
            this.Text = "High-Volume Data Simulator";
            this.Size = new Size(1600, 900);
            this.WindowState = FormWindowState.Maximized;

            TableLayoutPanel tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.RowCount = 1;
            TableLayoutPanel tableLayoutPanel2 = tableLayoutPanel1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            #endregion

            #region Create Data Grids
            this.orderBookGrid = this.CreateDataGridView("Order Book", 2000000, 50);
            this.tradeBookGrid = this.CreateDataGridView("Trade Book", 2000000, 50);
            tableLayoutPanel2.Controls.Add((Control)this.orderBookGrid, 0, 0);
            tableLayoutPanel2.Controls.Add((Control)this.tradeBookGrid, 1, 0);
            this.Controls.Add((Control)tableLayoutPanel2);
            #endregion

            #region Setup Logging
            string filePath = $"Latency_Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            this.dataStore = new DataStore();
            this.latencyLogger = new LatencyLogger(filePath);
            #endregion

            #region Setup UI Refresh Timer and Ingest Worker
            this.uiRefreshTimer = new System.Windows.Forms.Timer();
            this.uiRefreshTimer.Interval = 16 /*0x10*/;
            this.uiRefreshTimer.Tick += (EventHandler)((sender, e) =>
            {
                this.orderBookGrid.Invalidate();
                this.tradeBookGrid.Invalidate();
            });
            #endregion

            #region Establish connection to data simulator
            this.ingestWorker = new IngestWorker(this.dataStore, this.latencyLogger);
            this.ingestWorker.Start();
            this.uiRefreshTimer.Start();
            #endregion
        }

        DataGridView CreateDataGridView(string title, int rowCount, int columnCount)
        {
            #region Create and Configure DataGridView
            DataGridView dataGridView1 = new DataGridView();
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font(this.Font, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;


            DataGridView dataGridView2 = dataGridView1;
            dataGridView2.VirtualMode = true;
            dataGridView2.RowCount = rowCount;
            dataGridView2.ColumnCount = columnCount;
            dataGridView2.AllowUserToResizeColumns = true;
            dataGridView2.AllowUserToResizeRows = false;
            dataGridView2.RowHeadersVisible = false;
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            #endregion

            #region Set Grid Columns
            for (int index = 0; index < columnCount; ++index)
                dataGridView2.Columns[index].Name = $"Column {index + 1}";
            dataGridView2.Columns[0].Name = "ID";
            dataGridView2.Columns[1].Name = "Symbol";
            dataGridView2.Columns[2].Name = "Side";
            dataGridView2.Columns[3].Name = "Price";
            dataGridView2.Columns[4].Name = "Quantity";
            dataGridView2.Columns[0].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView2.Columns[1].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView2.CellValueNeeded += new DataGridViewCellValueEventHandler(this.grid_CellValueNeeded);
            #endregion

            #region To avoid flickering
            BookStore.SetDoubleBuffered((Control)dataGridView2, true);
            #endregion

            #region UI Settings
            Panel panel1 = new Panel();
            panel1.Dock = DockStyle.Fill;
            Panel panel2 = panel1;

            Label label1 = new Label();
            label1.Text = title;
            label1.Font = new Font("Arial", 12f, FontStyle.Bold);
            label1.Dock = DockStyle.Top;
            label1.TextAlign = ContentAlignment.MiddleCenter;

            Label label2 = label1;
            panel2.Controls.Add((Control)label2);
            panel2.Controls.Add((Control)dataGridView2);
            #endregion

            return dataGridView2;
        }

        private void grid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= (sender as DataGridView).RowCount)
                return;
            List<object> data = this.dataStore.GetData(sender == this.orderBookGrid ? "order" : "trade", e.RowIndex);
            if (data != null && e.ColumnIndex < data.Count)
                e.Value = data[e.ColumnIndex];
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.ingestWorker.Stop();
            this.latencyLogger.Dispose();
            base.OnClosing(e);
        }

        private static void SetDoubleBuffered(Control control, bool isDoubleBuffered)
        {
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue((object)control, (object)isDoubleBuffered, (object[])null);
        }
    }
}
