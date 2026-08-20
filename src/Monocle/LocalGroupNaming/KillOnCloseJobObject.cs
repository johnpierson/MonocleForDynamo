using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>
    /// Assigns child processes to a Windows job object with kill-on-close so the
    /// OS terminates them when this process exits for any reason, including a
    /// crash of the hosting application (Dynamo, Revit, Civil 3D).
    /// </summary>
    internal static class KillOnCloseJobObject
    {
        private static readonly IntPtr JobHandle = CreateKillOnCloseJob();

        public static void TryAssign(Process process)
        {
            if (JobHandle == IntPtr.Zero || process == null) return;

            try
            {
                AssignProcessToJobObject(JobHandle, process.Handle);
            }
            catch (Exception)
            {
                // Without the job object the server still stops on a clean shutdown.
            }
        }

        private static IntPtr CreateKillOnCloseJob()
        {
            try
            {
                var handle = CreateJobObject(IntPtr.Zero, null);
                if (handle == IntPtr.Zero) return IntPtr.Zero;

                var information = new JobObjectExtendedLimitInformation
                {
                    BasicLimitInformation = new JobObjectBasicLimitInformation
                    {
                        LimitFlags = JobObjectLimitKillOnJobClose
                    }
                };

                var length = Marshal.SizeOf(typeof(JobObjectExtendedLimitInformation));
                var informationPtr = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.StructureToPtr(information, informationPtr, false);
                    if (!SetInformationJobObject(
                        handle,
                        JobObjectInfoClassExtendedLimitInformation,
                        informationPtr,
                        (uint)length))
                    {
                        CloseHandle(handle);
                        return IntPtr.Zero;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(informationPtr);
                }

                // The handle is intentionally never closed while this process lives;
                // the OS closes it on process exit, which kills the assigned children.
                return handle;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        private const uint JobObjectLimitKillOnJobClose = 0x2000;
        private const int JobObjectInfoClassExtendedLimitInformation = 9;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr securityAttributes, string name);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(
            IntPtr jobHandle,
            int informationClass,
            IntPtr information,
            uint informationLength);

        [DllImport("kernel32.dll")]
        private static extern bool AssignProcessToJobObject(IntPtr jobHandle, IntPtr processHandle);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
