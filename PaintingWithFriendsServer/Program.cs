using GM.SpriteLibrary;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PaintingWithFriendsCommon;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PaintingWithFriendsServer
{
    static class Program
    {
        private static NetServer server;

        private static ServerForm serverForm;
        // TODO: Look at this and possibly make it better, maybe change from NetConnection 
        // to long(connection.RemoteUniqueIdentifier) or string(NetUtility.ToHexString(connection.RemoteUniqueIdentifier))
        private static List<NetConnection> notReady;

        private static GraphicsDevice device;
        private static PaintableSprite paintImage;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            serverForm = new ServerForm();

            InitializeServer();
            InitializeGraphicsDevice();
            InitializePaintImage(Color.White);

            Application.Run(serverForm);
        }

        // Initialize the graphics device
        private static void InitializeGraphicsDevice()
        {
            device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, new PresentationParameters() { BackBufferWidth = 800, BackBufferHeight = 480 });
        }

        // Initialize the paint image
        public static void InitializePaintImage(Color imageColor)
        {
            serverForm.OutputText("Initialized image: R " + imageColor.R + " G " + imageColor.G + " B " + imageColor.B);

            paintImage = new PaintableSprite();
            paintImage.upperLeft = new Vector2(75, 0);
            paintImage.GenerateTexture(device, device.Viewport.Width - 75, device.Viewport.Height, imageColor);

            SendImageData(null);
        }

        // Initialize the server
        private static void InitializeServer()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("PaintingWithFriends");
            config.MaximumConnections = 10;
            config.Port = 14242;
            server = new NetServer(config);
            server.Start();

            notReady = new List<NetConnection>();

            // TODO: replace this timer with a Thread and while loop
            System.Timers.Timer serverTimer = new System.Timers.Timer();
            serverTimer.Elapsed += new System.Timers.ElapsedEventHandler(HandleIM);
            serverTimer.Interval = 5; // 200 times per second, which gives us 20 updates per max 10 people per second
            serverTimer.Start();
        }

        // Handle incoming messages
        private static void HandleIM(object sender, EventArgs e)
        {
            NetIncomingMessage im = server.ReadMessage();

            if (im != null)
            {
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        string debugText = im.ReadString();
                        serverForm.OutputText("Debug: " + debugText);
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        string warningText = im.ReadString();
                        serverForm.OutputText("Warning: " + warningText);
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        string errorText = im.ReadString();
                        serverForm.OutputText("Error: " + errorText);
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        // Get the status, usually connected or disconnected
                        NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

                        // If the client has just connected
                        if (status == NetConnectionStatus.Connected)
                        {
                            AddNotReady(im.SenderConnection);
                        }

                        // Get the reason
                        string reason = im.ReadString();

                        // Output to the console
                        serverForm.OutputText(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);
                        break;
                    case NetIncomingMessageType.Data:
                        MessageDataType dataType = (MessageDataType)im.ReadByte();

                        switch (dataType)
                        {
                            case MessageDataType.Image:
                                serverForm.OutputText(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " requested initialization data");
                                SendImageData(im.SenderConnection);
                                break;
                            case MessageDataType.Ready:
                                serverForm.OutputText(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " is ready");
                                RemoveNotReady(im.SenderConnection);
                                break;
                            case MessageDataType.Line:
                                // TODO: Possibly store brushes of other clients to cut down on network load
                                DrawLine(new Vector2(im.ReadFloat(), im.ReadFloat()), new Vector2(im.ReadFloat(), im.ReadFloat()), new PaintBrush(new Color(im.ReadByte(), im.ReadByte(), im.ReadByte()), im.ReadByte(), im.ReadFloat(), im.ReadFloat(), (BrushType)im.ReadByte()), im.SenderConnection);
                                break;
                            case MessageDataType.RandomShape:
                                DrawStrangeShape(new Vector2(im.ReadFloat(), im.ReadFloat()), new PaintBrush(new Color(im.ReadByte(), im.ReadByte(), im.ReadByte()), im.ReadByte(), im.ReadFloat(), im.ReadFloat(), (BrushType)im.ReadByte()), new PaintBrush(new Color(im.ReadByte(), im.ReadByte(), im.ReadByte()), im.ReadByte(), 1, im.ReadFloat(), BrushType.circle), im.ReadInt32(), im.ReadBoolean(), im.SenderConnection);
                                break;
                            default:
                                break;
                        }

                        break;
                    default:
                        // Some kind of unhandled type
                        serverForm.OutputText(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " : unhandled message type.");
                        break;
                }
                // Don't fill up the RAM
                server.Recycle(im);
            }
        }

        // Receive line data
        private static void DrawLine(Vector2 oldPosition, Vector2 newPosition, PaintBrush receiveBrush, NetConnection sc)
        {
            if (!notReady.Contains(sc))
                if (paintImage.PaintLine(oldPosition + paintImage.upperLeft, newPosition + paintImage.upperLeft, receiveBrush))
                    SendLine(oldPosition, newPosition, receiveBrush, sc);
        }

        // Draw a strange shape and send it to the clients
        private static void DrawStrangeShape(Vector2 position, PaintBrush primaryBrush, PaintBrush secondaryBrush, int seed, bool isRandomShape, NetConnection sc)
        {
            if (!notReady.Contains(sc))
                if (paintImage.PaintStrangeShape(position, primaryBrush, secondaryBrush, seed, isRandomShape))
                    SendStrangeShape(position, primaryBrush, secondaryBrush, seed, isRandomShape, sc);
        }

        // Send initialization data to the clients
        private static void SendImageData(NetConnection sc)
        {
            Texture2D spriteTexture = paintImage.GetTexture();
            NetOutgoingMessage om = server.CreateMessage();
            om.Write((byte)MessageDataType.Image);
            byte[] theData = new byte[spriteTexture.Width * spriteTexture.Height * 4];
            spriteTexture.GetData(theData);
            om.Write(spriteTexture.Width);
            om.Write(spriteTexture.Height);
            om.Write(theData.Length);
            om.Write(theData);

            if (sc == null)
            {
                notReady = server.Connections;
                server.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                AddNotReady(sc);
                server.SendMessage(om, sc, NetDeliveryMethod.ReliableOrdered);
            }
        }

        // Send a line to the clients
        private static void SendLine(Vector2 oldPosition, Vector2 newPosition, PaintBrush sendBrush, NetConnection sc)
        {
            // Make sure the pixel is not outside the bounds of the image
            NetOutgoingMessage om = server.CreateMessage();
            om.Write((byte)MessageDataType.Line);
            om.Write(oldPosition.X);
            om.Write(oldPosition.Y);
            om.Write(newPosition.X);
            om.Write(newPosition.Y);
            om.Write(sendBrush.brushColor.R);
            om.Write(sendBrush.brushColor.G);
            om.Write(sendBrush.brushColor.B);
            om.Write(sendBrush.brushColor.A);
            om.Write(sendBrush.brushSize);
            om.Write(sendBrush.hardness);
            om.Write((byte)sendBrush.brushType);

            if (sc == null)
            {
                server.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                server.SendToAll(om, sc, NetDeliveryMethod.ReliableOrdered, 1);
            }
        }

        // Send a shape to the clients
        private static void SendStrangeShape(Vector2 position, PaintBrush primaryBrush, PaintBrush secondaryBrush, int seed, bool isRandomShape, NetConnection sc)
        {
            NetOutgoingMessage om = server.CreateMessage();
            om.Write((byte)MessageDataType.RandomShape);
            om.Write(position.X);
            om.Write(position.Y);
            om.Write(primaryBrush.brushColor.R);
            om.Write(primaryBrush.brushColor.G);
            om.Write(primaryBrush.brushColor.B);
            om.Write(primaryBrush.brushColor.A);
            om.Write(primaryBrush.brushSize);
            om.Write(primaryBrush.hardness);
            om.Write((byte)primaryBrush.brushType);
            om.Write(secondaryBrush.brushColor.R);
            om.Write(secondaryBrush.brushColor.G);
            om.Write(secondaryBrush.brushColor.B);
            om.Write(secondaryBrush.brushColor.A);
            om.Write(secondaryBrush.hardness);
            om.Write(seed);
            om.Write(isRandomShape);

            if (sc == null)
            {
                server.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                server.SendToAll(om, sc, NetDeliveryMethod.ReliableOrdered, 1);
            }
        }

        // Add a connection to notReady
        private static void AddNotReady(NetConnection sc)
        {
            // If notReady does not already contain sc
            if (!notReady.Contains(sc))
            {
                notReady.Add(sc);
            }
        }

        // Remove a connection from notReady
        private static void RemoveNotReady(NetConnection sc)
        {
            // If notReady contains sc
            if (notReady.Contains(sc))
            {
                notReady.Remove(sc);
            }
        }
    }
}
