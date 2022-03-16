using IKapBoardClassLibrary;
using System;
using System.Text;

namespace IKapLibrary
{
    public class LineScan
    {
        public IntPtr m_hDev = IntPtr.Zero;
        public int m_nGrabTotalFrameCount = 0;
        public int m_nSetBufferCount = 5;
        public int m_nDevIndex = -1;

        /// <summary>
        ///  获取采集卡个数
        /// </summary>
        /// <param name="nType">采集卡类型</param>
        /// <returns>返回采集卡个数</returns>
        public static uint GetDeviceCount(uint nType)
        {
            int res = (int)ErrorCode.IK_RTN_OK;
            uint nCount = 0;
            res = IKapBoard.IKapGetBoardCount(nType, ref nCount);
            if (res == (int)ErrorCode.IK_RTN_OK)
                return nCount;
            return 0;
        }

        /// <summary>
        /// 获取采集卡名称
        /// </summary>
        /// <param name="nType">采集卡类型</param>
        /// <param name="nIndex">当前采集卡索引</param>
        /// <returns>采集卡名</returns>
        public static string GetDeviceName(uint nType, uint nIndex)
        {
            uint nDevSize = 128;
            StringBuilder szDevName = new StringBuilder(128);
            int res = IKapBoard.IKapGetBoardName(nType, nIndex, szDevName, ref nDevSize);
            if (res != (int)ErrorCode.IK_RTN_OK)
                return "";

            string strDevName = szDevName.ToString();
            szDevName = null;

            return strDevName;
        }

        /// <summary>
        /// 打开采集卡
        /// </summary>
        /// <param name="nType">采集卡类型</param>
        /// <param name="nIndex">采集卡索引</param>
        /// <returns>是否打开</returns>
        public bool OpenDevice(uint nType, uint nIndex)
        {
            m_hDev = IKapBoard.IKapOpen(nType, nIndex);
            if (m_hDev == new IntPtr(-1))
                return false;
            m_nDevIndex = (int)nIndex;
            return true;
        }

        /// <summary>
        /// 打开CXP采集卡
        /// </summary>
        /// <param name="nType">采集卡类型</param>
        /// <param name="nIndex">采集卡索引</param>
        /// <param name="info">CXP采集卡信息</param>
        /// <returns>是否打开CXP采集卡</returns>
        public bool OpenDeviceCxp(uint nType, uint nIndex, IKapBoard.IKAP_CXP_BOARD_INFO info)
        {
            m_hDev = IKapBoard.IKapOpenCXP(nType, nIndex, info);
            if (m_hDev == new IntPtr(-1))
                return false;
            m_nDevIndex = (int)nIndex;
            return true;
        }

        /// <summary>
        /// 是否打开采集卡
        /// </summary>
        /// <returns>是否打开</returns>
        public bool IsOpenDevice()
        {
            if (m_hDev == new IntPtr(-1) || m_hDev == IntPtr.Zero)
                return false;
            return true;
        }

        /// <summary>
        /// 关闭采集卡
        /// </summary>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int CloseDevice()
        {
            int res = (int)ErrorCode.IK_RTN_OK;
            if (IsOpenDevice())
                res = IKapBoard.IKapClose(m_hDev);
            m_hDev = IntPtr.Zero;
            m_nDevIndex = -1;
            return res;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="strFileName">配置文件名</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int LoadConfigurationFromFile(string strFileName)
        {
            return IKapBoard.IKapLoadConfigurationFromFile(m_hDev, strFileName);
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="strFileName">配置文件名</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int SaveConfigurationToFile(string strFileName)
        {
            return IKapBoard.IKapSaveConfigurationToFile(m_hDev, strFileName);
        }

        /// <summary>
        /// 获取采集卡参数值
        /// </summary>
        /// <param name="nType">参数类型，参考INFO_ID</param>
        /// <param name="pValue">获取的参数值</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int GetInfo(uint nType, ref int pValue)
        {
            return IKapBoard.IKapGetInfo(m_hDev, nType, ref pValue);
        }

        /// <summary>
        /// 设置采集卡参数值
        /// </summary>
        /// <param name="nType">参数类型，参考INFO_ID</param>
        /// <param name="nValue">设置的参数值</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int SetInfo(uint nType, int nValue)
        {
            return IKapBoard.IKapSetInfo(m_hDev, nType, nValue);
        }

        /// <summary>
        /// 注册回调函数
        /// </summary>
        /// <param name="nEventType">回调类型，参照CallBackEvents</param>
        /// <param name="fEventFunc">回调函数</param>
        /// <param name="pContext">回调参数</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int RegisterCallback(uint nEventType, IntPtr fEventFunc, IntPtr pContext)
        {
            return IKapBoard.IKapRegisterCallback(m_hDev, nEventType, fEventFunc, pContext);
        }

        /// <summary>
        /// 注销回调函数
        /// </summary>
        /// <param name="nEventType">回调类型，参照CallBackEvents</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int UnRegisterCallback(uint nEventType)
        {
            return IKapBoard.IKapUnRegisterCallback(m_hDev, nEventType);
        }

        /// <summary>
        ///  开始采集，注意非连续采集在采集开始后要调用WaitGrab函数才能获取图像
        /// </summary>
        /// <param name="nFrameCount">采集帧数，0表示连续采集</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int StartGrab(int nFrameCount)
        {
            // 每次重新开始采集，要确保当前帧索引设置为0
            m_nGrabTotalFrameCount = 0;

            // 设置帧超时时间
            int timeout = -1;
            int ret = IKapBoard.IKapSetInfo(m_hDev, (uint)INFO_ID.IKP_TIME_OUT, timeout);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return ret;

            // 设置抓取模式，IKP_GRAB_NON_BLOCK为非阻塞模式
            int grab_mode = (int)GrabMode.IKP_GRAB_NON_BLOCK;
            ret = IKapBoard.IKapSetInfo(m_hDev, (uint)INFO_ID.IKP_GRAB_MODE, grab_mode);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return ret;

            // 设置帧传输模式，IKP_FRAME_TRANSFER_SYNCHRONOUS_NEXT_EMPTY_WITH_PROTECT为同步保存模式
            int transfer_mode = (int)FrameTransferMode.IKP_FRAME_TRANSFER_SYNCHRONOUS_NEXT_EMPTY_WITH_PROTECT;
            ret = IKapBoard.IKapSetInfo(m_hDev, (uint)INFO_ID.IKP_FRAME_TRANSFER_MODE, transfer_mode);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return ret;

            // 设置缓冲区格式
            ret = IKapBoard.IKapSetInfo(m_hDev, (uint)INFO_ID.IKP_FRAME_COUNT, m_nSetBufferCount);
            if (ret != (int)ErrorCode.IK_RTN_OK)
                return ret;

            return IKapBoard.IKapStartGrab(m_hDev, nFrameCount);
        }

        /// <summary>
        /// 停止抓取图像
        /// </summary>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int StopGrab()
        {
            return IKapBoard.IKapStopGrab(m_hDev);
        }

        /// <summary>
        /// 等待图像抓取结束，只用于单帧或多帧采集
        /// </summary>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int WaitGrab()
        {
            return IKapBoard.IKapWaitGrab(m_hDev);
        }

        /// <summary>
        /// 获取帧状态
        /// </summary>
        /// <param name="nFrameNum">帧索引</param>
        /// <param name="pStatus">帧状态</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int GetBufferStatus(int nFrameNum, ref IKapBoard.IKAPBUFFERSTATUS pStatus)
        {
            return IKapBoard.IKapGetBufferStatus(m_hDev, nFrameNum, ref pStatus);
        }

        /// <summary>
        /// 获取帧数据
        /// </summary>
        /// <param name="nFrameNum">帧索引</param>
        /// <param name="pAddress">帧地址</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int GetBufferAddress(int nFrameNum, ref IntPtr pAddress)
        {
            return IKapBoard.IKapGetBufferAddress(m_hDev, nFrameNum, ref pAddress);
        }

        /// <summary>
        /// 获取采集频率
        /// </summary>
        /// <param name="fFrameRate">采集频率，面阵相机是表示帧频率，线阵相机表示行频率</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int GetFrameRate(ref double fFrameRate)
        {
            return IKapBoard.IKapGetFrameRate(m_hDev, ref fFrameRate);
        }

        /// <summary>
        /// 保存帧数据到图像文件
        /// </summary>
        /// <param name="nFrameNum">帧索引</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="nFlag">保存类型，参考ImageCompressionFalg值</param>
        /// <returns>返回错误码，参考ErrorCode值</returns>
        public int SaveBuffer(int nFrameNum, string fileName, int nFlag)
        {
            return IKapBoard.IKapSaveBuffer(m_hDev, nFrameNum, fileName, nFlag);
        }
    }
}