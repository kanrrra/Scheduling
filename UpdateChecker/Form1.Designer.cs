namespace UpdateChecker
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            button1 = new Button();
            button2 = new Button();
            textBoxSource = new TextBox();
            textBoxUpdate = new TextBox();
            button3 = new Button();
            dataGridView1 = new DataGridView();
            Code = new DataGridViewTextBoxColumn();
            SourceDate = new DataGridViewTextBoxColumn();
            SourceTime = new DataGridViewTextBoxColumn();
            UpdateDate = new DataGridViewTextBoxColumn();
            UpdateTime = new DataGridViewTextBoxColumn();
            dateTimePickerStart = new DateTimePicker();
            dateTimePickerEnd = new DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(672, 34);
            button1.Name = "button1";
            button1.Size = new Size(116, 23);
            button1.TabIndex = 0;
            button1.Text = "Select Source";
            button1.UseVisualStyleBackColor = true;
            button1.Click += selectSourceButton_Click;
            // 
            // button2
            // 
            button2.Location = new Point(672, 80);
            button2.Name = "button2";
            button2.Size = new Size(116, 23);
            button2.TabIndex = 1;
            button2.Text = "Select update";
            button2.UseVisualStyleBackColor = true;
            button2.Click += selectUpdateButton_Click;
            // 
            // textBoxSource
            // 
            textBoxSource.Location = new Point(12, 35);
            textBoxSource.Name = "textBoxSource";
            textBoxSource.Size = new Size(639, 23);
            textBoxSource.TabIndex = 2;
            // 
            // textBoxUpdate
            // 
            textBoxUpdate.Location = new Point(12, 81);
            textBoxUpdate.Name = "textBoxUpdate";
            textBoxUpdate.Size = new Size(639, 23);
            textBoxUpdate.TabIndex = 3;
            // 
            // button3
            // 
            button3.Location = new Point(635, 116);
            button3.Name = "button3";
            button3.Size = new Size(153, 40);
            button3.TabIndex = 4;
            button3.Text = "Check update";
            button3.UseVisualStyleBackColor = true;
            button3.Click += processButton_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Code, SourceDate, SourceTime, UpdateDate, UpdateTime });
            dataGridView1.Dock = DockStyle.Bottom;
            dataGridView1.Location = new Point(0, 177);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            dataGridView1.RowTemplate.Height = 25;
            dataGridView1.Size = new Size(800, 273);
            dataGridView1.TabIndex = 5;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // Code
            // 
            Code.HeaderText = "Code";
            Code.Name = "Code";
            // 
            // SourceDate
            // 
            SourceDate.HeaderText = "SourceDate";
            SourceDate.Name = "SourceDate";
            // 
            // SourceTime
            // 
            SourceTime.HeaderText = "SourceTime";
            SourceTime.Name = "SourceTime";
            // 
            // UpdateDate
            // 
            UpdateDate.HeaderText = "UpdateDate";
            UpdateDate.Name = "UpdateDate";
            // 
            // UpdateTime
            // 
            UpdateTime.HeaderText = "UpdateTime";
            UpdateTime.Name = "UpdateTime";
            // 
            // dateTimePickerStart
            // 
            dateTimePickerStart.Location = new Point(12, 110);
            dateTimePickerStart.Name = "dateTimePickerStart";
            dateTimePickerStart.Size = new Size(200, 23);
            dateTimePickerStart.TabIndex = 6;
            dateTimePickerStart.Value = new DateTime(2023, 1, 1, 0, 0, 0, 0);
            dateTimePickerStart.ValueChanged += dateTimePickerStart_ValueChanged;
            // 
            // dateTimePickerEnd
            // 
            dateTimePickerEnd.Location = new Point(12, 139);
            dateTimePickerEnd.Name = "dateTimePickerEnd";
            dateTimePickerEnd.Size = new Size(200, 23);
            dateTimePickerEnd.TabIndex = 7;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(dateTimePickerEnd);
            Controls.Add(dateTimePickerStart);
            Controls.Add(dataGridView1);
            Controls.Add(button3);
            Controls.Add(textBoxUpdate);
            Controls.Add(textBoxSource);
            Controls.Add(button2);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button button2;
        private TextBox textBoxSource;
        private TextBox textBoxUpdate;
        private Button button3;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn Code;
        private DataGridViewTextBoxColumn SourceDate;
        private DataGridViewTextBoxColumn SourceTime;
        private DataGridViewTextBoxColumn UpdateDate;
        private DataGridViewTextBoxColumn UpdateTime;
        private DateTimePicker dateTimePickerStart;
        private DateTimePicker dateTimePickerEnd;
    }
}