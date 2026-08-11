namespace OrderFlow.Presentation;

partial class OrderListForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.DataGridView dataGridView;
    private System.Windows.Forms.Button viewButton;
    private System.Windows.Forms.Button refreshButton;
    private System.Windows.Forms.Panel buttonPanel;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        dataGridView = new System.Windows.Forms.DataGridView();
        viewButton = new System.Windows.Forms.Button();
        refreshButton = new System.Windows.Forms.Button();
        buttonPanel = new System.Windows.Forms.Panel();

        ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
        buttonPanel.SuspendLayout();
        SuspendLayout();

        // dataGridView
        dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
        dataGridView.ReadOnly = true;
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        dataGridView.MultiSelect = false;
        dataGridView.AutoGenerateColumns = true;
        dataGridView.Name = "dataGridView";
        dataGridView.SelectionChanged += DataGridView_SelectionChanged;

        // viewButton
        viewButton.Text = "View";
        viewButton.Location = new System.Drawing.Point(8, 8);
        viewButton.Size = new System.Drawing.Size(90, 30);
        viewButton.Name = "viewButton";
        viewButton.Enabled = false;
        viewButton.Click += ViewButton_Click;

        // refreshButton
        refreshButton.Text = "Refresh";
        refreshButton.Location = new System.Drawing.Point(106, 8);
        refreshButton.Size = new System.Drawing.Size(90, 30);
        refreshButton.Name = "refreshButton";
        refreshButton.Click += RefreshButton_Click;

        // buttonPanel
        buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
        buttonPanel.Height = 46;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Controls.Add(viewButton);
        buttonPanel.Controls.Add(refreshButton);

        // OrderListForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(700, 450);
        Controls.Add(dataGridView);
        Controls.Add(buttonPanel);
        Name = "OrderListForm";
        Text = "Orders";
        Load += OrderListForm_Load;

        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
        buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
