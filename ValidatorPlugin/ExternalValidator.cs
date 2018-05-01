// Copyright (c) 2017-2018 Dover Microsystems, Inc.  All rights reserved.
// Use and disclosure subject to license. No claim made to open source code or materials.

using System;
using System.Runtime.InteropServices;
using System.Text;

using Antmicro.Renode.Logging;
using Antmicro.Renode.Utilities.Binding;

namespace Antmicro.Renode.Plugins.ValidatorPlugin
{
    public class ExternalValidator : IExecutionValidator, IMetadataDebugger, IEmulationElement
    {
	public ExternalValidator(string shared_lib_name, string policy_path, string tag_info)
	{
	    binder = new NativeBinder(this, shared_lib_name);
        EVSetMetadata(policy_path, tag_info);

	}

	public void SetCallbacks(RegisterReader RegReader, MemoryReader MemReader)
	{
	    EVSetCallbacks(RegReader, MemReader);
	}

	public bool Validate(uint PC, uint InstructionBits)
	{
	    return EVValidate(PC, InstructionBits) != 0;
	}

	public bool Commit()
	{
	    return EVCommit() != 0;
	}

        public String GetEnvMetadata()
	{
        StringBuilder sb = new StringBuilder(1024);
	    EVPcTag(sb, sb.Capacity);
        return sb.ToString();
	}
        public String GetRegMetadata(UInt64 addr)
	{
        StringBuilder sb = new StringBuilder(1024);
	    EVRegTag(sb, sb.Capacity, addr);
        return sb.ToString();
	}
        public String GetAllRegMetadata()
        {
            string result = "Register Metadata:\n";
            for(ulong i =0; i <32; i++)
            {
                result += System.String.Format("{0} : {1}\n", riscvRegs[riscvRegsOrder[i]], GetRegMetadata(riscvRegsOrder[i]));
            }
            return result;
        }
        public String GetCsrMetadata(UInt64 addr)
	{
        StringBuilder sb = new StringBuilder(1024);
	    EVCsrTag(sb, sb.Capacity, addr);
        return sb.ToString();
	}
        public String GetMemMetadata(UInt64 addr)
	{
        StringBuilder sb = new StringBuilder(1024);
	    EVMemTag(sb, sb.Capacity, addr);
        return sb.ToString();
	}

        public void SetEnvMetadataWatch(bool watching)
	{
	    EVSetPcWatch(watching);
	}
        public void SetRegMetadataWatch(UInt64 addr)
	{
	    EVSetRegWatch(addr);
	}
        public void SetCsrMetadataWatch(UInt64 addr)
	{
	    EVSetCsrWatch(addr);
	}
        public void SetMemMetadataWatch(UInt64 addr)
	{
	    EVSetMemWatch(addr);
	}
        public String PolicyViolationMsg(){
            
            StringBuilder sb = new StringBuilder(1024);
	    EVViolationMsg(sb, sb.Capacity);
            return sb.ToString();

        }

	private NativeBinder binder;
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionSetCallbacks(RegisterReader param0, MemoryReader param1);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionPcTag(StringBuilder dest, int n);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionRegTag(StringBuilder dest, int n, UInt64 addr);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionCsrTag(StringBuilder dest, int n, UInt64 addr);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionMemTag(StringBuilder dest, int n, UInt64 addr);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionViolationMsgTag(StringBuilder dest, int n);
        
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ActionSetPcWatch(bool watching);

        // 649:  Field '...' is never assigned to, and will always have its default value null
        #pragma warning disable 649
	[Import]
	private ActionSetCallbacks EVSetCallbacks;
	[Import]
	private ActionStringString EVSetMetadata;
	[Import]
	private FuncUInt32UInt32UInt32 EVValidate;
	[Import]
	private FuncUInt32 EVCommit;
	[Import]
	private ActionPcTag EVPcTag;
	[Import]
	private ActionRegTag EVRegTag;
	[Import]
	private ActionCsrTag EVCsrTag;
	[Import]
	private ActionMemTag EVMemTag;
	[Import]
	private ActionViolationMsgTag EVViolationMsg;
        [Import]
	private ActionSetPcWatch EVSetPcWatch;
        [Import]
	private ActionUInt64 EVSetRegWatch;
        [Import]
	private ActionUInt64 EVSetCsrWatch;
        [Import]
	private ActionUInt64 EVSetMemWatch;

    private string[] riscvRegs =
        { "zero ",
          "ra   ",
          "sp   ",
          "gp   ",
          "tp   ",
          "t0   ",
          "t1   ",
          "t2   ",
          "s0/fp",
          "s1   ",
          "a0   ",
          "a1   ",
          "a2   ",
          "a3   ",
          "a4   ",
          "a5   ",
          "a6   ",
          "a7   ",
          "s2   ",
          "s3   ",
          "s4   ",
          "s5   ",
          "s6   ",
          "s7   ",
          "s8   ",
          "s9   ",
          "s10  ",
          "s11  ",
          "t3   ",
          "t4   ",
          "t5   ",
          "t6   "
        };

        private ulong[] riscvRegsOrder = {0, 3, 4, 8, 9, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 5, 6, 7, 28, 29, 30, 31, 10, 11, 12, 13, 14, 15, 16, 17, 2, 1};
                
    }
}
