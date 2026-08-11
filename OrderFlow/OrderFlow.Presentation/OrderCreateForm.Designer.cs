namespace OrderFlow.Presentation;

partial class OrderCreateForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label customerLabel;
    private System.Windows.Forms.ComboBox customerComboBox;
    private System.Windows.Forms.Label orderTypeLabel;
    private System.Windows.Forms.ComboBox orderTypeComboBox;
    private System.Windows.Forms.Label productLabel;
    private System.Windows.Forms.ComboBox productComboBox;
    private System.Windows.Forms.Label quantityLabel;
    private System.Windows.Forms.NumericUpDown quantityNumericUpDown;
    private System.Windows.Forms.Button addItemButton;
    private System.Windows.Forms.DataGridView lineItemsDataGridView;
    private System.Windows.Forms.Button removeItemButton;
    private System.Windows.Forms.Button confirmButton;
    private System.Windows.Forms.Button cancelButton;

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
        customerLabel = new System.Windows.Forms.Label();
        customerComboBox = new System.Windows.Forms.ComboBox();
        orderTypeLabel = new System.Windows.Forms.Label();
        orderTypeComboBox = new System.Windows.Forms.ComboBox();
        productLabel = new System.Windows.Forms.Label();
        productComboBox = new System.Windows.Forms.ComboBox();
        quantityLabel = new System.Windows.Forms.Label();
        quantityNumericUpDown = new System.Windows.Forms.NumericUpDown();
        addItemButton = new System.Windows.Forms.Button();
        lineItemsDataGridView = new System.Windows.Forms.DataGridView();
        removeItemButton = new System.Windows.Forms.Button();
        confirmButton = new System.Windows.Forms.Button();
        cancelButton = new System.Windows.Forms.Button();

        ((System.ComponentModel.ISupportInitialize)quantityNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)lineItemsDataGridView).BeginInit();
        SuspendLayout();

        // customerLabel
        customerLabel.Text = "Customer:";
        customerLabel.Location = new System.Drawing.Point(12, 15);
        customerLabel.Size = new System.Drawing.Size(80, 23);
        customerLabel.Name = "customerLabel";

        // customerComboBox
        customerComboBox.Location = new System.Drawing.Point(100, 12);
        customerComboBox.Size = new System.Drawing.Size(300, 23);
        customerComboBox.Name = "customerComboBox";
        customerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        // orderTypeLabel
        orderTypeLabel.Text = "Order Type:";
        orderTypeLabel.Location = new System.Drawing.Point(12, 45);
        orderTypeLabel.Size = new System.Drawing.Size(80, 23);
        orderTypeLabel.Name = "orderTypeLabel";

        // orderTypeComboBox
        orderTypeComboBox.Location = new System.Drawing.Point(100, 42);
        orderTypeComboBox.Size = new System.Drawing.Size(300, 23);
        orderTypeComboBox.Name = "orderTypeComboBox";
        orderTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        // productLabel
        productLabel.Text = "Product:";
        productLabel.Location = new System.Drawing.Point(12, 80);
        productLabel.Size = new System.Drawing.Size(80, 23);
        productLabel.Name = "productLabel";

        // productComboBox
        productComboBox.Location = new System.Drawing.Point(100, 77);
        productComboBox.Size = new System.Drawing.Size(200, 23);
        productComboBox.Name = "productComboBox";
        productComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        // quantityLabel
        quantityLabel.Text = "Qty:";
        quantityLabel.Location = new System.Drawing.Point(310, 80);
        quantityLabel.Size = new System.Drawing.Size(40, 23);
        quantityLabel.Name = "quantityLabel";

        // quantityNumericUpDown
        quantityNumericUpDown.Location = new System.Drawing.Point(354, 77);
        quantityNumericUpDown.Size = new System.Drawing.Size(60, 23);
        quantityNumericUpDown.Name = "quantityNumericUpDown";
        quantityNumericUpDown.Minimum = 1m;
        quantityNumericUpDown.Maximum = 100000m;
        quantityNumericUpDown.Value = 1m;

        // addItemButton
        addItemButton.Text = "Add Item";
        addItemButton.Location = new System.Drawing.Point(424, 76);
        addItemButton.Size = new System.Drawing.Size(120, 25);
        addItemButton.Name = "addItemButton";
        addItemButton.Click += AddItemButton_Click;

        // lineItemsDataGridView
        lineItemsDataGridView.Location = new System.Drawing.Point(12, 112);
        lineItemsDataGridView.Size = new System.Drawing.Size(536, 140);
        lineItemsDataGridView.ReadOnly = true;
        lineItemsDataGridView.AllowUserToAddRows = false;
        lineItemsDataGridView.AllowUserToDeleteRows = false;
        lineItemsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        lineItemsDataGridView.MultiSelect = false;
        lineItemsDataGridView.AutoGenerateColumns = true;
        lineItemsDataGridView.Name = "lineItemsDataGridView";

        // removeItemButton
        removeItemButton.Text = "Remove Item";
        removeItemButton.Location = new System.Drawing.Point(12, 262);
        removeItemButton.Size = new System.Drawing.Size(120, 28);
        removeItemButton.Name = "removeItemButton";
        removeItemButton.Click += RemoveItemButton_Click;

        // confirmButton
        confirmButton.Text = "Confirm";
        confirmButton.Location = new System.Drawing.Point(358, 262);
        confirmButton.Size = new System.Drawing.Size(90, 28);
        confirmButton.Name = "confirmButton";
        confirmButton.Click += ConfirmButton_Click;

        // cancelButton
        cancelButton.Text = "Cancel";
        cancelButton.Location = new System.Drawing.Point(456, 262);
        cancelButton.Size = new System.Drawing.Size(90, 28);
        cancelButton.Name = "cancelButton";
        cancelButton.Click += CancelButton_Click;

        // OrderCreateForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(560, 305);
        Controls.Add(customerLabel);
        Controls.Add(customerComboBox);
        Controls.Add(orderTypeLabel);
        Controls.Add(orderTypeComboBox);
        Controls.Add(productLabel);
        Controls.Add(productComboBox);
        Controls.Add(quantityLabel);
        Controls.Add(quantityNumericUpDown);
        Controls.Add(addItemButton);
        Controls.Add(lineItemsDataGridView);
        Controls.Add(removeItemButton);
        Controls.Add(confirmButton);
        Controls.Add(cancelButton);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Name = "OrderCreateForm";
        Text = "New Order";
        Load += OrderCreateForm_Load;

        ((System.ComponentModel.ISupportInitialize)quantityNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)lineItemsDataGridView).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
