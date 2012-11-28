// Simple Shape Checker sample application
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2007-2010
// andrew.kirillov@aforgenet.com


using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Reflection;

using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;

namespace ShapeChecker
{
    public partial class MainForm : Form
    {
        public MainForm( )
        {
            InitializeComponent( );
        }

        // Exit from application
        private void exitToolStripMenuItem_Click( object sender, EventArgs e )
        {
            this.Close( );
        }

        // On loading of the form
        private void MainForm_Load( object sender, EventArgs e )
        {
            LoadDemo( "coins.jpg" );
        }

        // Open file
        private void openToolStripMenuItem_Click( object sender, EventArgs e )
        {
            if ( openFileDialog.ShowDialog( ) == DialogResult.OK )
            {
                try
                {
                    ProcessImage( (Bitmap) Bitmap.FromFile( openFileDialog.FileName ) );
                }
                catch
                {
                    MessageBox.Show( "Failed loading selected image file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }
        }

        // Load first demo image
        private void loadDemoImage1ToolStripMenuItem_Click( object sender, EventArgs e )
        {
            LoadDemo( "coins.jpg" );
        }

        // Load second demo image
        private void loadDemoImage2ToolStripMenuItem_Click( object sender, EventArgs e )
        {
            LoadDemo( "demo.png" );
        }

        // Load third demo image
        private void loadDemoImage3ToolStripMenuItem_Click( object sender, EventArgs e )
        {
            LoadDemo( "demo1.png" );
        }

        // Load fourth demo image
        private void loadDemoImage4ToolStripMenuItem_Click( object sender, EventArgs e )
        {
            LoadDemo( "demo2.png" );
        }

        // Load one of the embedded demo image
        private void LoadDemo( string embeddedFileName )
        {
            // load arrow bitmap
            Assembly assembly = this.GetType( ).Assembly;
            Bitmap image = new Bitmap( assembly.GetManifestResourceStream( "ShapeChecker." + embeddedFileName ) );
            ProcessImage( image );
        }

        // Process image
        private void ProcessImage( Bitmap bitmap )
        {
            Grayscale grayscaleFilter = new Grayscale(0.33, 0.33, 0.34);
            // apply the filter
            Bitmap grayImage = grayscaleFilter.Apply(bitmap);


            //Get Histogram
            /*int[] histogram = new int[256];
            for (int i = 0; i < 256; i++) {
                histogram[i] = 0;
            }
            for (int i = 0; i < grayImage.Width; i++ )
            {
                for (int j = 0; j < grayImage.Height; j++) {
                    histogram[(Convert.ToInt32(grayImage.GetPixel(i, j).R.ToString()) + Convert.ToInt32(grayImage.GetPixel(i, j).G.ToString()) +
                        Convert.ToInt32(grayImage.GetPixel(i, j).B.ToString()))/3] +=1;
                }
            }*/



            //create filter
            SobelEdgeDetector sobelFilter = new SobelEdgeDetector();
            // apply the filter
            sobelFilter.ApplyInPlace(grayImage);
            Threshold filter = new Threshold(243);
            // apply the filter
            filter.ApplyInPlace(grayImage);




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
            int vertical = 0;
            int horizons = 0;

            int testColor = 0;
            Color color = Color.Black;


        
            foreach (HoughLine line in lines)
            {

                String temp = line.Theta.ToString();
                //MessageBox.Show(temp, "ok", MessageBoxButtons.OK);
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
                    // horizontal
                    // none-vertical line

                    if (line.Theta > 0 && line.Theta < 10 || line.Theta > 170)
                    {
                        x0 = -w2; // most left point
                        x1 = w2;  // most right point


                        y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                        y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);


                        color = Color.Red;

                        Drawing.Line(bitmapData,
                        new IntPoint((int)x0 + w2, h2 - (int)y0),
                        new IntPoint((int)x1 + w2, h2 - (int)y1),
                        color);
                        Drawing.Line(grayImageData,
                         new IntPoint((int)x0 + w2, h2 - (int)y0),
                         new IntPoint((int)x1 + w2, h2 - (int)y1),
                         color);                   
                    }

                    if (line.Theta > 80 && line.Theta < 100)
                    {
                        x0 = -w2; // most left point
                        x1 = w2;  // most right point


                        y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                        y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);


                        Drawing.Line(bitmapData,
                        new IntPoint((int)x0 + w2, h2 - (int)y0),
                        new IntPoint((int)x1 + w2, h2 - (int)y1),
                        Color.Blue);
                        Drawing.Line(grayImageData,
                         new IntPoint((int)x0 + w2, h2 - (int)y0),
                         new IntPoint((int)x1 + w2, h2 - (int)y1),
                         Color.Blue);                    
                    }



                }
                else
                {
                    // vertical line
                    x0 = line.Radius;
                    x1 = line.Radius;
                    y0 = h2;
                    y1 = -h2;

                    color = Color.Red;

                    Drawing.Line(bitmapData,
                    new IntPoint((int)x0 + w2, h2 - (int)y0),
                    new IntPoint((int)x1 + w2, h2 - (int)y1),
                    color);
                    Drawing.Line(grayImageData,
                     new IntPoint((int)x0 + w2, h2 - (int)y0),
                     new IntPoint((int)x1 + w2, h2 - (int)y1),
                     color);         
                }
            }





            bitmap.UnlockBits(bitmapData);
            grayImage.UnlockBits(grayImageData);
            MessageBox.Show(lines.Length+"", "ok", MessageBoxButtons.OK);
            // put new image to clipboard


            Clipboard.SetDataObject(bitmap);
            // and to picture box
            pictureBox.Image = bitmap;

            UpdatePictureBoxPosition();
        }





        // Size of main panel has changed
        private void splitContainer_Panel2_Resize( object sender, EventArgs e )
        {
            UpdatePictureBoxPosition( );
        }

        // Update size and position of picture box control
        private void UpdatePictureBoxPosition( )
        {
            int imageWidth;
            int imageHeight;

            if ( pictureBox.Image == null )
            {
                imageWidth  = 320;
                imageHeight = 240;
            }
            else
            {
                imageWidth  = pictureBox.Image.Width;
                imageHeight = pictureBox.Image.Height;
            }

            Rectangle rc = splitContainer.Panel2.ClientRectangle;

            pictureBox.SuspendLayout( );
            pictureBox.Location = new Point( ( rc.Width - imageWidth - 2 ) / 2, ( rc.Height - imageHeight - 2 ) / 2 );
            pictureBox.Size = new Size( imageWidth + 2, imageHeight + 2 );
            pictureBox.ResumeLayout( );
        }

        // Conver list of AForge.NET's points to array of .NET points
        private Point[] ToPointsArray( List<IntPoint> points )
        {
            Point[] array = new Point[points.Count];

            for ( int i = 0, n = points.Count; i < n; i++ )
            {
                array[i] = new Point( points[i].X, points[i].Y );
            }

            return array;
        }

        // Show about form
        private void aboutToolStripMenuItem_Click( object sender, EventArgs e )
        {
            AboutForm form = new AboutForm( );

            form.ShowDialog( );
        }
    }
}
