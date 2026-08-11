namespace OrderFlow.Presentation;

partial class CustomerDetailForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label nameLabel;
    private System.Windows.Forms.TextBox nameTextBox;
    private System.Windows.Forms.Label emailLabel;
    private System.Windows.Forms.TextBox emailTextBox;
    private System.Windows.Forms.Label phoneLabel;
    private System.Windows.Forms.TextBox phoneTextBox;
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
        emailLabel = new System.Windows.Forms.Label();
        emailTextBox = new System.Windows.Forms.TextBox();
        phoneLabel = new System.Windows.Forms.Label();
        phoneTextBox = new System.Windows.Forms.TextBox();
        saveButton = new System.Windows.Forms.Button();
        cancelButton = new System.Windows.Forms.Button();

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

        // emailLabel
        emailLabel.Text = "Email:";
        emailLabel.Location = new System.Drawing.Point(12, 45);
        emailLabel.Size = new System.Drawing.Size(80, 23);
        emailLabel.Name = "emailLabel";

        // emailTextBox
        emailTextBox.Location = new System.Drawing.Point(100, 42);
        emailTextBox.Size = new System.Drawing.Size(260, 23);
        emailTextBox.Name = "emailTextBox";

        // phoneLabel
        phoneLabel.Text = "Phone:";
        phoneLabel.Location = new System.Drawing.Point(12, 75);
        phoneLabel.Size = new System.Drawing.Size(80, 23);
        phoneLabel.Name = "phoneLabel";

        // phoneTextBox
        phoneTextBox.Location = new System.Drawing.Point(100, 72);
        phoneTextBox.Size = new System.Drawing.Size(260, 23);
        phoneTextBox.Name = "phoneTextBox";

        // saveButton
        saveButton.Text = "Save";
        saveButton.Location = new System.Drawing.Point(100, 110);
        saveButton.Size = new System.Drawing.Size(90, 30);
        saveButton.Name = "saveButton";
        saveButton.Click += SaveButton_Click;

        // cancelButton
        cancelButton.Text = "Cancel";
        cancelButton.Location = new System.Drawing.Point(198, 110);
        cancelButton.Size = new System.Drawing.Size(90, 30);
        cancelButton.Name = "cancelButton";
        cancelButton.Click += CancelButton_Click;

        // CustomerDetailForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(380, 155);
        Controls.Add(nameLabel);
        Controls.Add(nameTextBox);
        Controls.Add(emailLabel);
        Controls.Add(emailTextBox);
        Controls.Add(phoneLabel);
        Controls.Add(phoneTextBox);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Name = "CustomerDetailForm";
        Text = "Customer";
        Load += CustomerDetailForm_Load;

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
