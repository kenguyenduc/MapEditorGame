using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace MapEditor
{
    public partial class frmMain : Form
    {
        public Dictionary<int, Bitmap> lstTileSet = new Dictionary<int, Bitmap>();
        public static string enviroment = System.Environment.CurrentDirectory;
        public int _pixel;

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnBrowseFilepath_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                FileName = "Select a image file",
                Filter = "Image Files (*.bmp;*.jpg;*.jpeg,*.png)|*.BMP;*.JPG;*.JPEG;*.PNG",
                Title = "Open Image File..."
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StreamReader streamReader = new StreamReader(openFileDialog.FileName);
                    string fileName = openFileDialog.FileName;
                    txtFilePath.Text = System.IO.Path.GetFullPath(fileName);
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            //OpenFileDialog folderBrowser = new OpenFileDialog();
            //// Set validate names and check file exists to false otherwise windows will
            //// not let you select "Folder Selection."
            //folderBrowser.ValidateNames = false;
            //folderBrowser.CheckFileExists = false;
            //folderBrowser.CheckPathExists = true;
            //// Always default to Folder Selection.
            //folderBrowser.FileName = "Folder Selection.";
            //if (folderBrowser.ShowDialog() == DialogResult.OK)
            //{
            //    txtOutput.Text = Path.GetDirectoryName(folderBrowser.FileName);

            //}
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select a folder";
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                txtOutput.Text = folderBrowser.SelectedPath;
            }
        }

        private void txtWidth_TextChanged(object sender, EventArgs e)
        {
            this.txtHeight.Text = this.txtWidth.Text;
        }
        private void txtHeight_TextChanged(object sender, EventArgs e)
        {
            this.txtWidth.Text = this.txtHeight.Text;
        }

        #region handle bitmap background
        public bool IsEqual(Bitmap bmp1, Bitmap bmp2)
        {
            if (object.Equals(bmp1, bmp2))
                return true;

            int bytes = bmp1.Width * bmp1.Height * (Image.GetPixelFormatSize(bmp1.PixelFormat) / 8) - 1;

            bool result = true;
            var b1bytes = new byte[bytes];
            var b2bytes = new byte[bytes];

            var bitmapData1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadOnly, bmp1.PixelFormat);
            var bitmapData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, bmp2.PixelFormat);

            Marshal.Copy(bitmapData1.Scan0, b1bytes, 0, bytes);
            Marshal.Copy(bitmapData2.Scan0, b2bytes, 0, bytes);

            for (int n = 0; n < bytes; ++n)
            {
                if (b1bytes[n] != b2bytes[n])
                {
                    result = false;
                    break;
                }
            }

            bmp1.UnlockBits(bitmapData1);
            bmp2.UnlockBits(bitmapData2);

            return result;
        }
        public int AlreadyIncluded(Dictionary<int, Bitmap> source, Bitmap bitmap)
        {
            if (source.Count < 1)
            {
                return -1;
            }
            foreach (var b in source)
            {
                if (IsEqual(b.Value, bitmap))
                {
                    return b.Key;
                }
            }
            return -1;
        }

        public Bitmap CropTile(Bitmap source, Rectangle sourceRect)
        {
            // An empty bitmap which will hold the cropped image
            Bitmap bmp = new Bitmap(sourceRect.Width, sourceRect.Height);
            Rectangle destRect = new Rectangle(0, 0, _pixel, _pixel);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Draw the given area (section) of the source image
                // at location 0,0 on the empty bitmap (bmp)
                g.DrawImage(source, destRect, sourceRect, GraphicsUnit.Pixel);
            }

            return bmp;
        }

        #endregion

        #region Create Tile From Image
        public void CreateTileFromPicture(string filePath)
        {
            //filePath = Path.Combine(projectDirectory, filePath);
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File Path Invalid");
                return;
            }

            if (!filePath.EndsWith(".png") && !filePath.EndsWith(".PNG"))
            {
                Console.WriteLine("File is not an image");
                return;
            }
            lstTileSet.Clear();
            //Minimize
            int index = 0;
            int res = -1;
            Bitmap img = new Bitmap(filePath);
            int numTilesWidth = img.Width / _pixel;
            int numTilesHeight = img.Height / _pixel;
            string strTile = numTilesWidth + " " + numTilesHeight + "\n";
            List<Bitmap> lstTiles = new List<Bitmap>();
            Bitmap bitmapTile;
            //cut image into species
            for (int row = 0; row < numTilesHeight; row++)
            {
                for (int col = 0; col < numTilesWidth; col++)
                {
                    Rectangle sourceRect = new Rectangle(_pixel * col, _pixel * row, _pixel, _pixel);
                    lstTiles.Add(CropTile(img, sourceRect));
                    bitmapTile = CropTile(img, sourceRect);
                    res = AlreadyIncluded(lstTileSet, bitmapTile);
                    if (res == -1)
                    {
                        lstTileSet.Add(index, bitmapTile);
                        strTile = strTile + index + " ";
                        index++;
                    }
                    else
                    {
                        strTile = strTile + res + " ";
                    }
                }
                strTile += "\n";
            }
            if (lstTiles.Count <= 0)
            {
                Console.WriteLine("Could no cut into tiles");
                return;
            }
            //end write to file
            StreamWriter streamWriter = new StreamWriter(Path.Combine(txtOutput.Text, txtFileName.Text + ".txt"));
            streamWriter.Write(strTile);
            streamWriter.Close();
            //draw to image
            //calculate szie of image
            //Bitmap finalImg = new Bitmap(_pixel * lstTileSet.Count, _pixel);
            //using (Graphics g = Graphics.FromImage(finalImg))
            //{
            //    g.Clear(Color.Black);
            //    for (int i = 0; i < lstTileSet.Count; i++)
            //    {
            //        g.DrawImage(lstTileSet[i], new Rectangle(i * _pixel, 0, _pixel, _pixel), new Rectangle(0, 0, _pixel, _pixel), GraphicsUnit.Pixel);
            //    }
            //}
            //finalImg.Save(Path.Combine(txtOutput.Text, txtFileName.Text + ".png"));

            int _count = 0;
            int _splitRows =Convert.ToInt32(numSplitRow.Value);
            int _cols = lstTileSet.Count / _splitRows;
            int _rows = _splitRows;
            Bitmap finalImg = new Bitmap(_pixel * _cols, _pixel * _splitRows);
            Pen pen = new Pen(Color.SeaGreen);
            using (Graphics graphic = Graphics.FromImage(finalImg))
            {
                graphic.Clear(Color.Transparent);
                for (int i = 0; i < _rows; i++)
                {
                    for (int j = 0; j < _cols; j++)
                    {
                        graphic.DrawImage(lstTileSet[_count], new Rectangle(j * _pixel, i * _pixel, _pixel, _pixel), new Rectangle(0, 0, _pixel, _pixel), GraphicsUnit.Pixel);
                        _count++;
                    }
                }
            }
            finalImg.Save(Path.Combine(txtOutput.Text, txtFileName.Text + ".png"));
        }
        #endregion

        private void btnExport_Click(object sender, EventArgs e)
        {
            _pixel = Int32.Parse(txtHeight.Text);
            string filename = @"" + txtFilePath.Text;
            CreateTileFromPicture(filename);
            DialogResult dialogResult = MessageBox.Show("Done !!!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (dialogResult == DialogResult.OK)
            {
                //txtFilePath.Text = txtOutput.Text = "";
            }
        }
    }
}
