using IKapBoardClassLibrary;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IKapLibrary
{
    public class LineCamera
    {
        // 设备类
        public LineScan m_hDevice = new LineScan();

        // 数据处理类
        public BufferManager m_buffer = new BufferManager();

        // 判断是否在采集中
        public bool m_bGrabing = false;

        public bool m_bContinuousGrab = false;

        //  回调函数

        #region Callback

        private delegate void IKapCallBackProc(IntPtr pParam);

        private IKapCallBackProc OnGrabStartProc;
        private IKapCallBackProc OnFrameLostProc;
        private IKapCallBackProc OnTimeoutProc;
        private IKapCallBackProc OnFrameReadyProc;
        private IKapCallBackProc OnGrabStopProc;

        #endregion Callback

        #region Callback

        // 开始抓帧回调
        public void OnGrabStartFunc(IntPtr pParam)
        {
            Console.WriteLine("Start grabbing image");
        }

        // 丢帧回调
        public void OnFrameLostFunc(IntPtr pParam)
        {
            Console.WriteLine("Frame lost");
        }

        // 帧超时回调
        public void OnTimeoutFunc(IntPtr pParam)
        {
            Console.WriteLine("Grab image timeout");
        }

        // 一帧图像完成回调
        public void OnFrameReadyFunc(IntPtr pParam)
        {
            IntPtr pUserBuffer = IntPtr.Zero;
            int nFrameSize = 0;
            int nFrameCount = 0;
            int nCurIndex = 0;
            IKapBoard.IKAPBUFFERSTATUS status = new IKapBoard.IKAPBUFFERSTATUS();
            m_hDevice.GetInfo((uint)INFO_ID.IKP_FRAME_COUNT, ref nFrameCount);

            // 获取当前索引
            nCurIndex = m_hDevice.m_nGrabTotalFrameCount % nFrameCount;

            // 获取当前帧状态
            m_hDevice.GetBufferStatus(nCurIndex, ref status);
            if (status.uFull == 1)
            {
                // 获取当前帧长度和帧数据地址
                m_hDevice.GetInfo((uint)INFO_ID.IKP_FRAME_SIZE, ref nFrameSize);
                m_hDevice.GetBufferAddress(nCurIndex, ref pUserBuffer);

                m_buffer.WriteImage(pUserBuffer, nFrameSize);
            }
            m_hDevice.m_nGrabTotalFrameCount++;
        }

        // 停止抓取图像回调
        public void OnGrabStopFunc(IntPtr pParam)
        {
            m_bGrabing = false;
        }

        #endregion Callback

        public Bitmap LineScanImage = null;


        public async Task<bool> GrabImage(int nGrabFrame)
        {
            IntPtr hPtr = new IntPtr(-1);
            // 注册回调函数
            int ret = (int)ErrorCode.IK_RTN_OK;
            OnGrabStartProc = new IKapCallBackProc(OnGrabStartFunc);
            ret = m_hDevice.RegisterCallback((uint)CallBackEvents.IKEvent_GrabStart, Marshal.GetFunctionPointerForDelegate(OnGrabStartProc), hPtr);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return false;

            OnFrameReadyProc = new IKapCallBackProc(OnFrameReadyFunc);
            ret = m_hDevice.RegisterCallback((uint)CallBackEvents.IKEvent_FrameReady, Marshal.GetFunctionPointerForDelegate(OnFrameReadyProc), hPtr);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return false;

            OnFrameLostProc = new IKapCallBackProc(OnFrameLostFunc);
            ret = m_hDevice.RegisterCallback((uint)CallBackEvents.IKEvent_FrameLost, Marshal.GetFunctionPointerForDelegate(OnFrameLostProc), hPtr);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return false;

            OnTimeoutProc = new IKapCallBackProc(OnTimeoutFunc);
            ret = m_hDevice.RegisterCallback((uint)CallBackEvents.IKEvent_TimeOut, Marshal.GetFunctionPointerForDelegate(OnTimeoutProc), hPtr);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return false;

            OnGrabStopProc = new IKapCallBackProc(OnGrabStopFunc);
            ret = m_hDevice.RegisterCallback((uint)CallBackEvents.IKEvent_GrabStop, Marshal.GetFunctionPointerForDelegate(OnGrabStopProc), hPtr);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return false;

            // 创建缓冲区管理
            m_buffer.CreateBuffer(m_hDevice);

            ret = m_hDevice.StartGrab(nGrabFrame);
            if (ret != (int)ErrorCode.IK_RTN_OK)
            {
                m_bGrabing = false;
                return false;
            }
            m_bGrabing = true;

            while (m_bGrabing)
            {
                await Task.Run(() =>
                {
                    if (nGrabFrame == 1)
                        m_hDevice.WaitGrab();

                    if (m_buffer.m_bUpdateImage)

                    {
                        m_buffer.ReadImage();
                        LineScanImage = m_buffer.m_bmp;
                    }
                });

                await Task.Delay(30);
            }

            return true;
        }

        public void StopGrabImage()
        {
            m_hDevice.StopGrab();
            // 注销回调函数
            m_hDevice.UnRegisterCallback((uint)CallBackEvents.IKEvent_GrabStart);
            m_hDevice.UnRegisterCallback((uint)CallBackEvents.IKEvent_FrameReady);
            m_hDevice.UnRegisterCallback((uint)CallBackEvents.IKEvent_GrabStop);
            m_hDevice.UnRegisterCallback((uint)CallBackEvents.IKEvent_FrameLost);
            m_hDevice.UnRegisterCallback((uint)CallBackEvents.IKEvent_TimeOut);
            m_bGrabing = false;
        }

        public void SaveImage(string image_Format)
        {
            SaveFileDialog saveImg = new SaveFileDialog();
            saveImg.Title = "图片保存";
            saveImg.Filter = "BMP(*.bmp)|*.bmp";
            int formatIndex = 0;
            if (String.Equals(image_Format, "bmp"))
            {
                saveImg.FilterIndex = 1;
                formatIndex = 0;
            }
            else if (String.Equals(image_Format, "tif"))
            {
                saveImg.FilterIndex = 2;
                formatIndex = 1;
            }
            if (saveImg.ShowDialog() == true)
            {
                string fileName = saveImg.FileName.ToString();
                if (fileName != "" && fileName != null)
                {
                    System.Drawing.Imaging.ImageFormat imgFormat = System.Drawing.Imaging.ImageFormat.Png;
                    switch (formatIndex)
                    {
                        case 0:
                            imgFormat = System.Drawing.Imaging.ImageFormat.Bmp;
                            break;

                        case 1:
                            imgFormat = System.Drawing.Imaging.ImageFormat.Tiff;
                            break;
                    }
                    try
                    {
                        LineScanImage.Save(fileName, imgFormat);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}