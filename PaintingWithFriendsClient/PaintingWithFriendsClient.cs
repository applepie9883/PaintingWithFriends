using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Lidgren.Network;
using PaintingWithFriendsCommon;

namespace PaintingWithFriendsClient
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class PaintingWithFriendsClient : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // TODO: Change the font!
        SpriteFont defaultFont;

        OutputForm outputForm;
        static ConnectForm connectForm = Program.connectForm;

        NetClient client;

        MouseState oldMouseState;
        KeyboardState oldKeyboardState;

        public delegate void ColorDelegate(Color mainColor);

        public ColorDelegate MainColorDelegate;
        public ColorDelegate SecondaryColorDelegate;
        public ColorDelegate ResetColorDelegate;

        // TODO: Maybe change the paint image and world coordinate system I am using

        ButtonHandler toolBar;
        PaintableSprite paintPallet;
        PaintableSprite paintImage;
        PaintingWithFriendsCommon.PaintBrush paintBrush;
        PaintingWithFriendsCommon.PaintBrush eraseBrush;

        Random random;

        public PaintingWithFriendsClient()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.Title = "Painting With Friends";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f);

            InitializeClient();

            // Initialize the random number generator
            random = new Random(DateTime.Now.Millisecond);

            connectForm.Show();

            // Allow the user to see their mouse
            IsMouseVisible = true;

            oldMouseState = Mouse.GetState();
            oldKeyboardState = Keyboard.GetState();

            MainColorDelegate = new ColorDelegate(theColor => { paintBrush.brushColor = theColor; });
            SecondaryColorDelegate = new ColorDelegate(theColor => { eraseBrush.brushColor = theColor; });
            ResetColorDelegate = new ColorDelegate(InitializePaintImage);

            outputForm = new OutputForm();

            // Set the brush color to black
            paintBrush = new PaintBrush(Color.Black, 255, 1.0f, 1.0f, BrushType.circle);
            eraseBrush = new PaintBrush(Color.White, 255, 1.0f, 1.0f, BrushType.circle);

            // Initialize the paint image
            InitializePaintImage(Color.White);

            // Initialize the paint pallet
            InitializePaintPallet();

            base.Initialize();
        }

        // Initialize the client
        private void InitializeClient()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("PaintingWithFriends");
            config.AutoFlushSendQueue = false;
            client = new NetClient(config);
            client.Start();
        }

        // Initialize the toolbar
        private void InitializeToolbar()
        {
            toolBar = new ButtonHandler();

            // This button resets the paint image
            GMTextButton resetButton = new GMTextButton(defaultFont, Color.Red);
            resetButton.GenerateTexture(GraphicsDevice, 20, 20, Color.Black);
            resetButton.SetText("R");
            resetButton.OnClicked += new EventHandler(ResetPaintImage);

            // This button opens the color picker dialog for the main brush
            GMTextButton colorPickerButtonMain = new GMTextButton(defaultFont, Color.White);
            colorPickerButtonMain.GenerateTexture(GraphicsDevice, 20, 20, Color.Red);
            colorPickerButtonMain.SetText("C");
            colorPickerButtonMain.OnClicked += new EventHandler(OpenColorPickerDialogMain);

            // Put all of the buttons into an array, and add them to the button handler
            GMTextButton[,] buttonArray = new GMTextButton[1, 2];
            buttonArray[0, 0] = resetButton;
            buttonArray[0, 1] = colorPickerButtonMain;
            toolBar.AddWithSpacing(buttonArray, new Vector2(5, 20), 5);
        }

        // Initialize the paint image
        private void InitializePaintImage(Color imageColor)
        {
            paintImage = new PaintableSprite();
            paintImage.upperLeft = new Vector2(75, 0);
            paintImage.GenerateTexture(GraphicsDevice, GraphicsDevice.Viewport.Width - 75, GraphicsDevice.Viewport.Height, imageColor);
        }

        // Initialize the paint pallet
        private void InitializePaintPallet()
        {
            paintPallet = new PaintableSprite();
            paintPallet.upperLeft = new Vector2(0, GraphicsDevice.Viewport.Height - 70);
            paintPallet.GenerateTexture(GraphicsDevice, 70, 70, Color.White);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            defaultFont = Content.Load<SpriteFont>("Assets/Fonts/defaultFont");

            // Initialize the toolbar, not it is in LoadContent because it uses content manager content
            InitializeToolbar();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            try
            {
                MouseState currentMouseState = Mouse.GetState();
                KeyboardState currentKeyboardState = Keyboard.GetState();

                HandleIM();

                // Make the brush more opaque
                if (currentKeyboardState.IsKeyDown(Keys.Add))
                    paintBrush.SetAlpha((byte)(paintBrush.brushColor.A + 1));

                // Make the brush more transparent
                if (currentKeyboardState.IsKeyDown(Keys.Subtract))
                    paintBrush.SetAlpha((byte)(paintBrush.brushColor.A - 1));

                // Set the alpha of the brush to 255 (opaque)
                if (currentKeyboardState.IsKeyDown(Keys.PageUp))
                    paintBrush.SetAlpha(255);

                // Set the alpha of the brush to 0 (transparent)
                if (currentKeyboardState.IsKeyDown(Keys.PageDown))
                    paintBrush.SetAlpha(0);

                // Set the brush color the the color of the pixel under the mouse
                if (currentKeyboardState.IsKeyDown(Keys.P) && oldKeyboardState.IsKeyUp(Keys.P))
                    paintBrush.brushColor = paintImage.GetPixelColor(new Vector2(currentMouseState.Position.X, currentMouseState.Position.Y));

                // Open color picker dialog for main brush
                if (currentKeyboardState.IsKeyDown(Keys.C) && oldKeyboardState.IsKeyUp(Keys.C))
                {
                    Program.ShowMainColorDialog();
                }

                // Open color picker dialog for eraser
                if (currentKeyboardState.IsKeyDown(Keys.E) && oldKeyboardState.IsKeyUp(Keys.E))
                {
                    Program.ShowSecondaryColorDialog();
                }

                // Toggle the output form
                if (currentKeyboardState.IsKeyDown(Keys.OemTilde) && oldKeyboardState.IsKeyUp(Keys.OemTilde))
                {
                    if (outputForm.IsDisposed || outputForm == null)
                        outputForm = new OutputForm();

                    if (!outputForm.Visible)
                        outputForm.Show();
                    else
                        outputForm.Hide();
                }

                // Change the brush size using the scroll wheel
                int dV = (currentMouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue) / 100;
                paintBrush.brushSize = (paintBrush.brushSize + dV < 1.0f ? 1.0f : (paintBrush.brushSize + dV > 25.0f ? 25.0f : paintBrush.brushSize + dV));
                eraseBrush.brushSize = paintBrush.brushSize;

                // Change between square and circle brushes
                if (currentKeyboardState.IsKeyDown(Keys.B) && oldKeyboardState.IsKeyUp(Keys.B))
                {
                    if (paintBrush.brushType == BrushType.circle)
                    {
                        paintBrush.brushType = BrushType.square;
                        eraseBrush.brushType = BrushType.square;
                    }
                    else
                    {
                        paintBrush.brushType = BrushType.circle;
                        eraseBrush.brushType = BrushType.circle;
                    }
                }

                // If the user is clicking, draw stuff!
                if (currentMouseState.LeftButton == ButtonState.Pressed)
                {
                    paintPallet.PaintLine(oldMouseState.Position.ToVector2(), currentMouseState.Position.ToVector2(), paintBrush);
                    DrawLine(oldMouseState.Position.ToVector2(), currentMouseState.Position.ToVector2(), paintBrush);
                }
                else if (currentMouseState.RightButton == ButtonState.Pressed)
                {
                    paintPallet.PaintLine(oldMouseState.Position.ToVector2(), currentMouseState.Position.ToVector2(), eraseBrush);
                    DrawLine(oldMouseState.Position.ToVector2(), currentMouseState.Position.ToVector2(), eraseBrush);
                }

                // Draw some kind of strange circle or square
                if (currentKeyboardState.IsKeyDown(Keys.W) && oldKeyboardState.IsKeyUp(Keys.W))
                {
                    paintPallet.PaintStrangeShape(currentMouseState.Position.ToVector2(), paintBrush, eraseBrush, DateTime.Now.Millisecond, false);
                    DrawStrangeShape(currentMouseState.Position.ToVector2(), paintBrush, eraseBrush, DateTime.Now.Millisecond, false);
                }

                // Draw some kind of strange circle and square combo
                if (currentKeyboardState.IsKeyDown(Keys.S) && oldKeyboardState.IsKeyUp(Keys.S))
                {
                    paintPallet.PaintStrangeShape(currentMouseState.Position.ToVector2(), paintBrush, eraseBrush, DateTime.Now.Millisecond, true);
                    DrawStrangeShape(currentMouseState.Position.ToVector2(), paintBrush, eraseBrush, DateTime.Now.Millisecond, true);
                }

                // Update all of the buttons in the toolbar
                toolBar.Update(currentMouseState, oldMouseState);

                oldMouseState = currentMouseState;
                oldKeyboardState = currentKeyboardState;
            }
            catch (Exception e)
            {
                OutputText("Exception: " + e.Message);
            }
            

            base.Update(gameTime);
        }

        // Mostly the same as server side
        private void HandleIM()
        {
            NetIncomingMessage im = client.ReadMessage();

            if (im != null)
            {
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        string debugText = im.ReadString();
                        OutputText("Debug: " + debugText);
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        string warningText = im.ReadString();
                        OutputText("Warning: " + warningText);
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        string errorText = im.ReadString();
                        OutputText("Error: " + errorText);
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

                        // Get the reason
                        string reason = im.ReadString();

                        if (status == NetConnectionStatus.Connected)
                            RequestImageData();

                        OutputText(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);
                        break;
                    case NetIncomingMessageType.Data:
                        MessageDataType dataType = (MessageDataType)im.ReadByte();

                        switch (dataType)
                        {
                            case MessageDataType.Image:
                                ReceiveImageData(im);
                                break;
                            case MessageDataType.Line:
                                // TODO: Possibly store brushes of other clients to cut down on network load
                                paintImage.PaintLine(new Vector2(im.ReadFloat(), im.ReadFloat()) + paintImage.upperLeft, new Vector2(im.ReadFloat(), im.ReadFloat()) + paintImage.upperLeft, new PaintBrush(new Color(im.ReadByte(), im.ReadByte(), im.ReadByte()), im.ReadByte(), im.ReadFloat(), im.ReadFloat(), (BrushType)im.ReadByte()));
                                break;
                            case MessageDataType.RandomShape:
                                DrawStrangeShape(new Vector2(im.ReadFloat(), im.ReadFloat()), new PaintBrush(new Color(im.ReadByte(), im.ReadByte(), im.ReadByte()), im.ReadByte(), im.ReadFloat(), im.ReadFloat(), (BrushType)im.ReadByte()), new PaintBrush(new Color(im.ReadByte(), im.ReadByte(), im.ReadByte()), im.ReadByte(), 1, im.ReadFloat(), BrushType.circle), im.ReadInt32(), im.ReadBoolean());
                                break;
                            default:
                                break;
                        }

                        break;
                    default:
                        // Some kind of unhandled type
                        OutputText(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " : unhandled message type.");
                        break;
                }
            }

            client.FlushSendQueue();
            client.Recycle(im);
        }

        // Request initialization data from the server
        private void RequestImageData()
        {
            NetOutgoingMessage om = client.CreateMessage();
            om.Write((byte)MessageDataType.Image);
            client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
        }

        private void ReceiveImageData(NetIncomingMessage im)
        {
            OutputText("Received initialization data");
            paintImage.SetTexture(new Texture2D(GraphicsDevice, im.ReadInt32(), im.ReadInt32()));
            int byteNum = im.ReadInt32();
            paintImage.SetTextureData(im.ReadBytes(byteNum));

            NetOutgoingMessage om = client.CreateMessage();
            om.Write((byte)MessageDataType.Ready);

            client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
        }

        // Connects to the server
        public void Connect(string ip, int port)
        {
            client.Connect(ip, port);
        }

        // Disconnects from the server
        public void Disconnect()
        {
            client.Disconnect("bye");
        }

        // Draw and send lines
        private void DrawLine(Vector2 oldPosition, Vector2 newPosition, PaintBrush receiveBrush)
        {
            if (paintImage.PaintLine(oldPosition, newPosition, receiveBrush))
                SendLine(oldPosition - paintImage.upperLeft, newPosition - paintImage.upperLeft, receiveBrush);
        }

        // Draw a strange shape and send it to the server
        private void DrawStrangeShape(Vector2 position, PaintBrush primaryBrush, PaintBrush secondaryBrush, int seed, bool isRandomShape)
        {
            if (paintImage.PaintStrangeShape(position, primaryBrush, secondaryBrush, seed, isRandomShape))
                SendStrangeShape(position, primaryBrush, secondaryBrush, seed, isRandomShape);
        }

        // Send a line to the server
        private void SendLine(Vector2 oldPosition, Vector2 newPosition, PaintBrush sendBrush)
        {
            if (client.ServerConnection != null)
            {
                NetOutgoingMessage om = client.CreateMessage();
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

                client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
                client.FlushSendQueue();
            }
        }

        // Send a shape to the server
        private void SendStrangeShape(Vector2 position, PaintBrush primaryBrush, PaintBrush secondaryBrush, int seed, bool isRandomShape)
        {
            if (client.ServerConnection != null)
            {
                NetOutgoingMessage om = client.CreateMessage();
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

                client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
                client.FlushSendQueue();
            }
        }

        // Output text to the console
        private void OutputText(string text)
        {
            if (outputForm == null || outputForm.IsDisposed)
                outputForm = new OutputForm();

            outputForm.OutputText(text);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            spriteBatch.Begin();

            try
            {
                paintImage.Draw(spriteBatch);
                spriteBatch.DrawString(defaultFont, paintBrush.brushColor.A.ToString(), new Vector2(0, 0), Color.Black);
                toolBar.Draw(spriteBatch);
                paintPallet.Draw(spriteBatch);
            }
            catch (Exception e)
            {
                OutputText("Error drawing: " + e.Message);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Buttons

        private void ResetPaintImage(object sender, EventArgs e)
        {
            if (client.ServerConnection == null)
            {
                Program.ShowResetColorDialog();
            }
        }

        private void OpenColorPickerDialogMain(object sender, EventArgs e)
        {
            Program.ShowMainColorDialog();
        }

        #endregion
    }
}
