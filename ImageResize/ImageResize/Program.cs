using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageResize
{
    public class Program
    {
        static string DestinationPath = "C:/tmp/images";
        static string ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/5/59/Morgan_County_Courthouse_Ohio.jpg";

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"Downloading image from {ImageUrl} ...");

                var content = DownloadPhoto(ImageUrl);
                if (content != null && content.Length > 0)
                {
                    var photo = new Photo
                    {
                        Name = Path.GetFileNameWithoutExtension(ImageUrl),
                        Content = Convert.ToBase64String(content),
                        SmallSizeConent = Convert.ToBase64String(ResizeToSquare(content, 150)),
                        LargeSizeConent = Convert.ToBase64String(ResizeToSquare(content, 500))
                    };


                    SavePhisicalImage(photo.Content, $"{photo.Name}_original");
                    SavePhisicalImage(photo.SmallSizeConent, $"{photo.Name}_small");
                    SavePhisicalImage(photo.LargeSizeConent, $"{photo.Name}_large");

                    Console.WriteLine($"Images resized, converted to base64 for future uses and saved to {DestinationPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error : {ex}");
            }
            Console.Read();
        }

        private static byte[] DownloadPhoto(string url)
        {
            using (System.Net.WebClient webClient = new System.Net.WebClient())
            {
                return webClient.DownloadData(url);
            }
        }


        private static byte[] ResizeToSquare(byte[] imageBytes, int squareWidthInPixels)
        {
            Image image = ByteToImage(imageBytes);

            float percentWidth = (float)squareWidthInPixels / (float)image.Width;
            float percentHeight = (float)squareWidthInPixels / (float)image.Height;
            //The largest side of the image will be now the squareWidthInPixels
            float lowerPercentage = percentHeight < percentWidth ? percentHeight : percentWidth;
            var newWidth = (int)(image.Width * lowerPercentage);
            var newHeight = (int)(image.Height * lowerPercentage);

            using (Bitmap imageBitmap = new Bitmap(squareWidthInPixels, squareWidthInPixels))
            {
                using (Graphics graphics = Graphics.FromImage(imageBitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.High;

                    int rectangleY = (squareWidthInPixels / 2) - (newHeight / 2);
                    int rectangleX = (squareWidthInPixels / 2) - (newWidth / 2);
                    var rectangleDestination = new Rectangle(rectangleX, rectangleY, newWidth, newHeight);
                    graphics.DrawImage(image, rectangleDestination);

                    using (MemoryStream destination = new MemoryStream())
                    {
                        //convert to Png to keep the extra part transparent
                        imageBitmap.Save(destination, ImageFormat.Png);
                        return destination.ToArray();
                    }
                }
            }
        }

        private static void SavePhisicalImage(string base64Image, string name)
        {
            if (!string.IsNullOrEmpty(base64Image))
            {
                var imageBytes = Convert.FromBase64String(base64Image);
                var ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                ms.Write(imageBytes, 0, imageBytes.Length);
                Image image = Image.FromStream(ms, true);

                var format = ImageCodecInfo.GetImageDecoders().FirstOrDefault(x => x.FormatID == image.RawFormat.Guid);
                image.Save(Path.Combine(DestinationPath, $"{name}.{format.FormatDescription.ToLower()}"), image.RawFormat);
            }
        }

        private static Image ByteToImage(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                ms.Write(imageBytes, 0, imageBytes.Length);
                return Image.FromStream(ms, true);
            }
        }
    }
}
