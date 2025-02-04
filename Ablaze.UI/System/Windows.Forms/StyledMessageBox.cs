﻿using System.Drawing;
using System.Threading;

namespace System.Windows.Forms {
	/// <summary>
	/// A robust customizable message box.
	/// </summary>
	public class StyledMessageBox {
		/// <summary>
		/// Shows the specified message box.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="caption">The caption.</param>
		/// <param name="styled">Whether to use a more styled interface instead of a simpler one.</param>
		/// <param name="buttons">The buttons.</param>
		/// <param name="icon">The icon.</param>
		/// <param name="defaultButton">The default button.</param>
		/// <param name="font">The font of the text.</param>
		/// <param name="timeout">The window timeout in milliseconds. Less than 100 means no timeout.</param>
		public static DialogResult Show(object text, string caption = "Message", bool styled = true, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Exclamation, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, Font font = null, int timeout = 0) {
			if (text == null)
				text = string.Empty;
			if (caption == null)
				caption = string.Empty;
			if (styled)
				return MessageLoop.ShowDialog(new MessageBoxStyledForm(text.ToString().Trim(), caption.Trim(), buttons, icon, defaultButton, font, timeout));
			else
				return MessageLoop.ShowDialog(new MessageBoxForm(text.ToString().Trim(), caption.Trim(), buttons, icon, defaultButton, font, timeout));
		}

		/// <summary>
		/// Shows the specified message box asynchronously.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="caption">The caption.</param>
		/// <param name="styled">Whether to use a more styled interface instead of a simpler one.</param>
		/// <param name="buttons">The buttons.</param>
		/// <param name="icon">The icon.</param>
		/// <param name="defaultButton">The default button.</param>
		/// <param name="font">The font of the text.</param>
		/// <param name="timeout">The window timeout in milliseconds. Less than 100 means no timeout.</param>
		public static Form ShowAsync(object text, string caption = "Message", bool styled = true, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Exclamation, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, Font font = null, int timeout = 0) {
			if (text == null)
				text = string.Empty;
			if (caption == null)
				caption = string.Empty;
			if (styled) {
				MessageBoxStyledForm dialog = new MessageBoxStyledForm(text.ToString().Trim(), caption.Trim(), buttons, icon, defaultButton, font, timeout);
				dialog.TopMost = true;
				dialog.Show();
				return dialog;
			} else {
				MessageBoxForm dialog = new MessageBoxForm(text.ToString().Trim(), caption.Trim(), buttons, icon, defaultButton, font, timeout);
				dialog.TopMost = true;
				dialog.Show();
				return dialog;
			}
		}

		private sealed class MessageBoxForm : Form {
			private MessageBoxDefaultButton defaultButton;
			private StyledButton button1, button2, button3;
			private StyledLabel label;
			private int timeOut, visibleButtonsCount;

			public MessageBoxForm(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, Font font, int timeout) {
				CheckForIllegalCrossThreadCalls = false;
				InitializeComponent();
				Font = font;
				timeOut = timeout;
				Text = caption;
				text = "\n" + text;
				label.Text = text;
				this.defaultButton = defaultButton;
				label.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button1.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button2.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button3.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button1.Click += Button1_Click;
				button2.Click += Button1_Click;
				button3.Click += Button1_Click;
				switch (buttons) {
					case MessageBoxButtons.AbortRetryIgnore:
						visibleButtonsCount = 3;
						button1.Visible = true;
						button1.Text = "Abort";
						button1.DialogResult = DialogResult.Abort;
						button2.Visible = true;
						button2.Text = "Retry";
						button2.DialogResult = DialogResult.Retry;
						button3.Visible = true;
						button3.Text = "Ignore";
						button3.DialogResult = DialogResult.Ignore;
						AcceptButton = button2;
						CancelButton = button1;
						break;
					case MessageBoxButtons.OKCancel:
						visibleButtonsCount = 2;
						button1.Visible = true;
						button1.Text = "OK";
						button1.DialogResult = DialogResult.OK;
						button2.Visible = true;
						button2.Text = "Cancel";
						button2.DialogResult = DialogResult.Cancel;
						AcceptButton = button1;
						CancelButton = button2;
						break;
					case MessageBoxButtons.RetryCancel:
						visibleButtonsCount = 2;
						button1.Visible = true;
						button1.Text = "Retry";
						button1.DialogResult = DialogResult.Retry;
						button2.Visible = true;
						button2.Text = "Cancel";
						button2.DialogResult = DialogResult.Cancel;
						CancelButton = button2;
						break;
					case MessageBoxButtons.YesNo:
						visibleButtonsCount = 2;
						button1.Visible = true;
						button1.Text = "Yes";
						button1.DialogResult = DialogResult.Yes;
						button2.Visible = true;
						button2.Text = "No";
						button2.DialogResult = DialogResult.No;
						AcceptButton = button1;
						CancelButton = button2;
						break;
					case MessageBoxButtons.YesNoCancel:
						visibleButtonsCount = 3;
						button1.Visible = true;
						button1.Text = "Yes";
						button1.DialogResult = DialogResult.Yes;
						button2.Visible = true;
						button2.Text = "No";
						button2.DialogResult = DialogResult.No;
						button3.Visible = true;
						button3.Text = "Cancel";
						button3.DialogResult = DialogResult.Cancel;
						AcceptButton = button1;
						CancelButton = button3;
						break;
					default:
						visibleButtonsCount = 1;
						button1.Visible = true;
						button1.Text = "OK";
						button1.DialogResult = DialogResult.OK;
						AcceptButton = button1;
						break;
				}
				switch (icon) {
					case MessageBoxIcon.Information:
						Icon = SystemIcons.Information;
						break;
					case MessageBoxIcon.Warning:
						Icon = SystemIcons.Warning;
						break;
					case MessageBoxIcon.Error:
						Icon = SystemIcons.Error;
						break;
					case MessageBoxIcon.Question:
						Icon = SystemIcons.Question;
						break;
					default:
						ShowIcon = false;
						break;
				}
				label.Font = Font;
				button1.Font = Font;
				button2.Font = Font;
				button3.Font = Font;
				Size maxSize = Screen.GetWorkingArea(this).Size;
				Size size = label.GetAutoSize(maxSize, false);
				if (size.Width > size.Height * 3)
					size = label.GetAutoSize(new Size(Math.Max(size.Height * 3, maxSize.Width) + 20, maxSize.Height + 20), true);
				ClientSize = new Size(Math.Max(Math.Max(size.Width, TextRenderer.MeasureText(caption, Font).Width), (button1.Width + 4) * visibleButtonsCount) + 20,
					size.Height + label.Margin.Vertical + button1.Height + 12);
				Rectangle screenBounds = Screen.FromControl(this).Bounds;
				Location = new Point(screenBounds.Width / 2 - Width / 2, screenBounds.Height / 2 - Height / 2);
			}

			protected override void OnShown(EventArgs e) {
				base.OnShown(e);
				int buttonIndexToFocus;
				switch (defaultButton) {
					case MessageBoxDefaultButton.Button2:
						buttonIndexToFocus = 2;
						break;
					case MessageBoxDefaultButton.Button3:
						buttonIndexToFocus = 3;
						break;
					default:
						buttonIndexToFocus = 1;
						break;
				}
				if (buttonIndexToFocus > visibleButtonsCount)
					buttonIndexToFocus = visibleButtonsCount;
				switch (buttonIndexToFocus) {
					case 2:
						button2.Focus();
						break;
					case 3:
						button3.Focus();
						break;
					default:
						button1.Focus();
						break;
				}
				if (timeOut >= 100) {
					AsyncTimer timer = new AsyncTimer() {
						Tag = "MessageBoxTimer",
						Interval = timeOut
					};
					timer.Tick += Timer_Tick;
					timer.Running = true;
				}
			}

			protected override void OnClientSizeChanged(EventArgs e) {
				base.OnClientSizeChanged(e);
				StyledLabel label = this.label;
				if (label == null)
					return;
				Size clientSize = ClientSize;
				int textBottom = label.GetAutoSize(new Size(clientSize.Width, 0), true).Height;
				int top = Math.Min(clientSize.Height - (button1.Height + 6), textBottom + (clientSize.Height - (textBottom + button1.Height)) / 2);
				button1.Top = top;
				button2.Top = top;
				button3.Top = top;
				int loc;
				switch (visibleButtonsCount) {
					case 2:
						loc = 2 + clientSize.Width / 2 - (button1.Width + 4);
						button1.Left = loc;
						loc += button1.Width + 4;
						button2.Left = loc;
						break;
					case 3:
						loc = 2 + clientSize.Width / 2 - ((button1.Width + 4) * 3) / 2;
						button1.Left = loc;
						loc += button1.Width + 4;
						button2.Left = loc;
						loc += button2.Width + 4;
						button3.Left = loc;
						break;
					default:
						button1.Left = 2 + clientSize.Width / 2 - button1.Width / 2;
						break;
				}
			}

			private void Button1_Click(object sender, EventArgs e) {
				Close();
			}

			protected override void OnKeyUp(KeyEventArgs e) {
				base.OnKeyUp(e);
				if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.Insert))
					Clipboard.SetText(label.Text);
			}

			/*private static void label_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
				if (e.Link.LinkData == null)
					return;
				try {
					Process.Start(e.Link.LinkData.ToString());
				} catch {
				}
			}*/

			private void Timer_Tick(object sender) {
				((AsyncTimer) sender).Dispose();
				Close();
			}

			private void InitializeComponent() {
				this.button1 = new System.Windows.Forms.StyledButton();
				this.button2 = new System.Windows.Forms.StyledButton();
				this.button3 = new System.Windows.Forms.StyledButton();
				this.label = new System.Windows.Forms.StyledLabel();
				this.SuspendLayout();
				// 
				// button1
				// 
				this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.button1.Location = new System.Drawing.Point(11, 67);
				this.button1.Name = "button1";
				this.button1.Size = new System.Drawing.Size(75, 24);
				this.button1.TabIndex = 0;
				this.button1.Text = "OK";
				this.button1.UseVisualStyleBackColor = true;
				this.button1.Visible = false;
				// 
				// button2
				// 
				this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.button2.Location = new System.Drawing.Point(92, 67);
				this.button2.Name = "button2";
				this.button2.Size = new System.Drawing.Size(75, 24);
				this.button2.TabIndex = 1;
				this.button2.Text = "OK";
				this.button2.UseVisualStyleBackColor = true;
				this.button2.Visible = false;
				// 
				// button3
				// 
				this.button3.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.button3.Location = new System.Drawing.Point(173, 67);
				this.button3.Name = "button3";
				this.button3.Size = new System.Drawing.Size(75, 24);
				this.button3.TabIndex = 2;
				this.button3.Text = "OK";
				this.button3.UseVisualStyleBackColor = true;
				this.button3.Visible = false;
				// 
				// label
				// 
				this.label.AutoSize = false;
				this.label.BackColor = System.Drawing.Color.WhiteSmoke;
				this.label.BorderStyle = System.Windows.Forms.BorderStyle.None;
				this.label.Dock = System.Windows.Forms.DockStyle.Fill;
				this.label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.label.Margin = new System.Windows.Forms.Padding(3);
				this.label.Name = "label";
				this.label.Padding = new System.Windows.Forms.Padding(0, 1, 1, 7);
				this.label.Size = new System.Drawing.Size(200, 20);
				this.label.TextAlign = System.Drawing.ContentAlignment.TopCenter;
				//this.label.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(label_LinkClicked);
				// 
				// MessageBoxForm
				// 
				this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
				this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
				this.BackColor = System.Drawing.Color.WhiteSmoke;
				this.ClientSize = new System.Drawing.Size(260, 102);
				this.Controls.Add(this.label);
				this.Controls.Add(this.button1);
				this.Controls.Add(this.button2);
				this.Controls.Add(this.button3);
				this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
				this.KeyPreview = true;
				this.MaximizeBox = false;
				this.MinimizeBox = false;
				this.MinimumSize = new System.Drawing.Size(25, 25);
				this.Name = "MessageBoxForm";
				this.ShowIcon = true;
				this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
				this.ShowInTaskbar = false;
				this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
				this.Text = "MessageBoxForm";
				this.TopMost = true;
				this.Controls.SetChildIndex(this.button3, 0);
				this.Controls.SetChildIndex(this.button2, 0);
				this.Controls.SetChildIndex(this.button1, 0);
				this.ResumeLayout(false);
				this.PerformLayout();

			}
		}

		private sealed class MessageBoxStyledForm : StyledForm {
			private MessageBoxDefaultButton defaultButton;
			private StyledButton button1, button2, button3;
			private StyledLabel label;
			private int timeOut, visibleButtonsCount;

			public MessageBoxStyledForm(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, Font font, int timeout) {
				InitializeComponent();
				MaximizeEnabled = false;
				MinimizeEnabled = false;
				Font = font;
				timeOut = timeout;
				Text = caption;
				text = "\n" + text;
				label.Text = text;
				this.defaultButton = defaultButton;
				button1.Click += Button1_Click;
				button2.Click += Button1_Click;
				button3.Click += Button1_Click;
				label.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button1.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button2.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				button3.TextRenderingStyle = Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				switch (buttons) {
					case MessageBoxButtons.AbortRetryIgnore:
						visibleButtonsCount = 3;
						button1.Visible = true;
						button1.Text = "Abort";
						button1.DialogResult = DialogResult.Abort;
						button2.Visible = true;
						button2.Text = "Retry";
						button2.DialogResult = DialogResult.Retry;
						button3.Visible = true;
						button3.Text = "Ignore";
						button3.DialogResult = DialogResult.Ignore;
						AcceptButton = button2;
						CancelButton = button1;
						break;
					case MessageBoxButtons.OKCancel:
						visibleButtonsCount = 2;
						button1.Visible = true;
						button1.Text = "OK";
						button1.DialogResult = DialogResult.OK;
						button2.Visible = true;
						button2.Text = "Cancel";
						button2.DialogResult = DialogResult.Cancel;
						AcceptButton = button1;
						CancelButton = button2;
						break;
					case MessageBoxButtons.RetryCancel:
						visibleButtonsCount = 2;
						button1.Visible = true;
						button1.Text = "Retry";
						button1.DialogResult = DialogResult.Retry;
						button2.Visible = true;
						button2.Text = "Cancel";
						button2.DialogResult = DialogResult.Cancel;
						CancelButton = button2;
						break;
					case MessageBoxButtons.YesNo:
						visibleButtonsCount = 2;
						button1.Visible = true;
						button1.Text = "Yes";
						button1.DialogResult = DialogResult.Yes;
						button2.Visible = true;
						button2.Text = "No";
						button2.DialogResult = DialogResult.No;
						AcceptButton = button1;
						CancelButton = button2;
						break;
					case MessageBoxButtons.YesNoCancel:
						visibleButtonsCount = 3;
						button1.Visible = true;
						button1.Text = "Yes";
						button1.DialogResult = DialogResult.Yes;
						button2.Visible = true;
						button2.Text = "No";
						button2.DialogResult = DialogResult.No;
						button3.Visible = true;
						button3.Text = "Cancel";
						button3.DialogResult = DialogResult.Cancel;
						AcceptButton = button1;
						CancelButton = button3;
						break;
					default:
						visibleButtonsCount = 1;
						button1.Visible = true;
						button1.Text = "OK";
						button1.DialogResult = DialogResult.OK;
						AcceptButton = button1;
						break;
				}
				switch (icon) {
					case MessageBoxIcon.Information:
						Icon = SystemIcons.Information;
						break;
					case MessageBoxIcon.Warning:
						Icon = SystemIcons.Warning;
						break;
					case MessageBoxIcon.Error:
						Icon = SystemIcons.Error;
						break;
					case MessageBoxIcon.Question:
						Icon = SystemIcons.Question;
						break;
					default:
						ShowIcon = false;
						break;
				}
				label.Font = Font;
				button1.Font = Font;
				button2.Font = Font;
				button3.Font = Font;
				Size maxSize = Screen.GetBounds(this).Size;
				Size size = label.GetAutoSize(maxSize, false);
				if (size.Width > size.Height * 3)
					size = label.GetAutoSize(new Size(Math.Max(size.Height * 3, maxSize.Width) + 20, maxSize.Height + 20), true);
				ViewSize = new Size(Math.Max(Math.Max(size.Width, TitleLabel.GetAutoSize().Width), (button1.Width + 4) * visibleButtonsCount) + 20,
					size.Height + label.Margin.Vertical + button1.Height + 12);
			}

			private void Button1_Click(object sender, EventArgs e) {
				CloseAsync();
			}

			protected override void OnShown(EventArgs e) {
				base.OnShown(e);
				int buttonIndexToFocus;
				switch (defaultButton) {
					case MessageBoxDefaultButton.Button2:
						buttonIndexToFocus = 2;
						break;
					case MessageBoxDefaultButton.Button3:
						buttonIndexToFocus = 3;
						break;
					default:
						buttonIndexToFocus = 1;
						break;
				}
				if (buttonIndexToFocus > visibleButtonsCount)
					buttonIndexToFocus = visibleButtonsCount;
				switch (buttonIndexToFocus) {
					case 2:
						button2.Focus();
						break;
					case 3:
						button3.Focus();
						break;
					default:
						button1.Focus();
						break;
				}
				if (timeOut >= 100) {
					AsyncTimer timer = new AsyncTimer() {
						Tag = "MessageBoxTimer",
						Interval = timeOut
					};
					timer.Tick += Timer_Tick;
					timer.Running = true;
				}
			}

			protected override void OnClientSizeChanged(EventArgs e) {
				base.OnClientSizeChanged(e);
				StyledLabel label = this.label;
				if (label == null)
					return;
				Size clientSize = ClientSize;
				Size viewSize = ViewSize;
				int textBottom = label.GetAutoSize(new Size(ViewSize.Width, 0), true).Height;
				int top = ViewPortLocation.Y + Math.Min(viewSize.Height - (button1.Height + 6), textBottom + (viewSize.Height - (textBottom + button1.Height)) / 2);
				button1.Top = top;
				button2.Top = top;
				button3.Top = top;
				int loc;
				switch (visibleButtonsCount) {
					case 2:
						loc = 2 + clientSize.Width / 2 - (button1.Width + 4);
						button1.Left = loc;
						loc += button1.Width + 4;
						button2.Left = loc;
						break;
					case 3:
						loc = 2 + clientSize.Width / 2 - ((button1.Width + 4) * 3) / 2;
						button1.Left = loc;
						loc += button1.Width + 4;
						button2.Left = loc;
						loc += button2.Width + 4;
						button3.Left = loc;
						break;
					default:
						button1.Left = 2 + clientSize.Width / 2 - button1.Width / 2;
						break;
				}
			}

			protected override void OnKeyUp(KeyEventArgs e) {
				base.OnKeyUp(e);
				if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.Insert))
					Clipboard.SetText(label.Text);
			}

			/*private static void label_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
				if (e.Link.LinkData == null)
					return;
				try {
					Process.Start(e.Link.LinkData.ToString());
				} catch {
				}
			}*/

			private void Timer_Tick(object sender) {
				((AsyncTimer) sender).Dispose();
				CloseAsync();
			}

			/// <summary>
			/// Initializes the window and its controls.
			/// </summary>
			private void InitializeComponent() {
				this.button1 = new System.Windows.Forms.StyledButton();
				this.button2 = new System.Windows.Forms.StyledButton();
				this.button3 = new System.Windows.Forms.StyledButton();
				this.label = new System.Windows.Forms.StyledLabel();
				this.SuspendLayout();
				// 
				// button1
				// 
				this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.button1.Location = new System.Drawing.Point(11, 67);
				this.button1.Name = "button1";
				this.button1.Size = new System.Drawing.Size(75, 24);
				this.button1.TabIndex = 0;
				this.button1.Text = "OK";
				this.button1.UseVisualStyleBackColor = true;
				this.button1.Visible = false;
				// 
				// button2
				// 
				this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.button2.Location = new System.Drawing.Point(92, 67);
				this.button2.Name = "button2";
				this.button2.Size = new System.Drawing.Size(75, 24);
				this.button2.TabIndex = 1;
				this.button2.Text = "OK";
				this.button2.UseVisualStyleBackColor = true;
				this.button2.Visible = false;
				// 
				// button3
				// 
				this.button3.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.button3.Location = new System.Drawing.Point(173, 67);
				this.button3.Name = "button3";
				this.button3.Size = new System.Drawing.Size(75, 24);
				this.button3.TabIndex = 2;
				this.button3.Text = "OK";
				this.button3.UseVisualStyleBackColor = true;
				this.button3.Visible = false;
				// 
				// label
				// 
				this.label.AutoSize = false;
				this.label.BackColor = System.Drawing.Color.WhiteSmoke;
				this.label.BorderStyle = System.Windows.Forms.BorderStyle.None;
				this.label.Dock = System.Windows.Forms.DockStyle.Fill;
				this.label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
				this.label.Margin = new System.Windows.Forms.Padding(3);
				this.label.Name = "label";
				this.label.Padding = new System.Windows.Forms.Padding(0, 1, 1, 7);
				this.label.Size = new System.Drawing.Size(200, 20);
				this.label.TextAlign = System.Drawing.ContentAlignment.TopCenter;
				//this.label.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(label_LinkClicked);
				// 
				// MessageBoxFormWhite
				// 
				this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
				this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
				this.BackColor = System.Drawing.Color.White;
				this.ClientSize = new System.Drawing.Size(260, 102);
				this.Controls.Add(this.label);
				this.Controls.Add(this.button1);
				this.Controls.Add(this.button2);
				this.Controls.Add(this.button3);
				this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
				this.KeyPreview = true;
				this.MaximizeBox = false;
				this.MinimizeBox = false;
				this.MinimumSize = new System.Drawing.Size(25, 25);
				this.Name = "MessageBoxForm";
				this.ShowIcon = true;
				this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
				this.ShowInTaskbar = false;
				this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
				this.Text = "MessageBoxForm";
				this.TopMost = true;
				this.Controls.SetChildIndex(this.button3, 0);
				this.Controls.SetChildIndex(this.button2, 0);
				this.Controls.SetChildIndex(this.button1, 0);
				this.ResumeLayout(false);
				this.PerformLayout();

			}
		}
	}
}