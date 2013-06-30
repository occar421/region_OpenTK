namespace OpenTK_Sample
{
	partial class WinForm
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
			this.glControl1 = new OpenTK.GLControl();
			this.glControl2 = new OpenTK.GLControl();
			this.panel1 = new System.Windows.Forms.Panel();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.panel2 = new System.Windows.Forms.Panel();
			this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
			this.splitter = new System.Windows.Forms.Splitter();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			this.panel2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
			this.SuspendLayout();
			// 
			// glControl1
			// 
			this.glControl1.BackColor = System.Drawing.Color.Black;
			this.glControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.glControl1.Location = new System.Drawing.Point(0, 0);
			this.glControl1.Name = "glControl1";
			this.glControl1.Size = new System.Drawing.Size(395, 562);
			this.glControl1.TabIndex = 0;
			this.glControl1.VSync = false;
			this.glControl1.Load += new System.EventHandler(this.glControl1_Load);
			this.glControl1.Resize += new System.EventHandler(this.glControl1_Resize);
			// 
			// glControl2
			// 
			this.glControl2.BackColor = System.Drawing.Color.Black;
			this.glControl2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.glControl2.Location = new System.Drawing.Point(0, 0);
			this.glControl2.Name = "glControl2";
			this.glControl2.Size = new System.Drawing.Size(389, 562);
			this.glControl2.TabIndex = 1;
			this.glControl2.VSync = false;
			this.glControl2.Load += new System.EventHandler(this.glControl2_Load);
			this.glControl2.Resize += new System.EventHandler(this.glControl2_Resize);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.numericUpDown1);
			this.panel1.Controls.Add(this.glControl1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(395, 562);
			this.panel1.TabIndex = 2;
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.numericUpDown1.Location = new System.Drawing.Point(0, 543);
			this.numericUpDown1.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.numericUpDown1.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(395, 19);
			this.numericUpDown1.TabIndex = 1;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.numericUpDown2);
			this.panel2.Controls.Add(this.glControl2);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(395, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(389, 562);
			this.panel2.TabIndex = 3;
			// 
			// numericUpDown2
			// 
			this.numericUpDown2.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.numericUpDown2.Location = new System.Drawing.Point(0, 543);
			this.numericUpDown2.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.numericUpDown2.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
			this.numericUpDown2.Name = "numericUpDown2";
			this.numericUpDown2.Size = new System.Drawing.Size(389, 19);
			this.numericUpDown2.TabIndex = 2;
			// 
			// splitter
			// 
			this.splitter.Location = new System.Drawing.Point(395, 0);
			this.splitter.Name = "splitter";
			this.splitter.Size = new System.Drawing.Size(5, 562);
			this.splitter.TabIndex = 4;
			this.splitter.TabStop = false;
			// 
			// WinForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(784, 562);
			this.Controls.Add(this.splitter);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Name = "WinForm";
			this.Text = "0-4:WindowsForm";
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			this.panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private OpenTK.GLControl glControl1;
		private OpenTK.GLControl glControl2;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.NumericUpDown numericUpDown2;
		private System.Windows.Forms.Splitter splitter;


	}
}