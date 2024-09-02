using System;
using System.Drawing;
using IronBarCode;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using System.Drawing; 
using Net.Codecrete.QrCodeGenerator;


namespace CloudSync
{
    public partial class QRCodeForm : Form
    {
        public QRCodeForm(string url)
        {
            InitializeComponent();
            GenerateQRCode(url);
            AddInstructions(url);
        }

        private void GenerateQRCode(string url)
        {
            // Create a barcode writer instance using ZXing.Net
            var barcodeWriter = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300, // Reduced to avoid pixelation
                    Height = 300,
                    Margin = 4,  // Increased margin to improve scan accuracy
                    PureBarcode = true
                }
            };

            // Generate the QR code pixel data
            var pixelData = barcodeWriter.Write(url);

            // Create a bitmap from the pixel data
            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0,
                        pixelData.Pixels.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // Display the bitmap in the PictureBox
                pictureBoxQRCode.Image = (Bitmap)bitmap.Clone();
            }
        }


        private void AddInstructions(string url)
        {
            labelInstructions.Text = $"1. Scan this QR code with your mobile device\n" +
                                     $"2. Open the URL in your mobile browser\n" +
                                     $"3. Select files to upload\n" +
                                     $"4. Files will appear in your Google Drive\n\n" +
                                     $"URL: {url}";
        }
    }
}