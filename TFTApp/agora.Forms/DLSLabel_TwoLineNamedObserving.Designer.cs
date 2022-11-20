namespace Agora.Forms
{
    partial class DLSLabel_TwoLineNamedObserving
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelValueName = new System.Windows.Forms.Label();
            this.labelUnit = new System.Windows.Forms.Label();
            this.labelValue = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelValueName
            // 
            this.labelValueName.AutoSize = true;
            this.labelValueName.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelValueName.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.labelValueName.Location = new System.Drawing.Point(4, 4);
            this.labelValueName.Name = "labelValueName";
            this.labelValueName.Size = new System.Drawing.Size(38, 13);
            this.labelValueName.TabIndex = 0;
            this.labelValueName.Text = "Name";
            this.labelValueName.Paint += new System.Windows.Forms.PaintEventHandler(this.LabelValueName_Paint);
            // 
            // labelUnit
            // 
            this.labelUnit.AutoSize = true;
            this.labelUnit.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUnit.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.labelUnit.Location = new System.Drawing.Point(70, 4);
            this.labelUnit.Name = "labelUnit";
            this.labelUnit.Size = new System.Drawing.Size(34, 13);
            this.labelUnit.TabIndex = 0;
            this.labelUnit.Text = "(unit)";
            // 
            // labelValue
            // 
            this.labelValue.AutoSize = true;
            this.labelValue.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.labelValue.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.labelValue.Location = new System.Drawing.Point(28, 20);
            this.labelValue.Name = "labelValue";
            this.labelValue.Size = new System.Drawing.Size(49, 21);
            this.labelValue.TabIndex = 0;
            this.labelValue.Text = "Value";
            // 
            // NamedObservingLabel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.labelUnit);
            this.Controls.Add(this.labelValue);
            this.Controls.Add(this.labelValueName);
            this.Name = "NamedObservingLabel";
            this.Size = new System.Drawing.Size(196, 49);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelValueName;
        private System.Windows.Forms.Label labelUnit;
        private System.Windows.Forms.Label labelValue;
    }
}
