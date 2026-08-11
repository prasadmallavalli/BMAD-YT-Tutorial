namespace OrderFlow.Presentation;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Button customersButton;
    private System.Windows.Forms.Button productsButton;
    private System.Windows.Forms.Button newOrderButton;
    private System.Windows.Forms.Button ordersButton;
    private System.Windows.Forms.Label notificationsLabel;
    private System.Windows.Forms.DataGridView notificationDataGridView;

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
        customersButton = new System.Windows.Forms.Button();
        productsButton = new System.Windows.Forms.Button();
        newOrderButton = new System.Windows.Forms.Button();
        ordersButton = new System.Windows.Forms.Button();
        notificationsLabel = new System.Windows.Forms.Label();
        notificationDataGridView = new System.Windows.Forms.DataGridView();

        ((System.ComponentModel.ISupportInitialize)notificationDataGridView).BeginInit();
        SuspendLayout();

        // customersButton
        customersButton.Text = "Customers";
        customersButton.Location = new System.Drawing.Point(12, 12);
        customersButton.Size = new System.Drawing.Size(120, 30);
        customersButton.Name = "customersButton";
        customersButton.Click += CustomersButton_Click;

        // productsButton
        productsButton.Text = "Products";
        productsButton.Location = new System.Drawing.Point(144, 12);
        productsButton.Size = new System.Drawing.Size(120, 30);
        productsButton.Name = "productsButton";
        productsButton.Click += ProductsButton_Click;

        // newOrderButton
        newOrderButton.Text = "New Order";
        newOrderButton.Location = new System.Drawing.Point(276, 12);
        newOrderButton.Size = new System.Drawing.Size(120, 30);
        newOrderButton.Name = "newOrderButton";
        newOrderButton.Click += NewOrderButton_Click;

        // ordersButton
        ordersButton.Text = "Orders";
        ordersButton.Location = new System.Drawing.Point(408, 12);
        ordersButton.Size = new System.Drawing.Size(120, 30);
        ordersButton.Name = "ordersButton";
        ordersButton.Click += OrdersButton_Click;

        // notificationsLabel
        notificationsLabel.Text = "Notifications";
        notificationsLabel.Location = new System.Drawing.Point(12, 54);
        notificationsLabel.Size = new System.Drawing.Size(200, 20);
        notificationsLabel.Name = "notificationsLabel";

        // notificationDataGridView
        notificationDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
        notificationDataGridView.Height = 400;
        notificationDataGridView.ReadOnly = true;
        notificationDataGridView.AllowUserToAddRows = false;
        notificationDataGridView.AllowUserToDeleteRows = false;
        notificationDataGridView.AutoGenerateColumns = true;
        notificationDataGridView.MultiSelect = false;
        notificationDataGridView.Name = "notificationDataGridView";

        // MainForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 500);
        Controls.Add(customersButton);
        Controls.Add(productsButton);
        Controls.Add(newOrderButton);
        Controls.Add(ordersButton);
        Controls.Add(notificationsLabel);
        Controls.Add(notificationDataGridView);
        Text = "OrderFlow Desktop";

        ((System.ComponentModel.ISupportInitialize)notificationDataGridView).EndInit();
        ResumeLayout(false);
    }

    #endregion
}
