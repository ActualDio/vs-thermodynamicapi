using Mono.Cecil.Cil;
using OpenTK.Compute;
using OpenTK.Compute.Native;
using OpenTK.Compute.OpenCL;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThermalDynamics.SystemControl;
using Vintagestory.API.Common;

namespace ThermodynamicAPI.SystemControl
{    
    public class FluidSolver : CL
    {
        private static readonly Dictionary<CLPlatform, CLDevice[]> platformAndDeviceIds;
        private static readonly CLContext context;
        //private static const ContextProperties[] properties = { };
        private static CLBuffer buffer;

        static void CLResultHandler(CLResultCode code)
        {
            if (code == 0) return;
            else throw new Exception(code.ToString()); 
        }

        static FluidSolver() 
        {
            RegisterOpenCLResolver();
            CLResultCode result;


            result = GetPlatformIds(out CLPlatform[] platformIds);
            CLResultHandler(result);

            Console.WriteLine($"Platforms found: {platformIds.Length}");

            // Print all platforms and the available devices
            foreach(CLPlatform id in platformIds)
            {

                result = GetPlatformInfo(id, PlatformInfo.Name, out byte[] name);
                CLResultHandler(result);


                result = GetDeviceIds(id, DeviceType.All, out CLDevice[] tempDeviceIds);
                CLResultHandler(result);

                platformAndDeviceIds.Add(id, tempDeviceIds);

                Console.WriteLine($"Platform id -> {id} : {name}");
                Console.WriteLine($"Contains the following devices:");

                foreach (CLDevice dev in tempDeviceIds)
                {
                    result = GetDeviceInfo(dev, DeviceInfo.Name, out byte[] devName);
                    CLResultHandler(result);
                    Console.WriteLine($"    Device id -> {dev} : {name}");
                }
            }
            CLDevice[] deviceIds = GetDeviceIds();
            //Creating a context for the first platform found
            context = CreateContext(IntPtr.Zero, (uint)deviceIds.Length, deviceIds, IntPtr.Zero, IntPtr.Zero, out result);
            CLResultHandler(result);

            
            buffer = CreateBuffer(context, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPtr, new float[20], out result);
            CLResultHandler(result);
        }

        private static CLDevice[] GetDeviceIds(CLPlatform platform)
        {
            CLDevice[] devices;

            if (platformAndDeviceIds.TryGetValue(platform, out devices))
            {
                return devices;
            }
            else
            {
                throw new Exception("Could not get DeviceIDs from dict");
            }
        }
        private static CLDevice[] GetDeviceIds()
        {
            CLDevice[] devices;
            CLPlatform platform = platformAndDeviceIds.ElementAt(0).Key;
            if (platformAndDeviceIds.TryGetValue(platform, out devices))
            {
                return devices;
            }
            else
            {
                throw new Exception("Could not get DeviceIDs from dict");
            }
        }
        
    }
}
