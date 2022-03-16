using IKapBoardClassLibrary;
using IKapLibrary;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace IKapTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private LineCamera lc = new LineCamera();

        public MainWindow()
        {
            InitializeComponent();
            Canvas.SetLeft(ImageViewer, 0);
            Canvas.SetTop(ImageViewer, 0);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            if (boardID.Items.Count != 0)
            {
                boardID.Items.Clear();
            }

            uint nDevCount = 0;
            string strDevName = "";
            nDevCount = LineScan.GetDeviceCount((uint)BoardType.IKBoardPCIE);

            for (uint i = 0; i < nDevCount; i++)
            {
                strDevName = LineScan.GetDeviceName((uint)BoardType.IKBoardPCIE, i);
                boardID.Items.Add(strDevName);
            }
            if (boardID.Items.Count != 0)
            {
                boardID.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 查找采集卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void boardID_DropDownOpened(object sender, EventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            uint nDevIndex = (uint)boardID.SelectedIndex;
            if (nDevIndex < 0)
                return;
            bool bOpen = lc.m_hDevice.OpenDevice((uint)BoardType.IKBoardPCIE, nDevIndex);
            if (!bOpen)
                label2Info.Text = "打开采集卡失败";
            else
                label2Info.Text = "打开采集卡成功";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            lc.m_hDevice.CloseDevice();
            label2Info.Text = "关闭采集卡成功";
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int i = isContinues.SelectedIndex;
            await lc.GrabImage(i);
            try
            {
                ImageViewer.Source = Imaging.CreateBitmapSourceFromHBitmap(lc.LineScanImage.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch { }
            startg.IsEnabled = true;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            lc.StopGrabImage();
            startg.IsEnabled = true;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string vlcfFileName = null;
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "vlcf文件(*.vlcf)|*.vlcf|所有文件(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "选择打开文件";
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            vlcfFileName = ofd.FileName;

            if (lc.m_hDevice.LoadConfigurationFromFile(vlcfFileName) != (int)ErrorCode.IK_RTN_OK)
                label2Info.Text = "加载配置文件失败";
            else
                label2Info.Text = "加载配置文件成功";
        }

        /// <summary>
        /// 应用参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            int ret = (int)ErrorCode.IK_RTN_OK;
            bool setOK = true;
            if (triggerMode.SelectedIndex == 0)
            {
                // 设置采集卡触发模式为外部触发
                ret = lc.m_hDevice.SetInfo((uint)INFO_ID.IKP_BOARD_TRIGGER_MODE, (int)BoardTriggerMode.IKP_BOARD_TRIGGER_MODE_VAL_OUTTER);
                // 设置触发源General Input1
                ret = lc.m_hDevice.SetInfo((uint)INFO_ID.IKP_BOARD_TRIGGER_SOURCE, (int)BoardTriggerSource.IKP_BOARD_TRIGGER_SOURCE_VAL_GENERAL_INPUT1);
                if (ret != (int)ErrorCode.IK_RTN_OK)
                    setOK = false;
            }
            else
            {
                // 设置采集卡触发模式位内部触发
                ret = lc.m_hDevice.SetInfo((uint)INFO_ID.IKP_BOARD_TRIGGER_MODE, (int)BoardTriggerMode.IKP_BOARD_TRIGGER_MODE_VAL_INNER);
                // 设置触发源
                ret = lc.m_hDevice.SetInfo((uint)INFO_ID.IKP_BOARD_TRIGGER_SOURCE, (int)BoardTriggerSource.IKP_BOARD_TRIGGER_SOURCE_VAL_GENERAL_INPUT1);
                if (ret != (int)ErrorCode.IK_RTN_OK)
                    setOK = false;
            }

            ret = lc.m_hDevice.SetInfo((uint)INFO_ID.IKP_IMAGE_HEIGHT, int.Parse(imageHeight.Text));
            if (ret != (int)ErrorCode.IK_RTN_OK)
            {
                setOK = false;
            }

            ret = lc.m_hDevice.SetInfo((uint)INFO_ID.IKP_SHAFT_ENCODER1_VALID_DIRECTION, direction.IsChecked == true ? 1 : 2);
            if (ret != (int)ErrorCode.IK_RTN_OK)
            {
                setOK = false;
            }

            if (!setOK)
                label2Info.Text = "参数应用失败";
            else
                label2Info.Text = "参数应用成功";
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            lc.SaveImage("bmp");
        }

        private void cas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var currentPoint = Mouse.GetPosition(cas);
                //ImageViewer.Width = ImageViewer.Width * 1;
                //ImageViewer.Height = ImageViewer.Height * 1;


              double  biasX = currentPoint.X - startX;
              double biasY = currentPoint.Y - startY;

              

                Canvas.SetLeft(ImageViewer, left+biasX);
                Canvas.SetTop(ImageViewer, top+biasY);

            }
        }


        double startX, startY;
        double left=0, top=0;

        private void cas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var currentPoint = Mouse.GetPosition(cas);
            left = Canvas.GetLeft(ImageViewer);
            top = Canvas.GetTop(ImageViewer);

            double percentX = (currentPoint.X - left) / ImageViewer.ActualWidth;
            double percentY = (currentPoint.Y - top) / ImageViewer.ActualHeight;

           


            if (e.Delta > 0)
            {
                double lengthX = ImageViewer.ActualWidth * 1.1;
                double lengthY = ImageViewer.ActualHeight * 1.1;

                left = currentPoint.X - lengthX * percentX;
                top = currentPoint.Y - lengthY * percentY;

                ImageViewer.Width = lengthX;
                ImageViewer.Height = lengthY;

                Canvas.SetLeft(ImageViewer, left);
                Canvas.SetTop(ImageViewer, top);
            }
            else
            {
                double lengthX = ImageViewer.ActualWidth * 0.9;
                double lengthY = ImageViewer.ActualHeight * 0.9;

                left = currentPoint.X - lengthX * percentX;
                top = currentPoint.Y - lengthY * percentY;

                ImageViewer.Width = lengthX;
                ImageViewer.Height = lengthY;

                Canvas.SetLeft(ImageViewer, left);
                Canvas.SetTop(ImageViewer, top);

            }
        }

        private void cas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var currentPoint = Mouse.GetPosition(cas);
            startX= currentPoint.X;
            startY= currentPoint.Y;

             left = Canvas.GetLeft(ImageViewer);
             top = Canvas.GetTop(ImageViewer);
        }
    }
}