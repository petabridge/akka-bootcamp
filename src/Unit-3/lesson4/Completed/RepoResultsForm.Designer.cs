namespace GithubActors
{
    partial class RepoResultsForm
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
            this.dgUsers = new System.Windows.Forms.DataGridView();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.Owner = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RepoName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.URL = new System.Windows.Forms.DataGridViewLinkColumn();
            this.Shared = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Issues = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Stars = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Forks = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgUsers)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgUsers
            // 
            this.dgUsers.AllowUserToOrderColumns = true;
            this.dgUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgUsers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Owner,
            this.RepoName,
            this.URL,
            this.Shared,
            this.Issues,
            this.Stars,
            this.Forks});
            this.dgUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgUsers.Location = new System.Drawing.Point(0, 0);
            this.dgUsers.Name = "dgUsers";
            this.dgUsers.Size = new System.Drawing.Size(739, 322);
            this.dgUsers.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsProgress,
            this.tsStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 300);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(739, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsProgress
            // 
            this.tsProgress.Name = "tsProgress";
            this.tsProgress.Size = new System.Drawing.Size(100, 16);
            this.tsProgress.Visible = false;
            // 
            // tsStatus
            // 
            this.tsStatus.Name = "tsStatus";
            this.tsStatus.Size = new System.Drawing.Size(73, 17);
            this.tsStatus.Text = "Processing...";
            this.tsStatus.Visible = false;
            // 
            // Owner
            // 
            this.Owner.HeaderText = "Owner";
            this.Owner.Name = "Owner";
            // 
            // RepoName
            // 
            this.RepoName.HeaderText = "Name";
            this.RepoName.Name = "RepoName";
            // 
            // URL
            // 
            this.URL.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.URL.HeaderText = "URL";
            this.URL.Name = "URL";
            // 
            // Shared
            // 
            this.Shared.HeaderText = "SharedStars";
            this.Shared.Name = "Shared";
            // 
            // Issues
            // 
            this.Issues.HeaderText = "Open Issues";
            this.Issues.Name = "Issues";
            // 
            // Stars
            // 
            this.Stars.HeaderText = "Stars";
            this.Stars.Name = "Stars";
            // 
            // Forks
            // 
            this.Forks.HeaderText = "Forks";
            this.Forks.Name = "Forks";
            // 
            // RepoResultsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(739, 322);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.dgUsers);
            this.Name = "RepoResultsForm";
            this.Text = "Repos Similar to {RepoName}";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RepoResultsForm_FormClosing);
            this.Load += new System.EventHandler(this.RepoResultsForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgUsers)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgUsers;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar tsProgress;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn Owner;
        private System.Windows.Forms.DataGridViewTextBoxColumn RepoName;
        private System.Windows.Forms.DataGridViewLinkColumn URL;
        private System.Windows.Forms.DataGridViewTextBoxColumn Shared;
        private System.Windows.Forms.DataGridViewTextBoxColumn Issues;
        private System.Windows.Forms.DataGridViewTextBoxColumn Stars;
        private System.Windows.Forms.DataGridViewTextBoxColumn Forks;
    }
}