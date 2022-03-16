using IKapBoardClassLibrary;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace IKapLibrary
{
    /// <summary>
    ///  此类的功能是保存才采集卡读取的图像数据
    ///  并把读取的图像数据转换为BITMAP格式
    /// </summary>
    ///
    public class BufferManager
    {
        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        // 存放获取帧的原始数据和长度
        private IntPtr m_pBufferData = new IntPtr(-1);

        private int m_nBufferSize = 0;
        private object m_lockBuffer = new object();

        // 创建BITMAP图像
        public Bitmap m_bmp = null;

        public object m_lockBmp = new object();

        // 图像信息
        public bool m_bUpdateImage = false;

        public int m_nWidth = 0;
        public int m_nHeight = 0;
        public int m_nDataFormat = 8;
        public int m_nImageType = 8;

        /// <summary>
        ///  根据采集卡配置信息创建数据转换缓冲区和BITMAP图像
        /// </summary>
        /// <param name="hDev">采集卡句柄</param>
        /// <returns>是否创建成功</returns>
        public bool CreateBuffer(LineScan hDev)
        {
            int ret = (int)ErrorCode.IK_RTN_OK;
            int nFrameSize = 0;
            ret = hDev.GetInfo((uint)INFO_ID.IKP_FRAME_SIZE, ref nFrameSize);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return false;

            lock (m_lockBuffer)
            {
                m_pBufferData = Marshal.AllocHGlobal(nFrameSize);
                if (m_pBufferData == null)
                    return false;
                m_nBufferSize = nFrameSize;
            }

            // 创建Bitmap图像，16bit图像无法显示，故只创建8bit图像
            hDev.GetInfo((uint)INFO_ID.IKP_IMAGE_WIDTH, ref m_nWidth);
            hDev.GetInfo((uint)INFO_ID.IKP_IMAGE_HEIGHT, ref m_nHeight);
            hDev.GetInfo((uint)INFO_ID.IKP_DATA_FORMAT, ref m_nDataFormat);
            hDev.GetInfo((uint)INFO_ID.IKP_IMAGE_TYPE, ref m_nImageType);

            // 创建BITMAP图像
            PixelFormat nPixelFormat = PixelFormat.Undefined;
            switch (m_nImageType)
            {
                case 0:
                    nPixelFormat = PixelFormat.Format8bppIndexed;
                    break;

                case 1:
                case 3:
                case 2:
                case 4:
                    nPixelFormat = PixelFormat.Format24bppRgb;
                    break;

                default:
                    break;
            }

            lock (m_lockBmp)
            {
                m_bmp = new Bitmap(m_nWidth, m_nHeight, nPixelFormat);
                // 灰度图需要进行调色板初始化
                if (nPixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                {
                    ColorPalette cp = m_bmp.Palette;
                    for (int i = 0; i < 256; i++)
                        cp.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                    m_bmp.Palette = cp;
                }
            }

            return false;
        }

        /// <summary>
        ///  释放申请的缓冲区和BITMAP图像资源
        /// </summary>
        /// <param name></param>
        /// <returns>是否释放</returns>
        public bool FreeBuffer()
        {
            lock (m_lockBuffer)
            {
                if (m_pBufferData != null)
                    Marshal.FreeHGlobal(m_pBufferData);
                m_nBufferSize = 0;
            }

            lock (m_lockBmp)
            {
                if (m_bmp != null)
                    m_bmp.Dispose();
                m_bmp = null;
            }
            return false;
        }

        /// <summary>
        ///  向申请的缓冲区中写入原始数据
        /// </summary>
        /// <param name="pData"，"nDataLen">原始数据流指针，数据长度</param>
        /// <returns>是否写入成功</returns>
        public bool WriteImage(IntPtr pData, int nDataLen)
        {
            lock (m_lockBuffer)
            {
                if (m_pBufferData == null)
                    return false;

                // 计算拷贝长度
                int nCopyLen = nDataLen;
                if (nCopyLen > m_nBufferSize)
                    nCopyLen = m_nBufferSize;
                CopyMemory(m_pBufferData, pData, nCopyLen);
                m_bUpdateImage = true;
            }
            return false;
        }

        /// <summary>
        ///  将缓冲区中数据进行转换后写入BITMAP图像
        /// </summary>
        /// <param name></param>
        /// <returns>是否完成数据转换</returns>
        public bool ReadImage()
        {
            lock (m_lockBmp)
            {
                lock (m_lockBuffer)
                {
                    if (m_nImageType == 2 || m_nImageType == 4)
                    {
                        ReadRGBC();
                        return true;
                    }
                    if (m_pBufferData == null || m_bmp == null)
                        return false;
                    m_bUpdateImage = false;
                    Rectangle rect = new Rectangle(0, 0, m_bmp.Width, m_bmp.Height);
                    BitmapData bitmapData = m_bmp.LockBits(rect, ImageLockMode.ReadWrite, m_bmp.PixelFormat);

                    int nShift = m_nDataFormat - 8;
                    int nStride = m_nBufferSize / m_bmp.Height;
                    //  获取采集卡数据位
                    if (m_nDataFormat == 8)
                    {
                        for (int i = 0; i < m_bmp.Height; i++)
                        {
                            IntPtr iptrDst = bitmapData.Scan0 + bitmapData.Stride * i;
                            IntPtr iptrSrc = m_pBufferData + nStride * i;
                            CopyMemory(iptrDst, iptrSrc, nStride);
                        }
                        m_bmp.UnlockBits(bitmapData);
                        return true;
                    }

                    // 高位截取转换为Bitmap图像
                    short[] pData = new short[m_nBufferSize / 2];
                    byte[] pDstData = new byte[m_nBufferSize];
                    nStride = bitmapData.Stride;
                    Marshal.Copy(m_pBufferData, pData, 0, m_nBufferSize / 2);
                    for (int i = 0; i < bitmapData.Height; i++)
                    {
                        for (int j = 0; j < nStride; j++)
                        {
                            pDstData[i * nStride + j] = (byte)(pData[i * nStride + j] >> nShift);
                        }
                    }
                    Marshal.Copy(pDstData, 0, bitmapData.Scan0, (m_nBufferSize / 2));
                    m_bmp.UnlockBits(bitmapData);
                }
            }
            return true;
        }

        /// <summary>
        ///  将缓冲区中RGBC数据进行转换后写入BITMAP图像
        /// </summary>
        /// <param name></param>
        /// <returns>是否完成数据转换</returns>
        private bool ReadRGBC()
        {
            if (m_pBufferData == null || m_bmp == null)
                return false;
            m_bUpdateImage = false;
            Rectangle rect = new Rectangle(0, 0, m_bmp.Width, m_bmp.Height);
            BitmapData bitmapData = m_bmp.LockBits(rect, ImageLockMode.ReadWrite, m_bmp.PixelFormat);

            int nShift = m_nDataFormat - 8;
            int nStride = bitmapData.Stride;
            int nCount = 0;
            byte[] pByteData = new byte[m_nBufferSize];
            byte[] pDstData = new byte[(m_nBufferSize * 3) / 4];
            Marshal.Copy(m_pBufferData, pByteData, 0, m_nBufferSize);
            if (m_nDataFormat == 8)
            {
                for (int i = 0; i < m_bmp.Height; i++)
                {
                    for (int j = 0; j < nStride; j = j + 3)
                    {
                        pDstData[i * nStride + j] = (byte)(pByteData[nCount]);
                        pDstData[i * nStride + j + 1] = (byte)(pByteData[nCount + 1]);
                        pDstData[i * nStride + j + 2] = (byte)(pByteData[nCount + 2]);
                        nCount += 4;
                    }
                }
                Marshal.Copy(pDstData, 0, bitmapData.Scan0, (m_nBufferSize * 3 / 4));
                m_bmp.UnlockBits(bitmapData);
                return true;
            }
            short[] pShortData = new short[m_nBufferSize / 2];
            Marshal.Copy(m_pBufferData, pShortData, 0, m_nBufferSize / 2);
            for (int i = 0; i < bitmapData.Height; i++)
            {
                for (int j = 0; j < nStride; j = j + 3)
                {
                    pDstData[i * nStride + j] = (byte)(pShortData[nCount] >> nShift);
                    pDstData[i * nStride + j + 1] = (byte)(pShortData[nCount + 1] >> nShift);
                    pDstData[i * nStride + j + 2] = (byte)(pShortData[nCount + 2] >> nShift);
                    nCount += 4;
                }
            }
            Marshal.Copy(pDstData, 0, bitmapData.Scan0, (m_nBufferSize * 3 / 8));
            m_bmp.UnlockBits(bitmapData);
            return true;
        }
    }
}