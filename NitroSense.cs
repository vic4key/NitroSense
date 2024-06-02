using System;
using System.Threading.Tasks;
using System.IO.Pipes;
using TsDotNetLib;

namespace NitroSense
{
    public class NitroSense
    {
        // Token: 0x020000A8 RID: 168
        public enum System_Health_Information_Index
        {
            // Token: 0x0400069A RID: 1690
            sCPU_Temperature = 1,
            // Token: 0x0400069B RID: 1691
            sCPU_Fan_Speed,
            // Token: 0x0400069C RID: 1692
            sSystem_Temperature,
            // Token: 0x0400069D RID: 1693
            sSystem_Fan_Speed,
            // Token: 0x0400069E RID: 1694
            sFrostCore,
            // Token: 0x0400069F RID: 1695
            sGPU_Fan_Speed,
            // Token: 0x040006A0 RID: 1696
            sSystem2_Temperature,
            // Token: 0x040006A1 RID: 1697
            sSystem2_Fan_Speed,
            // Token: 0x040006A2 RID: 1698
            sGPU2_Fan_Speed,
            // Token: 0x040006A3 RID: 1699
            sGPU1_Temperature,
            // Token: 0x040006A4 RID: 1700
            sGPU2_Temperature
        }

        // Token: 0x060003AB RID: 939 RVA: 0x00032190 File Offset: 0x00030390
        public static async Task<ulong> GetAcerGamingSystemInformation(uint intput)
        {
            ulong result;
            try
            {
                NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
                cline_stream.Connect();
                ulong num = await Task.Run<ulong>(delegate ()
                {
                    IPCMethods.SendCommandByNamedPipe(cline_stream, 13, new object[]
                    {
                        intput
                    });
                    cline_stream.WaitForPipeDrain();
                    byte[] array = new byte[13];
                    cline_stream.Read(array, 0, array.Length);
                    return BitConverter.ToUInt64(array, 5);
                }).ConfigureAwait(false);
                cline_stream.Close();
                result = num;
            }
            catch (Exception)
            {
                result = unchecked((ulong)-1);
            }
            return result;
        }

        // Token: 0x060003AE RID: 942 RVA: 0x00032268 File Offset: 0x00030468
        public static async Task<uint> WMISetFunction(ulong intput)
        {
            uint result;
            try
            {
                NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
                cline_stream.Connect();
                uint num = await Task.Run<uint>(delegate ()
                {
                    IPCMethods.SendCommandByNamedPipe(cline_stream, 17, new object[]
                    {
                        intput
                    });
                    cline_stream.WaitForPipeDrain();
                    byte[] array = new byte[9];
                    cline_stream.Read(array, 0, array.Length);
                    return BitConverter.ToUInt32(array, 5);
                }).ConfigureAwait(false);
                cline_stream.Close();
                result = num;
            }
            catch (Exception)
            {
                result = uint.MaxValue;
            }
            return result;
        }

        // Token: 0x060003AF RID: 943 RVA: 0x000322B0 File Offset: 0x000304B0
        public static async Task<ulong> WMIGetFunction(uint intput)
        {
            ulong result;
            try
            {
                NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
                cline_stream.Connect();
                ulong num = await Task.Run<ulong>(delegate ()
                {
                    IPCMethods.SendCommandByNamedPipe(cline_stream, 20, new object[]
                    {
                        intput
                    });
                    cline_stream.WaitForPipeDrain();
                    byte[] array = new byte[13];
                    cline_stream.Read(array, 0, array.Length);
                    return BitConverter.ToUInt64(array, 5);
                }).ConfigureAwait(false);
                cline_stream.Close();
                result = num;
            }
            catch (Exception)
            {
                result = unchecked((ulong)-1);
            }
            return result;
        }

        // Token: 0x06000370 RID: 880 RVA: 0x0002FFB4 File Offset: 0x0002E1B4
        public static bool set_coolboost_state(bool state)
        {
            return (WMISetFunction((ulong)(7L | (state ? 1L : 0L) << 16)).GetAwaiter().GetResult() & 255u) == 0u;
        }

        // Token: 0x06000374 RID: 884 RVA: 0x00030158 File Offset: 0x0002E358
        public static bool get_coolboost_state()
        {
            ulong result = WMIGetFunction(519u).GetAwaiter().GetResult();
            return (result & 255UL) == 0UL && (result >> 8 & 255UL) == 1UL;
        }

        public static void fix_fan_noise_caused_by_high_fan_speed(int fan_speed_noise_rpm, bool debug = false) // fan is noise when fan speed is starting from 3000 rpm (in my laptop)
        {
            Func<System_Health_Information_Index, int> get_fan_speed = (index) =>
            {
                ulong result = GetAcerGamingSystemInformation((uint)System_Health_Information_Index.sCPU_Temperature | ((uint)index << 8)).Result;
                if ((result & 255UL) == 0UL)
                {
                    var info_data = (int)(result >> 8 & 65535UL);
                    return info_data;
                }
                return 0;
            };

            int high_fan_speed = 0;

            var cpu_fan_speed = get_fan_speed(System_Health_Information_Index.sCPU_Fan_Speed);
            high_fan_speed = Math.Max(high_fan_speed, cpu_fan_speed);

            var gpu_fan_speed = get_fan_speed(System_Health_Information_Index.sGPU_Fan_Speed);
            high_fan_speed = Math.Max(high_fan_speed, gpu_fan_speed);

            if (debug) Console.WriteLine($"CPU Fan SPeed = {cpu_fan_speed}, GPU Fan Speed = {gpu_fan_speed} => High Fan Speed = {high_fan_speed} (Noise Fan Speed = {fan_speed_noise_rpm})");

            if (high_fan_speed >= fan_speed_noise_rpm)
            {
                bool coolboost_state = get_coolboost_state();
                coolboost_state = !coolboost_state;
                set_coolboost_state(coolboost_state);
                if (debug) Console.WriteLine($"\tCoolBoost from {!coolboost_state} to {coolboost_state}");
            }
        }
    }
}
