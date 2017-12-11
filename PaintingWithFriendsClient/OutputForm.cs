using System.Windows.Forms;

namespace PaintingWithFriendsClient
{
    public partial class OutputForm : Form
    {
        // Static so if the user closes the form, it still saves the text
        static string outputString = "";

        public OutputForm()
        {
            InitializeComponent();

            // If the user closes the form, make sure you put the text back in
            outputBox.AppendText(outputString);
        }

        // Log the stuff!
        public void OutputText(string text)
        {
            outputString += text + "\r\n";
            outputBox.AppendText(text + "\r\n");
        }
    }
}
