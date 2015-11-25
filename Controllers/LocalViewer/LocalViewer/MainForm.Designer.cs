namespace LocalViewer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.camerasListGroupBox = new System.Windows.Forms.GroupBox();
            this.camerasListComboBox = new System.Windows.Forms.ComboBox();
            this.cameraViewGroupBox = new System.Windows.Forms.GroupBox();
            this.cameraViewPictureBox = new System.Windows.Forms.PictureBox();
            this.camerasListGroupBox.SuspendLayout();
            this.cameraViewGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cameraViewPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // camerasListGroupBox
            // 
            this.camerasListGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.camerasListGroupBox.Controls.Add(this.camerasListComboBox);
            this.camerasListGroupBox.Location = new System.Drawing.Point(13, 13);
            this.camerasListGroupBox.Name = "camerasListGroupBox";
            this.camerasListGroupBox.Size = new System.Drawing.Size(759, 49);
            this.camerasListGroupBox.TabIndex = 0;
            this.camerasListGroupBox.TabStop = false;
            this.camerasListGroupBox.Text = "Cameras list";
            // 
            // camerasListComboBox
            // 
            this.camerasListComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.camerasListComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.camerasListComboBox.FormattingEnabled = true;
            this.camerasListComboBox.Location = new System.Drawing.Point(7, 20);
            this.camerasListComboBox.Name = "camerasListComboBox";
            this.camerasListComboBox.Size = new System.Drawing.Size(746, 21);
            this.camerasListComboBox.TabIndex = 0;
            // 
            // cameraViewGroupBox
            // 
            this.cameraViewGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cameraViewGroupBox.Controls.Add(this.cameraViewPictureBox);
            this.cameraViewGroupBox.Location = new System.Drawing.Point(13, 69);
            this.cameraViewGroupBox.Name = "cameraViewGroupBox";
            this.cameraViewGroupBox.Size = new System.Drawing.Size(759, 480);
            this.cameraViewGroupBox.TabIndex = 1;
            this.cameraViewGroupBox.TabStop = false;
            this.cameraViewGroupBox.Text = "Camera view";
            // 
            // cameraViewPictureBox
            // 
            this.cameraViewPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cameraViewPictureBox.Location = new System.Drawing.Point(7, 20);
            this.cameraViewPictureBox.Name = "cameraViewPictureBox";
            this.cameraViewPictureBox.Size = new System.Drawing.Size(746, 454);
            this.cameraViewPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.cameraViewPictureBox.TabIndex = 0;
            this.cameraViewPictureBox.TabStop = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.cameraViewGroupBox);
            this.Controls.Add(this.camerasListGroupBox);
            this.Name = "MainForm";
            this.Text = "Local Viewer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.camerasListGroupBox.ResumeLayout(false);
            this.cameraViewGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cameraViewPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox camerasListGroupBox;
        private System.Windows.Forms.ComboBox camerasListComboBox;
        private System.Windows.Forms.GroupBox cameraViewGroupBox;
        private System.Windows.Forms.PictureBox cameraViewPictureBox;
    }
}

