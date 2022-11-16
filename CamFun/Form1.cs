using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using System.Threading;
using System.Diagnostics;

namespace CamFun
{
    public partial class Form1 : Form
    {
        OpenCvSharp.OpenCVException thing;
        private KeyHandler ghk;
        private KeyHandler ghk2;
        CameraModule cameraModule;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ghk = new KeyHandler(Keys.PageDown, this);
            ghk.Register();

            ghk2 = new KeyHandler(Keys.PageUp, this);
            ghk2.Register();

            cameraModule = new CameraModule();
            try
            {
                cameraModule.Init();
                cameraModule.ShowImage();
                //cameraModule.ShowImage(manipulatedImage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Oops something happened! {0}", ex.Message);
            }
            finally
            {
                cameraModule.Release();
            }
        }

        private void HandleHotkey(IntPtr ptr)
        {
            // Do stuff...
            Debug.WriteLine("Key: " + (int)ptr);
            cameraModule.handleKey(ptr);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                HandleHotkey(m.LParam);
            base.WndProc(ref m);
        }
    }
}
