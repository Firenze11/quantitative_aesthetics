namespace testmediasmall
{
    partial class RGBHistogram
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        ColorAnalysis img = new ColorAnalysis();
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
            this.histogramBox = new Emgu.CV.UI.HistogramBox();
            this.SuspendLayout();
            // 
            // histogramBox
            // 
            this.histogramBox.Location = new System.Drawing.Point(2, 2);
            this.histogramBox.Name = "histogramBox";
            this.histogramBox.Size = new System.Drawing.Size(280, 259);
            this.histogramBox.TabIndex = 0;
            // 
            // RGBHistogram
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.histogramBox);
            this.Name = "RGBHistogram";
            this.Text = "RGBHistogram";
            this.ResumeLayout(false);

        }

        #endregion

        private Emgu.CV.UI.HistogramBox histogramBox;
    }
}