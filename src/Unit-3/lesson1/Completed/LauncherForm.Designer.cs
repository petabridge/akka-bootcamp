namespace GithubActors
{
    partial class LauncherForm
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
            this.tbRepoUrl = new System.Windows.Forms.TextBox();
            this.lblRepo = new System.Windows.Forms.Label();
            this.lblIsValid = new System.Windows.Forms.Label();
            this.btnLaunch = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbRepoUrl
            // 
            this.tbRepoUrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbRepoUrl.Location = new System.Drawing.Point(95, 13);
            this.tbRepoUrl.Name = "tbRepoUrl";
            this.tbRepoUrl.Size = new System.Drawing.Size(455, 24);
            this.tbRepoUrl.TabIndex = 0;
            // 
            // lblRepo
            // 
            this.lblRepo.AutoSize = true;
            this.lblRepo.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRepo.Location = new System.Drawing.Point(3, 16);
            this.lblRepo.Name = "lblRepo";
            this.lblRepo.Size = new System.Drawing.Size(86, 18);
            this.lblRepo.TabIndex = 1;
            this.lblRepo.Text = "Repo URL";
            // 
            // lblIsValid
            // 
            this.lblIsValid.AutoSize = true;
            this.lblIsValid.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIsValid.Location = new System.Drawing.Point(95, 44);
            this.lblIsValid.Name = "lblIsValid";
            this.lblIsValid.Size = new System.Drawing.Size(46, 18);
            this.lblIsValid.TabIndex = 2;
            this.lblIsValid.Text = "label1";
            this.lblIsValid.Visible = false;
            // 
            // btnLaunch
            // 
            this.btnLaunch.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLaunch.Location = new System.Drawing.Point(218, 90);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(142, 37);
            this.btnLaunch.TabIndex = 3;
            this.btnLaunch.Text = "GO";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.btnLaunch_Click);
            // 
            // LauncherForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(562, 151);
            this.Controls.Add(this.btnLaunch);
            this.Controls.Add(this.lblIsValid);
            this.Controls.Add(this.lblRepo);
            this.Controls.Add(this.tbRepoUrl);
            this.Name = "LauncherForm";
            this.Text = "Who Starred This Repo?";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LauncherForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbRepoUrl;
        private System.Windows.Forms.Label lblRepo;
        private System.Windows.Forms.Label lblIsValid;
        private System.Windows.Forms.Button btnLaunch;
    }
}

