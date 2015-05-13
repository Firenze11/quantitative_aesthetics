namespace testmediasmall
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.button1_loadfile = new System.Windows.Forms.Button();
            this.flowLayoutPanel1.SuspendLayout();
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
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.Silver;
            this.flowLayoutPanel1.Controls.Add(this.button1_loadfile);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(371, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(100, 373);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // button1_loadfile
            // 
            this.button1_loadfile.BackColor = System.Drawing.Color.Gray;
            this.button1_loadfile.FlatAppearance.BorderSize = 0;
            this.button1_loadfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1_loadfile.ForeColor = System.Drawing.Color.White;
            this.button1_loadfile.Location = new System.Drawing.Point(3, 3);
            this.button1_loadfile.Name = "button1_loadfile";
            this.button1_loadfile.Size = new System.Drawing.Size(97, 21);
            this.button1_loadfile.TabIndex = 1;
            this.button1_loadfile.Text = "Load File";
            this.button1_loadfile.UseVisualStyleBackColor = false;
            this.button1_loadfile.MouseClick += new System.Windows.Forms.MouseEventHandler(this.button1_loadfile_MouseClick);
            // 
            // MainWindowForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 373);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.GLviewport);
            this.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(16, 38);
            this.Name = "MainWindowForm";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindowForm_FormClosing);
            this.Load += new System.EventHandler(this.MainWindowForm_Load);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl GLviewport;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button button1_loadfile;
    }
}

