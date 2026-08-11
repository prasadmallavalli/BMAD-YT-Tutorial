namespace OrderFlow.Presentation;

partial class ProductDetailForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label nameLabel;
    private System.Windows.Forms.TextBox nameTextBox;
    private System.Windows.Forms.Label skuLabel;
    private System.Windows.Forms.TextBox skuTextBox;
    private System.Windows.Forms.Label unitPriceLabel;
    private System.Windows.Forms.NumericUpDown unitPriceNumericUpDown;
    private System.Windows.Forms.Label stockQuantityLabel;
    private System.Windows.Forms.NumericUpDown stockQuantityNumericUpDown;
    private System.Windows.Forms.Button saveButton;
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
        nameLabel = new System.Windows.Forms.Label();
        nameTextBox = new System.Windows.Forms.TextBox();
        skuLabel = new System.Windows.Forms.Label();
        skuTextBox = new System.Windows.Forms.TextBox();
        unitPriceLabel = new System.Windows.Forms.Label();
        unitPriceNumericUpDown = new System.Windows.Forms.NumericUpDown();
        stockQuantityLabel = new System.Windows.Forms.Label();
        stockQuantityNumericUpDown = new System.Windows.Forms.NumericUpDown();
        saveButton = new System.Windows.Forms.Button();
        cancelButton = new System.Windows.Forms.Button();

        ((System.ComponentModel.ISupportInitialize)unitPriceNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)stockQuantityNumericUpDown).BeginInit();
        SuspendLayout();

        // nameLabel
        nameLabel.Text = "Name:";
        nameLabel.Location = new System.Drawing.Point(12, 15);
        nameLabel.Size = new System.Drawing.Size(80, 23);
        nameLabel.Name = "nameLabel";

        // nameTextBox
        nameTextBox.Location = new System.Drawing.Point(100, 12);
        nameTextBox.Size = new System.Drawing.Size(260, 23);
        nameTextBox.Name = "nameTextBox";

        // skuLabel
        skuLabel.Text = "SKU:";
        skuLabel.Location = new System.Drawing.Point(12, 45);
        skuLabel.Size = new System.Drawing.Size(80, 23);
        skuLabel.Name = "skuLabel";

        // skuTextBox
        skuTextBox.Location = new System.Drawing.Point(100, 42);
        skuTextBox.Size = new System.Drawing.Size(260, 23);
        skuTextBox.Name = "skuTextBox";

        // unitPriceLabel
        unitPriceLabel.Text = "Unit Price:";
        unitPriceLabel.Location = new System.Drawing.Point(12, 75);
        unitPriceLabel.Size = new System.Drawing.Size(80, 23);
        unitPriceLabel.Name = "unitPriceLabel";

        // unitPriceNumericUpDown
        unitPriceNumericUpDown.Location = new System.Drawing.Point(100, 72);
        unitPriceNumericUpDown.Size = new System.Drawing.Size(260, 23);
        unitPriceNumericUpDown.Name = "unitPriceNumericUpDown";
        unitPriceNumericUpDown.DecimalPlaces = 2;
        unitPriceNumericUpDown.Minimum = 0m;
        unitPriceNumericUpDown.Maximum = 999999.99m;
        unitPriceNumericUpDown.Increment = 0.01m;

        // stockQuantityLabel
        stockQuantityLabel.Text = "Stock Quantity:";
        stockQuantityLabel.Location = new System.Drawing.Point(12, 105);
        stockQuantityLabel.Size = new System.Drawing.Size(80, 23);
        stockQuantityLabel.Name = "stockQuantityLabel";

        // stockQuantityNumericUpDown
        stockQuantityNumericUpDown.Location = new System.Drawing.Point(100, 102);
        stockQuantityNumericUpDown.Size = new System.Drawing.Size(260, 23);
        stockQuantityNumericUpDown.Name = "stockQuantityNumericUpDown";
        stockQuantityNumericUpDown.DecimalPlaces = 0;
        stockQuantityNumericUpDown.Minimum = 0m;
        stockQuantityNumericUpDown.Maximum = 1000000m;

        // saveButton
        saveButton.Text = "Save";
        saveButton.Location = new System.Drawing.Point(100, 140);
        saveButton.Size = new System.Drawing.Size(90, 30);
        saveButton.Name = "saveButton";
        saveButton.Click += SaveButton_Click;

        // cancelButton
        cancelButton.Text = "Cancel";
        cancelButton.Location = new System.Drawing.Point(198, 140);
        cancelButton.Size = new System.Drawing.Size(90, 30);
        cancelButton.Name = "cancelButton";
        cancelButton.Click += CancelButton_Click;

        // ProductDetailForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(380, 185);
        Controls.Add(nameLabel);
        Controls.Add(nameTextBox);
        Controls.Add(skuLabel);
        Controls.Add(skuTextBox);
        Controls.Add(unitPriceLabel);
        Controls.Add(unitPriceNumericUpDown);
        Controls.Add(stockQuantityLabel);
        Controls.Add(stockQuantityNumericUpDown);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Name = "ProductDetailForm";
        Text = "Product";
        Load += ProductDetailForm_Load;

        ((System.ComponentModel.ISupportInitialize)unitPriceNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)stockQuantityNumericUpDown).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
