namespace eyetrack
{
    partial class MainWindowForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindowForm));
            this.GLviewport = new OpenTK.GLControl();
            this.SuspendLayout();
            // 
            // GLviewport
            // 
            this.GLviewport.BackColor = System.Drawing.Color.Black;
            this.GLviewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GLviewport.Location = new System.Drawing.Point(0, 0);
            this.GLviewport.Name = "GLviewport";
            this.GLviewport.Size = new System.Drawing.Size(471, 373);
            this.GLviewport.TabIndex = 0;
            this.GLviewport.VSync = false;
            this.GLviewport.Load += new System.EventHandler(this.GLviewport_Load);
            this.GLviewport.Paint += new System.Windows.Forms.PaintEventHandler(this.GLviewport_Paint);
            this.GLviewport.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GLviewport_MouseDown);
            this.GLviewport.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GLviewport_MouseMove);
            this.GLviewport.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GLviewport_MouseUp);
            this.GLviewport.Resize += new System.EventHandler(this.GLviewport_Resize);
            // 
            // MainWindowForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 373);
            this.Controls.Add(this.GLviewport);
            this.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(16, 39);
            this.Name = "MainWindowForm";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindowForm_FormClosing);
            this.Load += new System.EventHandler(this.MainWindowForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl GLviewport;
    }
}

