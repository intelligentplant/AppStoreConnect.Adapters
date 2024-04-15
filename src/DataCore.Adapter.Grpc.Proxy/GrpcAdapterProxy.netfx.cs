#if NETFRAMEWORK
using System;
using System.Runtime.InteropServices;

namespace DataCore.Adapter.Grpc.Proxy {

    partial class GrpcAdapterProxy {

        /// <summary>
        /// Specifies if .NET Framework has full support for gRPC clients on the current 
        /// operating system.
        /// </summary>
        private static bool? s_isGrpcClientFullySupported;


        /// <summary>
        /// Determines if .NET Framework has full support for gRPC clients.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if .NET Framework has full support for gRPC clients; 
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   .NET Framework has limited support for gRPC clients on some Windows versions.
        /// </para>
        /// 
        /// <para>
        ///   See <a href="https://learn.microsoft.com/en-us/aspnet/core/grpc/netstandard#net-framework">here</a> 
        ///   for more information about gRPC support on .NET Framework.
        /// </para>
        /// 
        /// </remarks>
        internal static bool IsGrpcClientFullySupported() {
            return s_isGrpcClientFullySupported ??= IsGrpcClientFullySupportedCore();
        }


        /// <summary>
        /// Determines if .NET Framework has full support for gRPC clients.
        /// </summary>
        private static bool IsGrpcClientFullySupportedCore() {
            var windowsVersion = GetWindowsVersion(out var isServer);

            // Windows 11 is required for full support on workstation versions of Windows.
            // No Windows Server versions are supported at time of writing (April 2024).

            if (isServer) {
                return false;
            }

            // Version number for Windows 11.
            var minWorkstationVersion = new Version(10, 0, 22000);
            return windowsVersion >= minWorkstationVersion;
        }


        /// <summary>
        /// Gets the version of Windows that Data Core is running on.
        /// </summary>
        /// <param name="isServer">
        ///   <see langword="true"/> if the operating system is a server version of Windows (e.g. 
        ///   Windows Server 2022), or <see langword="false"/> if it is a workstation version of 
        ///   Windows (e.g. Windows 11).
        /// </param>
        /// <returns>
        ///   The Windows version.
        /// </returns>
        /// <exception cref="COMException">
        ///   An error occurred while retrieving the Windows version.
        /// </exception>
        /// <remarks>
        ///   This method uses P/Invoke to call <c>RtlGetVersion</c> via the Windows API.
        /// </remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion"/>
        private static Version GetWindowsVersion(out bool isServer) {
            var osvi = default(OSVERSIONINFOEXW);
            osvi.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEXW));
            var result = RtlGetVersion(ref osvi);
            if (result != 0) {
                throw new COMException("Error retrieving Windows version.", result);
            }
            else {
                isServer = osvi.wProductType != PRODUCT_TYPE.VER_NT_WORKSTATION;
                return new Version(osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
            }
        }

        #region [ P/Invoke Types and Declarations ]

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInfo);


        /// <summary>
        /// Describes a Windows product type.
        /// </summary>
        public enum PRODUCT_TYPE {
            VER_NT_WORKSTATION = 0x0000001,
            VER_NT_DOMAIN_CONTROLLER = 0x0000002,
            VER_NT_SERVER = 0x0000003
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OSVERSIONINFOEXW {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public PRODUCT_TYPE wProductType;
            public byte wReserved;
        }

        #endregion

    }

}
#endif
