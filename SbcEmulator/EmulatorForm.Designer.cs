namespace SbcEmulator
{
    partial class EmulatorForm
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
            this.Registers = new System.Windows.Forms.DataGridView();
            this.RegisterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RegisterValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Code = new System.Windows.Forms.DataGridView();
            this.CodeBreakpoint = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.CodeAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CodeInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Output = new System.Windows.Forms.TextBox();
            this.MemoryTabPage = new System.Windows.Forms.TabPage();
            this.Memory = new System.Windows.Forms.DataGridView();
            this.FrameTabPage = new System.Windows.Forms.TabPage();
            this.Frame = new System.Windows.Forms.DataGridView();
            this.TabControl = new System.Windows.Forms.TabControl();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.RunMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RunToMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StepInMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StepOutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StepOverMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ResetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.PfxDetail = new System.Windows.Forms.ToolStripMenuItem();
            this.OpcodeDetail = new System.Windows.Forms.ToolStripMenuItem();
            this.AsmDetail = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.Hex = new System.Windows.Forms.ToolStripMenuItem();
            this.Dec = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MethodsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Status = new System.Windows.Forms.ToolStripMenuItem();
            this.FrameAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FrameValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FrameName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MemoryAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MemoryValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MemoryName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.Registers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Code)).BeginInit();
            this.MemoryTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Memory)).BeginInit();
            this.FrameTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Frame)).BeginInit();
            this.TabControl.SuspendLayout();
            this.MenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // Registers
            // 
            this.Registers.AllowUserToAddRows = false;
            this.Registers.AllowUserToDeleteRows = false;
            this.Registers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.Registers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Registers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.RegisterName,
            this.RegisterValue});
            this.Registers.Location = new System.Drawing.Point(0, 23);
            this.Registers.Name = "Registers";
            this.Registers.ReadOnly = true;
            this.Registers.RowHeadersVisible = false;
            this.Registers.Size = new System.Drawing.Size(240, 296);
            this.Registers.TabIndex = 0;
            this.Registers.VirtualMode = true;
            this.Registers.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Registers_CellContentDoubleClick);
            this.Registers.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.Registers_CellPainting);
            this.Registers.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.Registers_CellValueNeeded);
            // 
            // RegisterName
            // 
            this.RegisterName.HeaderText = "Register";
            this.RegisterName.Name = "RegisterName";
            this.RegisterName.ReadOnly = true;
            // 
            // RegisterValue
            // 
            this.RegisterValue.HeaderText = "Value";
            this.RegisterValue.Name = "RegisterValue";
            this.RegisterValue.ReadOnly = true;
            // 
            // Code
            // 
            this.Code.AllowUserToAddRows = false;
            this.Code.AllowUserToDeleteRows = false;
            this.Code.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Code.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Code.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CodeBreakpoint,
            this.CodeAddress,
            this.CodeInfo});
            this.Code.Location = new System.Drawing.Point(246, 28);
            this.Code.MultiSelect = false;
            this.Code.Name = "Code";
            this.Code.ReadOnly = true;
            this.Code.RowHeadersVisible = false;
            this.Code.Size = new System.Drawing.Size(407, 400);
            this.Code.TabIndex = 0;
            this.Code.VirtualMode = true;
            this.Code.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Code_CellClick);
            this.Code.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.Code_CellValueNeeded);
            // 
            // CodeBreakpoint
            // 
            this.CodeBreakpoint.HeaderText = "Breakpoint";
            this.CodeBreakpoint.Name = "CodeBreakpoint";
            this.CodeBreakpoint.ReadOnly = true;
            // 
            // CodeAddress
            // 
            this.CodeAddress.HeaderText = "Address";
            this.CodeAddress.Name = "CodeAddress";
            this.CodeAddress.ReadOnly = true;
            // 
            // CodeInfo
            // 
            this.CodeInfo.HeaderText = "Code";
            this.CodeInfo.Name = "CodeInfo";
            this.CodeInfo.ReadOnly = true;
            // 
            // Output
            // 
            this.Output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Output.Location = new System.Drawing.Point(0, 325);
            this.Output.Multiline = true;
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(240, 99);
            this.Output.TabIndex = 2;
            // 
            // MemoryTabPage
            // 
            this.MemoryTabPage.Controls.Add(this.Memory);
            this.MemoryTabPage.Location = new System.Drawing.Point(4, 22);
            this.MemoryTabPage.Name = "MemoryTabPage";
            this.MemoryTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.MemoryTabPage.Size = new System.Drawing.Size(340, 373);
            this.MemoryTabPage.TabIndex = 0;
            this.MemoryTabPage.Text = "Memory";
            this.MemoryTabPage.UseVisualStyleBackColor = true;
            // 
            // Memory
            // 
            this.Memory.AllowUserToAddRows = false;
            this.Memory.AllowUserToDeleteRows = false;
            this.Memory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Memory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Memory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MemoryAddress,
            this.MemoryValue,
            this.MemoryName});
            this.Memory.Location = new System.Drawing.Point(-2, 0);
            this.Memory.Name = "Memory";
            this.Memory.ReadOnly = true;
            this.Memory.RowHeadersVisible = false;
            this.Memory.Size = new System.Drawing.Size(344, 373);
            this.Memory.TabIndex = 1;
            this.Memory.VirtualMode = true;
            this.Memory.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Memory_CellContentDoubleClick);
            this.Memory.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.Memory_CellPainting);
            this.Memory.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.Memory_CellValueNeeded);
            // 
            // FrameTabPage
            // 
            this.FrameTabPage.Controls.Add(this.Frame);
            this.FrameTabPage.Location = new System.Drawing.Point(4, 22);
            this.FrameTabPage.Name = "FrameTabPage";
            this.FrameTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.FrameTabPage.Size = new System.Drawing.Size(340, 373);
            this.FrameTabPage.TabIndex = 1;
            this.FrameTabPage.Text = "Frame";
            this.FrameTabPage.UseVisualStyleBackColor = true;
            // 
            // Frame
            // 
            this.Frame.AllowUserToAddRows = false;
            this.Frame.AllowUserToDeleteRows = false;
            this.Frame.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Frame.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Frame.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FrameAddress,
            this.FrameValue,
            this.FrameName});
            this.Frame.Location = new System.Drawing.Point(-4, 0);
            this.Frame.Name = "Frame";
            this.Frame.ReadOnly = true;
            this.Frame.RowHeadersVisible = false;
            this.Frame.Size = new System.Drawing.Size(344, 377);
            this.Frame.TabIndex = 3;
            this.Frame.VirtualMode = true;
            this.Frame.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Memory_CellContentDoubleClick);
            this.Frame.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.Memory_CellPainting);
            this.Frame.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.Memory_CellValueNeeded);
            // 
            // TabControl
            // 
            this.TabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TabControl.Controls.Add(this.FrameTabPage);
            this.TabControl.Controls.Add(this.MemoryTabPage);
            this.TabControl.Location = new System.Drawing.Point(660, 29);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(348, 399);
            this.TabControl.TabIndex = 4;
            // 
            // MenuStrip
            // 
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RunMenuItem,
            this.RunToMenuItem,
            this.StepInMenuItem,
            this.StepOutMenuItem,
            this.StepOverMenuItem,
            this.ResetMenuItem,
            this.ViewMenu,
            this.Status});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(1008, 24);
            this.MenuStrip.TabIndex = 5;
            this.MenuStrip.Text = "menuStrip1";
            // 
            // RunMenuItem
            // 
            this.RunMenuItem.Font = new System.Drawing.Font("Arial", 9F);
            this.RunMenuItem.Name = "RunMenuItem";
            this.RunMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.RunMenuItem.ShowShortcutKeys = false;
            this.RunMenuItem.Size = new System.Drawing.Size(57, 20);
            this.RunMenuItem.Text = "► Run";
            this.RunMenuItem.Click += new System.EventHandler(this.Run_Click);
            // 
            // RunToMenuItem
            // 
            this.RunToMenuItem.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.RunToMenuItem.Name = "RunToMenuItem";
            this.RunToMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F10)));
            this.RunToMenuItem.ShowShortcutKeys = false;
            this.RunToMenuItem.Size = new System.Drawing.Size(71, 20);
            this.RunToMenuItem.Text = "↖ Run To";
            this.RunToMenuItem.Click += new System.EventHandler(this.RunToMenuItem_Click);
            // 
            // StepInMenuItem
            // 
            this.StepInMenuItem.Font = new System.Drawing.Font("Arial", 9F);
            this.StepInMenuItem.Name = "StepInMenuItem";
            this.StepInMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F11;
            this.StepInMenuItem.Size = new System.Drawing.Size(66, 20);
            this.StepInMenuItem.Text = "↓ Step In";
            this.StepInMenuItem.Click += new System.EventHandler(this.StepIn_Click);
            // 
            // StepOutMenuItem
            // 
            this.StepOutMenuItem.Font = new System.Drawing.Font("Arial", 9F);
            this.StepOutMenuItem.Name = "StepOutMenuItem";
            this.StepOutMenuItem.ShortcutKeyDisplayString = "";
            this.StepOutMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F11)));
            this.StepOutMenuItem.Size = new System.Drawing.Size(75, 20);
            this.StepOutMenuItem.Text = "↑ Step Out";
            this.StepOutMenuItem.Click += new System.EventHandler(this.StepOut_Click);
            // 
            // StepOverMenuItem
            // 
            this.StepOverMenuItem.Font = new System.Drawing.Font("Arial", 9F);
            this.StepOverMenuItem.Name = "StepOverMenuItem";
            this.StepOverMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F10;
            this.StepOverMenuItem.Size = new System.Drawing.Size(85, 20);
            this.StepOverMenuItem.Text = "↷ Step Over";
            this.StepOverMenuItem.Click += new System.EventHandler(this.StepOver_Click);
            // 
            // ResetMenuItem
            // 
            this.ResetMenuItem.Font = new System.Drawing.Font("Arial", 9F);
            this.ResetMenuItem.Name = "ResetMenuItem";
            this.ResetMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F5)));
            this.ResetMenuItem.ShowShortcutKeys = false;
            this.ResetMenuItem.Size = new System.Drawing.Size(65, 20);
            this.ResetMenuItem.Text = "⟲ Reset";
            this.ResetMenuItem.Click += new System.EventHandler(this.Reset_Click);
            // 
            // ViewMenu
            // 
            this.ViewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PfxDetail,
            this.OpcodeDetail,
            this.AsmDetail,
            this.toolStripSeparator1,
            this.Hex,
            this.Dec,
            this.toolStripSeparator2,
            this.MethodsMenuItem});
            this.ViewMenu.Font = new System.Drawing.Font("Arial", 9F);
            this.ViewMenu.Name = "ViewMenu";
            this.ViewMenu.Size = new System.Drawing.Size(45, 20);
            this.ViewMenu.Text = "&View";
            // 
            // PfxDetail
            // 
            this.PfxDetail.Name = "PfxDetail";
            this.PfxDetail.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D1)));
            this.PfxDetail.Size = new System.Drawing.Size(192, 22);
            this.PfxDetail.Text = "PFX Detail";
            this.PfxDetail.Click += new System.EventHandler(this.Detail_Click);
            // 
            // OpcodeDetail
            // 
            this.OpcodeDetail.Name = "OpcodeDetail";
            this.OpcodeDetail.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D2)));
            this.OpcodeDetail.Size = new System.Drawing.Size(192, 22);
            this.OpcodeDetail.Text = "Opcode Detail";
            this.OpcodeDetail.Click += new System.EventHandler(this.Detail_Click);
            // 
            // AsmDetail
            // 
            this.AsmDetail.Checked = true;
            this.AsmDetail.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AsmDetail.Name = "AsmDetail";
            this.AsmDetail.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D3)));
            this.AsmDetail.Size = new System.Drawing.Size(192, 22);
            this.AsmDetail.Text = "ASM Detail";
            this.AsmDetail.Click += new System.EventHandler(this.Detail_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(189, 6);
            // 
            // Hex
            // 
            this.Hex.Name = "Hex";
            this.Hex.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this.Hex.Size = new System.Drawing.Size(192, 22);
            this.Hex.Text = "Hexadecimal";
            this.Hex.Click += new System.EventHandler(this.Base_Click);
            // 
            // Dec
            // 
            this.Dec.Checked = true;
            this.Dec.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Dec.Name = "Dec";
            this.Dec.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.Dec.Size = new System.Drawing.Size(192, 22);
            this.Dec.Text = "Decimal";
            this.Dec.Click += new System.EventHandler(this.Base_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(189, 6);
            // 
            // MethodsMenuItem
            // 
            this.MethodsMenuItem.Name = "MethodsMenuItem";
            this.MethodsMenuItem.Size = new System.Drawing.Size(192, 22);
            this.MethodsMenuItem.Text = "Methods";
            // 
            // Status
            // 
            this.Status.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Status.Font = new System.Drawing.Font("Arial", 9F);
            this.Status.Name = "Status";
            this.Status.Size = new System.Drawing.Size(54, 20);
            this.Status.Text = "Status";
            this.Status.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Status.Click += new System.EventHandler(this.Status_Click);
            // 
            // FrameAddress
            // 
            this.FrameAddress.HeaderText = "Frame";
            this.FrameAddress.Name = "FrameAddress";
            this.FrameAddress.ReadOnly = true;
            this.FrameAddress.Width = 50;
            // 
            // FrameValue
            // 
            this.FrameValue.HeaderText = "Value";
            this.FrameValue.Name = "FrameValue";
            this.FrameValue.ReadOnly = true;
            this.FrameValue.Width = 50;
            // 
            // FrameName
            // 
            this.FrameName.HeaderText = "Name";
            this.FrameName.Name = "FrameName";
            this.FrameName.ReadOnly = true;
            this.FrameName.Width = 200;
            // 
            // MemoryAddress
            // 
            this.MemoryAddress.HeaderText = "Address";
            this.MemoryAddress.Name = "MemoryAddress";
            this.MemoryAddress.ReadOnly = true;
            this.MemoryAddress.Width = 50;
            // 
            // MemoryValue
            // 
            this.MemoryValue.HeaderText = "Value";
            this.MemoryValue.Name = "MemoryValue";
            this.MemoryValue.ReadOnly = true;
            this.MemoryValue.Width = 50;
            // 
            // MemoryName
            // 
            this.MemoryName.HeaderText = "Name";
            this.MemoryName.Name = "MemoryName";
            this.MemoryName.ReadOnly = true;
            this.MemoryName.Width = 200;
            // 
            // EmulatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 426);
            this.Controls.Add(this.TabControl);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.MenuStrip);
            this.Controls.Add(this.Code);
            this.Controls.Add(this.Registers);
            this.MainMenuStrip = this.MenuStrip;
            this.Name = "EmulatorForm";
            this.Text = "SBC Emulator";
            this.Load += new System.EventHandler(this.EmulatorForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Registers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Code)).EndInit();
            this.MemoryTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Memory)).EndInit();
            this.FrameTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Frame)).EndInit();
            this.TabControl.ResumeLayout(false);
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView Registers;
        private System.Windows.Forms.DataGridView Code;
        private System.Windows.Forms.TextBox Output;
        private System.Windows.Forms.DataGridViewTextBoxColumn RegisterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn RegisterValue;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CodeBreakpoint;
        private System.Windows.Forms.DataGridViewTextBoxColumn CodeAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn CodeInfo;
        private System.Windows.Forms.TabPage MemoryTabPage;
        private System.Windows.Forms.DataGridView Memory;
        private System.Windows.Forms.TabPage FrameTabPage;
        private System.Windows.Forms.DataGridView Frame;
        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem RunMenuItem;
        private System.Windows.Forms.ToolStripMenuItem StepInMenuItem;
        private System.Windows.Forms.ToolStripMenuItem StepOutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem StepOverMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ResetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ViewMenu;
        private System.Windows.Forms.ToolStripMenuItem PfxDetail;
        private System.Windows.Forms.ToolStripMenuItem OpcodeDetail;
        private System.Windows.Forms.ToolStripMenuItem AsmDetail;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem Hex;
        private System.Windows.Forms.ToolStripMenuItem Dec;
        private System.Windows.Forms.ToolStripMenuItem Status;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem MethodsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RunToMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn MemoryAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn MemoryValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn MemoryName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FrameAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn FrameValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn FrameName;
    }
}

