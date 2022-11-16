using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System.Diagnostics;

using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CamFun
{
    class CameraModule
    {

        

        FrameSource frameSource;
        VideoCapture videoCapture;
        HOGDescriptor hog = new HOGDescriptor();

        Mat[] capturedFrames = new Mat[600];

        bool recording = false;
        bool playBack = false;
        int numRecordedFrames = 0;
        float framesRead = 0;
        bool go = true;

        public void Init()
        {
            frameSource = Cv2.CreateFrameSource_Camera(0);
            videoCapture = VideoCapture.FromCamera(0);

            for (int i = 0; i < 600; i++) capturedFrames[i] = new Mat();

            //hog.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
        }

        public Mat Capture(bool save)
        {
            //Initialise the image matrix
            Mat img = new Mat();
            //Grab the frame to the img variable
            frameSource.NextFrame(img);
            //Check save variable is true
            if (save)
            {
                string imagePath = string.Format("{0}\\cam.jpg", AppDomain.CurrentDomain.BaseDirectory);
                //Save the captured image
                img.SaveImage(imagePath);
            }

            return img;
        }

        public Mat Manipulate(Mat image)
        {
            //Initialise a new Mat variable to store the edge detected image
            Mat edgeDetection = new Mat();

            //Run Canny algorithm to detect the edges with two threshold values. 
            //Learn about Canny: http://dasl.unlv.edu/daslDrexel/alumni/bGreen/www.pages.drexel.edu/_weg22/can_tut.html
            Cv2.Canny(image, edgeDetection, 100, 200);
            //Cv2.CornerHarris(image, edgeDetection, 100, 200, 5);
            return edgeDetection;
        }

        public void ShowImage()
        {
            int matWidth = 1920;
            int matHeight = 1080;
            Mat camMat = new Mat();
            Mat useMat = new Mat(new Size(matWidth, matHeight), camMat.Type(), Scalar.Green);


            // download model and prototxt from https://github.com/spmallick/learnopencv/tree/master/FaceDetectionComparison/models
            const string configFile = "deploy.prototxt";
            const string faceModel = "res10_300x300_ssd_iter_140000_fp16.caffemodel";
            using var faceNet = CvDnn.ReadNetFromCaffe(configFile, faceModel);

            using var window = new Window("Cam");
            using var windowFace = new Window("Face");
            

            videoCapture.Read(camMat);
            Mat greenMat = new Mat(200, 200, camMat.Type(), Scalar.Green);

            int camScale = 2;
            int camWidth = matWidth / camScale;
            int camHeight = matHeight / camScale;

            OpenCvSharp.Range camRangeWidth = new OpenCvSharp.Range(matWidth - camWidth - 1, matWidth - 1);
            OpenCvSharp.Range camRangeHeight = new OpenCvSharp.Range(matHeight - camHeight - 1, matHeight - 1);

            while (go)
            {
                videoCapture.Read(camMat);
                Cv2.Resize(camMat, camMat, new Size(camWidth, camHeight));

                useMat[camRangeHeight, camRangeWidth] = camMat;

                window.Image = useMat;

                //Rect[] humans = hog.DetectMultiScale(useMat, 0, new Size(8, 8), new Size(32, 32), 1.2, 2);
                //foreach (Rect h in humans)
                //{
                //    Cv2.Rectangle(useMat, h, Scalar.Green, 2);
                //    Debug.WriteLine("Found human");
                //}

                int frameHeight = useMat.Rows;
                int frameWidth = useMat.Cols;

                if (recording)
                {
                    using var blob = CvDnn.BlobFromImage(useMat, 1.0, new Size(300, 300), new Scalar(104, 117, 123), false, false);
                    faceNet.SetInput(blob, "data");

                    using var detection = faceNet.Forward("detection_out");
                    using var detectionMat = new Mat(detection.Size(2), detection.Size(3), MatType.CV_32F,
                        detection.Ptr(0));
                    for (int i = 0; i < detectionMat.Rows; i++)
                    {
                        float confidence = detectionMat.At<float>(i, 2);

                        if (confidence > 0.8)
                        {
                            int x1 = (int)(detectionMat.At<float>(i, 3) * frameWidth);
                            int y1 = (int)(detectionMat.At<float>(i, 4) * frameHeight);
                            int x2 = (int)(detectionMat.At<float>(i, 5) * frameWidth);
                            int y2 = (int)(detectionMat.At<float>(i, 6) * frameHeight);
                            Cv2.Rectangle(useMat, new Point(x1, y1), new Point(x2, y2), Scalar.Green);

                            // create a new Mat with the detected face
                            var fullFaceImg = new Mat(frameHeight, frameWidth, useMat.Type(), Scalar.Green);

                            var faceImg = new Mat(useMat,
                                new OpenCvSharp.Range(y1, y2),
                                new OpenCvSharp.Range(x1, x2));

                            fullFaceImg[new OpenCvSharp.Range(y1, y2), new OpenCvSharp.Range(x1, x2)] = faceImg;
                            capturedFrames[(int)framesRead] = fullFaceImg;

                            framesRead++;
                            if (framesRead > 600)
                            {
                                recording = false;
                                playBack = true;
                                numRecordedFrames = (int)framesRead;
                            }
                        }
                    }
                }

                if(playBack)
                {
                    windowFace.Image = capturedFrames[(int)framesRead];
                    framesRead += 0.5f;
                    if(framesRead >= numRecordedFrames)
                    { 
                        framesRead = 0;
                    }
                } else
                {
                    windowFace.Image = greenMat;
                }

                switch((char)Cv2.WaitKey(10))
                {
                    case (char)27:
                        go = false;
                        break;
                }
            }
        }

        public void Release()
        {
            Cv2.DestroyAllWindows();
        }

        public void handleKey(IntPtr ptr)
        {
            if((long)ptr == 2162688)
            {
                if (recording == false && playBack == false)
                    recording = true;
                else if (recording == true)
                {
                    recording = false;
                    playBack = true;
                    numRecordedFrames = (int)framesRead;
                    framesRead = 0;
                }
            }

            else if((long)ptr == 2228224)
            {
                recording = false;
                playBack = false;
                framesRead = 0;
                numRecordedFrames = 0;
            }
        }

    }

    public static class Constants
    {
        //windows message id for hotkey
        public const int WM_HOTKEY_MSG_ID = 0x0312;
    }

    public class KeyHandler
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private int key;
        private IntPtr hWnd;
        private int id;

        public KeyHandler(Keys key, Form form)
        {
            this.key = (int)key;
            this.hWnd = form.Handle;
            id = this.GetHashCode();
        }

        public override int GetHashCode()
        {
            return key ^ hWnd.ToInt32();
        }

        public bool Register()
        {
            return RegisterHotKey(hWnd, id, 0, key);
        }

        public bool Unregiser()
        {
            return UnregisterHotKey(hWnd, id);
        }
    }
}


