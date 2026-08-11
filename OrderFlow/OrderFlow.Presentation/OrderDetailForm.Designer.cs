namespace OrderFlow.Presentation;

partial class OrderDetailForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label customerLabel;
    private System.Windows.Forms.Label customerValueLabel;
    private System.Windows.Forms.Label orderTypeLabel;
    private System.Windows.Forms.Label orderTypeValueLabel;
    private System.Windows.Forms.Label statusLabel;
    private System.Windows.Forms.Label statusValueLabel;
    private System.Windows.Forms.ComboBox statusComboBox;
    private System.Windows.Forms.Button transitionButton;
    private System.Windows.Forms.DataGridView itemsDataGridView;
    private System.Windows.Forms.Label totalLabel;
    private System.Windows.Forms.Label totalValueLabel;
    private System.Windows.Forms.Button closeButton;

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
        customerValueLabel = new System.Windows.Forms.Label();
        orderTypeLabel = new System.Windows.Forms.Label();
        orderTypeValueLabel = new System.Windows.Forms.Label();
        statusLabel = new System.Windows.Forms.Label();
        statusValueLabel = new System.Windows.Forms.Label();
        statusComboBox = new System.Windows.Forms.ComboBox();
        transitionButton = new System.Windows.Forms.Button();
        itemsDataGridView = new System.Windows.Forms.DataGridView();
        totalLabel = new System.Windows.Forms.Label();
        totalValueLabel = new System.Windows.Forms.Label();
        closeButton = new System.Windows.Forms.Button();

        ((System.ComponentModel.ISupportInitialize)itemsDataGridView).BeginInit();
        SuspendLayout();

        // customerLabel
        customerLabel.Text = "Customer:";
        customerLabel.Location = new System.Drawing.Point(12, 15);
        customerLabel.Size = new System.Drawing.Size(80, 23);
        customerLabel.Name = "customerLabel";

        // customerValueLabel
        customerValueLabel.Location = new System.Drawing.Point(100, 15);
        customerValueLabel.Size = new System.Drawing.Size(360, 23);
        customerValueLabel.Name = "customerValueLabel";

        // orderTypeLabel
        orderTypeLabel.Text = "Order Type:";
        orderTypeLabel.Location = new System.Drawing.Point(12, 45);
        orderTypeLabel.Size = new System.Drawing.Size(80, 23);
        orderTypeLabel.Name = "orderTypeLabel";

        // orderTypeValueLabel
        orderTypeValueLabel.Location = new System.Drawing.Point(100, 45);
        orderTypeValueLabel.Size = new System.Drawing.Size(360, 23);
        orderTypeValueLabel.Name = "orderTypeValueLabel";

        // statusLabel
        statusLabel.Text = "Status:";
        statusLabel.Location = new System.Drawing.Point(12, 75);
        statusLabel.Size = new System.Drawing.Size(80, 23);
        statusLabel.Name = "statusLabel";

        // statusValueLabel
        statusValueLabel.Location = new System.Drawing.Point(100, 75);
        statusValueLabel.Size = new System.Drawing.Size(360, 23);
        statusValueLabel.Name = "statusValueLabel";

        // statusComboBox
        statusComboBox.Location = new System.Drawing.Point(100, 105);
        statusComboBox.Size = new System.Drawing.Size(260, 23);
        statusComboBox.Name = "statusComboBox";
        statusComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

        // transitionButton
        transitionButton.Text = "Transition";
        transitionButton.Location = new System.Drawing.Point(370, 104);
        transitionButton.Size = new System.Drawing.Size(90, 25);
        transitionButton.Name = "transitionButton";
        transitionButton.Click += TransitionButton_Click;

        // itemsDataGridView
        itemsDataGridView.Location = new System.Drawing.Point(12, 138);
        itemsDataGridView.Size = new System.Drawing.Size(456, 150);
        itemsDataGridView.ReadOnly = true;
        itemsDataGridView.AllowUserToAddRows = false;
        itemsDataGridView.AllowUserToDeleteRows = false;
        itemsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        itemsDataGridView.MultiSelect = false;
        itemsDataGridView.AutoGenerateColumns = true;
        itemsDataGridView.Name = "itemsDataGridView";

        // totalLabel
        totalLabel.Text = "Total:";
        totalLabel.Location = new System.Drawing.Point(12, 298);
        totalLabel.Size = new System.Drawing.Size(80, 23);
        totalLabel.Name = "totalLabel";

        // totalValueLabel
        totalValueLabel.Location = new System.Drawing.Point(100, 298);
        totalValueLabel.Size = new System.Drawing.Size(200, 23);
        totalValueLabel.Name = "totalValueLabel";

        // closeButton
        closeButton.Text = "Close";
        closeButton.Location = new System.Drawing.Point(378, 294);
        closeButton.Size = new System.Drawing.Size(90, 28);
        closeButton.Name = "closeButton";
        closeButton.Click += CloseButton_Click;

        // OrderDetailForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(480, 335);
        Controls.Add(customerLabel);
        Controls.Add(customerValueLabel);
        Controls.Add(orderTypeLabel);
        Controls.Add(orderTypeValueLabel);
        Controls.Add(statusLabel);
        Controls.Add(statusValueLabel);
        Controls.Add(statusComboBox);
        Controls.Add(transitionButton);
        Controls.Add(itemsDataGridView);
        Controls.Add(totalLabel);
        Controls.Add(totalValueLabel);
        Controls.Add(closeButton);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Name = "OrderDetailForm";
        Text = "Order Detail";
        Load += OrderDetailForm_Load;

        ((System.ComponentModel.ISupportInitialize)itemsDataGridView).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
