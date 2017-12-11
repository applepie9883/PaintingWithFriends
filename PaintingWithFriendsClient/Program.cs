using System;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace PaintingWithFriendsClient
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        public static ColorDialog mainColorDialog;
        public static ColorDialog secondaryColorDialog;
        public static ColorDialog resetColorDialog;
        public static ConnectForm connectForm;
        public static PaintingWithFriendsClient paintingWithFriendsClient;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            mainColorDialog = new ColorDialog();
            secondaryColorDialog = new ColorDialog();
            resetColorDialog = new ColorDialog();
            connectForm = new ConnectForm();

            paintingWithFriendsClient = new PaintingWithFriendsClient();
            paintingWithFriendsClient.Run();
        }

        public static void ShowMainColorDialog()
        {
            if ((string)mainColorDialog.Tag != "opened")
            {
                Task colorTask = new Task(() =>
                {
                    mainColorDialog.Tag = "opened";
                    mainColorDialog.ShowDialog();
                    paintingWithFriendsClient.MainColorDelegate(new Microsoft.Xna.Framework.Color(mainColorDialog.Color.R, mainColorDialog.Color.G, mainColorDialog.Color.B, mainColorDialog.Color.A));
                    mainColorDialog.Tag = "closed";
                });

                colorTask.Start();
            }
        }

        public static void ShowSecondaryColorDialog()
        {
            if ((string)secondaryColorDialog.Tag != "opened")
            {
                Task colorTask = new Task(() =>
                {
                    secondaryColorDialog.Tag = "opened";
                    secondaryColorDialog.ShowDialog();
                    paintingWithFriendsClient.SecondaryColorDelegate(new Microsoft.Xna.Framework.Color(secondaryColorDialog.Color.R, secondaryColorDialog.Color.G, secondaryColorDialog.Color.B, secondaryColorDialog.Color.A));
                    secondaryColorDialog.Tag = "closed";
                });

                colorTask.Start();
            }
        }

        public static void ShowResetColorDialog()
        {
            if ((string)resetColorDialog.Tag != "opened")
            {
                Task colorTask = new Task(() =>
                {
                    resetColorDialog.Tag = "opened";
                    resetColorDialog.ShowDialog();
                    paintingWithFriendsClient.ResetColorDelegate(new Microsoft.Xna.Framework.Color(resetColorDialog.Color.R, resetColorDialog.Color.G, resetColorDialog.Color.B, resetColorDialog.Color.A));
                    resetColorDialog.Tag = "closed";
                });

                colorTask.Start();
            }
        }

        // Called by ConnectForm
        public static void Connect(string ip, int port)
        {
            paintingWithFriendsClient.Connect(ip, port);
        }

        // Called by ConnectForm
        public static void Disconnect()
        {
            paintingWithFriendsClient.Disconnect();
        }
    }
#endif
}
