using CastorPlugin.Core.OpenRevitOleStorage.StructuredStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastorPlugin.Core.OpenRevitOleStorage
{
    internal static class Tool
    {
        /// <summary>
        ///  extract the privew of revit files
        /// </summary>
        /// <param name="fileName">Revit Files (*.rvt; *.rte; *.rfa; *.rft)|*.rvt; *.rte; *.rfa; *.rft</param>
        /// <returns>
        /// preview image in base64, 
        /// Use the base64String in your webpage <img> tag
        /// "<img src=\"data:image/png;base64,{base64String}\" alt=\"Image\">"

        /// </returns>
        static public string GetFamilyPreviewThumbnail(string fileName)
        {

            Storage storage = new Storage(fileName);

            if(storage.IsInitialized == false)
            {
                Debug.WriteLine("OpenRevitOleStorage returning because Storage is not initialized - error reading Structured Storage.");
                Log.Fatal( "OpenRevitOleStorage returning because Storage is not initialized - error reading Structured Storage.");
                return String.Empty;
            }

            //get the file preview image
            System.Drawing.Image img = storage.ThumbnailImage.GetPreviewAsImage();

            // Convert Image to BASE64 string
            string base64String = ImageToBase64(img);
             
            return base64String;


        }
        /// <summary>
        ///  extract basic information of revit files from Ole.
        /// </summary>
        /// <param name="fileName">Revit Files (*.rvt; *.rte; *.rfa; *.rft)|*.rvt; *.rte; *.rfa; *.rft</param>
        /// <returns>
        ///  basic information in string.
        /// </returns>
        static public string GetBasicInformation(string fileName)
        {
            String basicInfo = string.Empty;

            Storage storage = new Storage(fileName);

            if (storage.IsInitialized == false)
            {
                Debug.WriteLine("OpenRevitOleStorage returning because Storage is not initialized - error reading Structured Storage.");
                Log.Fatal("OpenRevitOleStorage returning because Storage is not initialized - error reading Structured Storage.");
                return String.Empty;
            }

            if (storage.BasicInfo != null)
            {
                basicInfo = storage.BasicInfo.ToString();
            }

            return basicInfo;
        }

        public static string ImageToBase64(Image image)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Save the image to the memory stream in PNG format
                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                // Convert the byte array to a base64 string
                byte[] imageBytes = memoryStream.ToArray();
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
        }
    }
}
