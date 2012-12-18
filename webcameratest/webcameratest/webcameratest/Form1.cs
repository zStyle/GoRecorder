using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
//using AForge.Imaging.ColorReduction;
using AForge.Math.Geometry;
using AForge.Vision.Motion;
namespace webcameratest
{
    public partial class Form1 : Form
    {
        private bool ExistenDispositive = false;
        private FilterInfoCollection DispositiveVideo;
        private VideoCaptureDevice FuenteVideo = null;
        public bool locked = false;
        public int[, ,] intersectPoint = new int[2, 19, 19];

        public void CargarDispositive(FilterInfoCollection Dispositive) {
            for (int i = 0; i < Dispositive.Count; i++) {
                cboDispositive.Items.Add(Dispositive[i].Name.ToString());
                cboDispositive.Text = cboDispositive.Items[0].ToString();
            }
        }
        public void BuscarDispositive() {
            DispositiveVideo = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (DispositiveVideo.Count == 0)
            {
                ExistenDispositive = false;
            }
            else {
                ExistenDispositive = true;
                CargarDispositive(DispositiveVideo);
            }
        }

        public void TerminarFuenteDeVideo() { 
            if(!(FuenteVideo == null))
                if (FuenteVideo.IsRunning) {
                    FuenteVideo.SignalToStop();
                    FuenteVideo = null;
                }
        }

        private void video_NuevoFrame(object sender, NewFrameEventArgs eventArgs) {
            Bitmap Image = (Bitmap)eventArgs.Frame.Clone();
            //Got Image

            if (!locked)
            {
                ProcessImage(Image);
            }
            else
            {
                EspacioCamera.Image = Image;

            }
            //EspacioCamera.Image = Image;
        }

        

        public Form1()
        {
            InitializeComponent();
            BuscarDispositive();
        }


        private void btnIniciar_Click(object sender, EventArgs e)
        {
            if (btnIniciar.Text == "Start")
            {
                if (ExistenDispositive)
                {
                    FuenteVideo = new VideoCaptureDevice(DispositiveVideo[cboDispositive.SelectedIndex].MonikerString);
                    FuenteVideo.NewFrame += new NewFrameEventHandler(video_NuevoFrame);
                    FuenteVideo.Start();
                    Estado.Text = "Connection Complete";
                    btnIniciar.Text = "Stop";
                    cboDispositive.Enabled = false;
                    groupBox1.Text = DispositiveVideo[cboDispositive.SelectedIndex].Name.ToString();
                }
                else
                    Estado.Text = "Error: No matching device.";
            }
            else {
                if (FuenteVideo.IsRunning) {
                    btnIniciar.Text = "Start";
                    TerminarFuenteDeVideo();
                    Estado.Text = "Dispositiveo detenio";
                    cboDispositive.Enabled = true;
                }
            }
            

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnIniciar.Text == "Detener")
                btnIniciar_Click(sender, e);

        }

        private void EspacioCamera_Click(object sender, EventArgs e)
        {

        }
        private Bitmap QuantizeImage(Bitmap image)
        {

            MessageBox.Show("r = " + image.GetPixel(1, 1).R.ToString() +
                "  G = " + image.GetPixel(1, 1).G.ToString() +
                "  B = " + image.GetPixel(1, 1).B.ToString() + " Height = " + image.Height + " Width = " + image.Width);

            int red, green, blue, avg;

            // Set each pixel in myBitmap to black. 
            for (int Xcount = 0; Xcount < image.Width; Xcount++)
            {
                for (int Ycount = 0; Ycount < image.Height; Ycount++)
                {
                    red = image.GetPixel(Xcount, Ycount).R;
                    green = image.GetPixel(Xcount, Ycount).G;
                    blue = image.GetPixel(Xcount, Ycount).B;

                    avg = (red + green + blue) / 3;

                    //detect component
                    if (avg > 200)
                    {
                        image.SetPixel(Xcount, Ycount, Color.White);
                    }
                    else if (avg < 100)
                    {
                        image.SetPixel(Xcount, Ycount, Color.Black);
                    }
                    else
                        image.SetPixel(Xcount, Ycount, Color.Khaki);
                }
            }
            return image;
        }
        private bool GetIntersect(Bitmap grayImage, Bitmap bitmap, int[, ,] intersectPoint)
        {

            HoughLineTransformation lineTransform = new HoughLineTransformation();
            lineTransform.ProcessImage(grayImage);
            Bitmap houghLineImage = lineTransform.ToBitmap();
            // get lines using relative intensity
            HoughLine[] lines = lineTransform.GetLinesByRelativeIntensity(0.5);

            BitmapData bitmapData = bitmap.LockBits(
               new Rectangle(0, 0, bitmap.Width, bitmap.Height),
               ImageLockMode.ReadWrite, bitmap.PixelFormat);
            BitmapData grayImageData = grayImage.LockBits(
               new Rectangle(0, 0, grayImage.Width, grayImage.Height),
               ImageLockMode.ReadWrite, grayImage.PixelFormat);
            UnmanagedImage unmanagedImage = new UnmanagedImage(bitmapData);
            
            int w2 = 0, h2 = 0;
            Color color = Color.Black;
            int[] mVer = new int[19];
            double[] mHor = new double[19];
            double[] cVer = new double[19];
            double[] cHor = new double[19];
            int verCount = 0;
            int horCount = 0;


            foreach (HoughLine line in lines)
            {
                
                String temp = line.Theta.ToString();
                // get line's radius and theta values
                int r = line.Radius;
                double t = line.Theta;

                // check if line is in lower part of the image
                if (r < 0)
                {
                    t += 180;
                    r = -r;
                }

                // convert degrees to radians
                t = (t / 180) * Math.PI;

                // get image centers (all coordinate are measured relative
                // to center)
                w2 = grayImage.Width / 2;
                h2 = grayImage.Height / 2;

                double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

                if (line.Theta != 0)
                {
                    // vertical line
                    if (line.Theta > 0 && line.Theta < 10 || line.Theta > 170)
                    {
                        if (verCount == 19)
                        {
                            bitmap.UnlockBits(bitmapData);
                            grayImage.UnlockBits(grayImageData);
                            return false;

                        }
                        y0 = -h2;
                        y1 = h2;
                        x0 = (r - (Math.Sin(t) * y0)) / Math.Cos(t);
                        x1 = (r - (Math.Sin(t) * y1)) / Math.Cos(t);

                        color = Color.Red;

                        x0 = w2 + (int)x0;
                        x1 = w2 + (int)x1;
                        y0 = (h2 - y0);
                        y1 = (h2 - y1);

                        //mVer[verCount] = (int) -(Math.Cos(t)/Math.Sin(t));
                        //cVer[verCount] = (double)r*(1/Math.Sin(t));

                        if ((int)x0 == (int)x1)
                        {
                            mVer[verCount] = (int)((y1 - y0) / (x1 - x0 + 1));
                            cVer[verCount] = y0 - (mVer[verCount] * x0);
                        }
                        else
                        {
                            mVer[verCount] = (int)((y1 - y0) / (x1 - x0));
                            cVer[verCount] = (y0 - (mVer[verCount] * x0));
                        }
                        Drawing.Line(bitmapData,
                        new IntPoint((int)x0, (int)y0),
                        new IntPoint((int)x1 ,(int)y1),
                        color);
                        Drawing.Line(grayImageData,
                        new IntPoint((int)x0 , (int)y0),
                        new IntPoint((int)x1 , (int)y1),
                        color);

                        verCount++;
                    }

                    // horizontal
                    if (line.Theta > 80 && line.Theta < 100)
                    {
                        if (horCount == 19)
                        {
                            bitmap.UnlockBits(bitmapData);
                            grayImage.UnlockBits(grayImageData);
                            return false;

                        }
                        
                        x0 = -w2; // most left point
                        x1 = w2;  // most right point6

                        y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                        y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);

                        x0 = x0 + w2;
                        x1 = x1 + w2;
                        y0 = (int)(h2 - (int)y0);
                        y1 = (int)(h2 - (int)y1);

                        //mHor[horCount] = (int)-(Math.Cos(t) / Math.Sin(t));
                        //cHor[horCount] = (double)r * (1 / Math.Sin(t));
                        if (y0 - (mHor[horCount] * x0) < 10 || y0 - (mHor[horCount] * x0) > bitmap.Height - 10)
                            continue;
                        mHor[horCount] = ((y1 - y0) / (x1 - x0));
                        cHor[horCount] = y0 - (mHor[horCount] * x0);
                        horCount++;

                        Drawing.Line(bitmapData,
                        new IntPoint((int)x0,(int)y0),
                        new IntPoint((int)x1, (int)y1),
                        Color.Blue);
                        Drawing.Line(grayImageData,
                         new IntPoint((int)x0, (int)y0),
                         new IntPoint((int)x1, (int)y1),
                         Color.Blue);
                    }
                }
                else
                {
                    if (verCount == 19)
                    {
                        bitmap.UnlockBits(bitmapData);
                        grayImage.UnlockBits(grayImageData);
                        return false;

                    }
                    // vertical line
                    x0 = line.Radius;
                    x1 = line.Radius;
                    y0 = h2;
                    y1 = -h2;

                    color = Color.Yellow;

                    x0 = x0 + w2;
                    x1 = x1 + w2;
                    y0 = (h2 - (int)y0);
                    y1 = (h2 - (int)y1);

                    //mVer[verCount] = (int)-(Math.Cos(t) / Math.Sin(t));
                    //cVer[verCount] = (double)r * (1 / Math.Sin(t));

                    if (x0 < 10 || x0 > bitmap.Width - 10)
                        continue;
                    mVer[verCount] = (int)((y1 - y0) / (x1 - x0 + 1));
                    cVer[verCount] = y0 - (mVer[verCount] * x0);

                    Drawing.Line(bitmapData,
                    new IntPoint((int)x0, (int)y0),
                    new IntPoint((int)x1, (int)y1),
                    color);
                    Drawing.Line(grayImageData,
                     new IntPoint((int)x0, (int)y0),
                     new IntPoint((int)x1, (int)y1),
                     color);
                    //MessageBox.Show("y0 = " + y0 + " y1 = " + y1 + " x0 = " + x0 + " x1 = " + x1 +
                    //       " m = " + mVer[verCount] + " c = " + cVer[verCount], "ok", MessageBoxButtons.OK);
                    verCount++;

                }
            }

            int m = 0;
            int n = 0;
            if (horCount == 19 && verCount == 19)
            {
                for (int i = 0; i < 19; i++)
                {
                    for (int j = 0; j < 19; j++)
                    {

                        intersectPoint[0, i, j] = (int)((cHor[j] - cVer[i]) / (mVer[i] - mHor[j]));
                        intersectPoint[1, i, j] = (int)((mHor[j] * intersectPoint[0, i, j]) + cHor[j]);  
                        Debug.WriteLine(intersectPoint[0, i, j] + " " + intersectPoint[1, i, j] + " " + cHor[j] + " " + mHor[j]);
                    }
                }
                bitmap.UnlockBits(bitmapData);
                grayImage.UnlockBits(grayImageData);
                return true;
            }
            else {
                bitmap.UnlockBits(bitmapData);
                grayImage.UnlockBits(grayImageData);
                return false;
            }


            //int num = 0;
            //MessageBox.Show("num = " + num + " mv =" + mVer[num] + "," + " cv = " + cVer[num] + " mh =" + mHor[num] + "," + " ch = " + cHor[num] + "," + "x = " 
            //    + intersectPoint[0, num, num] + "y = " + intersectPoint[1, num, num], "ok", MessageBoxButtons.OK);

            
            //MessageBox.Show(mVer[18] + "," + cVer[18] + "," + intersectPoint[0, 18, 18], "ok", MessageBoxButtons.OK);
            //MessageBox.Show(intersectPoint. + "", "ok", MessageBoxButtons.OK);
        }

        // Process image
        private void ProcessImage(Bitmap bitmap)
        {

            // create filter
            HistogramEqualization hisFilter = new HistogramEqualization();
            // process image
            //hisFilter.ApplyInPlace(Image);

            // create filter
            ContrastCorrection contrastFilter = new ContrastCorrection(4);
            // apply the filter
            contrastFilter.ApplyInPlace(bitmap);

            //QuantizeImage(bitmap);

            Grayscale grayscaleFilter = new Grayscale(0.33, 0.33, 0.34);
            // apply the filter
            Bitmap grayImage = grayscaleFilter.Apply(bitmap);

            // create filter
            SobelEdgeDetector sobelFilter = new SobelEdgeDetector();
            // apply the filter
            sobelFilter.ApplyInPlace(grayImage);
            Threshold filter = new Threshold(110);
            // apply the filter
            filter.ApplyInPlace(grayImage);

           
            if (GetIntersect(grayImage, bitmap, intersectPoint)) {
                //MessageBox.Show("draw");
                Graphics g = Graphics.FromImage(bitmap);
                //Sorting
                
                
                for (int i = 0; i < 19; i++)
                {
                    for (int j = 0; j < 19; j++)
                    {
                        for (int k = 0; k < 19 - 1; k++)
                        {
                            if (Math.Abs(intersectPoint[1, i, k]) > Math.Abs(intersectPoint[1, i, k + 1]))
                            {
                                int tempX = Math.Abs(intersectPoint[0, i, k]);
                                int tempY = Math.Abs(intersectPoint[1, i, k]);
                                intersectPoint[0, i, k] = Math.Abs(intersectPoint[0, i, k + 1]);
                                intersectPoint[1, i, k] = Math.Abs(intersectPoint[1, i, k + 1]);
                                intersectPoint[0, i, k + 1] = tempX;
                                intersectPoint[1, i, k + 1] = tempY;
                                //MessageBox.Show(i + " " + k +" " +tempX + " " + tempY + " " + intersectPoint[0, i, k] + " " + intersectPoint[1, i, k] ,"ok", MessageBoxButtons.OK);
                            }
                        }
                    }
                }

                for (int i = 0; i < 19; i++)
                {
                    for (int j = 0; j < 19; j++)
                    {
                        for (int k = 0; k < 19 - 1; k++)
                        {
                            if (Math.Abs(intersectPoint[0, k, i]) > Math.Abs(intersectPoint[0, k + 1, i]))
                            {
                                int tempX = Math.Abs(intersectPoint[0, k, i]);
                                int tempY = Math.Abs(intersectPoint[1, k, i]);
                                intersectPoint[0, k, i] = intersectPoint[0, k + 1, i];
                                intersectPoint[1, k, i] = intersectPoint[1, k + 1, i];
                                intersectPoint[0, k + 1, i] = tempX;
                                intersectPoint[1, k + 1, i] = tempY;
                            }
                        }
                    }
                }
                
                Pen redPen = new Pen(Color.Red, 4);
                for (int i = 0; i < 19; i++)
                {
                    for (int j = 0; j < 19; j++)
                    {
                        g.DrawEllipse(redPen, Math.Abs(intersectPoint[0, i, j]) , Math.Abs(intersectPoint[1, i, j]) , (int)5, (int)5);
                        //g.DrawEllipse(redPen, 0, 0, (int)5, (int)5);
                       
                        //Debug.WriteLine((Math.Abs(intersectPoint[0, i, j]) - 2) + " " + (Math.Abs(intersectPoint[1, i, j]) - 2));
                    }
                }

                Pen greenPen = new Pen(Color.Green, 4);

                greenPen.Dispose();
                redPen.Dispose();
                g.Dispose();

                // Initializes the variables to pass to the MessageBox.Show method.
                EspacioCamera.Image = bitmap;
                string message = "Do you accecpt this detection?";
                string caption = "The system found totally 38 lines.";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                // Displays the MessageBox.

                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    // Closes the parent form.
                    locked = true;
                }
            }

            EspacioCamera.Image = bitmap;
            

        }
    }
}
