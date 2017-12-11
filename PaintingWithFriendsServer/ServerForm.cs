using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PaintingWithFriendsServer
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
            InitializeComponent();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Enter))
            {
                ProcessInputBox();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            ProcessInputBox();
        }

        private void ProcessInputBox()
        {
            ProcessCommand(InputBox.Text);
            InputBox.Clear();
        }

        private void ProcessCommand(string command)
        {
            // TODO: Finish optimizing this, and maybe refactor

            OutputText("");
            OutputText(command);

            if (command.StartsWith("help ") || command == "help") // Show help for the commands
            {
                OutputText(""); // New line

                if (command.Length > "help ".Length) // If the commands is more than just "help" ie: "help reset"
                {
                    command = command.Substring(5); // command is now whatever was after "help " (if anything)

                    if (command.StartsWith("reset")) // Show help for the reset command
                    {
                        OutputText(string.Format("{0,-25} Resets the image to the specified color", "reset"));
                        OutputText(string.Format("{0,-25} reset color", "usage"));
                        OutputText(string.Format("{0,-25} reset r g b", ""));
                    }
                }
                else // If the command was just "help"
                {
                    OutputText("Commands:"); // Show a list of commands
                    OutputText(string.Format("{0,-25} Resets the image to the specified color", "reset"));
                }
            }
            else if (command.StartsWith("reset ") || command == "reset") // Reset the image
            {
                if (command.Length > "reset ".Length) // If the command is more than just "reset" ie: "reset White"
                {
                    command = command.Substring(6); // Command is now whatever was after "reset " (if anything)
                    
                    if (char.IsLetter(command[0]))
                    {
                        var f = typeof(Microsoft.Xna.Framework.Color).GetProperty(command);

                        if (f != null)
                        {
                            Program.InitializePaintImage((Microsoft.Xna.Framework.Color)f.GetValue(f));
                        }
                    }
                    else
                    {
                        Match numMatch = Regex.Match(command, @"\d{1,3}\s\d{1,3}\s\d{1,3}\z");

                        if (numMatch.Success && numMatch.Index == 0)
                        {
                            int[] values = command.Split(' ').Select(int.Parse).ToArray();

                            Program.InitializePaintImage(new Microsoft.Xna.Framework.Color(values[0], values[1], values[2]));
                        }
                    }
                }
                else // If the command is just "reset"
                {
                    OutputText(string.Format("{0,-25} reset color", "usage"));
                    OutputText(string.Format("{0,-25} reset r g b", ""));
                }
            }
        }

        private delegate void OutputTextCallback(string text);

        // Log the stuff!
        public void OutputText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (OutputBox.InvokeRequired)
            {
                OutputTextCallback d = new OutputTextCallback(OutputText);
                Invoke(d, new object[] { text });
            }
            else
            {
                OutputBox.AppendText(text + "\r\n");
            }
        }
    }
}
