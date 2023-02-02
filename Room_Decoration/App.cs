#region Namespaces
using Autodesk.Revit.UI;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Metal_Rolling;
#endregion
111
namespace Room_Decoration
{
    internal class App : IExternalApplication
    {
        public BitmapImage ConvertToBitmap(Image img, Size size)
        {
            img = (Image)(new Bitmap(img, size));
            using (MemoryStream memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        public Result OnStartup(UIControlledApplication Application)
        {
            string tabName = "Òðåòèé Òðåñò_Ïëàãèíû";
            string panelName = "Ïëàãèí";

            // creating the bitImages
            Image RoomDecorImg = Properties.Resources.icon_remonts;

            // creating the tab
            Application.CreateRibbonTab(tabName);

            // creating the panel
            RibbonPanel materials = Application.CreateRibbonPanel(tabName, panelName);

            // creating the buttons 
            PushButtonData button1 = new PushButtonData("Room_Decoration", "Îòäåëêà Ïîìåùåíèé", Assembly.GetExecutingAssembly().Location, typeof(Room_Decoration).FullName);
            button1.ToolTip = "Îòäåëêà Ïîìåùåíèé";
            PushButton btn1 = materials.AddItem(button1) as PushButton;

            // setting image to button
            btn1.LargeImage = ConvertToBitmap(RoomDecorImg, new Size(32, 32));
            //btn1.Image = ConvertToBitmap(MaterialsImg, new Size(16, 16));


            //
            //
            Image MaterialsImg = Properties.Resources.metalRolling;

            // creating the buttons 
            PushButtonData button2 = new PushButtonData("Metell_Rolling", "Ìåòàëëîïðîêàò", Assembly.GetExecutingAssembly().Location, typeof(Metal_Rolling_Command).FullName);
            button1.ToolTip = "Ìåòàëëîïðîêàò";
            PushButton btn2 = materials.AddItem(button2) as PushButton;

            // setting image to button
            btn2.LargeImage = ConvertToBitmap(MaterialsImg, new Size(32, 32));
            //btn1.Image = ConvertToBitmap(MaterialsImg, new Size(16, 16));

            //
            //
            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
